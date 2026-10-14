using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectT.Data.Tests
{
    public class DataTableTests
    {
        [Test]
        public void NodeNames_FollowObjectIdRule()
        {
            Assert.AreEqual("Obj_TempRespawn", DialogueNodeNames.Obj("TempRespawn"));
            Assert.AreEqual("Obj_TempRespawn_Recheck", DialogueNodeNames.ObjRecheck("TempRespawn"));
        }

        [Test]
        public void FindMissing_ReturnsOnlyAbsentNonEmptyNodesOnce()
        {
            var existing = new HashSet<string> { "A", "B" };
            var missing = YarnNodeCheck.FindMissing(new[] { "A", "", null, "  ", "C", "C", "B", "D" }, existing);
            CollectionAssert.AreEqual(new[] { "C", "D" }, missing);
        }

        [Test]
        public void ProjectNodeNames_ContainExistingPrototypeNode()
        {
            var names = YarnNodeCheck.LoadProjectNodeNames();
            Assert.Contains("Obj_TempRespawn", new List<string>(names));
            Assert.IsFalse(names.Contains("Obj_NoSuchNode"));
        }
    }
}
