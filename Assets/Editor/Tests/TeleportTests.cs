using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Player;
using ProjectT.Session;
using ProjectT.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using static ProjectT.Tests.TestObjects;
using Object = UnityEngine.Object;

namespace ProjectT.Area.Tests
{
    public class TeleportTests
    {
        Teleport teleport;
        TeleportEnd endA;
        TeleportEnd endB;
        PlayerMovement laris;
        BoxCollider2D larisBody;
        InputLock inputLock;

        // Teleport는 페이드·카메라를 씬에서 찾으므로 열린 씬의 오브젝트와 섞이지 않게 빈 씬에서 돌린다.
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            teleport = new GameObject("Teleport").AddComponent<Teleport>();
            endA = NewEnd("EndA", new Vector3(-10f, 0f, 0f));
            endB = NewEnd("EndB", new Vector3(10f, 5f, 0f));
            Set(teleport, so =>
            {
                so.FindProperty("endA").objectReferenceValue = endA;
                so.FindProperty("endB").objectReferenceValue = endB;
            });

            var larisGo = new GameObject("Laris");
            larisGo.transform.position = endA.transform.position;
            laris = larisGo.AddComponent<PlayerMovement>();
            larisBody = larisGo.AddComponent<BoxCollider2D>();
            larisBody.size = new Vector2(0.75f, 0.75f);
            inputLock = larisGo.AddComponent<InputLock>();
            Physics2D.SyncTransforms();
        }

        TeleportEnd NewEnd(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(teleport.transform);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            return go.AddComponent<TeleportEnd>();
        }

        const string FaderPrefabPath = "Assets/TeleportTestsFader.prefab";

        // 실제 ScreenFader.prefab은 페이드 시간이 있어 에디트 모드에서 끝나지 않으므로 시간 0인 임시 프리팹을 만든다.
        ScreenFader NewFaderPrefab()
        {
            GameObject source = NewFader().gameObject;
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(source, FaderPrefabPath);
            Object.DestroyImmediate(source);
            return asset.GetComponent<ScreenFader>();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(FaderPrefabPath);
        }

        // 첫 MoveNext는 페이드 아웃과 이동을 끝내고 물리 단계 대기에서 멈춘다.
        static void RunToPhysicsWait(IEnumerator routine)
        {
            Assert.IsTrue(routine.MoveNext());
            Assert.IsInstanceOf<WaitForFixedUpdate>(routine.Current);
            Physics2D.SyncTransforms();
        }

        static void RunToEnd(IEnumerator routine)
        {
            while (routine.MoveNext())
                Physics2D.SyncTransforms();
        }

        Vector2 LarisPosition => laris.transform.position;

        [Test]
        public void EndA_LocksFadesMovesToEndB_SnapsCamera_ThenReleases()
        {
            var fader = NewFader();
            var camera = new GameObject("CinemachineCamera").AddComponent<CinemachineCamera>();
            camera.PreviousStateIsValid = true;

            IEnumerator routine = teleport.TryBegin(endA, larisBody);
            Assert.IsNotNull(routine);
            Assert.IsTrue(inputLock.IsLocked);
            Assert.IsTrue(teleport.IsTransitioning);

            RunToPhysicsWait(routine);
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);
            Assert.AreEqual(Vector2.zero, laris.GetComponent<Rigidbody2D>().linearVelocity);
            Assert.AreEqual(1f, fader.Alpha);
            Assert.IsFalse(camera.PreviousStateIsValid);
            Assert.IsTrue(inputLock.IsLocked);

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(teleport.IsTransitioning);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void EndB_MovesBackToEndA()
        {
            laris.transform.position = endB.transform.position;
            Physics2D.SyncTransforms();

            RunToEnd(teleport.TryBegin(endB, larisBody));
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void ReentryWhileRunning_IsIgnored()
        {
            IEnumerator routine = teleport.TryBegin(endA, larisBody);
            RunToPhysicsWait(routine);

            Assert.IsNull(teleport.TryBegin(endA, larisBody));
            Assert.IsNull(teleport.TryBegin(endB, larisBody));

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void ArrivedEnd_StaysInactiveUntilLarisLeavesIt()
        {
            RunToEnd(teleport.TryBegin(endA, larisBody));
            Assert.IsTrue(endB.WaitingForExit);
            Assert.IsNull(teleport.TryBegin(endB, larisBody));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);

            teleport.OnEndExited(endB, larisBody);
            Assert.IsFalse(endB.WaitingForExit);
            Assert.IsNotNull(teleport.TryBegin(endB, larisBody));
        }

        [Test]
        public void ArrivalOutsideTrigger_IsUsableRightAway()
        {
            endB.GetComponent<BoxCollider2D>().offset = new Vector2(5f, 0f);

            RunToEnd(teleport.TryBegin(endA, larisBody));
            Assert.IsFalse(endB.WaitingForExit);
            Assert.IsNotNull(teleport.TryBegin(endB, larisBody));
        }

        [Test]
        public void TriggerAndNonLarisBodies_AreIgnored()
        {
            var detect = laris.gameObject.AddComponent<CircleCollider2D>();
            detect.isTrigger = true;
            Assert.IsNull(teleport.TryBegin(endA, detect));

            var box = new GameObject("Box");
            box.AddComponent<Rigidbody2D>();
            Assert.IsNull(teleport.TryBegin(endA, box.AddComponent<BoxCollider2D>()));

            Assert.IsNull(teleport.TryBegin(endA, new GameObject("Static").AddComponent<BoxCollider2D>()));
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void AlreadyLocked_DoesNotStart()
        {
            inputLock.Lock(InputLockReason.Dialogue);
            Assert.IsNull(teleport.TryBegin(endA, larisBody));
            inputLock.Unlock(InputLockReason.Dialogue);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void MissingOppositeEnd_WarnsWithoutMovingOrLocking()
        {
            Set(teleport, so => so.FindProperty("endB").objectReferenceValue = null);

            LogAssert.Expect(LogType.Warning, new Regex("반대쪽 끝이 없어"));
            Assert.IsNull(teleport.TryBegin(endA, larisBody));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(teleport.IsTransitioning);
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
        }

        [Test]
        public void ExceptionMidway_ReleasesLockAndFade()
        {
            var fader = NewFader();
            IEnumerator routine = teleport.TryBegin(endA, larisBody);
            Object.DestroyImmediate(endB.gameObject);

            Assert.That(() => routine.MoveNext(), Throws.InstanceOf<Exception>());
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(teleport.IsTransitioning);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void NoFaderInScene_UsesFaderPrefabOnceAndReusesIt()
        {
            ScreenFader prefab = NewFaderPrefab();
            Set(teleport, so => so.FindProperty("faderPrefab").objectReferenceValue = prefab);
            Assert.IsNull(Object.FindAnyObjectByType<ScreenFader>());

            IEnumerator routine = teleport.TryBegin(endA, larisBody);
            RunToPhysicsWait(routine);
            ScreenFader[] faders = Object.FindObjectsByType<ScreenFader>(FindObjectsSortMode.None);
            Assert.AreEqual(1, faders.Length);
            Assert.AreEqual(1f, faders[0].Alpha);
            RunToEnd(routine);
            Assert.AreEqual(0f, faders[0].Alpha);

            teleport.OnEndExited(endB, larisBody);
            RunToEnd(teleport.TryBegin(endB, larisBody));
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
            Assert.AreEqual(1, Object.FindObjectsByType<ScreenFader>(FindObjectsSortMode.None).Length);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NoFaderAndNoPrefabAndNoInputLock_WarnsOnceAndStillMoves()
        {
            Object.DestroyImmediate(inputLock);

            LogAssert.Expect(LogType.Warning, new Regex("ScreenFader가 없고 'faderPrefab'도 비어"));
            RunToEnd(teleport.TryBegin(endA, larisBody));
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);
            Assert.IsFalse(teleport.IsTransitioning);

            teleport.OnEndExited(endB, larisBody);
            RunToEnd(teleport.TryBegin(endB, larisBody));
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
