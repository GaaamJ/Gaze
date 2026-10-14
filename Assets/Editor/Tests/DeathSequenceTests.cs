using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Data;
using ProjectT.Session;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Death.Tests
{
    public class DeathSequenceTests
    {
        DeathData data;
        InputLock inputLock;
        int startedCount;
        DeathData startedWith;

        // DeathSequence는 씬의 InputLock·DialogueService를 찾으므로 열린 씬의 오브젝트와 섞이지 않게 빈 씬에서 돌린다.
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            data = ScriptableObject.CreateInstance<DeathData>();
            inputLock = new GameObject("Laris").AddComponent<InputLock>();
            startedCount = 0;
            startedWith = null;
            DeathSequence.Started += OnStarted;
        }

        [TearDown]
        public void TearDown()
        {
            DeathSequence.Started -= OnStarted;
            DeathSequence.End();
            Object.DestroyImmediate(data);
        }

        void OnStarted(DeathData started)
        {
            startedCount++;
            startedWith = started;
        }

        // 에디트 모드에서 AddComponent는 Awake를 부르지 않으므로 직접 부른다.
        internal static void CallAwake(InputLock target) =>
            typeof(InputLock).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [Test]
        public void Begin_LocksShowsScreenAndRaisesStarted_EndReleases()
        {
            DeathSequence.Begin(data);
            Assert.IsTrue(DeathSequence.IsActive);
            Assert.IsTrue(inputLock.IsLocked);
            Assert.IsNotNull(DeathSequence.BlackScreen);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, DeathSequence.BlackScreen.GetComponent<Canvas>().renderMode);
            Assert.AreEqual(Color.black, DeathSequence.BlackScreen.GetComponent<UnityEngine.UI.Image>().color);
            Assert.AreEqual(1, startedCount);
            Assert.AreSame(data, startedWith);

            GameObject screen = DeathSequence.BlackScreen;
            DeathSequence.End();
            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsTrue(screen == null);
            Assert.IsNull(DeathSequence.BlackScreen);
        }

        [Test]
        public void SecondBegin_IsIgnored()
        {
            DeathSequence.Begin(data);
            GameObject screen = DeathSequence.BlackScreen;
            var other = ScriptableObject.CreateInstance<DeathData>();
            DeathSequence.Begin(other);
            Object.DestroyImmediate(other);

            Assert.AreEqual(1, startedCount);
            Assert.AreSame(data, startedWith);
            Assert.AreSame(screen, DeathSequence.BlackScreen);

            DeathSequence.End();
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void NewInputLock_WhileDead_StartsLocked_AndEndReleasesIt()
        {
            DeathSequence.Begin(data);
            var reloaded = new GameObject("ReloadedLaris").AddComponent<InputLock>();
            CallAwake(reloaded);
            Assert.IsTrue(reloaded.IsLocked);

            DeathSequence.End();
            Assert.IsFalse(reloaded.IsLocked);
            Assert.IsFalse(inputLock.IsLocked);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NewInputLock_WhenNotDead_StartsUnlocked()
        {
            var fresh = new GameObject("FreshLaris").AddComponent<InputLock>();
            CallAwake(fresh);
            Assert.IsFalse(fresh.IsLocked);
        }

        [Test]
        public void End_WhenNotDead_DoesNothing()
        {
            DeathSequence.End();
            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(inputLock.IsLocked);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Begin_WithoutData_IsRejected()
        {
            LogAssert.Expect(LogType.Error, new Regex("DeathData가 비어"));
            DeathSequence.Begin(null);
            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual(0, startedCount);
        }

        [Test]
        public void OtherLockReasons_StayAfterEnd()
        {
            inputLock.Lock(InputLockReason.Dialogue);
            DeathSequence.Begin(data);
            DeathSequence.End();
            Assert.IsTrue(inputLock.IsLocked);
            inputLock.Unlock(InputLockReason.Dialogue);
            Assert.IsFalse(inputLock.IsLocked);
        }
    }
}
