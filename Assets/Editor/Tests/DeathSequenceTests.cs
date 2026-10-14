using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Player;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ProjectT.Death.Tests
{
    // 에디트 모드에서는 Awake·Start가 불리지 않고 씬을 불러올 수 없어, 진입점 메서드를 직접 부르고 리로드는 훅으로 바꾼다.
    public class DeathSequenceTests
    {
        const int MaxFrames = 300;

        InputLock inputLock;
        ScreenFader fader;
        int reloadCount;
        Action originalReload;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();
            reloadCount = 0;
            originalReload = DeathSequence.ReloadScene;
            DeathSequence.ReloadScene = () => reloadCount++;

            fader = NewFader();
            NewEntry(out inputLock);
        }

        [TearDown]
        public void TearDown()
        {
            DeathSequence.ResetState();
            DeathSequence.ReloadScene = originalReload;
        }

        static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static ScreenFader NewFader()
        {
            var go = new GameObject("ScreenFader");
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            var screenFader = go.AddComponent<ScreenFader>();
            Set(screenFader, so =>
            {
                so.FindProperty("image").objectReferenceValue = image;
                so.FindProperty("duration").floatValue = 0f;
            });
            screenFader.SetAlpha(0f);
            return screenFader;
        }

        static DeathSceneEntry NewEntry(out InputLock lockOwner)
        {
            lockOwner = new GameObject("Laris").AddComponent<InputLock>();
            var entry = new GameObject("DeathSceneEntry").AddComponent<DeathSceneEntry>();
            InputLock captured = lockOwner;
            Set(entry, so => so.FindProperty("inputLock").objectReferenceValue = captured);
            return entry;
        }

        IEnumerator WaitForReload()
        {
            for (int i = 0; i < MaxFrames && reloadCount == 0; i++)
                yield return null;
            Assert.AreEqual(1, reloadCount, "리로드까지 가지 못했다.");
        }

        static void ExpectNoDialogueWarning() =>
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));

        [UnityTest]
        public IEnumerator BeginTwiceSameFrame_ProcessesOnce()
        {
            int started = 0;
            Application.LogCallback count = (message, _, _) => { if (message.Contains("사망 시작")) started++; };
            Application.logMessageReceived += count;
            LogAssert.Expect(LogType.Log, new Regex(@"사망 시작: Death_A"));
            ExpectNoDialogueWarning();

            try
            {
                DeathSequence.Begin("Death_A");
                DeathSequence.Begin("Death_B");

                yield return WaitForReload();
                for (int i = 0; i < 10; i++)
                    yield return null;
            }
            finally
            {
                Application.logMessageReceived -= count;
            }
            Assert.AreEqual(1, reloadCount);
            Assert.AreEqual(1, started);
        }

        [UnityTest]
        public IEnumerator Begin_LocksInputAndIsActive_UntilEntryOfReloadedScene()
        {
            ExpectNoDialogueWarning();
            DeathSequence.Begin("Death_A");

            Assert.IsTrue(DeathSequence.IsActive);
            Assert.IsTrue(inputLock.IsLocked);

            yield return WaitForReload();
            Assert.IsTrue(DeathSequence.IsActive);
            Assert.IsTrue(DeathSequence.ReloadedAfterDeath);
            Assert.AreEqual(1f, fader.Alpha);

            // 리로드된 씬 흉내: 새 라리스·진입점을 만든다.
            var entry = NewEntry(out InputLock newLock);
            fader.SetAlpha(0f);
            entry.EnterScene();
            Assert.IsFalse(DeathSequence.ReloadedAfterDeath);
            Assert.IsTrue(newLock.IsLocked);

            IEnumerator run = entry.RunEntry();
            Assert.IsTrue(run.MoveNext());
            Assert.AreEqual(1f, fader.Alpha, "리로드 직후 화면이 검게 시작해야 한다.");
            Assert.IsTrue(newLock.IsLocked, "한 프레임 동안은 잠금을 유지해야 한다.");
            Assert.IsTrue(DeathSequence.IsActive);

            while (run.MoveNext()) { }
            Assert.IsFalse(newLock.IsLocked);
            Assert.IsFalse(DeathSequence.IsActive);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [UnityTest]
        public IEnumerator WithoutDialogue_FlowReachesReload_AndNextSceneUnlocks()
        {
            ExpectNoDialogueWarning();
            DeathSequence.Begin("Death_Missing");

            yield return WaitForReload();

            var entry = NewEntry(out InputLock newLock);
            entry.EnterScene();
            IEnumerator run = entry.RunEntry();
            while (run.MoveNext()) { }

            Assert.IsFalse(newLock.IsLocked);
            Assert.IsFalse(DeathSequence.IsActive);
        }

        [UnityTest]
        public IEnumerator ReloadThrows_ReleasesLockAndGuard()
        {
            DeathSequence.ReloadScene = () => throw new InvalidOperationException("reload failed");
            ExpectNoDialogueWarning();
            LogAssert.Expect(LogType.Exception, new Regex("reload failed"));

            DeathSequence.Begin("Death_A");
            for (int i = 0; i < MaxFrames && DeathSequence.IsActive; i++)
                yield return null;

            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(DeathSequence.ReloadedAfterDeath);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void Begin_WithoutEntry_LogsErrorAndStaysInactive()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LogAssert.Expect(LogType.Error, new Regex("DeathSceneEntry가 없어"));

            DeathSequence.Begin("Death_A");

            Assert.IsFalse(DeathSequence.IsActive);
        }

        static DeathSceneEntry NewEntryWithLaris(Vector3 start, out PlayerMovement laris)
        {
            var entry = NewEntry(out InputLock _);
            laris = new GameObject("LarisBody").AddComponent<PlayerMovement>();
            laris.transform.position = start;
            PlayerMovement captured = laris;
            Set(entry, so => so.FindProperty("laris").objectReferenceValue = captured);
            return entry;
        }

        [Test]
        public void RespawnPoint_TryGet_OnlyWhenFlagged()
        {
            var data = new SaveData { respawnPosition = new Vector2(3f, 4f) };
            Assert.IsFalse(RespawnPoint.TryGet(data, out _));
            Assert.IsFalse(RespawnPoint.TryGet(null, out _));

            data.hasRespawnPosition = true;
            Assert.IsTrue(RespawnPoint.TryGet(data, out Vector2 position));
            Assert.AreEqual(new Vector2(3f, 4f), position);
        }

        [Test]
        public void RestoreRespawn_WithSavedPosition_MovesLarisKeepingZ()
        {
            var entry = NewEntryWithLaris(new Vector3(0f, 0f, -1f), out PlayerMovement laris);
            var data = new SaveData { hasRespawnPosition = true, respawnPosition = new Vector2(6f, -2f) };
            LogAssert.Expect(LogType.Log, new Regex("리스폰 위치로 시작"));

            entry.RestoreRespawn(data);

            Assert.AreEqual(new Vector3(6f, -2f, -1f), laris.transform.position);
            Assert.AreEqual(new Vector2(6f, -2f), laris.GetComponent<Rigidbody2D>().position);
        }

        [Test]
        public void RestoreRespawn_WithoutSavedPosition_KeepsScenePosition()
        {
            var entry = NewEntryWithLaris(new Vector3(1f, 2f, 0f), out PlayerMovement laris);
            var data = new SaveData { respawnPosition = new Vector2(6f, -2f) };

            entry.RestoreRespawn(data);

            Assert.AreEqual(new Vector3(1f, 2f, 0f), laris.transform.position);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Entry_WithoutDeath_DoesNotLock()
        {
            var entry = NewEntry(out InputLock newLock);

            entry.EnterScene();
            IEnumerator run = entry.RunEntry();

            Assert.IsFalse(run.MoveNext());
            Assert.IsFalse(newLock.IsLocked);
            Assert.AreEqual(0f, fader.Alpha);
        }
    }
}
