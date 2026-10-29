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

        DialogueInteractable NewInteractable(string nodeId = "")
        {
            var go = new GameObject("DialogueInteractable");
            created.Add(go);
            var interactable = go.AddComponent<DialogueInteractable>();
            Set(interactable, so => so.FindProperty("dialogueNodeId").stringValue = nodeId);
            return interactable;
        }

        DialogueInteractable NewRespawn(string nodeId = "")
        {
            var interactable = NewInteractable(nodeId);
            interactable.gameObject.AddComponent<Respawn>();
            return interactable;
        }

        [Test]
        public void WithNodeId_TriesDialogue()
        {
            var target = NewInteractable("Desk");

            target.Interact(Context());

            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void WithoutNodeId_DoesNothingQuietly()
        {
            var target = NewInteractable();

            target.Interact(Context());

            LogAssert.NoUnexpectedReceived();
            Assert.IsEmpty(investigated);
        }

        [Test]
        public void CanInteract_TrueWithoutNodeId()
        {
            var target = NewInteractable();

            Assert.IsTrue(target.CanInteract);
            target.gameObject.SetActive(false);
            Assert.IsFalse(target.CanInteract);
        }

        [Test]
        public void ResolvesFirstThenRecheckNode()
        {
            var target = NewInteractable("Desk");

            Assert.AreEqual("Desk", target.ResolveDialogueNode(investigated, AllExist));
            investigated.Add("Desk");
            Assert.AreEqual("Desk_Recheck", target.ResolveDialogueNode(investigated, AllExist));
        }

        [Test]
        public void RecheckMissing_RepeatsFirstNodeWithoutWarning()
        {
            var target = NewInteractable("HintGap");
            investigated.Add("HintGap");

            Assert.AreEqual("HintGap", target.ResolveDialogueNode(investigated, NoneExist));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RecheckLookup_AsksForRecheckNodeName()
        {
            var target = NewInteractable("Desk");
            investigated.Add("Desk");
            var asked = new List<string>();

            target.ResolveDialogueNode(investigated, node => { asked.Add(node); return false; });

            CollectionAssert.AreEqual(new[] { "Desk_Recheck" }, asked);
        }

        [Test]
        public void SameNodeId_SharesInvestigatedRecord()
        {
            var first = NewInteractable("Symbol");
            var second = NewInteractable("Symbol");
            var other = NewInteractable("Other");

            investigated.Add(first.DialogueNodeId);

            Assert.AreEqual("Symbol_Recheck", second.ResolveDialogueNode(investigated, AllExist));
            Assert.AreEqual("Other", other.ResolveDialogueNode(investigated, AllExist));
        }

        [Test]
        public void RecordInvestigated_SavesNodeIdOnce()
        {
            DialogueInteractable.RecordInvestigated(investigated, "HintGapA");
            DialogueInteractable.RecordInvestigated(investigated, "HintGapA");

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

            DialogueInteractable.RecordInvestigated(investigated, "HintGapA");

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

            var target = NewInteractable("HintGapA");
            Assert.IsTrue(GameSession.Investigated.Contains("HintGapA"));
            Assert.AreEqual("HintGapA_Recheck", target.ResolveDialogueNode(GameSession.Investigated, AllExist));
        }

        [Test]
        public void Respawn_SavesPositionBeforeDialogue_AndLoadRestoresIt()
        {
            var target = NewRespawn("Respawn");
            target.transform.position = new Vector3(2.5f, -4f, 0f);
            var logs = new List<string>();
            Application.LogCallback capture = (message, _, _) => logs.Add(message);
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));
            LogAssert.Expect(LogType.Warning, new Regex("DialogueService가 없어"));

            Application.logMessageReceived += capture;
            try
            {
                target.Interact(Context());
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            int saved = logs.FindIndex(m => m.Contains("리스폰 지점 갱신·저장"));
            int dialogue = logs.FindIndex(m => m.Contains("DialogueService가 없어"));
            Assert.That(saved, Is.GreaterThanOrEqualTo(0));
            Assert.That(saved, Is.LessThan(dialogue), "리스폰 저장이 대사보다 먼저여야 한다.");

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
            Set(target.GetComponent<Respawn>(), so => so.FindProperty("spawnPoint").objectReferenceValue = point.transform);
            LogAssert.Expect(LogType.Log, new Regex("리스폰 지점 갱신·저장"));

            target.Interact(Context());

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

            first.Interact(Context());
            second.Interact(Context());
            SaveManager.Load();
            Assert.AreEqual(new Vector2(10f, -20f), SaveManager.Data.respawnPosition);

            first.Interact(Context());
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
            target.Interact(Context());

            Assert.IsTrue(SaveManager.Data.hasRespawnPosition);
            Assert.AreEqual(new Vector2(4f, 0f), SaveManager.Data.respawnPosition);
            Assert.IsFalse(File.Exists(save.Paths.SaveFile));
        }
    }
}
