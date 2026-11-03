using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Interaction;
using UnityEngine;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Events.Tests
{
    // 열쇠를 줍는 쪽(EventResponse)과 사라지는 쪽(EventToggle)이 이벤트로 이어지는지 본다.
    // EventToggle이 켜질 때 확인하는 OnEnable과 Changed 구독은 에디트 모드에서 돌지 않아 플레이로 확인한다.
    public class EventKeyTests
    {
        readonly List<Object> created = new();

        [SetUp]
        public void SetUp() => EventManager.Clear();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            EventManager.Clear();
        }

        EventFlag NewFlag(string id)
        {
            var flag = ScriptableObject.CreateInstance<EventFlag>();
            created.Add(flag);
            Set(flag, so => so.FindProperty("id").stringValue = id);
            return flag;
        }

        GameObject NewKey(EventFlag flag)
        {
            var go = new GameObject("Key");
            created.Add(go);
            var response = go.AddComponent<EventResponse>();
            Set(response, so => so.FindProperty("flag").objectReferenceValue = flag);
            return go;
        }

        EventToggle NewToggle(GameObject host, EventFlag flag, GameObject target = null)
        {
            var toggle = host.AddComponent<EventToggle>();
            Set(toggle, so =>
            {
                so.FindProperty("flag").objectReferenceValue = flag;
                so.FindProperty("target").objectReferenceValue = target;
            });
            return toggle;
        }

        void Interact(GameObject target) =>
            target.GetComponent<Interactable>().Interact(new InteractContext(null, new HashSet<string>()));

        [Test]
        public void Interact_SetsEvent()
        {
            var flag = NewFlag("Key");

            Interact(NewKey(flag));

            Assert.IsTrue(EventManager.Has(flag));
        }

        [Test]
        public void EmptyFlag_WarnsWithoutSetting()
        {
            LogAssert.Expect(LogType.Warning, new Regex("'flag'가 비어"));

            Interact(NewKey(null));

            Assert.IsFalse(EventManager.Has(NewFlag("Key")));
        }

        [Test]
        public void Toggle_TurnsOffTarget_WhenEventHappened()
        {
            var flag = NewFlag("Key");
            var key = NewKey(flag);
            var toggle = NewToggle(key, flag);

            toggle.Refresh();
            Assert.IsTrue(key.activeSelf);

            EventManager.Set(flag);
            toggle.Refresh();
            Assert.IsFalse(key.activeSelf);
        }

        [Test]
        public void Toggle_TurnsOffOtherTarget()
        {
            var flag = NewFlag("Key");
            var host = new GameObject("Host");
            var other = new GameObject("Other");
            created.Add(host);
            created.Add(other);
            var toggle = NewToggle(host, flag, other);

            EventManager.Set(flag);
            toggle.Refresh();

            Assert.IsTrue(host.activeSelf);
            Assert.IsFalse(other.activeSelf);
        }

        [Test]
        public void Toggle_IgnoresOtherEvent()
        {
            var flag = NewFlag("Key");
            var key = NewKey(flag);
            var toggle = NewToggle(key, flag);

            EventManager.Set(NewFlag("Other"));
            toggle.OnChanged(NewFlag("Other"));

            Assert.IsTrue(key.activeSelf);
        }

        [Test]
        public void Toggle_OnChanged_TurnsOffWhenItsFlagHappened()
        {
            var flag = NewFlag("Key");
            var key = NewKey(flag);
            var toggle = NewToggle(key, flag);

            EventManager.Set(flag);
            toggle.OnChanged(flag);

            Assert.IsFalse(key.activeSelf);
        }

        [Test]
        public void Toggle_EmptyFlag_WarnsAndKeepsTarget()
        {
            var key = NewKey(NewFlag("Key"));
            var toggle = NewToggle(key, null);
            LogAssert.Expect(LogType.Warning, new Regex("'flag'가 비어"));

            toggle.Refresh();

            Assert.IsTrue(key.activeSelf);
        }
    }
}
