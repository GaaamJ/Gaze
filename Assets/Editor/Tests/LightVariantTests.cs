using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Lighting.Tests
{
    // 밝기에 따라 lit·dark 중 하나만 켜지는지 본다.
    // Start와 RoomLight 등록은 에디트 모드에서 돌지 않아 Apply·OnSceneLit을 직접 부른다.
    public class LightVariantTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        LightVariant NewVariant(GameObject lit, GameObject dark)
        {
            var variant = NewObject("Variant").AddComponent<LightVariant>();
            Set(variant, so =>
            {
                so.FindProperty("lit").objectReferenceValue = lit;
                so.FindProperty("dark").objectReferenceValue = dark;
            });
            return variant;
        }

        [Test]
        public void Dark_TurnsOnlyDarkOn()
        {
            var lit = NewObject("Lit");
            var dark = NewObject("Dark");

            NewVariant(lit, dark).Apply(false);

            Assert.IsFalse(lit.activeSelf);
            Assert.IsTrue(dark.activeSelf);
        }

        [Test]
        public void Lit_TurnsOnlyLitOn()
        {
            var lit = NewObject("Lit");
            var dark = NewObject("Dark");

            NewVariant(lit, dark).Apply(true);

            Assert.IsTrue(lit.activeSelf);
            Assert.IsFalse(dark.activeSelf);
        }

        [Test]
        public void OnlyDarkAssigned_LitRoomTurnsItOff()
        {
            var dark = NewObject("Dark");

            NewVariant(null, dark).Apply(true);

            Assert.IsFalse(dark.activeSelf);
        }

        [Test]
        public void SceneLit_OfOtherScene_IsIgnored()
        {
            var lit = NewObject("Lit");
            var dark = NewObject("Dark");
            lit.SetActive(false);
            var variant = NewVariant(lit, dark);

            variant.OnSceneLit(default(Scene));

            Assert.IsFalse(lit.activeSelf);
            Assert.IsTrue(dark.activeSelf);
        }

        [Test]
        public void SceneLit_OfOwnScene_SwitchesToLit()
        {
            var lit = NewObject("Lit");
            var dark = NewObject("Dark");
            lit.SetActive(false);
            var variant = NewVariant(lit, dark);

            variant.OnSceneLit(variant.gameObject.scene);

            Assert.IsTrue(lit.activeSelf);
            Assert.IsFalse(dark.activeSelf);
        }

        [Test]
        public void BothEmpty_WarnsWithoutChanging()
        {
            LogAssert.Expect(LogType.Warning, new Regex("모두 비어"));

            NewVariant(null, null).Apply(true);
        }
    }
}
