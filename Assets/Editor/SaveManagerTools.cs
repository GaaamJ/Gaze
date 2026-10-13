using System.IO;
using ProjectT.Save;
using UnityEditor;
using UnityEngine;

public static class SaveManagerTools
{
    [MenuItem("Tools/Save/Delete Save File")]
    private static void DeleteSaveFile()
    {
        string path = SaveManager.SavePath;
        if (!File.Exists(path))
        {
            Debug.Log($"[SaveManagerTools] 세이브 없음: {path}");
            return;
        }
        File.Delete(path);
        Debug.Log($"[SaveManagerTools] 삭제 완료: {path}");
    }

    [MenuItem("Tools/Save/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        string root = SaveManager.Paths.Root;
        if (!Directory.Exists(root))
        {
            Debug.Log($"[SaveManagerTools] 게임 폴더 없음: {root}");
            return;
        }
        EditorUtility.RevealInFinder(root);
    }
}
