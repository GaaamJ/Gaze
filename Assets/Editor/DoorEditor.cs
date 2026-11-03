using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gaze.Area;

[CustomEditor(typeof(PairDoor), true)]
[CanEditMultipleObjects]
class DoorEditor : Editor
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
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((PairDoor)target), typeof(MonoScript), false);

        EditorGUILayout.PropertyField(doorId);
        string scenePath = DrawTargetScene();
        bool sameTarget = !targetScene.hasMultipleDifferentValues && !targetDoorId.hasMultipleDifferentValues;

        if (sameTarget && scenePath != null)
            DrawTargetDoorId(scenePath);
        else
            EditorGUILayout.PropertyField(targetDoorId);

        if (sameTarget)
            DrawDoorWarning(scenePath);

        serializedObject.ApplyModifiedProperties();

        if (targets.Length == 1)
            DrawLinkBack(scenePath);
    }

    // 상대 Room의 문 id를 목록에서 고름. 목록은 FindDoorIds가 씬 파일(열려 있으면 오브젝트)에서 읽음.
    // 지금 값이 목록에 없으면 "(없음)" 표시를 붙여 남겨 둠. 경고는 DrawDoorWarning이 띄움.
    void DrawTargetDoorId(string scenePath)
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
    // 이 문의 doorId, targetScene, targetDoorId가 모두 있고 이 문이 저장된 씬에 있을 때만 누를 수 있음.
    void DrawLinkBack(string scenePath)
    {
        var door = (PairDoor)target;
        Scene own = door.gameObject.scene;
        bool ready = !Application.isPlaying
            && scenePath != null
            && !string.IsNullOrEmpty(door.DoorId)
            && !string.IsNullOrEmpty(targetDoorId.stringValue)
            && own.IsValid() && !string.IsNullOrEmpty(own.path);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!ready))
        {
            if (GUILayout.Button("반대쪽 문도 이 문으로 잇기"))
            {
                LinkBack(door, own.name, scenePath, targetDoorId.stringValue);
                // 씬을 열고 닫으면 이번 GUI 그리기가 어긋나므로 여기서 끊음.
                GUIUtility.ExitGUI();
            }
        }
    }

    // 상대 씬을 열어(이미 열려 있으면 그대로) 그 문을 이 문으로 이음.
    // 직접 연 씬은 저장하고 닫음. 이미 열려 있던 씬은 다른 수정까지 저장되지 않도록 dirty 표시만 함.
    static void LinkBack(PairDoor door, string ownScene, string otherScenePath, string otherDoorId)
    {
        Scene other = SceneManager.GetSceneByPath(otherScenePath);
        bool openedHere = !(other.IsValid() && other.isLoaded);
        if (openedHere)
            other = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(otherScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

        try
        {
            var matches = new List<PairDoor>();
            foreach (GameObject root in other.GetRootGameObjects())
                foreach (PairDoor candidate in root.GetComponentsInChildren<PairDoor>(true))
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
        CollectDoorIds(scenePath, DoorScriptGuid(), new HashSet<string>(), found);
        return found;
    }

    static string DoorScriptGuid()
    {
        if (doorScriptGuid != null)
            return doorScriptGuid;
        foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {nameof(PairDoor)}"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if (script != null && script.GetClass() == typeof(PairDoor))
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
