using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Core.GameFolder;
using ProjectT.Data;
using ProjectT.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjectT.Interaction.Tests
{
    public class InteractableKindTests
    {
        static readonly ObjectKind[] LinkedKinds =
        {
            ObjectKind.Death, ObjectKind.Puzzle, ObjectKind.Respawn,
            ObjectKind.Clear, ObjectKind.ElevatorDoor, ObjectKind.Corpse
        };

        readonly List<Object> created = new();
        string tempBase;
        GameFolderPaths paths;
        HashSet<string> investigated;
        ObjectResultHandlers handlers;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Paths = paths;
            SaveManager.Load();
            investigated = new HashSet<string>();
            handlers = new ObjectResultHandlers();
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
            if (Directory.Exists(tempBase))
                Directory.Delete(tempBase, true);
        }

        InteractContext Context() => new InteractContext(null, investigated, handlers);

        ObjectData NewData(string id, ObjectKind kind, string node = "", string recheck = "", string link = "")
        {
            var data = ScriptableObject.CreateInstance<ObjectData>();
            created.Add(data);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("kind").intValue = (int)kind;
            so.FindProperty("dialogueNode").stringValue = node;
            so.FindProperty("recheckDialogueNode").stringValue = recheck;
            so.FindProperty("link").stringValue = link;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        Interactable NewInteractable(ObjectData data)
        {
            var go = new GameObject($"Interactable_{data.Id}");
            created.Add(go);
            var interactable = go.AddComponent<Interactable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            return interactable;
        }

        Interactable NewRespawn(string areaId)
        {
            var interactable = NewInteractable(NewData("Respawn", ObjectKind.Respawn));
            var respawn = interactable.gameObject.AddComponent<RespawnArea>();
            if (areaId != null)
            {
                var area = ScriptableObject.CreateInstance<AreaData>();
                created.Add(area);
                var areaSo = new SerializedObject(area);
                areaSo.FindProperty("areaId").stringValue = areaId;
                areaSo.ApplyModifiedPropertiesWithoutUndo();

                var so = new SerializedObject(respawn);
                so.FindProperty("area").objectReferenceValue = area;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return interactable;
        }

        [TestCaseSource(nameof(LinkedKinds))]
        public void LinkedKind_CallsRegisteredHandlerOnce(ObjectKind kind)
        {
            var target = NewInteractable(NewData($"Obj_{kind}", kind, link: "L"));

            var calls = new List<(Interactable, ObjectKind)>();
            foreach (var k in LinkedKinds)
            {
                var captured = k;
                handlers.Register(k, (source, _) => calls.Add((source, captured)));
            }

            target.Interact(Context());

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(target, calls[0].Item1);
            Assert.AreEqual(kind, calls[0].Item2);
        }

        [TestCaseSource(nameof(LinkedKinds))]
        public void LinkedKind_WithoutHandler_LogsKindAndLink(ObjectKind kind)
        {
            var target = NewInteractable(NewData($"Obj_{kind}", kind, link: "LinkId"));
            target.Interact(Context());

            LogAssert.Expect(LogType.Log, $"[Interactable] {kind} 조사: id=Obj_{kind}, link=LinkId");
            if (kind == ObjectKind.Respawn)
                LogAssert.Expect(LogType.Warning, new Regex("RespawnArea"));
            LogAssert.Expect(LogType.Log, new Regex("아직 연결되지 않았"));
        }

        [Test]
        public void LinkedKind_WithDialogueNodeAndNoHandler_PlaysDialogue()
        {
            var target = NewInteractable(NewData("Corpse", ObjectKind.Corpse, node: "Corpse_Refuse"));

            target.Interact(Context());

            LogAssert.Expect(LogType.Log, new Regex("Corpse 조사"));
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
        }

        [Test]
        public void Unregister_OtherHandler_KeepsCurrent()
        {
            int calls = 0;
            Action<Interactable, InteractContext> current = (_, _) => calls++;
            handlers.Register(ObjectKind.Clear, current);
            handlers.Unregister(ObjectKind.Clear, (_, _) => { });
            Assert.IsTrue(handlers.TryGet(ObjectKind.Clear, out _));

            handlers.Unregister(ObjectKind.Clear, current);
            Assert.IsFalse(handlers.TryGet(ObjectKind.Clear, out _));
        }

        [Test]
        public void Respawn_SavesAreaIdBeforeHandler_AndLoadRestoresIt()
        {
            var target = NewRespawn("UndergroundParking");
            string savedAtHandler = null;
            handlers.Register(ObjectKind.Respawn, (_, _) =>
                savedAtHandler = File.Exists(paths.SaveFile) ? File.ReadAllText(paths.SaveFile) : null);

            target.Interact(Context());

            StringAssert.Contains("\"respawnAreaId\": \"UndergroundParking\"", savedAtHandler);
            Assert.AreEqual("UndergroundParking", RespawnAreaIds.Current);

            SaveManager.Load();
            Assert.AreEqual("UndergroundParking", SaveManager.Data.respawnAreaId);
        }

        [Test]
        public void Respawn_WithoutArea_KeepsStartPoint()
        {
            var target = NewRespawn(null);
            LogAssert.Expect(LogType.Warning, new Regex("RespawnArea의 지역이 비어"));

            target.Interact(Context());

            Assert.AreEqual("", RespawnAreaIds.Current);
            Assert.IsFalse(File.Exists(paths.SaveFile));
        }

        [Test]
        public void Respawn_SaveFails_KeepsMemoryValueAndStillRunsResult()
        {
            Directory.CreateDirectory(tempBase);
            File.WriteAllText(paths.Root, "blocks root folder");
            var target = NewRespawn("Temp");
            bool handled = false;
            handlers.Register(ObjectKind.Respawn, (_, _) => handled = true);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            LogAssert.Expect(LogType.Warning, new Regex("저장에 실패했다"));
            target.Interact(Context());

            Assert.AreEqual("Temp", RespawnAreaIds.Current);
            Assert.IsTrue(handled);
            Assert.IsFalse(File.Exists(paths.SaveFile));
        }

        [Test]
        public void Item_OnlyLogs_AndIgnoresHandlers()
        {
            var target = NewInteractable(NewData("Item", ObjectKind.Item));
            handlers.Register(ObjectKind.Item, (_, _) => Assert.Fail("아이템은 핸들러를 거치지 않는다."));

            target.Interact(Context());

            LogAssert.Expect(LogType.Log, "[Interactable] 아이템 조사: Item");
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void Basic_IgnoresHandlers_AndTriesDialogue()
        {
            var target = NewInteractable(NewData("Desk", ObjectKind.Basic, "Desk", "Desk_Recheck"));
            handlers.Register(ObjectKind.Basic, (_, _) => Assert.Fail("기본은 핸들러를 거치지 않는다."));

            target.Interact(Context());

            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void Basic_ResolvesFirstThenRecheckNode()
        {
            var target = NewInteractable(NewData("Desk", ObjectKind.Basic, "Desk", "Desk_Recheck"));

            Assert.AreEqual("Desk", target.ResolveDialogueNode(investigated));
            investigated.Add("Desk");
            Assert.AreEqual("Desk_Recheck", target.ResolveDialogueNode(investigated));
        }

        [Test]
        public void Basic_RecheckEmpty_RepeatsFirstNodeWithWarning()
        {
            var target = NewInteractable(NewData("Desk", ObjectKind.Basic, "Desk"));
            investigated.Add("Desk");

            LogAssert.Expect(LogType.Warning, new Regex("재조사 대사 노드가 비어"));
            Assert.AreEqual("Desk", target.ResolveDialogueNode(investigated));
        }
    }
}
