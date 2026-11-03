using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Events.Tests
{
    // 이벤트를 남기면 읽을 수 있고, 새로 일어났을 때만 알리는지 본다.
    public class EventManagerTests
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

        [Test]
        public void Set_MakesHasTrue()
        {
            var key = NewFlag("Key");

            Assert.IsFalse(EventManager.Has(key));
            Assert.IsTrue(EventManager.Set(key));
            Assert.IsTrue(EventManager.Has(key));
        }

        [Test]
        public void OtherFlag_IsNotSet()
        {
            EventManager.Set(NewFlag("Key"));

            Assert.IsFalse(EventManager.Has(NewFlag("Other")));
        }

        [Test]
        public void SetTwice_NotifiesOnce()
        {
            var key = NewFlag("Key");
            var notified = new List<EventFlag>();
            void OnChanged(EventFlag flag) => notified.Add(flag);

            EventManager.Changed += OnChanged;
            try
            {
                Assert.IsTrue(EventManager.Set(key));
                Assert.IsFalse(EventManager.Set(key));
            }
            finally
            {
                EventManager.Changed -= OnChanged;
            }

            CollectionAssert.AreEqual(new[] { key }, notified);
        }

        [Test]
        public void NullOrEmptyId_IsIgnored()
        {
            Assert.IsFalse(EventManager.Set(null));
            Assert.IsFalse(EventManager.Set(NewFlag("")));
            Assert.IsFalse(EventManager.Has(null));
            Assert.IsFalse(EventManager.Has(NewFlag("")));
        }
    }
}
