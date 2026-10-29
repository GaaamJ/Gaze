using System;
using System.IO;
using NUnit.Framework;
using Gaze.Core.GameFolder;

namespace Gaze.Core.Tests
{
    public class CorpseFilesTests
    {
        string _tempBase;
        GameFolderPaths _paths;

        [SetUp]
        public void SetUp()
        {
            _tempBase = Path.Combine(Path.GetTempPath(), "Gaze.Tests." + Guid.NewGuid().ToString("N"));
            _paths = new GameFolderPaths(Path.Combine(_tempBase, GameFolderPaths.RootFolderName));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempBase))
                Directory.Delete(_tempBase, true);
        }

        string Corpse(string name) => Path.Combine(_paths.Corpses, name);

        [Test]
        public void TryCreate_WithoutFolder_CreatesFolderAndCorpse001Empty()
        {
            Assert.IsTrue(CorpseFiles.TryCreate(_paths, out string path, out string error), error);

            Assert.AreEqual(Corpse("Corpse001.txt"), path);
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(0, new FileInfo(path).Length);
        }

        [Test]
        public void TryCreate_Repeated_NumbersInOrder()
        {
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(CorpseFiles.TryCreate(_paths, out _, out _));

            CollectionAssert.AreEquivalent(
                new[] { Corpse("Corpse001.txt"), Corpse("Corpse002.txt"), Corpse("Corpse003.txt") },
                Directory.GetFiles(_paths.Corpses));
        }

        [Test]
        public void TryCreate_UsesMaxNumberPlusOne_IgnoringCaseAndOtherNames()
        {
            Directory.CreateDirectory(_paths.Corpses);
            File.WriteAllText(Corpse("Corpse002.txt"), "");
            File.WriteAllText(Corpse("CORPSE7.TXT"), "");
            File.WriteAllText(Corpse("Corpse099.txt.bak"), "");
            File.WriteAllText(Corpse("Corpse.txt"), "");
            File.WriteAllText(Corpse("MyCorpse050.txt"), "");
            File.WriteAllText(Corpse("Corpse-3.txt"), "");

            Assert.IsTrue(CorpseFiles.TryCreate(_paths, out string path, out _));

            Assert.AreEqual(Corpse("Corpse008.txt"), path);
        }

        [Test]
        public void TryCreate_NumberOver999_KeepsGrowing()
        {
            Directory.CreateDirectory(_paths.Corpses);
            File.WriteAllText(Corpse("Corpse999.txt"), "");

            Assert.IsTrue(CorpseFiles.TryCreate(_paths, out string path, out _));

            Assert.AreEqual(Corpse("Corpse1000.txt"), path);
        }

        [Test]
        public void TryCreate_WhenNextNameIsTakenByFolder_TriesNextNumber()
        {
            Directory.CreateDirectory(_paths.Corpses);
            File.WriteAllText(Corpse("Corpse001.txt"), "");
            Directory.CreateDirectory(Corpse("Corpse002.txt"));

            Assert.IsTrue(CorpseFiles.TryCreate(_paths, out string path, out _));

            Assert.AreEqual(Corpse("Corpse003.txt"), path);
        }

        [Test]
        public void TryCreate_KeepsExistingCorpseContents()
        {
            Directory.CreateDirectory(_paths.Corpses);
            File.WriteAllText(Corpse("Corpse001.txt"), "body");

            Assert.IsTrue(CorpseFiles.TryCreate(_paths, out _, out _));

            Assert.AreEqual("body", File.ReadAllText(Corpse("Corpse001.txt")));
        }

        [Test]
        public void TryCreate_WhenFileOccupiesFolderName_FailsAndKeepsFile()
        {
            Directory.CreateDirectory(_paths.Root);
            File.WriteAllText(_paths.Corpses, "not a folder");

            Assert.IsFalse(CorpseFiles.TryCreate(_paths, out string path, out string error));

            Assert.IsNull(path);
            StringAssert.Contains(_paths.Corpses, error);
            Assert.AreEqual("not a folder", File.ReadAllText(_paths.Corpses));
        }
    }
}
