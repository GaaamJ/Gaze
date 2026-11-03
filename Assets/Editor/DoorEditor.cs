using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gaze.Area;

[CustomEditor(typeof(Door), true)]
[CanEditMultipleObjects]
class DoorEditor : Editor
{
    static readonly Regex BlockHeader = new Regex(@"^--- !u!", RegexOptions.Multiline);
    static readonly Regex ScriptGuid = new Regex(@"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})");
    static readonly Regex DoorIdField = new Regex(@"^\s*doorId: ?(.*)$", RegexOptions.Multiline);
    static readonly Regex DoorIdOverride = new Regex(@"propertyPath: doorId\s*\r?\n\s*value: ?(.*)$", RegexOptions.Multiline);
    static readonly Regex SourcePrefab = new Regex(@"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})");

    // 행선지 하나를 이루는 Scene 필드와 도착 문 id 필드 이름. 문 클래스에 있는 것만 그림.
    static readonly (string scene, string doorId)[] TargetFields =
    {
        ("targetScene", "targetDoorId"),
        ("defaultTargetScene", "defaultTargetDoorId"),
    };

    // 행선지 목록 필드 이름. 원소는 scene·doorId 필드를 가짐.
    static readonly string[] TargetListFields = { "shiftingTargets" };

    class FileScan
    {
        public DateTime written;
        public readonly HashSet<string> ids = new HashSet<string>();
        public readonly List<string> prefabs = new List<string>();
    }

    static readonly Dictionary<string, FileScan> cache = new Dictionary<string, FileScan>();
    static HashSet<string> doorScriptGuids;

    SerializedProperty doorId;
    readonly List<(SerializedProperty scene, SerializedProperty doorId)> targetProps = new List<(SerializedProperty, SerializedProperty)>();
    readonly List<SerializedProperty> targetLists = new List<SerializedProperty>();
    string[] drawnFields;

    void OnEnable()
    {
        doorId = serializedObject.FindProperty("doorId");
        targetProps.Clear();
        targetLists.Clear();
        var drawn = new List<string> { "m_Script", "doorId" };
        foreach ((string scene, string id) in TargetFields)
        {
            SerializedProperty sceneProp = serializedObject.FindProperty(scene);
            SerializedProperty idProp = serializedObject.FindProperty(id);
            if (sceneProp == null || idProp == null)
                continue;
            targetProps.Add((sceneProp, idProp));
            drawn.Add(scene);
            drawn.Add(id);
        }
        foreach (string list in TargetListFields)
        {
            SerializedProperty listProp = serializedObject.FindProperty(list);
            if (listProp == null || !listProp.isArray)
                continue;
            targetLists.Add(listProp);
            drawn.Add(list);
        }
        drawnFields = drawn.ToArray();
    }

    // 행선지마다 Scene 목록, 문 id 목록, 경고, 반대쪽 잇기 버튼을 그림. 행선지 목록은 원소마다 같은 모양에 더하기·빼기 버튼.
    // 나머지 필드는 기본 모양으로 그림.
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);

        EditorGUILayout.PropertyField(doorId);
        foreach ((SerializedProperty scene, SerializedProperty id) in targetProps)
            DrawTarget(scene, id, "");

        foreach (SerializedProperty list in targetLists)
            DrawTargetList(list);

        DrawPropertiesExcluding(serializedObject, drawnFields);
        serializedObject.ApplyModifiedProperties();
    }

    // 행선지 목록. 여러 문을 같이 고르면 목록 길이가 다를 수 있어 기본 모양으로 그림.
    void DrawTargetList(SerializedProperty list)
    {
        EditorGUILayout.Space();
        if (targets.Length > 1)
        {
            EditorGUILayout.PropertyField(list, true);
            return;
        }

        EditorGUILayout.LabelField(list.displayName, EditorStyles.boldLabel);
        int remove = -1;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i + 1}번째", EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("빼기", GUILayout.Width(48)))
                        remove = i;
                }
                DrawTarget(element.FindPropertyRelative("scene"), element.FindPropertyRelative("doorId"), $" ({i + 1}번째)");
            }
        }
        if (remove >= 0)
            list.DeleteArrayElementAtIndex(remove);
        if (GUILayout.Button("행선지 더하기"))
        {
            list.arraySize++;
            SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("scene").stringValue = "";
            added.FindPropertyRelative("doorId").stringValue = "";
        }
    }

    // 행선지 하나: Scene 목록, 문 id 목록, 경고, 반대쪽 잇기 버튼. label은 버튼 이름 뒤에 붙여 행선지를 구분함.
    void DrawTarget(SerializedProperty scene, SerializedProperty id, string label)
    {
        string scenePath = DrawTargetScene(scene);
        bool sameTarget = !scene.hasMultipleDifferentValues && !id.hasMultipleDifferentValues;

        if (sameTarget && scenePath != null)
            DrawTargetDoorId(id, scenePath);
        else
            EditorGUILayout.PropertyField(id);

        if (sameTarget)
            DrawDoorWarning(id, scenePath);
        if (targets.Length == 1)
            DrawLinkBack(id, scenePath, label);
    }

    // 상대 Room의 문 id를 목록에서 고름. 목록은 FindDoorIds가 씬 파일(열려 있으면 오브젝트)에서 읽음.
    // 지금 값이 목록에 없으면 "(없음)" 표시를 붙여 남겨 둠. 경고는 DrawDoorWarning이 띄움.
    void DrawTargetDoorId(SerializedProperty targetDoorId, string scenePath)
    {
        List<string> ids = FindDoorIds(scenePath).Where(id => !string.IsNullOrEmpty(id)).OrderBy(id => id).ToList();
        var names = new List<string> { "(없음)" };
        names.AddRange(ids);

        string current = targetDoorId.stringValue;
        int index = string.IsNullOrEmpty(current) ? 0 : names.IndexOf(current, 1);
        bool missing = index < 0;
        if (missing)
        {
            names.Add($"{current} (없음)");
            index = names.Count - 1;
        }

        EditorGUI.BeginChangeCheck();
        int picked = EditorGUILayout.Popup(new GUIContent(targetDoorId.displayName, targetDoorId.tooltip), index, names.ToArray());
        if (EditorGUI.EndChangeCheck() && !(missing && picked == names.Count - 1))
            targetDoorId.stringValue = picked == 0 ? "" : names[picked];
    }

    // 상대 문의 targetScene·targetDoorId를 이 문으로 채우는 버튼.
    // 이 문의 doorId, 행선지 Scene, 도착 문 id가 모두 있고 이 문이 저장된 씬에 있을 때만 누를 수 있음.
    // 행선지가 여럿이면 버튼도 행선지마다 하나씩, label로 구분함.
    // 누르면 지금까지 고친 값을 먼저 적용한 뒤 상대 씬을 엶.
    void DrawLinkBack(SerializedProperty targetDoorId, string scenePath, string label)
    {
        var door = (Door)target;
        Scene own = door.gameObject.scene;
        bool ready = !Application.isPlaying
            && scenePath != null
            && !string.IsNullOrEmpty(door.DoorId)
            && !string.IsNullOrEmpty(targetDoorId.stringValue)
            && own.IsValid() && !string.IsNullOrEmpty(own.path);

        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button("반대쪽 문도 이 문으로 잇기" + label))
            {
                serializedObject.ApplyModifiedProperties();
                LinkBack(door, own.name, scenePath, targetDoorId.stringValue);
                // 씬을 열고 닫으면 이번 GUI 그리기가 어긋나므로 여기서 끊음.
                GUIUtility.ExitGUI();
            }
        }
    }

    // 상대 씬을 열어(이미 열려 있으면 그대로) 그 문을 이 문으로 이음.
    // 상대는 행선지가 하나인 NormalDoor(와 자식 클래스)만 찾음.
    // 직접 연 씬은 저장하고 닫음. 이미 열려 있던 씬은 다른 수정까지 저장되지 않도록 dirty 표시만 함.
    static void LinkBack(Door door, string ownScene, string otherScenePath, string otherDoorId)
    {
        Scene other = SceneManager.GetSceneByPath(otherScenePath);
        bool openedHere = !(other.IsValid() && other.isLoaded);
        if (openedHere)
            other = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(otherScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

        try
        {
            var matches = new List<NormalDoor>();
            foreach (GameObject root in other.GetRootGameObjects())
                foreach (NormalDoor candidate in root.GetComponentsInChildren<NormalDoor>(true))
                    if (candidate != door && candidate.DoorId == otherDoorId)
                        matches.Add(candidate);

            if (matches.Count != 1)
            {
                EditorUtility.DisplayDialog("문 잇기", $"Room '{other.name}'에 doorId가 '{otherDoorId}'인 문이 {matches.Count}개다. 하나여야 한다.", "확인");
                return;
            }

            var so = new SerializedObject(matches[0]);
            SerializedProperty scene = so.FindProperty("targetScene");
            SerializedProperty id = so.FindProperty("targetDoorId");
            bool pointsElsewhere = !string.IsNullOrEmpty(id.stringValue) && (scene.stringValue != ownScene || id.stringValue != door.DoorId);
            if (pointsElsewhere && !EditorUtility.DisplayDialog("문 잇기",
                    $"'{other.name}/{otherDoorId}'는 지금 '{scene.stringValue}/{id.stringValue}'로 이어져 있다. 이 문으로 바꿀까?", "바꾸기", "취소"))
                return;

            scene.stringValue = ownScene;
            id.stringValue = door.DoorId;
            so.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(other);

            if (openedHere)
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(other);
            Debug.Log($"[DoorEditor] '{other.name}/{otherDoorId}' → '{ownScene}/{door.DoorId}'로 이었다." + (openedHere ? "" : " 열려 있던 씬이라 저장은 직접 해야 한다."), matches[0]);
        }
        finally
        {
            if (openedHere)
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(other, true);
        }
    }

    string DrawTargetScene(SerializedProperty targetScene)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        var names = new List<string> { "(없음)" };
        names.AddRange(scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)));

        string current = targetScene.stringValue;
        int index = string.IsNullOrEmpty(current) ? 0 : names.IndexOf(current, 1);
        bool missing = index < 0;
        if (missing)
        {
            names.Add($"{current} (빌드 설정에 없음)");
            index = names.Count - 1;
        }

        EditorGUI.showMixedValue = targetScene.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int picked = EditorGUILayout.Popup(new GUIContent(targetScene.displayName, targetScene.tooltip), index, names.ToArray());
        if (EditorGUI.EndChangeCheck() && !(missing && picked == names.Count - 1))
            targetScene.stringValue = picked == 0 ? "" : names[picked];
        EditorGUI.showMixedValue = false;

        if (targetScene.hasMultipleDifferentValues)
            return null;
        if (missing)
        {
            EditorGUILayout.HelpBox($"Room '{current}'이 빌드 설정에 없다.", MessageType.Warning);
            return null;
        }
        return index > 0 ? scenes[index - 1].path : null;
    }

    void DrawDoorWarning(SerializedProperty targetDoorId, string scenePath)
    {
        if (scenePath == null)
            return;
        string id = targetDoorId.stringValue;
        if (FindDoorIds(scenePath).Contains(id))
            return;
        EditorGUILayout.HelpBox($"Room '{Path.GetFileNameWithoutExtension(scenePath)}'에 doorId가 '{id}'인 Door가 없다.", MessageType.Warning);
    }

    static HashSet<string> FindDoorIds(string scenePath)
    {
        // 열려 있는 씬은 저장 전 수정이 반영되도록 파일 대신 오브젝트를 본다.
        Scene open = SceneManager.GetSceneByPath(scenePath);
        if (open.IsValid() && open.isLoaded)
        {
            var ids = new HashSet<string>();
            foreach (GameObject root in open.GetRootGameObjects())
                foreach (Door door in root.GetComponentsInChildren<Door>(true))
                    ids.Add(door.DoorId ?? "");
            return ids;
        }

        var found = new HashSet<string>();
        CollectDoorIds(scenePath, DoorScriptGuids(), new HashSet<string>(), found);
        return found;
    }

    // Door를 상속한 모든 문 클래스의 스크립트 guid.
    // 파일에는 실제 클래스의 guid만 적혀 있어 Door 하나로는 찾을 수 없음. 컴파일하면 static이 비워져 새 문 클래스도 다시 모임.
    static HashSet<string> DoorScriptGuids()
    {
        if (doorScriptGuids != null)
            return doorScriptGuids;

        doorScriptGuids = new HashSet<string>();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Door>())
        {
            if (type.IsAbstract)
                continue;
            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type)
                    doorScriptGuids.Add(guid);
            }
        }
        return doorScriptGuids;
    }

    // 씬을 열지 않고 텍스트 직렬화된 파일을 읽는다. 프리팹 인스턴스는 원본 프리팹까지 따라가 기본값과 덮어쓴 값을 모두 후보로 본다.
    static void CollectDoorIds(string assetPath, HashSet<string> scriptGuids, HashSet<string> visited, HashSet<string> found)
    {
        if (!visited.Add(assetPath) || !File.Exists(assetPath))
            return;

        FileScan scan = Scan(assetPath, scriptGuids);
        found.UnionWith(scan.ids);
        foreach (string prefabPath in scan.prefabs)
            CollectDoorIds(prefabPath, scriptGuids, visited, found);
    }

    static FileScan Scan(string assetPath, HashSet<string> scriptGuids)
    {
        DateTime written = File.GetLastWriteTimeUtc(assetPath);
        if (cache.TryGetValue(assetPath, out FileScan cached) && cached.written == written)
            return cached;

        var scan = new FileScan { written = written };
        foreach (string block in BlockHeader.Split(File.ReadAllText(assetPath)))
        {
            Match script = ScriptGuid.Match(block);
            if (script.Success && scriptGuids.Contains(script.Groups[1].Value))
            {
                Match field = DoorIdField.Match(block);
                if (field.Success)
                    scan.ids.Add(Unquote(field.Groups[1].Value));
            }

            foreach (Match over in DoorIdOverride.Matches(block))
                scan.ids.Add(Unquote(over.Groups[1].Value));

            Match source = SourcePrefab.Match(block);
            if (source.Success)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(source.Groups[1].Value);
                if (!string.IsNullOrEmpty(prefabPath))
                    scan.prefabs.Add(prefabPath);
            }
        }

        cache[assetPath] = scan;
        return scan;
    }

    static string Unquote(string value)
    {
        value = value.Trim();
        if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            return value.Substring(1, value.Length - 2).Replace("''", "'");
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            return Regex.Unescape(value.Substring(1, value.Length - 2));
        return value;
    }
}
