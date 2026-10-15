using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProjectT.Save;
using UnityEditor;
using UnityEngine;

public static class SaveManagerTools
{
    const string ResetMenu = "Tools/데이터 초기화";
    const string Tag = "[SaveManagerTools]";

    [MenuItem(ResetMenu, true)]
    static bool ValidateResetData() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem(ResetMenu, false, 0)]
    static void ResetData()
    {
        string root = SaveManager.Paths.Root;
        string savePath = Path.GetFullPath(SaveManager.SavePath);
        bool saveInsideRoot = savePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        var targets = new List<string> { root };
        if (!saveInsideRoot)
            targets.Add(savePath);

        var message = new StringBuilder("다음 대상을 삭제합니다.\n\n");
        foreach (var target in targets)
            message.AppendLine(target);
        message.Append("\n되돌릴 수 없습니다. 계속할까요?");

        if (!EditorUtility.DisplayDialog("데이터 초기화", message.ToString(), "삭제", "취소"))
        {
            Debug.Log($"{Tag} 데이터 초기화 취소");
            return;
        }

        foreach (var target in targets)
            DeleteTarget(target);
    }

    static void DeleteTarget(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                ClearReadOnly(new DirectoryInfo(path));
                Directory.Delete(path, true);
                Debug.Log($"{Tag} 삭제 완료: {path}");
            }
            else if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                Debug.Log($"{Tag} 삭제 완료: {path}");
            }
            else
            {
                Debug.Log($"{Tag} 없음: {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{Tag} 삭제 실패: {path}\n{e.Message}");
        }
    }

    // Directory.Delete는 읽기 전용 파일이 하나라도 있으면 UnauthorizedAccessException으로 중단된다.
    static void ClearReadOnly(DirectoryInfo dir)
    {
        dir.Attributes &= ~FileAttributes.ReadOnly;
        foreach (var info in dir.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            info.Attributes &= ~FileAttributes.ReadOnly;
    }

    [MenuItem("Tools/Save/Open Save Folder")]
    static void OpenSaveFolder()
    {
        string root = SaveManager.Paths.Root;
        if (!Directory.Exists(root))
        {
            Debug.Log($"{Tag} 게임 폴더 없음: {root}");
            return;
        }
        EditorUtility.RevealInFinder(root);
    }
}
