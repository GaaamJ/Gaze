using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using UnityEditor;

namespace ProjectT.Data.Tests
{
    public class AreaFloorFolderTests
    {
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

        static AreaData LoadArea(string assetName) =>
            AssetDatabase.LoadAssetAtPath<AreaData>($"Assets/Data/Areas/{assetName}.asset");

        static AreaData[] AllAreas() =>
            AssetDatabase.FindAssets("t:AreaData")
                .Select(g => AssetDatabase.LoadAssetAtPath<AreaData>(AssetDatabase.GUIDToAssetPath(g)))
                .ToArray();

        [TestCase("Temp", "Temp", "Temp")]
        [TestCase("UndergroundParking", "UndergroundParking", "UndergroundParking")]
        public void AreaAsset_HasOnlyIdAndFloorFolderName(string assetName, string expectedId, string expectedFolder)
        {
            var area = LoadArea(assetName);
            Assert.IsNotNull(area);
            Assert.AreEqual(expectedId, area.AreaId);
            Assert.AreEqual(expectedFolder, area.FloorFolderName);

            var fields = new System.Collections.Generic.List<string>();
            var property = new SerializedObject(area).GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
                if (property.name != "m_Script")
                    fields.Add(property.name);
            CollectionAssert.AreEqual(new[] { "areaId", "floorFolderName" }, fields);
        }

        [TestCase("Temp", "Temp")]
        [TestCase("UndergroundParking", "UndergroundParking")]
        public void FloorFolder_IsBuiltFromAreaDataName(string assetName, string expectedFolder)
        {
            var area = LoadArea(assetName);
            Assert.IsNotNull(area);

            Assert.AreEqual(Path.Combine(_paths.Root, "Areas", expectedFolder), AreaFloorFolders.FloorFolder(_paths, area));
            Assert.AreEqual(Path.Combine(_paths.Root, "Areas", expectedFolder, "elevator.ev"), AreaFloorFolders.ElevatorFile(_paths, area));
        }

        [Test]
        public void Names_ComeFromProjectAreaDataAndSkipNull()
        {
            var names = AreaFloorFolders.Names(AllAreas().Append(null));
            CollectionAssert.AreEquivalent(new[] { "Temp", "UndergroundParking" }, names);
        }

        [Test]
        public void EnsureStructure_CreatesFloorFoldersFromAreaData()
        {
            var result = AreaFloorFolders.EnsureStructure(_paths, AllAreas());

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(Directory.Exists(Path.Combine(_paths.Areas, "Temp")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_paths.Areas, "UndergroundParking")));
            Assert.IsTrue(Directory.Exists(_paths.Corpses));
            Assert.AreEqual(2, Directory.GetDirectories(_paths.Areas).Length);
        }
    }
}
