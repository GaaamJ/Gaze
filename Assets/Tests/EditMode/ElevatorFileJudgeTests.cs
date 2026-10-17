using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProjectT.Core.Elevator;
using ProjectT.Core.GameFolder;

namespace ProjectT.Core.Tests
{
    public class ElevatorFileJudgeTests
    {
        const string Current = "b3c1e2d4-0000-4000-8000-000000000001";
        const string Old = "b3c1e2d4-0000-4000-8000-000000000002";
        const string Stranger = "b3c1e2d4-0000-4000-8000-000000000009";

        static readonly string[] Floors = { "Temp", "UndergroundParking" };

        TempSaveSandbox _save;
        GameFolderPaths _paths;
        List<string> _history;

        [SetUp]
        public void SetUp()
        {
            _save = new TempSaveSandbox();
            _paths = _save.Paths;
            GameFolderStructure.EnsureStructure(_paths, Floors);
            _history = new List<string> { Old, Current };
        }

        [TearDown]
        public void TearDown()
        {
            _save.Dispose();
        }

        ElevatorJudgement Judge(string currentId = Current) =>
            ElevatorFileJudge.Judge(_paths, Floors, currentId, _history);

        string Put(string floor, string fileName, string content)
        {
            string file = Path.Combine(_paths.FloorFolder(floor), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, content);
            return file;
        }

        void PutElevator(string floor = "Temp") => Put(floor, GameFolderPaths.ElevatorFileName, Current);

        [Test]
        public void SingleFile_IsNormalOnThatFloor()
        {
            PutElevator();

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("Temp", result.Floor);
            Assert.IsFalse(result.IsViolation);
        }

        [Test]
        public void SingleFileOnOtherFloor_IsNormalOnThatFloor()
        {
            PutElevator("UndergroundParking");

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("UndergroundParking", result.Floor);
        }

        [Test]
        public void TwoCurrentFilesOnSameFloor_IsCopy()
        {
            PutElevator();
            Put("Temp", "elevator - 복사본.ev", Current);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Copy, result.Verdict);
            Assert.IsNull(result.Floor);
            Assert.IsTrue(result.IsViolation);
        }

        [Test]
        public void TwoCurrentFilesOnTwoFloors_IsCopy()
        {
            PutElevator("Temp");
            PutElevator("UndergroundParking");

            Assert.AreEqual(ElevatorVerdict.Copy, Judge().Verdict);
        }

        [Test]
        public void NoFile_IsDeleted()
        {
            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);
        }

        [Test]
        public void FloorFolderWithFileRemoved_IsDeleted()
        {
            PutElevator();
            Directory.Delete(_paths.FloorFolder("Temp"), true);

            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);
        }

        [Test]
        public void FileMovedToAreasRoot_IsDeleted()
        {
            File.WriteAllText(Path.Combine(_paths.Areas, GameFolderPaths.ElevatorFileName), Current);

            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);
        }

        [Test]
        public void FileMovedToSubfolder_IsDeleted()
        {
            Put("Temp", Path.Combine("Sub", GameFolderPaths.ElevatorFileName), Current);

            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);
        }

        [Test]
        public void OtherName_IsRenamed()
        {
            Put("Temp", "a.ev", Current);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Renamed, result.Verdict);
            Assert.IsNull(result.Floor);
        }

        [TestCase("ELEVATOR.EV")]
        [TestCase("Elevator.ev")]
        public void CaseOnlyDifferentName_IsCollectedAndRenamed(string fileName)
        {
            Put("Temp", fileName, Current);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Renamed, result.Verdict);
            StringAssert.Contains(fileName, result.Reason);
        }

        [TestCase(Stranger)]
        [TestCase("")]
        [TestCase("not a guid")]
        public void UnknownOrBrokenContent_IsFake(string content)
        {
            Put("Temp", GameFolderPaths.ElevatorFileName, content);

            Assert.AreEqual(ElevatorVerdict.Fake, Judge().Verdict);
        }

        [Test]
        public void FakeBesideNormal_IsFakeFirst()
        {
            PutElevator("Temp");
            Put("UndergroundParking", GameFolderPaths.ElevatorFileName, Stranger);
            Put("UndergroundParking", "copy.ev", Current);

            Assert.AreEqual(ElevatorVerdict.Fake, Judge().Verdict);
        }

        [Test]
        public void OldValueBesideNormal_IsIgnored()
        {
            PutElevator("Temp");
            Put("UndergroundParking", GameFolderPaths.ElevatorFileName, Old);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("Temp", result.Floor);
        }

        [Test]
        public void OnlyOldValue_IsDeleted()
        {
            Put("Temp", GameFolderPaths.ElevatorFileName, Old);

            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);
        }

        [Test]
        public void NonEvFilesAndCorpses_AreIgnored()
        {
            PutElevator();
            Put("Temp", "note.txt", Stranger);
            Put("Temp", "elevator.ev.txt", Current);
            Put("Temp", "elevator", Current);
            File.WriteAllText(Path.Combine(_paths.Corpses, "body.ev"), Stranger);
            File.WriteAllText(Path.Combine(_paths.Corpses, GameFolderPaths.ElevatorFileName), Current);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("Temp", result.Floor);
        }

        [Test]
        public void UpperCaseExtension_IsCollected()
        {
            PutElevator();
            Put("UndergroundParking", "other.EV", Stranger);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Fake, result.Verdict);
            StringAssert.Contains("other.EV", result.Reason);
        }

        [Test]
        public void FolderOutsideFloorList_IsIgnored()
        {
            Directory.CreateDirectory(Path.Combine(_paths.Areas, "Foo"));
            File.WriteAllText(Path.Combine(_paths.Areas, "Foo", GameFolderPaths.ElevatorFileName), Current);

            Assert.AreEqual(ElevatorVerdict.Deleted, Judge().Verdict);

            PutElevator();
            File.WriteAllText(Path.Combine(_paths.Areas, "Foo", "fake.ev"), Stranger);

            Assert.AreEqual(ElevatorVerdict.Normal, Judge().Verdict);
        }

        [Test]
        public void FloorNameCaseOnlyDifferent_IsSameFloor()
        {
            PutElevator();

            var result = ElevatorFileJudge.Judge(_paths, new[] { "TEMP", "temp", "UndergroundParking" }, Current, _history);

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("TEMP", result.Floor);
        }

        [Test]
        public void EmptyCurrentId_MakesEveryFileFake()
        {
            PutElevator();

            Assert.AreEqual(ElevatorVerdict.Fake, ElevatorFileJudge.Judge(_paths, Floors, "", new List<string>()).Verdict);
            Assert.AreEqual(ElevatorVerdict.Fake, ElevatorFileJudge.Judge(_paths, Floors, null, null).Verdict);
        }

        [Test]
        public void EmptyFloorFolderRemoved_StaysNormal()
        {
            PutElevator("Temp");
            Directory.Delete(_paths.FloorFolder("UndergroundParking"), true);

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("Temp", result.Floor);
        }

        [Test]
        public void BomFileFromNotepad_IsNormal()
        {
            File.WriteAllText(_paths.ElevatorFilePath("Temp"), Current.ToUpperInvariant() + "\r\n", new UTF8Encoding(true));

            var result = Judge();

            Assert.AreEqual(ElevatorVerdict.Normal, result.Verdict);
            Assert.AreEqual("Temp", result.Floor);
        }

        [Test]
        public void Judge_DoesNotThrowOnMissingInputs()
        {
            Assert.DoesNotThrow(() => ElevatorFileJudge.Judge(null, null, null, null));
            Assert.DoesNotThrow(() => ElevatorFileJudge.Judge(_paths, new[] { null, "", "..", "a/b" }, Current, _history));
            Assert.AreEqual(ElevatorVerdict.Deleted, ElevatorFileJudge.Judge(null, Floors, Current, _history).Verdict);
        }
    }
}
