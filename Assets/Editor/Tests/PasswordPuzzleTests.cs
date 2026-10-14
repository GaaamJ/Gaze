using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjectT.Puzzle.Tests
{
    public class PasswordPuzzleTests
    {
        const string PuzzleId = "PasswordDoor";

        readonly List<Object> created = new();
        string tempBase;
        GameFolderPaths paths;

        PasswordSymbol[] symbols;
        PasswordPuzzle puzzle;
        SpriteRenderer doorRenderer;
        GameObject[] targets;
        Sprite closedSprite;
        Sprite openSprite;
        int wrongCount;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Paths = paths;
            SaveManager.Load();
            wrongCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            SaveManager.Paths = null;
            SaveManager.Load();
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        Sprite NewSprite(string name)
        {
            var texture = new Texture2D(2, 2);
            created.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            sprite.name = name;
            created.Add(sprite);
            return sprite;
        }

        GameObject NewInactiveTarget(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            go.SetActive(false);
            return go;
        }

        void Build() => Build(NewInactiveTarget("TeleportA"), NewInactiveTarget("TeleportB"));

        void Build(params GameObject[] activateOnOpen)
        {
            targets = activateOnOpen;
            symbols = new PasswordSymbol[5];
            for (int i = 0; i < symbols.Length; i++)
            {
                var go = new GameObject($"0{i + 1}");
                created.Add(go);
                symbols[i] = go.AddComponent<PasswordSymbol>();
            }

            var door = new GameObject("PuzzleDoor");
            created.Add(door);
            closedSprite = NewSprite("Closed");
            openSprite = NewSprite("Open");
            doorRenderer = door.AddComponent<SpriteRenderer>();
            doorRenderer.sprite = closedSprite;

            LogAssert.Expect(LogType.Warning, new Regex("puzzleId가 비어 있다"));
            puzzle = door.AddComponent<PasswordPuzzle>();
            var so = new SerializedObject(puzzle);
            var list = so.FindProperty("symbols");
            list.arraySize = symbols.Length;
            for (int i = 0; i < symbols.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = symbols[i];
            var answerList = so.FindProperty("answers");
            answerList.arraySize = 2;
            answerList.GetArrayElementAtIndex(0).objectReferenceValue = symbols[0];
            answerList.GetArrayElementAtIndex(1).objectReferenceValue = symbols[3];
            so.FindProperty("puzzleId").stringValue = PuzzleId;
            so.FindProperty("doorRenderer").objectReferenceValue = doorRenderer;
            so.FindProperty("openSprite").objectReferenceValue = openSprite;
            var targetList = so.FindProperty("activateOnOpen");
            targetList.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                targetList.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            puzzle.WrongSymbolPressed += () => wrongCount++;
        }

        void Press(int number) => puzzle.HandlePressed(symbols[number - 1]);

        void AssertClosed()
        {
            Assert.IsFalse(puzzle.IsSolved);
            Assert.AreSame(closedSprite, doorRenderer.sprite);
            foreach (var target in targets)
            {
                if (target != null)
                    Assert.IsFalse(target.activeSelf);
            }
            CollectionAssert.DoesNotContain(SaveManager.Data.solvedPuzzleIds, PuzzleId);
        }

        void AssertOpened()
        {
            Assert.IsTrue(puzzle.IsSolved);
            Assert.AreSame(openSprite, doorRenderer.sprite);
            foreach (var target in targets)
            {
                if (target != null)
                    Assert.IsTrue(target.activeSelf);
            }
            CollectionAssert.Contains(SaveManager.Data.solvedPuzzleIds, PuzzleId);
        }

        [Test]
        public void AnswerSymbol_TogglesOnAndOff()
        {
            Build();

            Press(1);
            Assert.IsTrue(symbols[0].IsLit);

            Press(1);
            Assert.IsFalse(symbols[0].IsLit);
            AssertClosed();
            Assert.AreEqual(0, wrongCount);
        }

        [Test]
        public void BothAnswersLit_OpensAndSaves()
        {
            Build();

            Press(1);
            AssertClosed();
            Press(4);

            AssertOpened();
            StringAssert.Contains(PuzzleId, File.ReadAllText(paths.SaveFile));
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { PuzzleId }, SaveManager.Data.solvedPuzzleIds);
        }

        [Test]
        public void ReverseOrder_AlsoOpens()
        {
            Build();

            Press(4);
            Press(1);

            AssertOpened();
        }

        [Test]
        public void AlreadySavedId_IsNotDuplicated()
        {
            SaveManager.Data.solvedPuzzleIds.Add(PuzzleId);
            Build();

            Press(1);
            Press(4);

            CollectionAssert.AreEqual(new[] { PuzzleId }, SaveManager.Data.solvedPuzzleIds);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void WrongSymbol_ClearsAllAndRaisesOnce(int wrong)
        {
            Build();
            Press(1);

            LogAssert.Expect(LogType.Log, new Regex(@"\[PasswordPuzzle\] 오답 문양"));
            LogAssert.Expect(LogType.Warning, new Regex("wrongDeathNodeId가 비어"));
            Press(wrong);

            foreach (var symbol in symbols)
                Assert.IsFalse(symbol.IsLit);
            Assert.AreEqual(1, wrongCount);
            AssertClosed();
        }

        [Test]
        public void WrongSymbol_ThenAnswersAgain_Opens()
        {
            Build();
            Press(1);
            LogAssert.Expect(LogType.Log, new Regex(@"\[PasswordPuzzle\] 오답 문양"));
            LogAssert.Expect(LogType.Warning, new Regex("wrongDeathNodeId가 비어"));
            Press(2);

            Press(4);
            AssertClosed();
            Press(1);
            AssertOpened();
        }

        [Test]
        public void AfterSolved_PressesChangeNothing()
        {
            Build();
            Press(1);
            Press(4);

            for (int i = 1; i <= 5; i++)
                Press(i);

            Assert.IsTrue(symbols[0].IsLit);
            Assert.IsTrue(symbols[3].IsLit);
            Assert.IsFalse(symbols[1].IsLit);
            Assert.AreEqual(0, wrongCount);
            AssertOpened();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void StartWithSavedId_StartsOpened()
        {
            SaveManager.Data.solvedPuzzleIds.Add(PuzzleId);
            Build();

            puzzle.RestoreFromSave();

            AssertOpened();
            Assert.IsTrue(symbols[0].IsLit);
            Assert.IsTrue(symbols[3].IsLit);
            Assert.IsFalse(symbols[1].IsLit);

            Press(2);
            Assert.AreEqual(0, wrongCount);
            Assert.IsTrue(symbols[0].IsLit);
        }

        [Test]
        public void StartWithoutSavedId_StaysClosed()
        {
            SaveManager.Data.solvedPuzzleIds.Add("OtherPuzzle");
            Build();

            puzzle.RestoreFromSave();

            AssertClosed();
        }

        [Test]
        public void NullEntryInActivateList_IsSkipped()
        {
            Build(NewInactiveTarget("TeleportA"), null, NewInactiveTarget("TeleportB"));

            Press(1);
            Press(4);

            AssertOpened();
            Assert.IsTrue(targets[0].activeSelf);
            Assert.IsTrue(targets[2].activeSelf);
        }

        [Test]
        public void EmptyActivateList_StillOpens()
        {
            Build(Array.Empty<GameObject>());

            Press(1);
            Press(4);

            AssertOpened();
        }

        [Test]
        public void StartWithSavedId_EmptyActivateList_StartsOpened()
        {
            SaveManager.Data.solvedPuzzleIds.Add(PuzzleId);
            Build(Array.Empty<GameObject>());

            puzzle.RestoreFromSave();

            AssertOpened();
        }
    }
}
