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
    // 훅 등록은 빌드에서만 하므로 여기서는 Application.quitting이 부를 처리만 직접 부른다.
    public class QuitHooksTests
    {
        TempSaveSandbox save;
        Action originalStop;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            ExitRecorder.ResetState();
            originalStop = NormalQuit.StopApplication;
            NormalQuit.StopApplication = () => { };
        }

        [TearDown]
        public void TearDown()
        {
            NormalQuit.StopApplication = originalStop;
            ExitRecorder.ResetState();
            save.Dispose();
        }

        int CorpseCount() =>
            Directory.Exists(save.Paths.Corpses) ? Directory.GetFiles(save.Paths.Corpses).Length : 0;

        [Test]
        public void OnQuitting_RecordsWindowCloseDeath()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[ExitRecorder\] 창 닫기 사망 저장"));
            QuitHooks.OnQuitting();

            SaveManager.Load();
            Assert.AreEqual(ExitState.DeathPending, SaveManager.Data.exitState);
            CollectionAssert.Contains(SaveManager.Data.diedNodeIds, ExitRecorder.WindowCloseDeathNodeId);
            Assert.AreEqual(1, CorpseCount());
        }

        [Test]
        public void OnQuitting_DuringNormalQuit_DoesNotDie()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[NormalQuit\] 정상 종료"));
            NormalQuit.Request();
            QuitHooks.OnQuitting();

            SaveManager.Load();
            Assert.AreEqual(ExitState.Normal, SaveManager.Data.exitState);
            CollectionAssert.IsEmpty(SaveManager.Data.diedNodeIds);
            Assert.AreEqual(0, CorpseCount());
        }

        [Test]
        public void OnQuitting_AfterSimulatedWindowClose_DoesNotAddSecondCorpse()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[ExitRecorder\] 창 닫기 사망 저장"));
            ExitRecorder.RecordWindowClose();
            QuitHooks.OnQuitting();

            Assert.AreEqual(1, CorpseCount());
        }
    }
}
