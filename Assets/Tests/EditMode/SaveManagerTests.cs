using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Core.Tests
{
    public class SaveManagerTests
    {
        TempSaveSandbox _save;

        [SetUp]
        public void SetUp()
        {
            _save = new TempSaveSandbox();
        }

        [TearDown]
        public void TearDown()
        {
            _save.Dispose();
        }

        static void Fill(SaveData d)
        {
            d.solvedPuzzleIds.Add("PasswordDoor");
            d.solvedPuzzleIds.Add("GapA");
            d.respawnAreaId = "UndergroundParking";
            d.hasRespawnPosition = true;
            d.respawnPosition = new Vector2(3.25f, -7.5f);
            d.investigatedNodeIds.Add("HintGapA");
            d.investigatedNodeIds.Add("TempRespawn");
            d.diedNodeIds.Add("Death_DeathObject");
            d.diedNodeIds.Add("Death_Falling001");
            d.elevatorAreaId = "Temp";
            d.elevatorFileId = "b3c1e2d4-0000-4000-8000-000000000002";
            d.elevatorFileIdHistory.Add("b3c1e2d4-0000-4000-8000-000000000001");
            d.elevatorFileIdHistory.Add("b3c1e2d4-0000-4000-8000-000000000002");
            d.exitState = ExitState.DeathPending;
        }

        [Test]
        public void Save_CreatesRootAndSaveJsonUnderInjectedRoot()
        {
            Assert.IsFalse(Directory.Exists(_save.Paths.Root));

            Assert.IsTrue(SaveManager.Save());

            Assert.AreEqual(Path.Combine(_save.Paths.Root, "save.json"), SaveManager.SavePath);
            Assert.IsTrue(File.Exists(_save.Paths.SaveFile));
            Assert.IsFalse(File.Exists(SaveManager.TempPath));
        }

        [Test]
        public void Save_DoesNotTouchRealGameFolder()
        {
            string real = GameFolderPaths.CreateDefault().SaveFile;
            bool existed = File.Exists(real);
            DateTime before = existed ? File.GetLastWriteTimeUtc(real) : default;

            Fill(SaveManager.Data);
            Assert.IsTrue(SaveManager.Save());

            StringAssert.StartsWith(_save.Base, SaveManager.SavePath);
            Assert.AreEqual(existed, File.Exists(real));
            if (existed)
                Assert.AreEqual(before, File.GetLastWriteTimeUtc(real));
        }

        [Test]
        public void SaveThenLoad_RestoresAllFields()
        {
            Fill(SaveManager.Data);
            Assert.IsTrue(SaveManager.Save());

            SaveManager.Load();
            var d = SaveManager.Data;
            var e = new SaveData();
            Fill(e);

            CollectionAssert.AreEqual(e.solvedPuzzleIds, d.solvedPuzzleIds);
            Assert.AreEqual(e.respawnAreaId, d.respawnAreaId);
            Assert.AreEqual(e.hasRespawnPosition, d.hasRespawnPosition);
            Assert.AreEqual(e.respawnPosition, d.respawnPosition);
            CollectionAssert.AreEqual(e.investigatedNodeIds, d.investigatedNodeIds);
            CollectionAssert.AreEqual(e.diedNodeIds, d.diedNodeIds);
            Assert.AreEqual(e.elevatorAreaId, d.elevatorAreaId);
            Assert.AreEqual(e.elevatorFileId, d.elevatorFileId);
            CollectionAssert.AreEqual(e.elevatorFileIdHistory, d.elevatorFileIdHistory);
            Assert.AreEqual(e.exitState, d.exitState);
        }

        [Test]
        public void Save_OverwritesExistingSave()
        {
            Assert.IsTrue(SaveManager.Save());
            SaveManager.Data.respawnAreaId = "UndergroundParking";
            Assert.IsTrue(SaveManager.Save());

            SaveManager.Load();
            Assert.AreEqual("UndergroundParking", SaveManager.Data.respawnAreaId);
            Assert.IsFalse(File.Exists(SaveManager.TempPath));
        }

        [Test]
        public void Save_WhenReplaceFails_KeepsPreviousSaveAndReturnsFalse()
        {
            SaveManager.Data.respawnAreaId = "Temp";
            Assert.IsTrue(SaveManager.Save());
            string previous = File.ReadAllText(_save.Paths.SaveFile);

            SaveManager.Data.respawnAreaId = "UndergroundParking";
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            bool result;
            using (new FileStream(_save.Paths.SaveFile, FileMode.Open, FileAccess.Read, FileShare.None))
                result = SaveManager.Save();

            Assert.IsFalse(result);
            Assert.AreEqual(previous, File.ReadAllText(_save.Paths.SaveFile));
            Assert.IsFalse(File.Exists(SaveManager.TempPath));

            SaveManager.Load();
            Assert.AreEqual("Temp", SaveManager.Data.respawnAreaId);
        }

        [Test]
        public void Save_WhenRootCannotBeCreated_ReturnsFalse()
        {
            Directory.CreateDirectory(_save.Base);
            File.WriteAllText(_save.Paths.Root, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            Assert.IsFalse(SaveManager.Save());
        }

        [Test]
        public void Load_WhenFileMissing_UsesDefaults()
        {
            Fill(SaveManager.Data);
            SaveManager.Load();
            AssertDefaults(SaveManager.Data);
        }

        [Test]
        public void Load_WhenFileCorrupted_UsesDefaultsAndKeepsFile()
        {
            Directory.CreateDirectory(_save.Paths.Root);
            File.WriteAllText(_save.Paths.SaveFile, "{ broken json");

            LogAssert.Expect(LogType.Warning, new Regex(@"\[SaveManager\] 세이브 로드 실패"));
            SaveManager.Load();

            AssertDefaults(SaveManager.Data);
            Assert.AreEqual("{ broken json", File.ReadAllText(_save.Paths.SaveFile));
        }

        [Test]
        public void Load_WhenFileEmpty_UsesDefaults()
        {
            Directory.CreateDirectory(_save.Paths.Root);
            File.WriteAllText(_save.Paths.SaveFile, "");

            SaveManager.Load();
            AssertDefaults(SaveManager.Data);
        }

        [Test]
        public void Load_SaveMissingRespawnInvestigatedDiedFields_DefaultsThemAndKeepsOtherFields()
        {
            Directory.CreateDirectory(_save.Paths.Root);
            File.WriteAllText(_save.Paths.SaveFile,
                "{\n    \"solvedPuzzleIds\": [\"PasswordDoor\"],\n    \"deathRecords\": [],\n    \"corpses\": [],\n" +
                "    \"respawnAreaId\": \"Temp\",\n    \"elevatorAreaId\": \"\",\n    \"elevatorFileId\": \"\",\n" +
                "    \"elevatorFileIdHistory\": [],\n    \"exitState\": 0\n}");

            SaveManager.Load();
            var d = SaveManager.Data;

            Assert.IsFalse(d.hasRespawnPosition);
            Assert.AreEqual(Vector2.zero, d.respawnPosition);
            Assert.IsNotNull(d.investigatedNodeIds);
            CollectionAssert.IsEmpty(d.investigatedNodeIds);
            Assert.IsNotNull(d.diedNodeIds);
            CollectionAssert.IsEmpty(d.diedNodeIds);
            CollectionAssert.AreEqual(new[] { "PasswordDoor" }, d.solvedPuzzleIds);
            Assert.AreEqual("Temp", d.respawnAreaId);
        }

        [Test]
        public void Load_OldSaveWithRemovedDeathRecordsAndCorpses_IgnoresThem()
        {
            Directory.CreateDirectory(_save.Paths.Root);
            File.WriteAllText(_save.Paths.SaveFile,
                "{\n    \"solvedPuzzleIds\": [\"PasswordDoor\"],\n" +
                "    \"deathRecords\": [{\"deathId\": \"FallingRock\", \"count\": 2}],\n" +
                "    \"corpses\": [{\"id\": \"FallingRock\", \"position\": {\"x\": 1.5, \"y\": -2.0, \"z\": 0.0}, \"areaId\": \"Temp\", \"kind\": 1, \"fileName\": \"corpse_001.txt\"}],\n" +
                "    \"respawnAreaId\": \"Temp\",\n    \"diedNodeIds\": [\"Death_DeathObject\"],\n" +
                "    \"elevatorAreaId\": \"\",\n    \"elevatorFileId\": \"\",\n" +
                "    \"elevatorFileIdHistory\": [],\n    \"exitState\": 1\n}");

            SaveManager.Load();
            var d = SaveManager.Data;

            CollectionAssert.AreEqual(new[] { "PasswordDoor" }, d.solvedPuzzleIds);
            Assert.AreEqual("Temp", d.respawnAreaId);
            CollectionAssert.AreEqual(new[] { "Death_DeathObject" }, d.diedNodeIds);
            Assert.AreEqual(ExitState.Normal, d.exitState);

            Assert.IsTrue(SaveManager.Save());
            string saved = File.ReadAllText(_save.Paths.SaveFile);
            StringAssert.DoesNotContain("deathRecords", saved);
            StringAssert.DoesNotContain("corpses", saved);
        }

        static void AssertDefaults(SaveData d)
        {
            Assert.IsNotNull(d);
            CollectionAssert.IsEmpty(d.solvedPuzzleIds);
            Assert.AreEqual("", d.respawnAreaId);
            Assert.IsFalse(d.hasRespawnPosition);
            CollectionAssert.IsEmpty(d.investigatedNodeIds);
            CollectionAssert.IsEmpty(d.diedNodeIds);
            Assert.AreEqual("", d.elevatorAreaId);
            Assert.AreEqual("", d.elevatorFileId);
            CollectionAssert.IsEmpty(d.elevatorFileIdHistory);
            Assert.AreEqual(ExitState.None, d.exitState);
        }
    }
}
