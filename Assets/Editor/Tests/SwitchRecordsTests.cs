using System.Collections.Generic;
using NUnit.Framework;
using Gaze.Interaction;
using Gaze.Save;
using Gaze.Tests;
using UnityEngine;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Switches.Tests
{
    // 스위치를 켜면 읽을 수 있고, 새로 켜졌을 때만 알리는지 본다.
    // 리스폰 지점에서 확정하고 죽으면 되돌아가는 규칙도 본다.
    public class SwitchRecordsTests
    {
        readonly List<Object> created = new();
        TempSaveSandbox save;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            SwitchRecords.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            SwitchRecords.Clear();
            save.Dispose();
        }

        Switch NewSwitch(string id)
        {
            var made = ScriptableObject.CreateInstance<Switch>();
            created.Add(made);
            Set(made, so => so.FindProperty("id").stringValue = id);
            return made;
        }

        [Test]
        public void TurnOn_MakesIsOnTrue()
        {
            var key = NewSwitch("Key");

            Assert.IsFalse(SwitchRecords.IsOn(key));
            Assert.IsTrue(SwitchRecords.TurnOn(key));
            Assert.IsTrue(SwitchRecords.IsOn(key));
        }

        [Test]
        public void OtherSwitch_IsNotOn()
        {
            SwitchRecords.TurnOn(NewSwitch("Key"));

            Assert.IsFalse(SwitchRecords.IsOn(NewSwitch("Other")));
        }

        [Test]
        public void TurnOnTwice_NotifiesOnce()
        {
            var key = NewSwitch("Key");
            var notified = new List<Switch>();
            void OnChanged(Switch changed) => notified.Add(changed);

            SwitchRecords.Changed += OnChanged;
            try
            {
                Assert.IsTrue(SwitchRecords.TurnOn(key));
                Assert.IsFalse(SwitchRecords.TurnOn(key));
            }
            finally
            {
                SwitchRecords.Changed -= OnChanged;
            }

            CollectionAssert.AreEqual(new[] { key }, notified);
        }

        [Test]
        public void NullOrEmptyId_IsIgnored()
        {
            Assert.IsFalse(SwitchRecords.TurnOn(null));
            Assert.IsFalse(SwitchRecords.TurnOn(NewSwitch("")));
            Assert.IsFalse(SwitchRecords.IsOn(null));
            Assert.IsFalse(SwitchRecords.IsOn(NewSwitch("")));
        }

        [Test]
        public void Commit_SavesSwitches_AndLoadKeepsThem()
        {
            SwitchRecords.TurnOn(NewSwitch("B"));
            SwitchRecords.TurnOn(NewSwitch("A"));

            Assert.IsTrue(SwitchRecords.Commit());

            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "A", "B" }, SaveManager.Data.onSwitchIds);
        }

        [Test]
        public void Revert_DropsSwitchesNotCommitted()
        {
            var key = NewSwitch("Key");
            var other = NewSwitch("Other");
            SwitchRecords.TurnOn(key);
            SwitchRecords.Commit();
            SwitchRecords.TurnOn(other);

            SwitchRecords.Revert();

            Assert.IsTrue(SwitchRecords.IsOn(key));
            Assert.IsFalse(SwitchRecords.IsOn(other));
        }

        [Test]
        public void Revert_WithoutCommit_TurnsEverythingOff()
        {
            var key = NewSwitch("Key");
            SwitchRecords.TurnOn(key);

            SwitchRecords.Revert();

            Assert.IsFalse(SwitchRecords.IsOn(key));
        }

        [Test]
        public void Revert_DoesNotNotify()
        {
            var key = NewSwitch("Key");
            SwitchRecords.TurnOn(key);
            var notified = new List<Switch>();
            void OnChanged(Switch changed) => notified.Add(changed);

            SwitchRecords.Changed += OnChanged;
            try
            {
                SwitchRecords.Revert();
            }
            finally
            {
                SwitchRecords.Changed -= OnChanged;
            }

            CollectionAssert.IsEmpty(notified);
        }

        [Test]
        public void Revert_AfterReload_RestoresCommittedSwitches()
        {
            var key = NewSwitch("Key");
            SwitchRecords.TurnOn(key);
            SwitchRecords.Commit();

            SwitchRecords.Clear();
            SaveManager.Load();
            SwitchRecords.Revert();

            Assert.IsTrue(SwitchRecords.IsOn(key));
        }

        [Test]
        public void RespawnResponse_CommitsSwitches()
        {
            var key = NewSwitch("Key");
            SwitchRecords.TurnOn(key);
            var go = new GameObject("Respawn");
            created.Add(go);
            go.AddComponent<RespawnResponse>();

            go.GetComponent<Interactable>().Interact(new InteractContext(null, new HashSet<string>()));

            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Key" }, SaveManager.Data.onSwitchIds);
        }
    }
}
