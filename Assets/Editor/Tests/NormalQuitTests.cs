using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Save;
using Gaze.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gaze.Session.Tests
{
    public class NormalQuitTests
    {
        TempSaveSandbox save;
        Action originalStop;
        int stopCount;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            ExitRecorder.ResetState();
            stopCount = 0;
            originalStop = NormalQuit.StopApplication;
            NormalQuit.StopApplication = () => stopCount++;
        }

        [TearDown]
        public void TearDown()
        {
            NormalQuit.StopApplication = originalStop;
            ExitRecorder.ResetState();
            save.Dispose();
        }

        string[] Corpses() =>
            Directory.Exists(save.Paths.Corpses) ? Directory.GetFiles(save.Paths.Corpses) : new string[0];

        [Test]
        public void Request_SavesNormalThenStops()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();

            Assert.AreEqual(1, stopCount);
            SaveManager.Load();
            Assert.AreEqual(ExitState.Normal, SaveManager.Data.exitState);
        }

        [Test]
        public void Request_ThenWindowCloseHook_DoesNotRecordDeathOrCorpse()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();
            ExitRecorder.RecordWindowClose();

            SaveManager.Load();
            Assert.AreEqual(ExitState.Normal, SaveManager.Data.exitState);
            CollectionAssert.IsEmpty(SaveManager.Data.diedNodeIds);
            CollectionAssert.IsEmpty(Corpses());
        }

        [Test]
        public void Request_Twice_StopsOnce()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();
            NormalQuit.Request();

            Assert.AreEqual(1, stopCount);
        }

        [Test]
        public void NextRun_AfterNormalQuit_HasNoPendingDeath()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();

            ExitRecorder.ResetState();
            GameSession.Begin();

            Assert.IsFalse(GameSession.ConsumePendingDeath());
            SaveManager.Load();
            Assert.AreEqual(ExitState.None, SaveManager.Data.exitState);
        }

        [Test]
        public void Request_WhenSaveFails_StillStops()
        {
            Directory.CreateDirectory(save.Base);
            File.WriteAllText(save.Paths.Root, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();

            Assert.AreEqual(1, stopCount);
        }

        [Test]
        public void Request_WhenStopThrows_LogsException()
        {
            NormalQuit.StopApplication = () => throw new InvalidOperationException("stop failed");

            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            LogAssert.Expect(LogType.Exception, new Regex("stop failed"));
            Assert.DoesNotThrow(NormalQuit.Request);
        }
    }
}
