using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Dialogue;
using ProjectT.Player;
using ProjectT.Save;
using ProjectT.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace ProjectT.Death.Tests
{
    // 에디트 모드에서는 YarnTask.Yield(Awaitable.NextFrameAsync)가 플레이어 루프 없이 재개되지 않아, 테스트가 직접 프레임을 넘긴다.
    public static class TestFrames
    {
        static readonly List<AwaitableCompletionSource> pending = new List<AwaitableCompletionSource>();

        public static async YarnTask Next()
        {
            var source = new AwaitableCompletionSource();
            pending.Add(source);
            await source.Awaitable;
        }

        public static void Advance()
        {
            var due = pending.ToArray();
            pending.Clear();
            foreach (var source in due)
                source.SetResult();
        }

        public static void Clear() => pending.Clear();
    }

    public class RecordingReaction : MonoBehaviour, IRefusalReaction
    {
        public static List<string> Calls;
        public static InputLock WatchedLock;

        public string label;
        public int frames;
        public bool throws;

        public async YarnTask Run(PlayerMovement laris)
        {
            Calls.Add($"{label}:start:{(WatchedLock != null && WatchedLock.IsLocked ? "locked" : "unlocked")}");
            if (throws)
                throw new InvalidOperationException("reaction failed");
            for (int i = 0; i < frames; i++)
                await TestFrames.Next();
            Calls.Add($"{label}:end");
        }
    }

    // 에디트 모드에서는 DialogueRunner가 돌지 않아 대사 재생은 훅으로 바꾼다.
    public abstract class DeathRefusalFixture
    {
        const int MaxFrames = 300;

        protected InputLock inputLock;
        protected List<string> calls;
        protected DialogueResult dialogueResult;
        protected Func<DialogueService, string, YarnTask<DialogueResult>> originalPlayDialogue;
        string tempBase;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            SaveManager.Paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Load();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();
            DeathRefusal.ResetState();

            calls = new List<string>();
            dialogueResult = DialogueResult.Completed;
            originalPlayDialogue = DeathRefusal.PlayDialogue;
            DeathRefusal.PlayDialogue = FakeDialogue;

            inputLock = new GameObject("Laris").AddComponent<InputLock>();
            var entry = new GameObject("DeathSceneEntry").AddComponent<DeathSceneEntry>();
            Set(entry, so => so.FindProperty("inputLock").objectReferenceValue = inputLock);

            RecordingReaction.Calls = calls;
            RecordingReaction.WatchedLock = inputLock;
        }

        [TearDown]
        public void TearDown()
        {
            DeathRefusal.PlayDialogue = originalPlayDialogue;
            DeathRefusal.ResetState();
            DeathSequence.ResetState();
            TestFrames.Clear();
            RecordingReaction.Calls = null;
            RecordingReaction.WatchedLock = null;
            SaveManager.Paths = null;
            SaveManager.Load();
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        async YarnTask<DialogueResult> FakeDialogue(DialogueService dialogue, string nodeId)
        {
            calls.Add($"dialogue:{nodeId}:{(inputLock.IsLocked ? "locked" : "unlocked")}");
            await TestFrames.Next();
            return dialogueResult;
        }

        protected static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        protected static RecordingReaction AddReaction(GameObject go, string label, int frames, bool throws = false)
        {
            var reaction = go.AddComponent<RecordingReaction>();
            reaction.label = label;
            reaction.frames = frames;
            reaction.throws = throws;
            return reaction;
        }

        protected static IEnumerator WaitUntilDone()
        {
            for (int i = 0; i < MaxFrames && DeathRefusal.IsActive; i++)
            {
                TestFrames.Advance();
                yield return null;
            }
            Assert.IsFalse(DeathRefusal.IsActive, "거부가 끝나지 않았다.");
        }
    }

    public class DeathRefusalTests : DeathRefusalFixture
    {
        [UnityTest]
        public IEnumerator Play_RunsReactionsInOrderAfterDialogue_ThenUnlocks()
        {
            var source = new GameObject("Hole");
            AddReaction(source, "A", 2);
            AddReaction(source, "B", 0);

            Assert.IsTrue(DeathRefusal.Play(source, "Death_Falling001_Refuse"));
            Assert.IsTrue(DeathRefusal.IsActive);
            Assert.IsTrue(inputLock.IsLocked);

            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[]
            {
                "dialogue:Death_Falling001_Refuse:locked",
                "A:start:locked", "A:end",
                "B:start:locked", "B:end",
            }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_WithoutReactions_PlaysDialogueOnly()
        {
            var source = new GameObject("Corpse");

            Assert.IsTrue(DeathRefusal.Play(source, "Death_DeathObject_Refuse"));
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "dialogue:Death_DeathObject_Refuse:locked" }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_WhileRefusing_IsIgnored()
        {
            var source = new GameObject("Hole");
            AddReaction(source, "A", 3);

            Assert.IsTrue(DeathRefusal.Play(source, "Node_A"));
            Assert.IsFalse(DeathRefusal.Play(source, "Node_B"));
            TestFrames.Advance();
            yield return null;
            Assert.IsFalse(DeathRefusal.Play(source, "Node_C"));

            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "dialogue:Node_A:locked", "A:start:locked", "A:end" }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void Play_WhileInputLocked_DoesNotStart()
        {
            inputLock.Lock(InputLockReason.Dialogue);

            Assert.IsFalse(DeathRefusal.Play(new GameObject("Hole"), "Node_A"));

            Assert.IsFalse(DeathRefusal.IsActive);
            CollectionAssert.IsEmpty(calls);
            inputLock.Unlock(InputLockReason.Dialogue);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_DialogueRejected_StillRunsReactionsAndUnlocks()
        {
            dialogueResult = DialogueResult.Rejected;
            var source = new GameObject("Hole");
            AddReaction(source, "A", 1);
            LogAssert.Expect(LogType.Warning, new Regex("'Missing_Refuse'를 재생하지 못했다"));

            Assert.IsTrue(DeathRefusal.Play(source, "Missing_Refuse"));
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "dialogue:Missing_Refuse:locked", "A:start:locked", "A:end" }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_WithoutDialogueService_Unlocks()
        {
            DeathRefusal.PlayDialogue = originalPlayDialogue;
            var source = new GameObject("Hole");
            AddReaction(source, "A", 0);
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            LogAssert.Expect(LogType.Warning, new Regex("재생하지 못했다"));

            Assert.IsTrue(DeathRefusal.Play(source, "Death_Falling001_Refuse"));
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "A:start:locked", "A:end" }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_ReactionThrows_Unlocks()
        {
            var source = new GameObject("Hole");
            AddReaction(source, "A", 0, throws: true);
            AddReaction(source, "B", 0);
            LogAssert.Expect(LogType.Exception, new Regex("reaction failed"));

            Assert.IsTrue(DeathRefusal.Play(source, "Node_A"));
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "dialogue:Node_A:locked", "A:start:locked" }, calls);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator Play_SkipsDisabledReaction()
        {
            var source = new GameObject("Hole");
            AddReaction(source, "A", 0).enabled = false;
            AddReaction(source, "B", 0);

            Assert.IsTrue(DeathRefusal.Play(source, "Node_A"));
            yield return WaitUntilDone();

            CollectionAssert.AreEqual(new[] { "dialogue:Node_A:locked", "B:start:locked", "B:end" }, calls);
        }

        [Test]
        public void Play_WithoutEntry_LogsErrorAndStaysInactive()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LogAssert.Expect(LogType.Error, new Regex("DeathSceneEntry가 없어"));

            Assert.IsFalse(DeathRefusal.Play(new GameObject("Hole"), "Node_A"));
            Assert.IsFalse(DeathRefusal.IsActive);
        }

        [Test]
        public void PushBack_MovesAwayFromCenterByDistance()
        {
            Vector2 destination = PushBackReaction.Destination(new Vector2(2f, 1f), new Vector2(2f, 3f), Vector2Int.down, 1f);
            Assert.That(Vector2.Distance(new Vector2(2f, 4f), destination), Is.LessThan(1e-5f));

            destination = PushBackReaction.Destination(Vector2.zero, new Vector2(3f, 4f), Vector2Int.down, 1.5f);
            Assert.That(Vector2.Distance(new Vector2(3.9f, 5.2f), destination), Is.LessThan(1e-5f));
        }

        [Test]
        public void PushBack_Overlapping_FallsBackOppositeToFacing()
        {
            Vector2 at = new Vector2(5f, 5f);

            Assert.AreEqual(Vector2.up, PushBackReaction.AwayDirection(at, at, Vector2Int.down));
            Assert.AreEqual(Vector2.left, PushBackReaction.AwayDirection(at, at, Vector2Int.right));
            Vector2 diagonal = PushBackReaction.AwayDirection(at, at, new Vector2Int(1, 1));
            Assert.That(Vector2.Distance(new Vector2(-1f, -1f).normalized, diagonal), Is.LessThan(1e-5f));
        }
    }
}
