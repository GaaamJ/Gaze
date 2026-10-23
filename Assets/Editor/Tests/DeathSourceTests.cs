using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Interaction;
using ProjectT.Session;
using ProjectT.Tests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using static ProjectT.Tests.TestObjects;

namespace ProjectT.Death.Tests
{
    // Begin 이후의 연출은 DeathSequenceTests가 본다. 여기서는 DeathZone·DeathInteractable이 입구를 부르는지만 본다.
    public class DeathSourceTests
    {
        TempSaveSandbox save;
        InputLock inputLock;
        Action originalReload;
        int reloadCount;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DeathSequence.ResetState();
            originalReload = DeathSequence.ReloadScene;
            reloadCount = 0;
            DeathSequence.ReloadScene = () => reloadCount++;

            inputLock = new GameObject("LockOwner").AddComponent<InputLock>();
            var entry = new GameObject("SceneEntry").AddComponent<SceneEntry>();
            Set(entry, so => so.FindProperty("inputLock").objectReferenceValue = inputLock);
        }

        [TearDown]
        public void TearDown()
        {
            DeathSequence.ResetState();
            DeathSequence.ReloadScene = originalReload;
            save.Dispose();
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

        [Test]
        public void DeathInteractable_BlankNode_WarnsWithoutDying()
        {
            var target = new GameObject("Gap").AddComponent<DeathInteractable>();

            LogAssert.Expect(LogType.Warning, new Regex("사망 노드 이름이 비어"));
            target.Interact(new InteractContext(null, null));

            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(inputLock.IsLocked);
        }

        static DeathZone NewZone(string nodeId = "Death_Falling001")
        {
            var go = new GameObject("Zone");
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = go.AddComponent<DeathZone>();
            Set(zone, so => so.FindProperty("deathNodeId").stringValue = nodeId);
            return zone;
        }

        [Test]
        public void DeathZone_BlankNode_WarnsWithoutDying()
        {
            var zone = NewZone("");
            var body = NewLaris(out _);

            LogAssert.Expect(LogType.Warning, new Regex("사망 노드 이름이 비어"));
            zone.HandleEntered(body);

            Assert.IsFalse(DeathSequence.IsActive);
            Assert.IsFalse(inputLock.IsLocked);
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
    }
}
