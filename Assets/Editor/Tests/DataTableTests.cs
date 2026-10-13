using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectT.Area;
using ProjectT.Data;

namespace ProjectT.Data.Tests
{
    public class DataTableTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        T NewAsset<T>(System.Action<SerializedObject> setup) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            var so = new SerializedObject(asset);
            setup(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        AreaData NewArea(string areaId) =>
            NewAsset<AreaData>(so => so.FindProperty("areaId").stringValue = areaId);

        AreaMarker NewMarker(AreaData area, string markerId, Vector3 position)
        {
            var go = new GameObject($"Marker_{markerId}");
            created.Add(go);
            go.transform.position = position;
            var marker = go.AddComponent<AreaMarker>();
            var so = new SerializedObject(marker);
            so.FindProperty("area").objectReferenceValue = area;
            so.FindProperty("markerId").stringValue = markerId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return marker;
        }

        AreaMarkerRegistry NewRegistry(params AreaMarker[] markers)
        {
            var go = new GameObject("Registry");
            created.Add(go);
            var registry = go.AddComponent<AreaMarkerRegistry>();
            registry.Rebuild(markers);
            return registry;
        }

        [Test]
        public void NodeNames_FollowDeathIdRule()
        {
            Assert.AreEqual("Death_FallingRock", DialogueNodeNames.Death("FallingRock"));
            Assert.AreEqual("Death_FallingRock_Refuse", DialogueNodeNames.DeathRefuse("FallingRock"));
            Assert.AreEqual("Death_CloseWindow_Retrigger", DialogueNodeNames.DeathRetrigger("CloseWindow"));
            Assert.AreEqual("Death_CloseWindow_Sign", DialogueNodeNames.DeathSign("CloseWindow"));
            Assert.AreEqual("Death_ElevatorFileTamper_Sign_Delete", DialogueNodeNames.DeathSign("ElevatorFileTamper", "Delete"));
        }

        [Test]
        public void FindMissing_ReturnsOnlyAbsentNonEmptyNodesOnce()
        {
            var existing = new HashSet<string> { "A", "B" };
            var missing = YarnNodeCheck.FindMissing(new[] { "A", "", null, "  ", "C", "C", "B", "D" }, existing);
            CollectionAssert.AreEqual(new[] { "C", "D" }, missing);
        }

        [Test]
        public void ProjectNodeNames_ContainExistingPrototypeNode()
        {
            var names = YarnNodeCheck.LoadProjectNodeNames();
            Assert.Contains("Temp_Respawn", new List<string>(names));
            Assert.IsFalse(names.Contains("Death_NoSuchNode"));
        }

        [Test]
        public void MarkerIndex_SeparatesAreasAndRejectsDuplicates()
        {
            var index = new MarkerIndex<int>();
            Assert.IsTrue(index.TryAdd("Temp", "Respawn", 1));
            Assert.IsTrue(index.TryAdd("UndergroundParking", "Respawn", 2));
            Assert.IsFalse(index.TryAdd("Temp", "Respawn", 3));
            Assert.IsFalse(index.TryAdd("", "Respawn", 4));

            Assert.IsTrue(index.TryGet("Temp", "Respawn", out var temp));
            Assert.AreEqual(1, temp);
            Assert.IsTrue(index.TryGet("UndergroundParking", "Respawn", out var parking));
            Assert.AreEqual(2, parking);
            Assert.IsFalse(index.TryGet("Temp", "Start", out _));
            Assert.IsFalse(index.TryGet(null, "Respawn", out _));
        }

        [Test]
        public void Registry_ReturnsMarkerPositionByAreaAndMarkerId()
        {
            var temp = NewArea("Temp");
            var parking = NewArea("UndergroundParking");
            var registry = NewRegistry(
                NewMarker(temp, "Respawn", new Vector3(1, 2, 0)),
                NewMarker(parking, "Respawn", new Vector3(5, 6, 0)));

            Assert.IsTrue(registry.TryGetPosition(temp, "Respawn", out var tempPos));
            Assert.AreEqual(new Vector3(1, 2, 0), tempPos);
            Assert.IsTrue(registry.TryGetPosition(parking, "Respawn", out var parkingPos));
            Assert.AreEqual(new Vector3(5, 6, 0), parkingPos);
            Assert.IsFalse(registry.TryGetPosition(temp, "Start", out _));
            Assert.IsFalse(registry.TryGetPosition(null, "Respawn", out _));
        }

        [Test]
        public void Registry_KeepsFirstMarkerOnDuplicateId()
        {
            var temp = NewArea("Temp");
            var first = NewMarker(temp, "Start", Vector3.zero);
            var second = NewMarker(temp, "Start", Vector3.one);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Temp/Start"));
            var registry = NewRegistry(first, second);

            Assert.IsTrue(registry.TryGet(temp, "Start", out var marker));
            Assert.AreSame(first, marker);
        }

        [Test]
        public void Registry_RangeMarkerExposesColliderBounds()
        {
            var temp = NewArea("Temp");
            var marker = NewMarker(temp, "ElevatorFront", new Vector3(3, 0, 0));
            var box = marker.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2, 4);
            Physics2D.SyncTransforms();

            Assert.IsTrue(marker.TryGetBounds(out var bounds));
            Assert.AreEqual(3f, bounds.center.x, 0.001f);
            Assert.AreEqual(2f, bounds.size.x, 0.001f);
            Assert.AreEqual(4f, bounds.size.y, 0.001f);
            Assert.IsFalse(NewMarker(temp, "Start", Vector3.zero).TryGetBounds(out _));
        }

        [Test]
        public void DeathData_WarnsOnRefuseRuleViolations()
        {
            var metaRefusing = NewAsset<DeathData>(so =>
            {
                so.FindProperty("id").stringValue = "CloseWindow";
                so.FindProperty("triggerKind").enumValueIndex = (int)DeathTriggerKind.Metafiction;
                so.FindProperty("condition").enumValueIndex = (int)DeathCondition.CloseWindow;
                so.FindProperty("refuses").boolValue = true;
                so.FindProperty("refuseNode").stringValue = "Death_CloseWindow_Refuse";
            });
            var warnings = metaRefusing.CollectRuleWarnings();
            Assert.That(warnings, Has.Some.Contains("거부할 수 없다"));
            Assert.That(warnings, Has.Some.Contains("거부 대사 노드를 두지 않는다"));

            var valid = NewAsset<DeathData>(so =>
            {
                so.FindProperty("id").stringValue = "FallingRock";
                so.FindProperty("triggerKind").enumValueIndex = (int)DeathTriggerKind.Location;
                so.FindProperty("condition").enumValueIndex = (int)DeathCondition.EnterArea;
                so.FindProperty("refuses").boolValue = true;
                so.FindProperty("refuseNode").stringValue = "Death_FallingRock_Refuse";
            });
            CollectionAssert.IsEmpty(valid.CollectRuleWarnings());
        }

        [Test]
        public void DeathData_WarnsWhenNonRefusingHasNoRetrigger()
        {
            var password = NewAsset<DeathData>(so =>
            {
                so.FindProperty("id").stringValue = "PasswordWrong";
                so.FindProperty("triggerKind").enumValueIndex = (int)DeathTriggerKind.Investigate;
                so.FindProperty("condition").enumValueIndex = (int)DeathCondition.InvestigateObject;
            });
            Assert.That(password.CollectRuleWarnings(), Has.Some.Contains("재발동 대사 노드가 비어"));
        }

        [Test]
        public void ObjectData_WarnsWhenLinkOnNonLinkKind()
        {
            var clear = NewAsset<ObjectData>(so =>
            {
                so.FindProperty("id").stringValue = "Clear";
                so.FindProperty("kind").enumValueIndex = (int)ObjectKind.Clear;
                so.FindProperty("link").stringValue = "Something";
            });
            Assert.That(clear.CollectRuleWarnings(), Has.Some.Contains("연결을 두지 않는다"));

            var death = NewAsset<ObjectData>(so =>
            {
                so.FindProperty("id").stringValue = "DeathObject";
                so.FindProperty("kind").enumValueIndex = (int)ObjectKind.Death;
                so.FindProperty("link").stringValue = "DeathObject";
            });
            CollectionAssert.IsEmpty(death.CollectRuleWarnings());
        }

        [Test]
        public void GapPuzzle_DangerousNeedsDeathTrigger()
        {
            var dangerous = NewAsset<GapPuzzleData>(so =>
            {
                so.FindProperty("id").stringValue = "DangerousGap";
                so.FindProperty("kind").enumValueIndex = (int)GapKind.Dangerous;
            });
            Assert.That(dangerous.CollectRuleWarnings(), Has.Some.Contains("사망 트리거 id가 비어"));
        }

        [Test]
        public void PasswordDoor_WarnsOnBadAnswers()
        {
            var door = NewAsset<PasswordDoorPuzzleData>(so =>
            {
                so.FindProperty("id").stringValue = "PasswordDoor";
                so.FindProperty("wrongDeathTriggerId").stringValue = "PasswordWrong";
                var answers = so.FindProperty("answerIndices");
                answers.GetArrayElementAtIndex(0).intValue = 2;
                answers.GetArrayElementAtIndex(1).intValue = 2;
            });
            Assert.That(door.CollectRuleWarnings(), Has.Some.Contains("같은 문양"));
        }

        [Test]
        public void DeathSettings_RefusalAreaIsWiderThanTrigger()
        {
            var settings = ScriptableObject.CreateInstance<DeathSettings>();
            created.Add(settings);
            var size = settings.RefusalAreaSize(new Vector2(2, 1));
            Assert.Greater(size.x, 2f);
            Assert.Greater(size.y, 1f);
            Assert.AreEqual(settings.RefusalAreaMargin * 2f, size.x - 2f, 0.0001f);
        }
    }
}
