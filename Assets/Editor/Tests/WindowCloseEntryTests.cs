using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Dialogue;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.Tests;
using ProjectT.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;
using static ProjectT.Tests.TestObjects;

namespace ProjectT.Death.Tests
{
    // 다음 실행 흉내: 세이브 파일에 종료 상태를 써 두고 GameSession.Begin으로 다시 켠다.
    public class WindowCloseEntryTests
    {
        const string Node = ExitRecorder.WindowCloseDeathNodeId;

        TempSaveSandbox save;
        InputLock inputLock;
        ScreenFader fader;
        Func<DialogueService, string, YarnTask<DialogueResult>> originalPlay;
        Action originalReload;
        int reloadCount;
        List<string> played;
        List<float> alphaAtPlay;
        List<bool> lockedAtPlay;
        DialogueResult result;
        AwaitableCompletionSource pending;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();
            ExitRecorder.ResetState();
            reloadCount = 0;
            originalReload = DeathSequence.ReloadScene;
            DeathSequence.ReloadScene = () => reloadCount++;

            played = new List<string>();
            alphaAtPlay = new List<float>();
            lockedAtPlay = new List<bool>();
            result = DialogueResult.Completed;
            pending = null;
            originalPlay = SceneEntry.PlayDialogue;
            SceneEntry.PlayDialogue = FakeDialogue;

            fader = NewFader();
            fader.SetAlpha(0f);
            inputLock = new GameObject("InputLock").AddComponent<InputLock>();
        }

        [TearDown]
        public void TearDown()
        {
            SceneEntry.PlayDialogue = originalPlay;
            DeathSequence.ReloadScene = originalReload;
            DeathSequence.ResetState();
            ExitRecorder.ResetState();
            save.Dispose();
        }

        async YarnTask<DialogueResult> FakeDialogue(DialogueService dialogue, string nodeId)
        {
            played.Add(nodeId);
            alphaAtPlay.Add(fader.Alpha);
            lockedAtPlay.Add(inputLock.IsLocked);
            if (result == DialogueResult.Rejected)
                return DialogueResult.Rejected;

            pending = new AwaitableCompletionSource();
            await pending.Awaitable;
            return result;
        }

        void WriteExitState(int value)
        {
            Directory.CreateDirectory(save.Paths.Root);
            File.WriteAllText(save.Paths.SaveFile, "{\n    \"diedNodeIds\": [\"" + Node + "\"],\n    \"exitState\": " + value + "\n}");
        }

        int SavedExitState() => (int)JsonUtility.FromJson<SaveData>(File.ReadAllText(save.Paths.SaveFile)).exitState;

        SceneEntry NewEntry(bool withDialogue = true)
        {
            var entry = new GameObject("SceneEntry").AddComponent<SceneEntry>();
            DialogueService dialogue = withDialogue ? new GameObject("Dialogue").AddComponent<DialogueService>() : null;
            Set(entry, so =>
            {
                so.FindProperty("inputLock").objectReferenceValue = inputLock;
                so.FindProperty("dialogueService").objectReferenceValue = dialogue;
            });
            return entry;
        }

        static IEnumerator Frames(Func<bool> until)
        {
            for (int i = 0; i < 300 && !until(); i++)
                yield return null;
        }

        [Test]
        public void Begin_WithDeathPending_ConsumesOnceAndSavesNone()
        {
            WriteExitState(2);

            GameSession.Begin();

            Assert.AreEqual(0, SavedExitState(), "읽은 직후 '없음'으로 저장해야 한다.");
            CollectionAssert.AreEqual(new[] { Node }, SaveManager.Data.diedNodeIds);
            Assert.IsTrue(GameSession.ConsumePendingDeath());
            Assert.IsFalse(GameSession.ConsumePendingDeath(), "한 번만 소비한다.");
        }

        [Test]
        public void Begin_AfterHandledOnce_DoesNotDieAgain()
        {
            WriteExitState(2);
            GameSession.Begin();
            Assert.IsTrue(GameSession.ConsumePendingDeath());

            GameSession.Begin();

            Assert.IsFalse(GameSession.ConsumePendingDeath());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(7)]
        public void Begin_WithOtherStates_NoPendingDeath_AndSavesNone(int value)
        {
            WriteExitState(value);

            GameSession.Begin();

            Assert.IsFalse(GameSession.ConsumePendingDeath());
            Assert.AreEqual(0, SavedExitState());
            Assert.AreEqual(ExitState.None, SaveManager.Data.exitState);
        }

        [Test]
        public void Begin_WithoutSave_DoesNotCreateSave()
        {
            GameSession.Begin();

            Assert.IsFalse(GameSession.ConsumePendingDeath());
            Assert.IsFalse(File.Exists(save.Paths.SaveFile));
        }

        [UnityTest]
        public IEnumerator PendingDeath_FadesInThenPlaysNodeOnly_ThenUnlocks()
        {
            WriteExitState(2);
            GameSession.Begin();
            var entry = NewEntry();

            entry.EnterScene();
            Assert.IsTrue(inputLock.IsLocked, "진입 첫 프레임부터 잠근다.");
            Assert.IsTrue(entry.EnteringAfterWindowClose);

            entry.RunWindowCloseEntry().Forget();
            yield return Frames(() => pending != null);

            CollectionAssert.AreEqual(new[] { Node }, played);
            Assert.AreEqual(0f, alphaAtPlay[0], "페이드 인이 끝난 뒤 대사를 재생한다.");
            Assert.IsTrue(lockedAtPlay[0]);
            Assert.IsFalse(DeathSequence.IsActive, "즉시 사망 흐름을 타지 않는다.");

            pending.SetResult();
            yield return Frames(() => !inputLock.IsLocked);

            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(entry.EnteringAfterWindowClose);
            Assert.AreEqual(0, reloadCount, "리로드하지 않는다.");
            CollectionAssert.AreEqual(new[] { Node }, played);
        }

        [UnityTest]
        public IEnumerator PendingDeath_WhenNodeRejected_WarnsAndResumes()
        {
            WriteExitState(2);
            GameSession.Begin();
            var entry = NewEntry();
            result = DialogueResult.Rejected;

            entry.EnterScene();
            LogAssert.Expect(LogType.Warning, new Regex("창 닫기 사망 노드 'Death_CloseWindow'를 재생하지 못했다"));
            entry.RunWindowCloseEntry().Forget();
            yield return Frames(() => !inputLock.IsLocked);

            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [UnityTest]
        public IEnumerator PendingDeath_WithoutDialogueService_WarnsAndResumes()
        {
            WriteExitState(2);
            GameSession.Begin();
            var entry = NewEntry(withDialogue: false);

            entry.EnterScene();
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어 창 닫기 사망 노드"));
            entry.RunWindowCloseEntry().Forget();
            yield return Frames(() => !inputLock.IsLocked);

            Assert.IsFalse(inputLock.IsLocked);
            CollectionAssert.IsEmpty(played);
        }

        [UnityTest]
        public IEnumerator NoPendingDeath_EntryDoesNothing()
        {
            WriteExitState(0);
            GameSession.Begin();
            var entry = NewEntry();

            entry.EnterScene();
            Assert.IsFalse(inputLock.IsLocked);

            entry.RunWindowCloseEntry().Forget();
            yield return null;

            CollectionAssert.IsEmpty(played);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void PendingDeath_IsConsumedByFirstEntryOnly()
        {
            WriteExitState(2);
            GameSession.Begin();

            var first = NewEntry();
            first.EnterScene();
            var second = NewEntry();
            second.EnterScene();

            Assert.IsTrue(first.EnteringAfterWindowClose);
            Assert.IsFalse(second.EnteringAfterWindowClose);
        }
    }
}
