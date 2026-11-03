using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Gaze.Save;
using Gaze.Session;
using Gaze.Tests;
using UnityEngine;
using UnityEngine.TestTools;
using static Gaze.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace Gaze.Interaction.Tests
{
    public class InteractableKindTests
    {
        readonly List<Object> created = new();
        TempSaveSandbox save;
        HashSet<string> investigated;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            investigated = new HashSet<string>();
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
            save.Dispose();
        }

        InteractContext Context() => new InteractContext(null, investigated);

        static bool AllExist(string node) => true;
        static bool NoneExist(string node) => false;

        // Interactable(진입점) + DialogueResponse. RequireComponent로 Interactable이 같이 붙음.
        DialogueResponse NewDialogue(string nodeId = "")
        {
            var go = new GameObject("Interactable");
            created.Add(go);
            var dialogue = go.AddComponent<DialogueResponse>();
            Set(dialogue, so => so.FindProperty("dialogueNodeId").stringValue = nodeId);
            return dialogue;
        }

        // 대사 부품 뒤에 리스폰 부품을 붙임. 부품 순서와 상관없이 저장되는지 보려고 대사를 먼저 둠.
        RespawnResponse NewRespawn(string nodeId = "")
        {
            var dialogue = NewDialogue(nodeId);
            return dialogue.gameObject.AddComponent<RespawnResponse>();
        }

        void Interact(Component target) => target.GetComponent<Interactable>().Interact(Context());

        [Test]
        public void WithNodeId_TriesDialogue()
        {
            var target = NewDialogue("Desk");

            Interact(target);

            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void WithoutNodeId_DoesNothingQuietly()
        {
            var target = NewDialogue();

            Interact(target);

            LogAssert.NoUnexpectedReceived();
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void CanInteract_TrueWithoutNodeId()
        {
            var target = NewDialogue();

            var entry = target.GetComponent<Interactable>();

            Assert.IsTrue(entry.CanInteract);
            target.gameObject.SetActive(false);
            Assert.IsFalse(entry.CanInteract);
        }

        [Test]
        public void DisabledResponse_IsSkipped()
        {
            var target = NewRespawn();
            target.enabled = false;

            Interact(target);

            Assert.IsFalse(SaveManager.Data.hasRespawnPosition);
        }

        [Test]
        public void ResolvesFirstThenRecheckNode()
        {
            var target = NewDialogue("Desk");

            Assert.AreEqual("Desk", target.ResolveDialogueNode(investigated, AllExist));
            investigated.Add("Desk");
            Assert.AreEqual("Desk_Recheck", target.ResolveDialogueNode(investigated, AllExist));
        }

        [Test]
        public void RecheckMissing_RepeatsFirstNodeWithoutWarning()
        {
            var target = NewDialogue("HintGap");
            investigated.Add("HintGap");

            Assert.AreEqual("HintGap", target.ResolveDialogueNode(investigated, NoneExist));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RecheckLookup_AsksForRecheckNodeName()
        {
            var target = NewDialogue("Desk");
            investigated.Add("Desk");
            var asked = new List<string>();

            target.ResolveDialogueNode(investigated, node => { asked.Add(node); return false; });

            CollectionAssert.AreEqual(new[] { "Desk_Recheck" }, asked);
        }

        [Test]
        public void SameNodeId_SharesInvestigatedRecord()
        {
            var first = NewDialogue("Symbol");
            var second = NewDialogue("Symbol");
            var other = NewDialogue("Other");

            investigated.Add(first.DialogueNodeId);

            Assert.AreEqual("Symbol_Recheck", second.ResolveDialogueNode(investigated, AllExist));
            Assert.AreEqual("Other", other.ResolveDialogueNode(investigated, AllExist));
        }

        [Test]
        public void RecordInvestigated_SavesNodeIdOnce()
        {
            DialogueResponse.RecordInvestigated(investigated, "HintGapA");
            DialogueResponse.RecordInvestigated(investigated, "HintGapA");

            CollectionAssert.AreEquivalent(new[] { "HintGapA" }, investigated);
            SaveManager.Load();
            CollectionAssert.AreEqual(new[] { "HintGapA" }, SaveManager.Data.investigatedNodeIds);
        }

        [Test]
        public void RecordInvestigated_SaveFails_KeepsMemoryRecord()
        {
            Directory.CreateDirectory(save.Base);
            File.WriteAllText(save.Paths.Root, "blocks root folder");
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));

            DialogueResponse.RecordInvestigated(investigated, "HintGapA");

            Assert.IsTrue(investigated.Contains("HintGapA"));
            CollectionAssert.AreEqual(new[] { "HintGapA" }, SaveManager.Data.investigatedNodeIds);
        }

        [Test]
        public void SessionBegin_WithSavedInvestigated_FirstInteractUsesRecheck()
        {
            SaveManager.Data.investigatedNodeIds.Add("HintGapA");
            Assert.IsTrue(SaveManager.Save());
            SaveManager.Data.investigatedNodeIds.Clear();

            GameSession.Begin();

            var target = NewDialogue("HintGapA");
            Assert.IsTrue(GameSession.Investigated.Contains("HintGapA"));
            Assert.AreEqual("HintGapA_Recheck", target.ResolveDialogueNode(GameSession.Investigated, AllExist));
        }

        // 대사 부품이 앞에 있어도 Interact가 돌아올 때는 이미 저장돼 있어야 함.
        // 대사 중 창을 닫아도 리스폰 지점이 남는 근거.
        [Test]
        public void Respawn_SavedWhenInteractReturns_AndLoadRestoresIt()
        {
            var target = NewRespawn("Respawn");
            target.transform.position = new Vector3(2.5f, -4f, 0f);
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));

            Interact(target);

            SaveManager.Load();
            Assert.IsTrue(SaveManager.Data.hasRespawnPosition);
            Assert.AreEqual(new Vector2(2.5f, -4f), SaveManager.Data.respawnPosition);
            Assert.AreEqual(target.gameObject.scene.name, SaveManager.Data.roomName);
        }

        [Test]
        public void Respawn_UsesSpawnPointWhenSet()
        {
            var target = NewRespawn();
            target.transform.position = new Vector3(5f, 5f, 0f);
            var point = new GameObject("SpawnPoint");
            created.Add(point);
            point.transform.position = new Vector3(-3f, 7f, 0f);
            Set(target, so => so.FindProperty("spawnPoint").objectReferenceValue = point.transform);
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));

            Interact(target);

            Assert.AreEqual(new Vector2(-3f, 7f), SaveManager.Data.respawnPosition);
        }

        [Test]
        public void Respawn_LastInvestigatedWins()
        {
            var first = NewRespawn();
            first.transform.position = new Vector3(1f, 2f, 0f);
            var second = NewRespawn();
            second.transform.position = new Vector3(10f, -20f, 0f);
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));

            Interact(first);
            Interact(second);
            SaveManager.Load();
            Assert.AreEqual(new Vector2(10f, -20f), SaveManager.Data.respawnPosition);

            Interact(first);
            SaveManager.Load();
            Assert.AreEqual(new Vector2(1f, 2f), SaveManager.Data.respawnPosition);
        }

        [Test]
        public void Respawn_SaveFails_KeepsMemoryValue()
        {
            Directory.CreateDirectory(save.Base);
            File.WriteAllText(save.Paths.Root, "blocks root folder");
            var target = NewRespawn();
            target.transform.position = new Vector3(4f, 0f, 0f);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveManager\] 세이브 저장 실패"));
            Interact(target);

            Assert.IsTrue(SaveManager.Data.hasRespawnPosition);
            Assert.AreEqual(new Vector2(4f, 0f), SaveManager.Data.respawnPosition);
            Assert.IsFalse(File.Exists(save.Paths.SaveFile));
        }
    }
}
