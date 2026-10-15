using System;
using System.Collections;
using System.IO;
using ProjectT.Core.GameFolder;
using ProjectT.Save;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Interaction;
using ProjectT.Player;
using ProjectT.Puzzle;
using ProjectT.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectT.Death.Tests
{
    // Begin 이후의 연출은 DeathSequenceTests가 본다. 여기서는 각 트리거가 입구를 부르는지만 본다.
    public class DeathTriggerTests
    {
        InputLock inputLock;
        Action originalReload;
        int reloadCount;
        string tempBase;

        [SetUp]
        public void SetUp()
        {
            // 오답 문양 테스트가 실수로 퍼즐을 풀어도 실제 세이브를 건드리지 않게 한다.
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            SaveManager.Paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Load();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();
            originalReload = DeathSequence.ReloadScene;
            reloadCount = 0;
            DeathSequence.ReloadScene = () => reloadCount++;

            inputLock = new GameObject("LockOwner").AddComponent<InputLock>();
            var entry = new GameObject("DeathSceneEntry").AddComponent<DeathSceneEntry>();
            Set(entry, so => so.FindProperty("inputLock").objectReferenceValue = inputLock);
        }

        [TearDown]
        public void TearDown()
        {
            DeathSequence.ResetState();
            DeathSequence.ReloadScene = originalReload;
            SaveManager.Paths = null;
            SaveManager.Load();
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ExpectDeath(string node)
        {
            LogAssert.Expect(LogType.Log, new Regex($"사망 시작: {node}"));
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            LogAssert.Expect(LogType.Warning, new Regex("페이드 없이"));
        }

        // 시작된 사망 흐름이 다음 테스트로 새지 않게 리로드 단계까지 보낸다.
        IEnumerator WaitForReload()
        {
            for (int i = 0; i < 300 && reloadCount == 0; i++)
                yield return null;
            Assert.AreEqual(1, reloadCount);
        }

        [UnityTest]
        public IEnumerator DeathInteractable_Interact_BeginsDeath()
        {
            var target = new GameObject("Gap").AddComponent<DeathInteractable>();
            Set(target, so => so.FindProperty("deathNodeId").stringValue = "Death_DangerousGap");
            Assert.IsTrue(target.CanInteract);

            ExpectDeath("Death_DangerousGap");
            target.Interact(new InteractContext(null, null));

            Assert.IsTrue(DeathSequence.IsActive);
            Assert.IsTrue(inputLock.IsLocked);
            yield return WaitForReload();
        }

        DeathZone NewZone()
        {
            var go = new GameObject("Zone");
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = go.AddComponent<DeathZone>();
            Set(zone, so => so.FindProperty("deathNodeId").stringValue = "Death_Falling001");
            return zone;
        }

        static Collider2D NewLaris(out Collider2D detectTrigger)
        {
            var go = new GameObject("Laris");
            go.AddComponent<PlayerMovement>();
            var body = go.AddComponent<BoxCollider2D>();
            detectTrigger = go.AddComponent<CircleCollider2D>();
            detectTrigger.isTrigger = true;
            return body;
        }

        [UnityTest]
        public IEnumerator DeathZone_LarisBody_BeginsDeath()
        {
            var zone = NewZone();
            var body = NewLaris(out _);

            ExpectDeath("Death_Falling001");
            zone.HandleEntered(body);

            Assert.IsTrue(DeathSequence.IsActive);
            yield return WaitForReload();
        }

        [Test]
        public void DeathZone_IgnoresDetectTriggerAndOthers()
        {
            var zone = NewZone();
            NewLaris(out var detectTrigger);
            var other = new GameObject("Box").AddComponent<BoxCollider2D>();

            zone.HandleEntered(detectTrigger);
            zone.HandleEntered(other);

            Assert.IsFalse(DeathSequence.IsActive);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PasswordPuzzle_WrongSymbol_BeginsDeathAfterClearing()
        {
            var symbols = new PasswordSymbol[3];
            for (int i = 0; i < symbols.Length; i++)
                symbols[i] = new GameObject($"0{i + 1}").AddComponent<PasswordSymbol>();

            LogAssert.Expect(LogType.Warning, new Regex("puzzleId가 비어 있다"));
            var puzzle = new GameObject("PuzzleDoor").AddComponent<PasswordPuzzle>();
            Set(puzzle, so =>
            {
                var list = so.FindProperty("symbols");
                list.arraySize = symbols.Length;
                for (int i = 0; i < symbols.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = symbols[i];
                var answers = so.FindProperty("answers");
                answers.arraySize = 2;
                answers.GetArrayElementAtIndex(0).objectReferenceValue = symbols[0];
                answers.GetArrayElementAtIndex(1).objectReferenceValue = symbols[1];
                so.FindProperty("puzzleId").stringValue = "PasswordDoor";
                so.FindProperty("wrongDeathNodeId").stringValue = "Death_PasswordWrong";
            });
            int wrongCount = 0;
            bool litWhenRaised = true;
            puzzle.WrongSymbolPressed += () => { wrongCount++; litWhenRaised = symbols[0].IsLit; };

            puzzle.HandlePressed(symbols[0]);
            LogAssert.Expect(LogType.Log, new Regex(@"\[PasswordPuzzle\] 오답 문양"));
            ExpectDeath("Death_PasswordWrong");
            puzzle.HandlePressed(symbols[2]);

            Assert.AreEqual(1, wrongCount);
            Assert.IsFalse(puzzle.IsSolved);
            Assert.IsFalse(litWhenRaised);
            Assert.IsFalse(symbols[0].IsLit);
            Assert.IsTrue(DeathSequence.IsActive);
            yield return WaitForReload();
        }
    }
}
