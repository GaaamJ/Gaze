using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectT.Area;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.Tests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static ProjectT.Tests.TestObjects;

namespace ProjectT.Death.Tests
{
    public class DeathAftermathTests
    {
        const string NodeId = "Death_DeathObject";
        const string PrefabName = "CorpseTemplate";

        TempSaveSandbox save;
        GameObject prefab;

        [SetUp]
        public void SetUp()
        {
            save = new TempSaveSandbox();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            prefab = new GameObject(PrefabName);
        }

        [TearDown]
        public void TearDown()
        {
            save.Dispose();
        }

        static DeathInteractable NewDeathObject(string nodeId = NodeId)
        {
            var source = new GameObject("DeathObject").AddComponent<DeathInteractable>();
            Set(source, so => so.FindProperty("deathNodeId").stringValue = nodeId);
            return source;
        }

        static DeathAftermath NewAftermath(MonoBehaviour source, Vector3 position, (GameObject prefab, Transform point)[] spawns, GameObject[] deactivate, Transform parent = null)
        {
            var go = new GameObject("Aftermath");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var aftermath = go.AddComponent<DeathAftermath>();
            Set(aftermath, so =>
            {
                so.FindProperty("source").objectReferenceValue = source;
                var list = so.FindProperty("spawns");
                list.arraySize = spawns.Length;
                for (int i = 0; i < spawns.Length; i++)
                {
                    var item = list.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("prefab").objectReferenceValue = spawns[i].prefab;
                    item.FindPropertyRelative("point").objectReferenceValue = spawns[i].point;
                }
                var off = so.FindProperty("deactivate");
                off.arraySize = deactivate.Length;
                for (int i = 0; i < deactivate.Length; i++)
                    off.GetArrayElementAtIndex(i).objectReferenceValue = deactivate[i];
            });
            return aftermath;
        }

        static GameObject[] Clones()
        {
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject)
                .Where(go => go.name == PrefabName + "(Clone)")
                .ToArray();
        }

        static Transform NewPoint(Vector3 position, float angle)
        {
            var point = new GameObject("Point").transform;
            point.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            return point;
        }

        [Test]
        public void WithRecord_SpawnsUnderOwnParentAtPointAndDeactivates()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            var parent = new GameObject("Group").transform;
            parent.position = new Vector3(10f, 20f, 0f);
            Transform point = NewPoint(new Vector3(30f, -5f, 0f), 90f);
            var aftermath = NewAftermath(source, new Vector3(1f, 2f, 0f), new[] { (prefab, point) }, new[] { source.gameObject }, parent);

            aftermath.Apply();

            GameObject[] clones = Clones();
            Assert.AreEqual(1, clones.Length);
            Assert.AreSame(parent, clones[0].transform.parent);
            Assert.AreEqual(point.position, clones[0].transform.position);
            Assert.AreEqual(point.rotation.eulerAngles.z, clones[0].transform.rotation.eulerAngles.z, 0.01f);
            Assert.IsFalse(source.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AtRoot_SpawnsAtRoot()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            Transform point = NewPoint(new Vector3(30f, -5f, 0f), 0f);
            var aftermath = NewAftermath(source, Vector3.zero, new[] { (prefab, point) }, new GameObject[0]);

            aftermath.Apply();

            GameObject[] clones = Clones();
            Assert.AreEqual(1, clones.Length);
            Assert.IsNull(clones[0].transform.parent);
            Assert.AreEqual(point.position, clones[0].transform.position);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ParentScaled_WarnsAndStillSpawnsUnderParent()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            var parent = new GameObject("ScaledGroup").transform;
            parent.localScale = new Vector3(2f, 1f, 1f);
            Transform point = NewPoint(new Vector3(6f, 4f, 0f), 0f);
            var aftermath = NewAftermath(source, Vector3.zero, new[] { (prefab, point) }, new GameObject[0], parent);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathAftermath\].*스케일"));
            aftermath.Apply();

            GameObject[] clones = Clones();
            Assert.AreEqual(1, clones.Length);
            Assert.AreSame(parent, clones[0].transform.parent);
            Assert.That(Vector3.Distance(point.position, clones[0].transform.position), Is.LessThan(0.001f));
        }

        [Test]
        public void WithoutRecord_DoesNothing()
        {
            SaveManager.Data.diedNodeIds.Add("Death_Falling001");
            var source = NewDeathObject();
            var aftermath = NewAftermath(source, Vector3.zero, new[] { (prefab, (Transform)null) }, new[] { source.gameObject });

            aftermath.Apply();

            Assert.AreEqual(0, Clones().Length);
            Assert.IsTrue(source.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PointEmpty_SpawnsAtOwnPosition()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            var own = new Vector3(4f, -7f, 0f);
            var aftermath = NewAftermath(source, own, new[] { (prefab, (Transform)null) }, new GameObject[0]);

            aftermath.Apply();

            GameObject[] clones = Clones();
            Assert.AreEqual(1, clones.Length);
            Assert.AreEqual(own, clones[0].transform.position);
        }

        [Test]
        public void NullPrefabAndNullDeactivate_SkipsWithWarnings()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            var aftermath = NewAftermath(source, Vector3.zero,
                new (GameObject, Transform)[] { (null, null), (prefab, null) },
                new GameObject[] { null, source.gameObject });

            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathAftermath\].*prefab이 빈 항목"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathAftermath\].*deactivate에 빈 항목"));
            Assert.DoesNotThrow(() => aftermath.Apply());

            Assert.AreEqual(1, Clones().Length);
            Assert.IsFalse(source.gameObject.activeSelf);
        }

        [Test]
        public void SourceEmpty_WarnsAndDoesNothing()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var target = new GameObject("Target");
            var aftermath = NewAftermath(null, Vector3.zero, new[] { (prefab, (Transform)null) }, new[] { target });

            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathAftermath\].*source가 비었거나"));
            Assert.DoesNotThrow(() => aftermath.Apply());

            Assert.AreEqual(0, Clones().Length);
            Assert.IsTrue(target.activeSelf);
        }

        [Test]
        public void SourceNotDeathSource_WarnsAndDoesNothing()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var other = new GameObject("Other").AddComponent<InputLock>();
            var aftermath = NewAftermath(other, Vector3.zero, new[] { (prefab, (Transform)null) }, new[] { other.gameObject });

            LogAssert.Expect(LogType.Warning, new Regex(@"\[DeathAftermath\].*source가 비었거나"));
            aftermath.Apply();

            Assert.AreEqual(0, Clones().Length);
            Assert.IsTrue(other.gameObject.activeSelf);
        }

        [Test]
        public void AppliedTwice_SpawnsOnce()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var source = NewDeathObject();
            var aftermath = NewAftermath(source, Vector3.zero, new[] { (prefab, (Transform)null) }, new GameObject[0]);

            aftermath.Apply();
            aftermath.Apply();

            Assert.AreEqual(1, Clones().Length);
        }

        [Test]
        public void DeathZone_IsAcceptedAsSource()
        {
            SaveManager.Data.diedNodeIds.Add("Death_Falling001");
            var zoneGo = new GameObject("Zone");
            zoneGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var zone = zoneGo.AddComponent<DeathZone>();
            Set(zone, so => so.FindProperty("deathNodeId").stringValue = "Death_Falling001");

            var zoneTarget = new GameObject("ZoneTarget");
            NewAftermath(zone, Vector3.zero, new (GameObject, Transform)[0], new[] { zoneTarget }).Apply();

            Assert.IsFalse(zoneTarget.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RoomEntryEnter_AppliesInactiveAftermathsInOwnSceneOnly()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var entry = new GameObject("RoomEntry").AddComponent<RoomEntry>();

            var source = NewDeathObject();
            var group = new GameObject("InactiveGroup");
            var inactive = NewAftermath(source, Vector3.zero, new[] { (prefab, (Transform)null) }, new[] { source.gameObject }, group.transform);
            group.SetActive(false);

            Scene other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var otherSource = NewDeathObject();
            var otherAftermath = NewAftermath(otherSource, Vector3.zero, new (GameObject, Transform)[0], new[] { otherSource.gameObject });
            SceneManager.MoveGameObjectToScene(otherSource.gameObject, other);
            SceneManager.MoveGameObjectToScene(otherAftermath.gameObject, other);

            entry.Enter();

            Assert.AreEqual(1, Clones().Length);
            Assert.AreSame(group.transform, Clones()[0].transform.parent);
            Assert.IsFalse(source.gameObject.activeSelf);
            Assert.IsTrue(otherSource.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RoomEntryEnteredTwice_SpawnsOnce()
        {
            SaveManager.Data.diedNodeIds.Add(NodeId);
            var entry = new GameObject("RoomEntry").AddComponent<RoomEntry>();
            NewAftermath(NewDeathObject(), Vector3.zero, new[] { (prefab, (Transform)null) }, new GameObject[0]);

            entry.Enter();
            entry.Enter();

            Assert.AreEqual(1, Clones().Length);
        }
    }
}
