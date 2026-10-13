using System.Reflection;
using NUnit.Framework;
using ProjectT.Data;
using ProjectT.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectT.Death.Tests
{
    public class DeathTriggerTests
    {
        DeathData data;
        DeathTrigger trigger;
        PlayerMovement laris;
        BoxCollider2D larisBody;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            data = ScriptableObject.CreateInstance<DeathData>();

            var area = new GameObject("DeathArea");
            area.AddComponent<BoxCollider2D>();
            trigger = area.AddComponent<DeathTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("deathData").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            var larisGo = new GameObject("Laris");
            laris = larisGo.AddComponent<PlayerMovement>();
            larisBody = larisGo.AddComponent<BoxCollider2D>();
        }

        [TearDown]
        public void TearDown()
        {
            DeathSequence.End();
            Object.DestroyImmediate(data);
        }

        static void Call(Object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [Test]
        public void LarisBodyEnters_BeginsDeath()
        {
            DeathData started = null;
            void OnStarted(DeathData d) => started = d;
            DeathSequence.Started += OnStarted;
            try
            {
                Call(trigger, "OnTriggerEnter2D", larisBody);
                Assert.IsTrue(DeathSequence.IsActive);
                Assert.AreSame(data, started);
            }
            finally
            {
                DeathSequence.Started -= OnStarted;
            }
        }

        [Test]
        public void DetectCircleAndOtherBodies_AreIgnored()
        {
            var detect = laris.gameObject.AddComponent<CircleCollider2D>();
            detect.isTrigger = true;
            Call(trigger, "OnTriggerEnter2D", detect);

            var box = new GameObject("Box");
            box.AddComponent<Rigidbody2D>();
            Call(trigger, "OnTriggerEnter2D", box.AddComponent<BoxCollider2D>());
            Call(trigger, "OnTriggerEnter2D", new GameObject("Static").AddComponent<BoxCollider2D>());

            Assert.IsFalse(DeathSequence.IsActive);
        }

        [Test]
        public void MissingData_DoesNotBeginDeath()
        {
            var so = new SerializedObject(trigger);
            so.FindProperty("deathData").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Call(trigger, "OnTriggerEnter2D", larisBody);
            Assert.IsFalse(DeathSequence.IsActive);
        }

        [Test]
        public void Reset_TurnsColliderIntoTrigger()
        {
            var area = trigger.GetComponent<Collider2D>();
            area.isTrigger = false;
            Call(trigger, "Reset");
            Assert.IsTrue(area.isTrigger);
        }

        [Test]
        public void TryFrom_AcceptsOnlyLarisBody()
        {
            Assert.IsTrue(PlayerMovement.TryFrom(larisBody, out PlayerMovement found));
            Assert.AreSame(laris, found);

            var detect = laris.gameObject.AddComponent<CircleCollider2D>();
            detect.isTrigger = true;
            Assert.IsFalse(PlayerMovement.TryFrom(detect, out found));
            Assert.IsNull(found);

            var box = new GameObject("Box");
            box.AddComponent<Rigidbody2D>();
            Assert.IsFalse(PlayerMovement.TryFrom(box.AddComponent<BoxCollider2D>(), out _));
            Assert.IsFalse(PlayerMovement.TryFrom(new GameObject("Static").AddComponent<BoxCollider2D>(), out _));
            Assert.IsFalse(PlayerMovement.TryFrom(null, out _));
        }
    }
}
