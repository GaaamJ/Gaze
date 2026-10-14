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
        readonly List<Object> created = new();
        string tempBase;
        GameFolderPaths paths;
        HashSet<InteractableData> investigated;

        [SetUp]
        public void SetUp()
        {
            tempBase = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            paths = new GameFolderPaths(Path.Combine(tempBase, GameFolderPaths.RootFolderName));
            SaveManager.Paths = paths;
            SaveManager.Load();
            investigated = new HashSet<InteractableData>();
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

        InteractContext Context() => new InteractContext(null, investigated);

        InteractableData NewData(string node = "", string recheck = "")
        {
            var data = ScriptableObject.CreateInstance<InteractableData>();
            created.Add(data);
            var so = new SerializedObject(data);
            so.FindProperty("dialogueNode").stringValue = node;
            so.FindProperty("recheckDialogueNode").stringValue = recheck;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        Interactable NewInteractable(InteractableData data)
        {
            var go = new GameObject("Interactable");
            created.Add(go);
            var interactable = go.AddComponent<Interactable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            return interactable;
        }

        Interactable NewRespawn(string areaId, string node = "")
        {
            var interactable = NewInteractable(NewData(node));
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

        [Test]
        public void WithDialogueNode_TriesDialogue()
        {
            var target = NewInteractable(NewData("Desk", "Desk_Recheck"));

            target.Interact(Context());

            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void WithoutDialogueNode_DoesNothingQuietly()
        {
            var target = NewInteractable(NewData());

            target.Interact(Context());

            LogAssert.NoUnexpectedReceived();
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void WithOverride_PassesToOverrideInsteadOfDialogue()
        {
            var target = NewInteractable(NewData("Desk", "Desk_Recheck"));
            var handler = target.gameObject.AddComponent<RecordingOverride>();

            target.Interact(Context());

            Assert.AreEqual(1, handler.Calls);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WithOverrideAndRespawn_SavesRespawnFirst()
        {
            var target = NewRespawn("Temp");
            var handler = target.gameObject.AddComponent<RecordingOverride>();
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장: Temp"));

            target.Interact(Context());

            Assert.AreEqual(1, handler.Calls);
            Assert.AreEqual("Temp", handler.RespawnAtCall);
        }

        [Test]
        public void ResolvesFirstThenRecheckNode()
        {
            var target = NewInteractable(NewData("Desk", "Desk_Recheck"));

            Assert.AreEqual("Desk", target.ResolveDialogueNode(investigated));
            investigated.Add(target.Data);
            Assert.AreEqual("Desk_Recheck", target.ResolveDialogueNode(investigated));
        }

        [Test]
        public void SharedData_SharesInvestigatedRecord()
        {
            var shared = NewData("Symbol", "Symbol_Recheck");
            var first = NewInteractable(shared);
            var second = NewInteractable(shared);
            var other = NewInteractable(NewData("Symbol", "Symbol_Recheck"));

            investigated.Add(first.Data);

            Assert.AreEqual("Symbol_Recheck", second.ResolveDialogueNode(investigated));
            Assert.AreEqual("Symbol", other.ResolveDialogueNode(investigated));
        }

        [Test]
        public void RecheckEmpty_RepeatsFirstNodeWithoutWarning()
        {
            var target = NewInteractable(NewData("HintGap"));
            investigated.Add(target.Data);

            Assert.AreEqual("HintGap", target.ResolveDialogueNode(investigated));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Respawn_SavesAreaIdBeforeDialogue_AndLoadRestoresIt()
        {
            var target = NewRespawn("UndergroundParking", "Obj_Respawn");
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장: UndergroundParking"));
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));

            target.Interact(Context());

            Assert.AreEqual("UndergroundParking", RespawnAreaIds.Current);
            StringAssert.Contains("\"respawnAreaId\": \"UndergroundParking\"", File.ReadAllText(paths.SaveFile));

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
        public void Respawn_SaveFails_KeepsMemoryValue()
        {
            Directory.CreateDirectory(tempBase);
            File.WriteAllText(paths.Root, "blocks root folder");
            var target = NewRespawn("Temp");

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            LogAssert.Expect(LogType.Warning, new Regex("저장에 실패했다"));
            target.Interact(Context());

            Assert.AreEqual("Temp", RespawnAreaIds.Current);
            Assert.IsFalse(File.Exists(paths.SaveFile));
        }
    }

    public class RecordingOverride : MonoBehaviour, IInteractionOverride
    {
        public int Calls { get; private set; }
        public string RespawnAtCall { get; private set; }

        public void Interact(InteractContext context)
        {
            Calls++;
            RespawnAtCall = RespawnAreaIds.Current;
        }
    }
}
