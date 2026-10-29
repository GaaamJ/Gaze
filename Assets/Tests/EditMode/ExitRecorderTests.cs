using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gaze.Core.Tests
{
    public class ExitRecorderTests
    {
        const string Node = ExitRecorder.WindowCloseDeathNodeId;

        TempSaveSandbox _save;

        [SetUp]
        public void SetUp()
        {
            _save = new TempSaveSandbox();
            ExitRecorder.ResetState();
        }

        [TearDown]
        public void TearDown()
        {
            ExitRecorder.ResetState();
            _save.Dispose();
        }

        string[] Corpses() =>
            Directory.Exists(_save.Paths.Corpses) ? Directory.GetFiles(_save.Paths.Corpses) : new string[0];

        [Test]
        public void RecordWindowClose_SavesDeathPendingAndRecordInOneJson_ThenCreatesCorpse()
        {
            SaveManager.Data.diedNodeIds.Add("Death_DeathObject");

            ExpectRecordedLog();
            ExitRecorder.RecordWindowClose();

            string json = File.ReadAllText(_save.Paths.SaveFile);
            var saved = JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(ExitState.DeathPending, saved.exitState);
            CollectionAssert.AreEqual(new[] { "Death_DeathObject", Node }, saved.diedNodeIds);
            CollectionAssert.AreEqual(new[] { Path.Combine(_save.Paths.Corpses, "Corpse001.txt") }, Corpses());
        }

        [Test]
        public void RecordWindowClose_WhenAlreadyDied_DoesNotDuplicateRecord_ButAddsCorpse()
        {
            SaveManager.Data.diedNodeIds.Add(Node);
            Directory.CreateDirectory(_save.Paths.Corpses);
            File.WriteAllText(Path.Combine(_save.Paths.Corpses, "Corpse001.txt"), "");

            ExpectRecordedLog();
            ExitRecorder.RecordWindowClose();

            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { Node }, SaveManager.Data.diedNodeIds);
            Assert.AreEqual(2, Corpses().Length);
        }

        [Test]
        public void RecordWindowClose_Twice_CreatesOneCorpse()
        {
            ExpectRecordedLog();
            ExitRecorder.RecordWindowClose();
            ExitRecorder.RecordWindowClose();

            Assert.IsTrue(ExitRecorder.WindowCloseRecorded);
            Assert.AreEqual(1, Corpses().Length);
            CollectionAssert.AreEqual(new[] { Node }, SaveManager.Data.diedNodeIds);
        }

        [Test]
        public void RecordWindowClose_AfterNormalQuit_IsIgnored()
        {
            Assert.IsTrue(ExitRecorder.BeginNormalQuit());

            ExitRecorder.RecordWindowClose();

            SaveManager.Load();
            Assert.AreEqual(ExitState.Normal, SaveManager.Data.exitState);
            CollectionAssert.IsEmpty(SaveManager.Data.diedNodeIds);
            CollectionAssert.IsEmpty(Corpses());
            Assert.IsFalse(ExitRecorder.WindowCloseRecorded);
        }

        [Test]
        public void BeginNormalQuit_AfterWindowClose_IsIgnored()
        {
            ExpectRecordedLog();
            ExitRecorder.RecordWindowClose();

            Assert.IsFalse(ExitRecorder.BeginNormalQuit());

            SaveManager.Load();
            Assert.AreEqual(ExitState.DeathPending, SaveManager.Data.exitState);
        }

        [Test]
        public void RecordWindowClose_WhenSaveFails_DoesNotCreateCorpse()
        {
            Directory.CreateDirectory(_save.Base);
            File.WriteAllText(_save.Paths.Root, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            LogAssert.Expect(LogType.Error, new Regex(@"\[ExitRecorder\] 창 닫기 사망을 저장하지 못해"));
            ExitRecorder.RecordWindowClose();

            Assert.IsFalse(Directory.Exists(_save.Paths.Corpses));
        }

        [Test]
        public void RecordWindowClose_WhenCorpseFolderIsFile_KeepsSaveAndLogsError()
        {
            Directory.CreateDirectory(_save.Paths.Root);
            File.WriteAllText(_save.Paths.Corpses, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[ExitRecorder\] 창 닫기 사망은 저장했지만 시체 파일을 만들지 못했다"));
            ExitRecorder.RecordWindowClose();

            SaveManager.Load();
            Assert.AreEqual(ExitState.DeathPending, SaveManager.Data.exitState);
            Assert.AreEqual("not a folder", File.ReadAllText(_save.Paths.Corpses));
        }

        static void ExpectRecordedLog() =>
            LogAssert.Expect(LogType.Log, new Regex(@"\[ExitRecorder\] 창 닫기 사망 저장, 시체 파일 생성"));
    }
}
