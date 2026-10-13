using System;
using System.IO;
using System.Text;
using ProjectT.Core.GameFolder;
using UnityEngine;

namespace ProjectT.Save
{
    public static class SaveManager
    {
        static GameFolderPaths paths;

        // 테스트는 임시 루트를 주입해 실제 Salbyeol\ 폴더를 건드리지 않는다.
        public static GameFolderPaths Paths
        {
            get => paths ??= GameFolderPaths.CreateDefault();
            set => paths = value;
        }

        public static string SavePath => Paths.SaveFile;
        public static string TempPath => SavePath + ".tmp";

        public static SaveData Data { get; private set; } = new SaveData();

        public static void Load()
        {
            string path = SavePath;
            if (!File.Exists(path))
            {
                Data = new SaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (Exception e)
            {
                // 손상 파일은 지우지 않는다. 플레이어가 원본을 확인할 여지를 남긴다.
                Debug.LogWarning($"[SaveManager] 세이브 로드 실패, 기본값으로 시작: {path}\n{e.Message}");
                Data = new SaveData();
            }
        }

        public static bool Save()
        {
            string path = SavePath;
            string temp = TempPath;
            try
            {
                Directory.CreateDirectory(Paths.Root);
                string json = JsonUtility.ToJson(Data, true);

                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    // 강제 종료 직후에도 교체된 세이브가 디스크에 남도록 OS 캐시까지 비운다.
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 세이브 저장 실패: {path}\n{e.Message}");
                TryDelete(temp);
                return false;
            }
        }

        static void TryDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception)
            {
            }
        }
    }
}
