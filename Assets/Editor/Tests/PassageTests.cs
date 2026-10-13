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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectT.Area.Tests
{
    public class PassageTests
    {
        Passage passage;
        PassageEnd endA;
        PassageEnd endB;
        PlayerMovement laris;
        BoxCollider2D larisBody;
        InputLock inputLock;

        // Passage는 페이드·카메라를 씬에서 찾으므로 열린 씬의 오브젝트와 섞이지 않게 빈 씬에서 돌린다.
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            passage = new GameObject("Passage").AddComponent<Passage>();
            endA = NewEnd("EndA", new Vector3(-10f, 0f, 0f));
            endB = NewEnd("EndB", new Vector3(10f, 5f, 0f));
            Set(passage, so =>
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

        PassageEnd NewEnd(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(passage.transform);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            return go.AddComponent<PassageEnd>();
        }

        ScreenFader NewFader()
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

        (CinemachineConfiner2D confiner, CinemachineCamera camera) NewCamera(Collider2D startBounds)
        {
            var go = new GameObject("CinemachineCamera");
            var camera = go.AddComponent<CinemachineCamera>();
            var confiner = go.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = startBounds;
            return (confiner, camera);
        }

        static T NewBounds<T>(string name) where T : Collider2D
        {
            var collider = new GameObject(name).AddComponent<T>();
            collider.isTrigger = true;
            return collider;
        }

        static void SetBounds(PassageEnd end, Collider2D bounds) =>
            Set(end, so => so.FindProperty("cameraBounds").objectReferenceValue = bounds);

        static void SetFacing(PassageEnd end, ArrivalFacing facing) =>
            Set(end, so => so.FindProperty("arrivalFacing").enumValueIndex = (int)facing);

        static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
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
        public void EndA_LocksFadesMovesToEndB_SwitchesBounds_ThenReleases()
        {
            var fader = NewFader();
            var startBounds = NewBounds<BoxCollider2D>("RoomA");
            var roomB = NewBounds<PolygonCollider2D>("RoomB");
            var (confiner, camera) = NewCamera(startBounds);
            SetBounds(endB, roomB);

            IEnumerator routine = passage.Prepare(endA, larisBody);
            Assert.IsNotNull(routine);
            Assert.IsTrue(inputLock.IsLocked);
            Assert.IsTrue(passage.IsTransitioning);

            RunToPhysicsWait(routine);
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);
            Assert.AreEqual(Vector2.zero, laris.GetComponent<Rigidbody2D>().linearVelocity);
            Assert.AreEqual(1f, fader.Alpha);
            Assert.AreSame(roomB, confiner.BoundingShape2D);
            Assert.IsFalse(camera.PreviousStateIsValid);
            Assert.IsTrue(inputLock.IsLocked);

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(passage.IsTransitioning);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void WithoutBounds_StillSnapsCameraRightAfterMove()
        {
            var camera = new GameObject("CinemachineCamera").AddComponent<CinemachineCamera>();
            camera.PreviousStateIsValid = true;

            RunToPhysicsWait(passage.Prepare(endA, larisBody));
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);
            Assert.IsFalse(camera.PreviousStateIsValid);
        }

        [Test]
        public void EndB_MovesBackToEndA()
        {
            laris.transform.position = endB.transform.position;
            Physics2D.SyncTransforms();

            RunToEnd(passage.Prepare(endB, larisBody));
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void ArrivalFacing_IsAppliedOnlyWhenSet()
        {
            laris.Face(Vector2Int.left);
            SetFacing(endB, ArrivalFacing.UpRight);
            RunToEnd(passage.Prepare(endA, larisBody));
            Assert.AreEqual(new Vector2Int(1, 1), laris.Facing);

            passage.OnEndExited(endB, larisBody);
            RunToEnd(passage.Prepare(endB, larisBody));
            Assert.AreEqual(new Vector2Int(1, 1), laris.Facing);
        }

        [Test]
        public void ReentryWhileRunning_IsIgnored()
        {
            IEnumerator routine = passage.Prepare(endA, larisBody);
            RunToPhysicsWait(routine);

            Assert.IsNull(passage.Prepare(endA, larisBody));
            Assert.IsNull(passage.Prepare(endB, larisBody));

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void ArrivedEnd_StaysInactiveUntilLarisLeavesIt()
        {
            RunToEnd(passage.Prepare(endA, larisBody));
            Assert.IsTrue(endB.WaitingForExit);
            Assert.IsNull(passage.Prepare(endB, larisBody));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);

            passage.OnEndExited(endB, larisBody);
            Assert.IsFalse(endB.WaitingForExit);
            Assert.IsNotNull(passage.Prepare(endB, larisBody));
        }

        [Test]
        public void ArrivalOutsideTrigger_IsUsableRightAway()
        {
            endB.GetComponent<BoxCollider2D>().offset = new Vector2(5f, 0f);

            RunToEnd(passage.Prepare(endA, larisBody));
            Assert.IsFalse(endB.WaitingForExit);
            Assert.IsNotNull(passage.Prepare(endB, larisBody));
        }

        [Test]
        public void TriggerAndNonLarisBodies_AreIgnored()
        {
            var detect = laris.gameObject.AddComponent<CircleCollider2D>();
            detect.isTrigger = true;
            Assert.IsNull(passage.Prepare(endA, detect));

            var box = new GameObject("Box");
            box.AddComponent<Rigidbody2D>();
            Assert.IsNull(passage.Prepare(endA, box.AddComponent<BoxCollider2D>()));

            Assert.IsNull(passage.Prepare(endA, new GameObject("Static").AddComponent<BoxCollider2D>()));
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void AlreadyLocked_DoesNotStart()
        {
            inputLock.Lock(InputLockReason.Dialogue);
            Assert.IsNull(passage.Prepare(endA, larisBody));
            inputLock.Unlock(InputLockReason.Dialogue);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void MissingOppositeEnd_WarnsWithoutMovingOrLocking()
        {
            Set(passage, so => so.FindProperty("endB").objectReferenceValue = null);

            LogAssert.Expect(LogType.Warning, new Regex("반대쪽 끝이 없어"));
            Assert.IsNull(passage.Prepare(endA, larisBody));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(passage.IsTransitioning);
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
        }

        [Test]
        public void UnusableBoundsShape_WarnsWithoutMovingOrLocking()
        {
            SetBounds(endB, NewBounds<CircleCollider2D>("RoundRoom"));

            LogAssert.Expect(LogType.Warning, new Regex("카메라 경계 'RoundRoom'"));
            Assert.IsNull(passage.Prepare(endA, larisBody));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.AreEqual((Vector2)endA.transform.position, LarisPosition);
        }

        [Test]
        public void ExceptionMidway_ReleasesLockAndFade()
        {
            var fader = NewFader();
            IEnumerator routine = passage.Prepare(endA, larisBody);
            Object.DestroyImmediate(endB.gameObject);

            Assert.That(() => routine.MoveNext(), Throws.InstanceOf<Exception>());
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(passage.IsTransitioning);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void WithoutFaderCameraOrInputLock_StillMoves()
        {
            Object.DestroyImmediate(inputLock);
            SetBounds(endB, NewBounds<BoxCollider2D>("RoomB"));

            RunToEnd(passage.Prepare(endA, larisBody));
            Assert.AreEqual((Vector2)endB.transform.position, LarisPosition);
            Assert.IsFalse(passage.IsTransitioning);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
