using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Interaction;
using UnityEngine;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Switches.Tests
{
    // 열쇠를 줍는 쪽(SwitchResponse)과 사라지는 쪽(SwitchToggle)이 스위치로 이어지는지 본다.
    // SwitchToggle이 켜질 때 확인하는 OnEnable과 Changed 구독은 에디트 모드에서 돌지 않아 플레이로 확인한다.
    public class SwitchKeyTests
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

        GameObject NewKey(Switch targetSwitch)
        {
            var go = new GameObject("Key");
            created.Add(go);
            var response = go.AddComponent<SwitchResponse>();
            Set(response, so => so.FindProperty("targetSwitch").objectReferenceValue = targetSwitch);
            return go;
        }

        SwitchToggle NewToggle(GameObject host, Switch watchedSwitch, GameObject target = null)
        {
            var toggle = host.AddComponent<SwitchToggle>();
            Set(toggle, so =>
            {
                so.FindProperty("watchedSwitch").objectReferenceValue = watchedSwitch;
                so.FindProperty("target").objectReferenceValue = target;
            });
            return toggle;
        }

        void Interact(GameObject target) =>
            target.GetComponent<Interactable>().Interact(new InteractContext(null, new HashSet<string>()));

        [Test]
        public void Interact_TurnsSwitchOn()
        {
            var key = NewSwitch("Key");

            Interact(NewKey(key));

            Assert.IsTrue(SwitchRecords.IsOn(key));
        }

        [Test]
        public void EmptySwitch_WarnsWithoutTurningOn()
        {
            LogAssert.Expect(LogType.Warning, new Regex("'targetSwitch'가 비어"));

            Interact(NewKey(null));

            Assert.IsFalse(SwitchRecords.IsOn(NewSwitch("Key")));
        }

        [Test]
        public void Toggle_TurnsOffTarget_WhenSwitchIsOn()
        {
            var key = NewSwitch("Key");
            var item = NewKey(key);
            var toggle = NewToggle(item, key);

            toggle.Refresh();
            Assert.IsTrue(item.activeSelf);

            SwitchRecords.TurnOn(key);
            toggle.Refresh();
            Assert.IsFalse(item.activeSelf);
        }

        [Test]
        public void Toggle_TurnsOffOtherTarget()
        {
            var key = NewSwitch("Key");
            var host = new GameObject("Host");
            var other = new GameObject("Other");
            created.Add(host);
            created.Add(other);
            var toggle = NewToggle(host, key, other);

            SwitchRecords.TurnOn(key);
            toggle.Refresh();

            Assert.IsTrue(host.activeSelf);
            Assert.IsFalse(other.activeSelf);
        }

        [Test]
        public void Toggle_IgnoresOtherSwitch()
        {
            var key = NewSwitch("Key");
            var item = NewKey(key);
            var toggle = NewToggle(item, key);

            SwitchRecords.TurnOn(NewSwitch("Other"));
            toggle.OnChanged(NewSwitch("Other"));

            Assert.IsTrue(item.activeSelf);
        }

        [Test]
        public void Toggle_OnChanged_TurnsOffWhenItsSwitchTurnedOn()
        {
            var key = NewSwitch("Key");
            var item = NewKey(key);
            var toggle = NewToggle(item, key);

            SwitchRecords.TurnOn(key);
            toggle.OnChanged(key);

            Assert.IsFalse(item.activeSelf);
        }

        [Test]
        public void Toggle_EmptySwitch_WarnsAndKeepsTarget()
        {
            var item = NewKey(NewSwitch("Key"));
            var toggle = NewToggle(item, null);
            LogAssert.Expect(LogType.Warning, new Regex("'watchedSwitch'가 비어"));

            toggle.Refresh();

            Assert.IsTrue(item.activeSelf);
        }
    }
}
