using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Data;
using ProjectT.Session;
using ProjectT.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectT.Area.Tests
{
    public class ZoneTransitionTests
    {
        readonly List<Object> created = new();
        AreaData area;
        InputLock inputLock;
        ZoneTransition transition;
        ScreenFader fader;
        CameraBounds cameraBounds;
        CinemachineConfiner2D confiner;
        AreaMarker arrival;
        BoxCollider2D arrivalBounds;
        BoxCollider2D startBounds;
        ZoneExit exit;

        [SetUp]
        public void SetUp()
        {
            area = ScriptableObject.CreateInstance<AreaData>();
            created.Add(area);
            Set(area, so => so.FindProperty("areaId").stringValue = "TestArea");

            arrival = NewMarker("Arrival", new Vector3(10f, 5f, 0f)).GetComponent<AreaMarker>();
            var exitMarker = NewMarker("Exit", Vector3.zero);
            Set(exitMarker, so => so.FindProperty("pair").objectReferenceValue = arrival);
            exit = exitMarker.gameObject.AddComponent<ZoneExit>();
            Set(exit, so => so.FindProperty("arrivalCameraBoundsId").stringValue = "ArrivalBounds");

            arrivalBounds = NewMarker("ArrivalBounds", new Vector3(10f, 5f, 0f)).gameObject.AddComponent<BoxCollider2D>();
            startBounds = NewMarker("StartBounds", Vector3.zero).gameObject.AddComponent<BoxCollider2D>();

            var registryGo = NewGameObject("Registry");
            var registry = registryGo.AddComponent<AreaMarkerRegistry>();
            registry.Rebuild(new[] { exitMarker, arrival, arrivalBounds.GetComponent<AreaMarker>(), startBounds.GetComponent<AreaMarker>() });

            var cameraGo = NewGameObject("Camera");
            cameraGo.AddComponent<CinemachineCamera>();
            confiner = cameraGo.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = startBounds;
            cameraBounds = cameraGo.AddComponent<CameraBounds>();
            Set(cameraBounds, so => so.FindProperty("registry").objectReferenceValue = registry);

            fader = NewFader(withImage: true);

            var laris = NewGameObject("Laris");
            laris.AddComponent<Rigidbody2D>();
            inputLock = laris.AddComponent<InputLock>();
            transition = laris.AddComponent<ZoneTransition>();
            Wire();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            created.Clear();
        }

        GameObject NewGameObject(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        AreaMarker NewMarker(string markerId, Vector3 position)
        {
            var go = NewGameObject(markerId);
            go.transform.position = position;
            var marker = go.AddComponent<AreaMarker>();
            Set(marker, so =>
            {
                so.FindProperty("area").objectReferenceValue = area;
                so.FindProperty("markerId").stringValue = markerId;
            });
            return marker;
        }

        ScreenFader NewFader(bool withImage)
        {
            var go = NewGameObject("Fader");
            go.AddComponent<RectTransform>();
            var image = withImage ? go.AddComponent<Image>() : null;
            var screenFader = go.AddComponent<ScreenFader>();
            Set(screenFader, so =>
            {
                so.FindProperty("image").objectReferenceValue = image;
                so.FindProperty("duration").floatValue = 0f;
            });
            return screenFader;
        }

        void Wire()
        {
            Set(transition, so =>
            {
                so.FindProperty("inputLock").objectReferenceValue = inputLock;
                so.FindProperty("fader").objectReferenceValue = fader;
                so.FindProperty("cameraBounds").objectReferenceValue = cameraBounds;
            });
        }

        static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RunToEnd(IEnumerator routine)
        {
            while (routine.MoveNext()) { }
        }

        [Test]
        public void Transition_LocksUntilFadeInEnds_MovesToArrival_SwitchesBounds()
        {
            IEnumerator routine = transition.Prepare(exit);
            Assert.IsNotNull(routine);
            Assert.IsTrue(inputLock.IsLocked);

            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual((Vector2)arrival.Position, (Vector2)transition.transform.position);
            Assert.AreSame(arrivalBounds, confiner.BoundingShape2D);
            Assert.AreEqual(1f, fader.Alpha);
            Assert.IsTrue(inputLock.IsLocked);
            Assert.IsTrue(transition.IsTransitioning);

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(transition.IsTransitioning);
            Assert.AreEqual(0f, fader.Alpha);
        }

        [Test]
        public void Transition_RejectsReentryWhileRunning()
        {
            IEnumerator routine = transition.Prepare(exit);
            routine.MoveNext();

            Assert.IsNull(transition.Prepare(exit));

            RunToEnd(routine);
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsNotNull(transition.Prepare(exit));
        }

        [Test]
        public void Transition_MissingPair_WarnsAndDoesNotLock()
        {
            Set(exit.Marker, so => so.FindProperty("pair").objectReferenceValue = null);

            LogAssert.Expect(LogType.Warning, new Regex("짝이 되는 도착 지점이 없어"));
            Assert.IsNull(transition.Prepare(exit));
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(transition.IsTransitioning);
        }

        [Test]
        public void Transition_ExceptionMidway_ReleasesLock()
        {
            fader = NewFader(withImage: false);
            Wire();

            IEnumerator routine = transition.Prepare(exit);
            Assert.Throws<NullReferenceException>(() => routine.MoveNext());
            Assert.IsFalse(inputLock.IsLocked);
            Assert.IsFalse(transition.IsTransitioning);
        }

        [Test]
        public void Transition_MissingBoundsMarker_WarnsKeepsBoundsAndStillCompletes()
        {
            Set(exit, so => so.FindProperty("arrivalCameraBoundsId").stringValue = "NoSuchBounds");

            LogAssert.Expect(LogType.Warning, new Regex("카메라 경계 마커 TestArea/NoSuchBounds"));
            RunToEnd(transition.Prepare(exit));
            Assert.AreSame(startBounds, confiner.BoundingShape2D);
            Assert.AreEqual((Vector2)arrival.Position, (Vector2)transition.transform.position);
            Assert.IsFalse(inputLock.IsLocked);
        }

        [Test]
        public void Fader_FadeOutThenIn_TogglesImage()
        {
            var image = fader.GetComponent<Image>();
            RunToEnd(fader.FadeOut());
            Assert.AreEqual(1f, fader.Alpha);
            Assert.IsTrue(image.enabled);

            RunToEnd(fader.FadeIn());
            Assert.AreEqual(0f, fader.Alpha);
            Assert.IsFalse(image.enabled);
        }
    }
}
