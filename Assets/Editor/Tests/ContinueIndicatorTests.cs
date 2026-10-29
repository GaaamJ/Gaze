using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

namespace Gaze.Dialogue.Tests
{
    public class ContinueIndicatorTests
    {
        GameObject go;
        GameObject textGo;
        Image image;
        ContinueIndicator indicator;
        InstantTypewriter typewriter;
        MarkupParseResult line;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("ContinueIndicatorTests", typeof(RectTransform));
            image = go.AddComponent<Image>();
            indicator = go.AddComponent<ContinueIndicator>();
            var so = new SerializedObject(indicator);
            so.FindProperty("graphic").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();

            textGo = new GameObject("Text", typeof(RectTransform));
            typewriter = new InstantTypewriter { TextElement = textGo.AddComponent<TextMeshProUGUI>() };
            typewriter.ActionMarkupHandlers.Add(indicator);
            line = new MarkupParseResult("대사", new List<MarkupAttribute>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo);
        }

        [Test]
        public void HiddenWhilePreparingLine()
        {
            image.enabled = true;
            typewriter.PrepareForContent(line);
            Assert.IsFalse(image.enabled);
            Assert.IsFalse(indicator.Waiting);
        }

        [Test]
        public void ShownWhenLineDisplayComplete()
        {
            typewriter.PrepareForContent(line);
            indicator.OnLineDisplayBegin(line, typewriter.TextElement);
            indicator.OnLineDisplayComplete();
            Assert.IsTrue(image.enabled);
            Assert.IsTrue(indicator.Waiting);
        }

        [Test]
        public void HiddenWhenLineDismissed()
        {
            typewriter.PrepareForContent(line);
            indicator.OnLineDisplayBegin(line, typewriter.TextElement);
            indicator.OnLineDisplayComplete();
            Assert.IsTrue(image.enabled);
            typewriter.ContentWillDismiss();
            Assert.IsFalse(image.enabled);
            Assert.IsFalse(indicator.Waiting);
        }

        [Test]
        public void HiddenAgainWhenNextLineBegins()
        {
            indicator.OnLineDisplayComplete();
            Assert.IsTrue(image.enabled);
            indicator.OnLineDisplayBegin(line, typewriter.TextElement);
            Assert.IsFalse(image.enabled);
            Assert.IsFalse(indicator.Waiting);
        }
    }
}
