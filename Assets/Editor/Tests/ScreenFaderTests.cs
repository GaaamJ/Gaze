using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Gaze.UI.Tests
{
    public class ScreenFaderTests
    {
        GameObject go;
        Image image;
        ScreenFader fader;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("ScreenFader");
            go.AddComponent<RectTransform>();
            image = go.AddComponent<Image>();
            image.color = Color.black;
            fader = go.AddComponent<ScreenFader>();
            var so = new SerializedObject(fader);
            so.FindProperty("image").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void Ends_AreFullyClearAndFullyBlack()
        {
            fader.SetAlpha(0f);
            Assert.AreEqual(0f, image.color.a);
            Assert.IsFalse(image.enabled);

            fader.SetAlpha(1f);
            Assert.AreEqual(1f, image.color.a, 1e-6f);
            Assert.IsTrue(image.enabled);
            Assert.AreEqual(1f, fader.Alpha);
        }

        [Test]
        public void MidFade_DarkensScreenBrightnessByAlpha()
        {
            fader.SetAlpha(0.75f);
            Assert.AreEqual(0.75f, fader.Alpha);

            // 흰색 UI가 화면에 보이는 밝기(감마 공간)가 1 - 0.75가 되어야 한다.
            float white = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? Mathf.LinearToGammaSpace(1f - image.color.a)
                : 1f - image.color.a;
            Assert.AreEqual(0.25f, white, 1e-3f);
        }
    }
}
