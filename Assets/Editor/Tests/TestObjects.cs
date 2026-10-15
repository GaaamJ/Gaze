using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProjectT.Player;
using ProjectT.Puzzle;
using ProjectT.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectT.Tests
{
    internal static class TestObjects
    {
        public static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObjects<T>(SerializedProperty list, IList<T> values) where T : Object
        {
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // 몸 콜라이더를 돌려준다. 감지용 트리거는 라리스로 인정되지 않는 경우를 시험할 때 쓴다.
        public static Collider2D NewLaris(out Collider2D detectTrigger)
        {
            var go = new GameObject("Laris");
            go.AddComponent<PlayerMovement>();
            var body = go.AddComponent<BoxCollider2D>();
            detectTrigger = go.AddComponent<CircleCollider2D>();
            detectTrigger.isTrigger = true;
            return body;
        }

        // 페이드 시간이 0이라 시간이 흐르지 않는 에디트 모드에서도 한 번에 끝난다.
        public static ScreenFader NewFader()
        {
            var go = new GameObject("ScreenFader");
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            var fader = go.AddComponent<ScreenFader>();
            Set(fader, so =>
            {
                so.FindProperty("image").objectReferenceValue = image;
                so.FindProperty("duration").floatValue = 0f;
            });
            fader.SetAlpha(0f);
            return fader;
        }

        public static PasswordSymbol[] NewSymbols(int count)
        {
            var symbols = new PasswordSymbol[count];
            for (int i = 0; i < count; i++)
                symbols[i] = new GameObject($"0{i + 1}").AddComponent<PasswordSymbol>();
            return symbols;
        }

        public static PasswordPuzzle AddPuzzle(GameObject go, PasswordSymbol[] symbols, PasswordSymbol[] answers, string puzzleId, string wrongDeathNodeId = "")
        {
            // AddComponent 때 OnValidate가 값이 비어 있는 상태로 먼저 불린다.
            LogAssert.Expect(LogType.Warning, new Regex("puzzleId가 비어 있다"));
            var puzzle = go.AddComponent<PasswordPuzzle>();
            Set(puzzle, so =>
            {
                SetObjects(so.FindProperty("symbols"), symbols);
                SetObjects(so.FindProperty("answers"), answers);
                so.FindProperty("puzzleId").stringValue = puzzleId;
                so.FindProperty("wrongDeathNodeId").stringValue = wrongDeathNodeId;
            });
            return puzzle;
        }
    }
}
