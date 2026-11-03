using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Interaction;
using Gaze.Save;
using Gaze.Tests;
using UnityEngine;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Lighting.Tests
{
    // 등불을 켜면 세이브에 남는지 본다.
    // RoomLight가 밝아지는 건 OnEnable·코루틴이라 에디트 모드에서 돌지 않아 플레이로 확인한다.
    public class LampTests
    {
        readonly List<Object> created = new();
        TempSaveSandbox save;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
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
            save.Dispose();
        }

        Lamp NewLamp(string id)
        {
            var lamp = ScriptableObject.CreateInstance<Lamp>();
            created.Add(lamp);
            Set(lamp, so => so.FindProperty("id").stringValue = id);
            return lamp;
        }

        LampResponse NewLampResponse(Lamp lamp)
        {
            var go = new GameObject("Lamp");
            created.Add(go);
            var response = go.AddComponent<LampResponse>();
            Set(response, so => so.FindProperty("lamp").objectReferenceValue = lamp);
            return response;
        }

        void Interact(Component target) =>
            target.GetComponent<Interactable>().Interact(new InteractContext(null, new HashSet<string>()));

        [Test]
        public void Interact_SavesLampId_AndLoadKeepsIt()
        {
            var lamp = NewLamp("Kitchen");

            Interact(NewLampResponse(lamp));

            Assert.IsTrue(LitLamps.Contains("Kitchen"));
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "Kitchen" }, SaveManager.Data.litLampIds);
        }

        [Test]
        public void InteractTwice_RecordsOnce()
        {
            var target = NewLampResponse(NewLamp("Kitchen"));

            Interact(target);
            Interact(target);

            CollectionAssert.AreEqual(new[] { "Kitchen" }, SaveManager.Data.litLampIds);
        }

        [Test]
        public void OtherLamp_IsNotLit()
        {
            Interact(NewLampResponse(NewLamp("Kitchen")));

            Assert.IsFalse(LitLamps.Contains("Cellar"));
            Assert.IsFalse(LitLamps.Contains(""));
        }

        [Test]
        public void EmptyLamp_WarnsWithoutSaving()
        {
            LogAssert.Expect(LogType.Warning, new Regex("'lamp'가 비어"));

            Interact(NewLampResponse(null));

            CollectionAssert.IsEmpty(SaveManager.Data.litLampIds);
        }
    }
}
