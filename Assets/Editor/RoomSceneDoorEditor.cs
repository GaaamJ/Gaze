using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gaze.Area;

[CustomEditor(typeof(RoomSceneDoor))]
[CanEditMultipleObjects]
class RoomSceneDoorEditor : Editor
{
    static readonly Regex BlockHeader = new Regex(@"^--- !u!", RegexOptions.Multiline);
    static readonly Regex ScriptGuid = new Regex(@"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})");
    static readonly Regex DoorIdField = new Regex(@"^\s*doorId: ?(.*)$", RegexOptions.Multiline);
    static readonly Regex DoorIdOverride = new Regex(@"propertyPath: doorId\s*\r?\n\s*value: ?(.*)$", RegexOptions.Multiline);
    static readonly Regex SourcePrefab = new Regex(@"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})");

    class FileScan
    {
        public DateTime written;
        public readonly HashSet<string> ids = new HashSet<string>();
        public readonly List<string> prefabs = new List<string>();
    }

    static readonly Dictionary<string, FileScan> cache = new Dictionary<string, FileScan>();
    static string doorScriptGuid;

    SerializedProperty doorId;
    SerializedProperty targetScene;
    SerializedProperty targetDoorId;

    void OnEnable()
    {
        doorId = serializedObject.FindProperty("doorId");
        targetScene = serializedObject.FindProperty("targetScene");
        targetDoorId = serializedObject.FindProperty("targetDoorId");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((RoomSceneDoor)target), typeof(MonoScript), false);

        EditorGUILayout.PropertyField(doorId);
        string scenePath = DrawTargetScene();
        EditorGUILayout.PropertyField(targetDoorId);

        if (!targetScene.hasMultipleDifferentValues && !targetDoorId.hasMultipleDifferentValues)
            DrawDoorWarning(scenePath);

        serializedObject.ApplyModifiedProperties();
    }

    string DrawTargetScene()
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

    void DrawDoorWarning(string scenePath)
    {
        if (scenePath == null)
            return;
        string id = targetDoorId.stringValue;
        if (FindDoorIds(scenePath).Contains(id))
            return;
        EditorGUILayout.HelpBox($"Room '{Path.GetFileNameWithoutExtension(scenePath)}'에 doorId가 '{id}'인 RoomSceneDoor가 없다.", MessageType.Warning);
    }

    static HashSet<string> FindDoorIds(string scenePath)
    {
        // 열려 있는 씬은 저장 전 수정이 반영되도록 파일 대신 오브젝트를 본다.
        Scene open = SceneManager.GetSceneByPath(scenePath);
        if (open.IsValid() && open.isLoaded)
        {
            var ids = new HashSet<string>();
            foreach (GameObject root in open.GetRootGameObjects())
                foreach (RoomSceneDoor door in root.GetComponentsInChildren<RoomSceneDoor>(true))
                    ids.Add(door.DoorId ?? "");
            return ids;
        }

        var found = new HashSet<string>();
        CollectDoorIds(scenePath, DoorScriptGuid(), new HashSet<string>(), found);
        return found;
    }

    static string DoorScriptGuid()
    {
        if (doorScriptGuid != null)
            return doorScriptGuid;
        foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {nameof(RoomSceneDoor)}"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if (script != null && script.GetClass() == typeof(RoomSceneDoor))
                return doorScriptGuid = guid;
        }
        return doorScriptGuid = "";
    }

    // 씬을 열지 않고 텍스트 직렬화된 파일을 읽는다. 프리팹 인스턴스는 원본 프리팹까지 따라가 기본값과 덮어쓴 값을 모두 후보로 본다.
    static void CollectDoorIds(string assetPath, string scriptGuid, HashSet<string> visited, HashSet<string> found)
    {
        if (!visited.Add(assetPath) || !File.Exists(assetPath))
            return;

        FileScan scan = Scan(assetPath, scriptGuid);
        found.UnionWith(scan.ids);
        foreach (string prefabPath in scan.prefabs)
            CollectDoorIds(prefabPath, scriptGuid, visited, found);
    }

    static FileScan Scan(string assetPath, string scriptGuid)
    {
        DateTime written = File.GetLastWriteTimeUtc(assetPath);
        if (cache.TryGetValue(assetPath, out FileScan cached) && cached.written == written)
            return cached;

        var scan = new FileScan { written = written };
        foreach (string block in BlockHeader.Split(File.ReadAllText(assetPath)))
        {
            Match script = ScriptGuid.Match(block);
            if (script.Success && script.Groups[1].Value == scriptGuid)
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
