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
    public class DeathRecordsTests
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
            SaveManager.Load();
            if (Directory.Exists(_tempBase))
                Directory.Delete(_tempBase, true);
        }

        [Test]
        public void Add_SavesToFile_AndHasReturnsTrue()
        {
            Assert.IsFalse(DeathRecords.Has("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Has("Death_DeathObject"));
            Assert.IsFalse(DeathRecords.Has("Death_FallingDeath"));
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Death_DeathObject" }, SaveManager.Data.diedNodeIds);
        }

        [Test]
        public void Add_SameNodeTwice_KeepsOneEntry()
        {
            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));
            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));
            Assert.IsTrue(DeathRecords.Add("Death_FallingDeath"));

            CollectionAssert.AreEqual(new[] { "Death_DeathObject", "Death_FallingDeath" }, SaveManager.Data.diedNodeIds);
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Death_DeathObject", "Death_FallingDeath" }, SaveManager.Data.diedNodeIds);
        }

        [Test]
        public void Add_NullOrBlank_IsIgnoredWithoutSaving()
        {
            Assert.IsTrue(DeathRecords.Add(null));
            Assert.IsTrue(DeathRecords.Add(""));
            Assert.IsTrue(DeathRecords.Add("   "));

            CollectionAssert.IsEmpty(SaveManager.Data.diedNodeIds);
            Assert.IsFalse(File.Exists(_paths.SaveFile));
            Assert.IsFalse(DeathRecords.Has(null));
            Assert.IsFalse(DeathRecords.Has(""));
        }

        [Test]
        public void Add_WhenSaveFails_WarnsAndKeepsInMemory()
        {
            Directory.CreateDirectory(_tempBase);
            File.WriteAllText(_paths.Root, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathRecords\] 사망 기록 저장에 실패했다"));
            Assert.IsFalse(DeathRecords.Add("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Has("Death_DeathObject"));
            CollectionAssert.AreEqual(new[] { "Death_DeathObject" }, SaveManager.Data.diedNodeIds);
        }
    }
}
