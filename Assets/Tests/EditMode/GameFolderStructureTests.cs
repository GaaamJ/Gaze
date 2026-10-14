using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectT.Core.GameFolder;

namespace ProjectT.Core.Tests
{
    public class GameFolderStructureTests
    {
        static readonly string[] Floors = { "Temp", "UndergroundParking" };

        string _tempBase;
        GameFolderPaths _paths;

        [SetUp]
        public void SetUp()
        {
            _tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            _paths = new GameFolderPaths(Path.Combine(_tempBase, GameFolderPaths.RootFolderName));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempBase))
                Directory.Delete(_tempBase, true);
        }

        [Test]
        public void DefaultRoot_IsUserProfileSalbyeol()
        {
            var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Salbyeol");
            Assert.AreEqual(expected, GameFolderPaths.DefaultRoot);
            Assert.AreEqual(expected, GameFolderPaths.CreateDefault().Root);
        }

        [Test]
        public void Paths_AreBuiltFromInjectedRoot()
        {
            var root = Path.Combine(_tempBase, "Salbyeol");
            Assert.AreEqual(root, _paths.Root);
            Assert.AreEqual(Path.Combine(root, "Areas"), _paths.Areas);
            Assert.AreEqual(Path.Combine(root, "Corpses"), _paths.Corpses);
            Assert.AreEqual(Path.Combine(root, "save.json"), _paths.SaveFile);
            Assert.AreEqual(Path.Combine(root, "Areas", "Temp"), _paths.FloorFolder("Temp"));
            Assert.AreEqual(Path.Combine(root, "Areas", "UndergroundParking"), _paths.FloorFolder("UndergroundParking\\"));
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase("..")]
        [TestCase("Areas\\Temp")]
        [TestCase("a/b")]
        [TestCase(null)]
        public void FloorFolder_RejectsInvalidNames(string name)
        {
            Assert.Throws<ArgumentException>(() => _paths.FloorFolder(name));
        }

        [Test]
        public void Ensure_OnEmptyRoot_CreatesAllFolders()
        {
            var result = GameFolderStructure.EnsureStructure(_paths, Floors);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(Directory.Exists(Path.Combine(_paths.Areas, "Temp")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_paths.Areas, "UndergroundParking")));
            Assert.IsTrue(Directory.Exists(_paths.Corpses));
            Assert.IsFalse(File.Exists(_paths.SaveFile));
        }

        [Test]
        public void Ensure_AfterDeletingOne_RecreatesOnlyThatOne()
        {
            GameFolderStructure.EnsureStructure(_paths, Floors);
            var elevatorFile = Path.Combine(_paths.FloorFolder("Temp"), "elevator.ev");
            File.WriteAllText(elevatorFile, "keep");
            Directory.Delete(_paths.FloorFolder("UndergroundParking"));

            var result = GameFolderStructure.EnsureStructure(_paths, Floors);

            CollectionAssert.AreEqual(new[] { _paths.FloorFolder("UndergroundParking") }, result.Created);
            Assert.AreEqual("keep", File.ReadAllText(elevatorFile));
        }

        [Test]
        public void Ensure_KeepsExistingFiles()
        {
            Directory.CreateDirectory(_paths.Corpses);
            var corpse = Path.Combine(_paths.Corpses, "corpse.txt");
            File.WriteAllText(corpse, "body");
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.SaveFile, "{}");

            var result = GameFolderStructure.EnsureStructure(_paths, Floors);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("body", File.ReadAllText(corpse));
            Assert.AreEqual("{}", File.ReadAllText(_paths.SaveFile));
            CollectionAssert.DoesNotContain(result.Created, _paths.Corpses);
        }

        [Test]
        public void Ensure_WhenFileOccupiesFolderName_KeepsFileAndCreatesOthers()
        {
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.Areas, "not a folder");

            var result = GameFolderStructure.EnsureStructure(_paths, Floors);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("not a folder", File.ReadAllText(_paths.Areas));
            Assert.IsTrue(result.Failed.Any(f => f.Path == _paths.Areas));
            Assert.IsTrue(Directory.Exists(_paths.Corpses));
        }

        [Test]
        public void Ensure_SkipsInvalidAndDuplicateFloorNames()
        {
            var result = GameFolderStructure.EnsureStructure(_paths, new[] { "Temp", "temp\\", "..", "" });

            CollectionAssert.AreEquivalent(new[] { "..", "" }, result.InvalidFloorNames);
            Assert.AreEqual(1, Directory.GetDirectories(_paths.Areas).Length);
        }

        [Test]
        public void CoreAssembly_DoesNotReferenceAssemblyCSharp()
        {
            var references = typeof(GameFolderPaths).Assembly.GetReferencedAssemblies().Select(a => a.Name);
            CollectionAssert.DoesNotContain(references, "Assembly-CSharp");
            CollectionAssert.DoesNotContain(references, "Assembly-CSharp-firstpass");
        }
    }
}
