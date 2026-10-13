using System.Threading;
using NUnit.Framework;
using ProjectT.Session;
using UnityEditor;
using UnityEngine;
using Yarn.Unity;

namespace ProjectT.Dialogue.Tests
{
    public class OptionInputTests
    {
        GameObject go;
        DialogueService service;
        OptionInputPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("OptionInputTests");
            service = go.AddComponent<DialogueService>();
            presenter = go.AddComponent<OptionInputPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("dialogueService").objectReferenceValue = service;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        static DialogueOption[] Options(bool available) =>
            new[] { new DialogueOption { IsAvailable = available }, new DialogueOption { IsAvailable = available } };

        static LineCancellationToken Token(CancellationTokenSource source) =>
            new LineCancellationToken { NextContentToken = source.Token, HurryUpToken = source.Token };

        [Test]
        public void RequestCancel_WhenNotPlaying_DoesNothing()
        {
            Assert.IsFalse(service.RequestCancel());
        }

        [Test]
        public void Cancel_WithoutOptions_DoesNothing()
        {
            Assert.IsFalse(presenter.OptionsShowing);
            Assert.IsFalse(presenter.TryCancelDialogue());
        }

        [Test]
        public void OptionsShowing_FollowsOptionsToken()
        {
            using var source = new CancellationTokenSource();
            presenter.RunOptionsAsync(Options(true), Token(source));
            Assert.IsTrue(presenter.OptionsShowing);

            source.Cancel();
            Assert.IsFalse(presenter.OptionsShowing);
            Assert.IsFalse(presenter.TryCancelDialogue());
        }

        [Test]
        public void Cancel_WhileOptionsShowingButNoDialogue_DoesNotThrow()
        {
            using var source = new CancellationTokenSource();
            presenter.RunOptionsAsync(Options(true), Token(source));

            Assert.DoesNotThrow(() => Assert.IsFalse(presenter.TryCancelDialogue()));
            source.Cancel();
        }

        [Test]
        public void NoAvailableOptions_DoesNotWait()
        {
            using var source = new CancellationTokenSource();
            presenter.RunOptionsAsync(Options(false), Token(source));
            Assert.IsFalse(presenter.OptionsShowing);
        }

        [Test]
        public void LinkUpDown_FollowsShownOrderAndSkipsHidden()
        {
            var root = new GameObject("Root").transform;
            root.SetParent(go.transform);
            var items = new OptionItem[4];
            for (int i = 0; i < items.Length; i++)
            {
                var child = new GameObject("Item" + i);
                child.transform.SetParent(root);
                items[i] = child.AddComponent<OptionItem>();
            }
            items[3].gameObject.SetActive(false);

            presenter.LinkUpDown(root);

            Assert.AreEqual(UnityEngine.UI.Navigation.Mode.Explicit, items[0].navigation.mode);
            Assert.AreSame(items[1], items[0].navigation.selectOnDown);
            Assert.AreSame(items[2], items[1].navigation.selectOnDown);
            Assert.AreSame(items[0], items[1].navigation.selectOnUp);
            Assert.AreSame(items[0], items[2].navigation.selectOnDown);
            Assert.AreSame(items[2], items[0].navigation.selectOnUp);
            Assert.IsNull(items[0].navigation.selectOnLeft);
            Assert.IsNull(items[0].navigation.selectOnRight);
        }

        [Test]
        public void InputLock_DialogueReason_IsReleasedAfterUnlock()
        {
            var inputLock = go.AddComponent<InputLock>();
            inputLock.Lock(InputLockReason.Dialogue);
            Assert.IsTrue(inputLock.IsLocked);
            inputLock.Unlock(InputLockReason.Dialogue);
            Assert.IsFalse(inputLock.IsLocked);
        }
    }
}
