using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Save;

namespace ProjectT.Core.Tests
{
    public class ElevatorFileTests
    {
        const string SampleId = "b3c1e2d4-0000-4000-8000-000000000001";

        TempSaveSandbox _save;
        string _file;

        [SetUp]
        public void SetUp()
        {
            _save = new TempSaveSandbox();
            GameFolderStructure.EnsureStructure(_save.Paths, new[] { "Temp", "UndergroundParking" });
            _file = _save.Paths.ElevatorFilePath("Temp");
        }

        [TearDown]
        public void TearDown()
        {
            _save.Dispose();
        }

        [Test]
        public void ElevatorFilePath_IsInsideFloorFolder()
        {
            Assert.AreEqual(Path.Combine(_save.Paths.Root, "Areas", "Temp", "elevator.ev"), _file);
        }

        [Test]
        public void Write_StoresOnlyGuidText()
        {
            Assert.IsTrue(ElevatorFile.Write(_file, SampleId));

            byte[] bytes = File.ReadAllBytes(_file);
            Assert.AreEqual(SampleId, Encoding.ASCII.GetString(bytes));
        }

        [Test]
        public void Write_OverwritesExistingFile()
        {
            File.WriteAllText(_file, "old content that is longer than a guid ...........................");
            Assert.IsTrue(ElevatorFile.Write(_file, SampleId));
            Assert.AreEqual(SampleId, File.ReadAllText(_file));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-guid")]
        public void Write_RejectsInvalidId(string id)
        {
            Assert.IsFalse(ElevatorFile.Write(_file, id));
            Assert.IsFalse(File.Exists(_file));
        }

        [Test]
        public void Write_WhenFolderMissing_ReturnsFalse()
        {
            Assert.IsFalse(ElevatorFile.Write(Path.Combine(_save.Paths.Areas, "Nowhere", "elevator.ev"), SampleId));
        }

        [Test]
        public void TryReadId_ReadsWrittenId()
        {
            ElevatorFile.Write(_file, SampleId);

            Assert.IsTrue(ElevatorFile.TryReadId(_file, out var id));
            Assert.AreEqual(SampleId, id);
        }

        [Test]
        public void TryReadId_ToleratesBomAndSurroundingWhitespace()
        {
            File.WriteAllText(_file, "  " + SampleId.ToUpperInvariant() + "\r\n", new UTF8Encoding(true));

            Assert.IsTrue(ElevatorFile.TryReadId(_file, out var id));
            Assert.AreEqual(SampleId, id);
        }

        [Test]
        public void TryReadId_WhenMissing_Fails()
        {
            Assert.IsFalse(ElevatorFile.TryReadId(_file, out var id));
            Assert.IsNull(id);
        }

        [TestCase("")]
        [TestCase("   \r\n")]
        [TestCase("b3c1e2d4-0000-4000-8000-00000000000")]
        [TestCase("{b3c1e2d4-0000-4000-8000-000000000001}")]
        [TestCase("b3c1e2d4-0000-4000-8000-000000000001 extra")]
        [TestCase("b3c1e2d4-0000-4000-8000-000000000001\nb3c1e2d4-0000-4000-8000-000000000002")]
        public void TryReadId_WhenEmptyOrCorrupted_Fails(string content)
        {
            File.WriteAllText(_file, content);

            Assert.IsFalse(ElevatorFile.TryReadId(_file, out var id));
            Assert.IsNull(id);
        }

        [Test]
        public void TryReadId_WhenBinaryGarbage_Fails()
        {
            File.WriteAllBytes(_file, new byte[] { 0xFF, 0x00, 0x13, 0x80, 0xC3 });
            Assert.IsFalse(ElevatorFile.TryReadId(_file, out _));
        }

        [Test]
        public void TryReadId_WhenPathIsFolder_Fails()
        {
            Directory.CreateDirectory(_file);
            Assert.IsFalse(ElevatorFile.TryReadId(_file, out _));
        }

        [Test]
        public void NewId_IsUniqueGuidText()
        {
            string a = ElevatorFile.NewId();
            string b = ElevatorFile.NewId();
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(ElevatorFile.TryNormalizeId(a, out var normalized));
            Assert.AreEqual(a, normalized);
        }

        [Test]
        public void Issue_SetsCurrentAddsHistoryAndSaves()
        {
            Assert.IsTrue(ElevatorFileIds.Issue(out var id));

            Assert.AreEqual(id, SaveManager.Data.elevatorFileId);
            CollectionAssert.AreEqual(new[] { id }, SaveManager.Data.elevatorFileIdHistory);
            StringAssert.Contains(id, File.ReadAllText(_save.Paths.SaveFile));
        }

        [Test]
        public void Issue_ManyTimes_CurrentIsLastAndHistoryKeepsAll()
        {
            var issued = new string[3];
            for (int i = 0; i < issued.Length; i++)
                Assert.IsTrue(ElevatorFileIds.Issue(out issued[i]));

            CollectionAssert.AllItemsAreUnique(issued);
            SaveManager.Load();
            Assert.AreEqual(issued[2], SaveManager.Data.elevatorFileId);
            CollectionAssert.AreEqual(issued, SaveManager.Data.elevatorFileIdHistory);
        }

        [Test]
        public void FileCreation_WritesCurrentIdFromSave()
        {
            ElevatorFileIds.Issue(out _);
            ElevatorFileIds.Issue(out var last);

            Assert.IsTrue(ElevatorFileIds.WriteCurrent(_file));

            SaveManager.Load();
            Assert.IsTrue(ElevatorFile.TryReadId(_file, out var read));
            Assert.AreEqual(last, read);
            Assert.AreEqual(SaveManager.Data.elevatorFileId, read);
        }

        [Test]
        public void WriteCurrent_WhenNothingIssued_Fails()
        {
            Assert.IsFalse(ElevatorFileIds.WriteCurrent(_file));
            Assert.IsFalse(File.Exists(_file));
        }

        [Test]
        public void Issue_DoesNotTouchRealGameFolder()
        {
            var real = GameFolderPaths.CreateDefault();
            string realSave = real.SaveFile;
            string realElevator = real.ElevatorFilePath("Temp");
            bool saveExisted = File.Exists(realSave);
            bool elevatorExisted = File.Exists(realElevator);
            DateTime saveBefore = saveExisted ? File.GetLastWriteTimeUtc(realSave) : default;
            DateTime elevatorBefore = elevatorExisted ? File.GetLastWriteTimeUtc(realElevator) : default;

            ElevatorFileIds.Issue(out _);
            ElevatorFileIds.WriteCurrent(_file);

            StringAssert.StartsWith(_save.Base, SaveManager.SavePath);
            StringAssert.StartsWith(_save.Base, _file);
            Assert.AreEqual(saveExisted, File.Exists(realSave));
            Assert.AreEqual(elevatorExisted, File.Exists(realElevator));
            if (saveExisted) Assert.AreEqual(saveBefore, File.GetLastWriteTimeUtc(realSave));
            if (elevatorExisted) Assert.AreEqual(elevatorBefore, File.GetLastWriteTimeUtc(realElevator));
        }
    }
}
