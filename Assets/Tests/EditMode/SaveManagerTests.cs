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
        string _tempBase;
        GameFolderPaths _paths;

        [SetUp]
        public void SetUp()
        {
            _tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            _paths = new GameFolderPaths(Path.Combine(_tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Paths = _paths;
            SaveManager.Load();
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.Paths = null;
            if (Directory.Exists(_tempBase))
                Directory.Delete(_tempBase, true);
        }

        static void Fill(SaveData d)
        {
            d.solvedPuzzleIds.Add("PasswordDoor");
            d.solvedPuzzleIds.Add("GapA");
            d.deathRecords.Add(new DeathRecord { deathId = "FallingRock", count = 2 });
            d.deathRecords.Add(new DeathRecord { deathId = "CloseWindow", count = 1 });
            d.corpses.Add(new CorpseRecord { id = "FallingRock", position = new Vector3(1.5f, -2f, 0f), areaId = "UndergroundParking", kind = CorpseKind.World, fileName = "" });
            d.corpses.Add(new CorpseRecord { id = "CloseWindow", position = Vector3.zero, areaId = "Temp", kind = CorpseKind.File, fileName = "CloseWindow_1.txt" });
            d.corpses.Add(new CorpseRecord { id = "CloseWindow", position = Vector3.zero, areaId = "Temp", kind = CorpseKind.File, fileName = "corpse_002.txt" });
            d.respawnAreaId = "UndergroundParking";
            d.hasRespawnPosition = true;
            d.respawnPosition = new Vector2(3.25f, -7.5f);
            d.investigatedNodeIds.Add("HintGapA");
            d.investigatedNodeIds.Add("TempRespawn");
            d.elevatorAreaId = "Temp";
            d.elevatorFileId = "b3c1e2d4-0000-4000-8000-000000000002";
            d.elevatorFileIdHistory.Add("b3c1e2d4-0000-4000-8000-000000000001");
            d.elevatorFileIdHistory.Add("b3c1e2d4-0000-4000-8000-000000000002");
            d.exitState = ExitState.DeathPendingRetrigger;
        }

        [Test]
        public void Save_CreatesRootAndSaveJsonUnderInjectedRoot()
        {
            Assert.IsFalse(Directory.Exists(_paths.Root));

            Assert.IsTrue(SaveManager.Save());

            Assert.AreEqual(Path.Combine(_paths.Root, "save.json"), SaveManager.SavePath);
            Assert.IsTrue(File.Exists(_paths.SaveFile));
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
            Assert.IsTrue(SaveManager.Save());

            StringAssert.StartsWith(_tempBase, SaveManager.SavePath);
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
            Assert.AreEqual(e.deathRecords.Count, d.deathRecords.Count);
            for (int i = 0; i < e.deathRecords.Count; i++)
            {
                Assert.AreEqual(e.deathRecords[i].deathId, d.deathRecords[i].deathId);
                Assert.AreEqual(e.deathRecords[i].count, d.deathRecords[i].count);
            }
            Assert.AreEqual(e.corpses.Count, d.corpses.Count);
            for (int i = 0; i < e.corpses.Count; i++)
            {
                Assert.AreEqual(e.corpses[i].id, d.corpses[i].id);
                Assert.AreEqual(e.corpses[i].position, d.corpses[i].position);
                Assert.AreEqual(e.corpses[i].areaId, d.corpses[i].areaId);
                Assert.AreEqual(e.corpses[i].kind, d.corpses[i].kind);
                Assert.AreEqual(e.corpses[i].fileName, d.corpses[i].fileName);
            }
            Assert.AreEqual(e.respawnAreaId, d.respawnAreaId);
            Assert.AreEqual(e.hasRespawnPosition, d.hasRespawnPosition);
            Assert.AreEqual(e.respawnPosition, d.respawnPosition);
            CollectionAssert.AreEqual(e.investigatedNodeIds, d.investigatedNodeIds);
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
            string previous = File.ReadAllText(_paths.SaveFile);

            SaveManager.Data.respawnAreaId = "UndergroundParking";
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            bool result;
            using (new FileStream(_paths.SaveFile, FileMode.Open, FileAccess.Read, FileShare.None))
                result = SaveManager.Save();

            Assert.IsFalse(result);
            Assert.AreEqual(previous, File.ReadAllText(_paths.SaveFile));
            Assert.IsFalse(File.Exists(SaveManager.TempPath));

            SaveManager.Load();
            Assert.AreEqual("Temp", SaveManager.Data.respawnAreaId);
        }

        [Test]
        public void Save_WhenRootCannotBeCreated_ReturnsFalse()
        {
            Directory.CreateDirectory(_tempBase);
            File.WriteAllText(_paths.Root, "not a folder");

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
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.SaveFile, "{ broken json");

            LogAssert.Expect(LogType.Warning, new Regex(@"\[SaveManager\] 세이브 로드 실패"));
            SaveManager.Load();

            AssertDefaults(SaveManager.Data);
            Assert.AreEqual("{ broken json", File.ReadAllText(_paths.SaveFile));
        }

        [Test]
        public void Load_WhenFileEmpty_UsesDefaults()
        {
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.SaveFile, "");

            SaveManager.Load();
            AssertDefaults(SaveManager.Data);
        }

        [Test]
        public void Load_OldSaveWithoutNewFields_HasNoRespawnPositionAndNoInvestigated()
        {
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.SaveFile,
                "{\n    \"solvedPuzzleIds\": [\"PasswordDoor\"],\n    \"deathRecords\": [],\n    \"corpses\": [],\n" +
                "    \"respawnAreaId\": \"Temp\",\n    \"elevatorAreaId\": \"\",\n    \"elevatorFileId\": \"\",\n" +
                "    \"elevatorFileIdHistory\": [],\n    \"exitState\": 0\n}");

            SaveManager.Load();
            var d = SaveManager.Data;

            Assert.IsFalse(d.hasRespawnPosition);
            Assert.AreEqual(Vector2.zero, d.respawnPosition);
            Assert.IsNotNull(d.investigatedNodeIds);
            CollectionAssert.IsEmpty(d.investigatedNodeIds);
            CollectionAssert.AreEqual(new[] { "PasswordDoor" }, d.solvedPuzzleIds);
            Assert.AreEqual("Temp", d.respawnAreaId);
        }

        static void AssertDefaults(SaveData d)
        {
            Assert.IsNotNull(d);
            CollectionAssert.IsEmpty(d.solvedPuzzleIds);
            CollectionAssert.IsEmpty(d.deathRecords);
            CollectionAssert.IsEmpty(d.corpses);
            Assert.AreEqual("", d.respawnAreaId);
            Assert.IsFalse(d.hasRespawnPosition);
            CollectionAssert.IsEmpty(d.investigatedNodeIds);
            Assert.AreEqual("", d.elevatorAreaId);
            Assert.AreEqual("", d.elevatorFileId);
            CollectionAssert.IsEmpty(d.elevatorFileIdHistory);
            Assert.AreEqual(ExitState.None, d.exitState);
        }
    }
}
