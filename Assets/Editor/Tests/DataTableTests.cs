using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectT.Data.Tests
{
    public class DataTableTests
    {
        [Test]
        public void NodeNames_FollowNodeIdRule()
        {
            Assert.AreEqual("TempRespawn", DialogueNodeNames.First("TempRespawn"));
            Assert.AreEqual("TempRespawn_Recheck", DialogueNodeNames.Recheck("TempRespawn"));
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
