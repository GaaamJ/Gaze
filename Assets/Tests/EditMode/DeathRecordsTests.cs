using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Core.Tests
{
    public class DeathRecordsTests
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

        [Test]
        public void Add_SavesToFile_AndHasReturnsTrue()
        {
            Assert.IsFalse(DeathRecords.Has("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Has("Death_DeathObject"));
            Assert.IsFalse(DeathRecords.Has("Death_Falling001"));
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Death_DeathObject" }, SaveManager.Data.diedNodeIds);
        }

        [Test]
        public void Add_SameNodeTwice_KeepsOneEntry()
        {
            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));
            Assert.IsTrue(DeathRecords.Add("Death_DeathObject"));
            Assert.IsTrue(DeathRecords.Add("Death_Falling001"));

            CollectionAssert.AreEqual(new[] { "Death_DeathObject", "Death_Falling001" }, SaveManager.Data.diedNodeIds);
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Death_DeathObject", "Death_Falling001" }, SaveManager.Data.diedNodeIds);
        }

        [Test]
        public void Add_NullOrBlank_IsIgnoredWithoutSaving()
        {
            Assert.IsTrue(DeathRecords.Add(null));
            Assert.IsTrue(DeathRecords.Add(""));
            Assert.IsTrue(DeathRecords.Add("   "));

            CollectionAssert.IsEmpty(SaveManager.Data.diedNodeIds);
            Assert.IsFalse(File.Exists(_save.Paths.SaveFile));
            Assert.IsFalse(DeathRecords.Has(null));
            Assert.IsFalse(DeathRecords.Has(""));
        }

        [Test]
        public void Add_WhenSaveFails_KeepsInMemory()
        {
            Directory.CreateDirectory(_save.Base);
            File.WriteAllText(_save.Paths.Root, "not a folder");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            Assert.IsFalse(DeathRecords.Add("Death_DeathObject"));

            Assert.IsTrue(DeathRecords.Has("Death_DeathObject"));
            CollectionAssert.AreEqual(new[] { "Death_DeathObject" }, SaveManager.Data.diedNodeIds);
        }
    }
}
