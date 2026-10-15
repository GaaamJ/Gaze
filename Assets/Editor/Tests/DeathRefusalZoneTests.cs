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
    // 에디트 모드에서는 DialogueRunner가 돌지 않아 대사 재생은 훅으로 바꾸고, 테스트가 직접 끝낸다.
    public class DeathRefusalZoneTests
    {
        const string Node = "Death_Falling001_Refuse";

        string tempBase;
        Func<string, YarnTask<DialogueResult>> originalPlayDialogue;
        List<string> played;
        DialogueResult result;
        AwaitableCompletionSource pending;
        InputLock inputLock;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            SaveManager.Paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            GameSession.Begin();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();

            played = new List<string>();
            result = DialogueResult.Completed;
            pending = null;
            originalPlayDialogue = DeathRefusalZone.PlayDialogue;
            DeathRefusalZone.PlayDialogue = FakeDialogue;
            inputLock = new GameObject("InputLock").AddComponent<InputLock>();
        }

        [TearDown]
        public void TearDown()
        {
            DeathRefusalZone.PlayDialogue = originalPlayDialogue;
            DeathSequence.ResetState();
            SaveManager.Paths = null;
            GameSession.Begin();
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        // DialogueService.Play처럼 재생하는 동안만 Dialogue 잠금을 건다.
        async YarnTask<DialogueResult> FakeDialogue(string nodeId)
        {
            played.Add(nodeId);
            if (result == DialogueResult.Rejected)
                return DialogueResult.Rejected;

            inputLock.Lock(InputLockReason.Dialogue);
            try
            {
                pending = new AwaitableCompletionSource();
                await pending.Awaitable;
            }
            finally
            {
                inputLock.Unlock(InputLockReason.Dialogue);
            }
            return result;
        }

        IEnumerator FinishDialogue()
        {
            Assert.IsNotNull(pending, "재생 중인 대사가 없다.");
            var source = pending;
            pending = null;
            source.SetResult();
            for (int i = 0; i < 3; i++)
                yield return null;
        }

        static DeathRefusalZone NewZone(string nodeId)
        {
            var go = new GameObject("Hole");
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = go.AddComponent<DeathRefusalZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("refusalNodeId").stringValue = nodeId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        static Collider2D NewLaris(out Collider2D detectTrigger)
        {
            var go = new GameObject("LarisBody");
            go.AddComponent<PlayerMovement>();
            var body = go.AddComponent<BoxCollider2D>();
            detectTrigger = go.AddComponent<CircleCollider2D>();
            detectTrigger.isTrigger = true;
            return body;
        }

        static bool SavedOnDisk(string nodeId)
        {
            return File.Exists(SaveManager.SavePath) && File.ReadAllText(SaveManager.SavePath).Contains($"\"{nodeId}\"");
        }

        [UnityTest]
        public IEnumerator FirstEntry_PlaysOnce_RecordsAndSavesWhenCompleted()
        {
            var zone = NewZone(Node);
            var body = NewLaris(out _);

            zone.HandleEntered(body);
            Assert.IsTrue(inputLock.IsLocked, "대사가 재생되는 동안 입력이 잠긴다.");
            zone.HandleEntered(body);
            CollectionAssert.AreEqual(new[] { Node }, played, "재생 중에는 새로 시작하지 않는다.");
            Assert.IsFalse(GameSession.Investigated.Contains(Node), "끝나기 전에는 기록하지 않는다.");

            yield return FinishDialogue();

            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsTrue(GameSession.Investigated.Contains(Node));
            Assert.IsTrue(SavedOnDisk(Node), "기록이 세이브 파일에 저장돼야 한다.");

            zone.HandleEntered(body);
            CollectionAssert.AreEqual(new[] { Node }, played, "기록이 있으면 다시 들어와도 재생하지 않는다.");
        }

        [Test]
        public void AlreadyRecorded_DoesNotPlay()
        {
            GameSession.Investigated.Add(Node);
            var zone = NewZone(Node);
            var body = NewLaris(out _);

            zone.HandleEntered(body);

            CollectionAssert.IsEmpty(played);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [UnityTest]
        public IEnumerator SavedRecord_SurvivesRestart()
        {
            var zone = NewZone(Node);
            var body = NewLaris(out _);
            zone.HandleEntered(body);
            yield return FinishDialogue();

            // 게임을 다시 켠 것처럼 세이브에서 세션 기록을 다시 채운다.
            GameSession.Begin();
            var reloaded = NewZone(Node);
            reloaded.HandleEntered(body);

            Assert.IsTrue(GameSession.Investigated.Contains(Node));
            CollectionAssert.AreEqual(new[] { Node }, played);
        }

        [Test]
        public void IgnoresDetectTriggerAndOthers()
        {
            var zone = NewZone(Node);
            NewLaris(out var detectTrigger);
            var other = new GameObject("Box").AddComponent<BoxCollider2D>();

            zone.HandleEntered(detectTrigger);
            zone.HandleEntered(other);

            CollectionAssert.IsEmpty(played);
        }

        [UnityTest]
        public IEnumerator Rejected_LeavesNoLockAndNoRecord()
        {
            result = DialogueResult.Rejected;
            var zone = NewZone(Node);
            var body = NewLaris(out _);
            LogAssert.Expect(LogType.Warning, new Regex($"'{Node}'를 재생하지 못했다"));

            zone.HandleEntered(body);
            yield return null;

            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(GameSession.Investigated.Contains(Node));
            Assert.IsFalse(SavedOnDisk(Node));
        }

        [UnityTest]
        public IEnumerator Cancelled_DoesNotRecord()
        {
            result = DialogueResult.Cancelled;
            var zone = NewZone(Node);
            var body = NewLaris(out _);

            zone.HandleEntered(body);
            yield return FinishDialogue();

            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(GameSession.Investigated.Contains(Node));
        }

        [UnityTest]
        public IEnumerator WithoutDialogueService_WarnsAndDoesNotRecord()
        {
            DeathRefusalZone.PlayDialogue = originalPlayDialogue;
            var zone = NewZone(Node);
            var body = NewLaris(out _);
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            LogAssert.Expect(LogType.Warning, new Regex("재생하지 못했다"));

            zone.HandleEntered(body);
            yield return null;

            Assert.IsFalse(GameSession.Investigated.Contains(Node));
        }
    }
}
