using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Switches;
using UnityEngine;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Area.Tests
{
    // 잠긴 문이 스위치가 켜진 뒤에만 열리는지 본다.
    // 라리스가 문에 닿아 Room 이동이 시작되는 부분은 Core·Room 씬이 필요해 플레이로 확인한다.
    public class LockedDoorTests
    {
        readonly List<Object> created = new();

        [SetUp]
        public void SetUp() => SwitchRecords.Clear();

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
        }

        Switch NewSwitch(string id)
        {
            var made = ScriptableObject.CreateInstance<Switch>();
            created.Add(made);
            Set(made, so => so.FindProperty("id").stringValue = id);
            return made;
        }

        LockedDoor NewDoor(Switch requiredSwitch)
        {
            var go = new GameObject("Room3Door");
            created.Add(go);
            go.AddComponent<BoxCollider2D>();
            var door = go.AddComponent<LockedDoor>();
            Set(door, so => so.FindProperty("requiredSwitch").objectReferenceValue = requiredSwitch);
            return door;
        }

        // CanEnter는 protected라 리플렉션으로 부름. 문 스크립트에 테스트용 통로를 내지 않으려고.
        static bool CanEnter(Door door) =>
            (bool)typeof(Door).GetMethod("CanEnter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(door, null);

        [Test]
        public void Locked_UntilSwitchTurnsOn()
        {
            var key = NewSwitch("Key");
            var door = NewDoor(key);

            Assert.IsFalse(CanEnter(door));

            SwitchRecords.TurnOn(key);
            Assert.IsTrue(CanEnter(door));
        }

        [Test]
        public void OtherSwitch_DoesNotOpen()
        {
            var door = NewDoor(NewSwitch("Key"));

            SwitchRecords.TurnOn(NewSwitch("Other"));

            Assert.IsFalse(CanEnter(door));
        }

        [Test]
        public void EmptySwitch_WarnsAndStaysLocked()
        {
            var door = NewDoor(null);
            LogAssert.Expect(LogType.Warning, new Regex("'requiredSwitch'가 비어"));

            Assert.IsFalse(CanEnter(door));
        }
    }
}
