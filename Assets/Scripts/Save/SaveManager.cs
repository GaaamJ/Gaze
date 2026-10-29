using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gaze.Core.GameFolder;
using UnityEngine;

namespace Gaze.Save
{
    // save.json 읽기·쓰기와 메모리의 SaveData(Data)를 맡음.
    // 기록 클래스(DeathRecords, RespawnPoint 등)가 Data를 고친 뒤 Save로 저장함.
    public static class SaveManager
    {
        static GameFolderPaths paths;

        // 세이브·시체 폴더 경로. 기본값은 사용자 폴더 아래 Salbyeol.
        // 테스트는 임시 루트를 주입해 실제 Salbyeol\ 폴더를 건드리지 않음.
        public static GameFolderPaths Paths
        {
            get => paths ??= GameFolderPaths.CreateDefault();
            set => paths = value;
        }

        public static string SavePath => Paths.SaveFile;
        public static string TempPath => SavePath + ".tmp";

        public static SaveData Data { get; private set; } = new SaveData();

        // 세이브 파일을 읽어 Data를 채움. 파일이 없거나 읽기에 실패하면 기본값.
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
                // 손상 파일은 여기서 지우지 않지만 다음 Save()가 기본값 기준 데이터로 덮어씀.
                Debug.LogWarning($"[SaveManager] 세이브 로드 실패, 기본값으로 시작: {path}\n{e.Message}");
                Data = new SaveData();
            }
        }

        // Data를 JSON으로 저장. 임시 파일에 쓰고 디스크에 비움 → 세이브 파일로 교체.
        // 성공 여부를 돌려줌. 실패 로그는 여기서만 남김. 호출자가 바꾼 메모리 값은 그대로 남아 다음 저장 때 함께 기록됨.
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
                    // 강제 종료 직후에도 교체된 세이브가 디스크에 남도록 OS 캐시까지 비움.
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

        // 목록에 id를 중복 없이 추가하고 저장.
        // 공백 id는 무시하고 이미 있는 id는 다시 저장하지 않음. 할 일이 없으면 저장 성공으로 봄.
        public static bool AddUnique(List<string> ids, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || ids.Contains(id))
                return true;

            ids.Add(id);
            return Save();
        }

        // 파일을 지우되 실패는 무시함(저장 실패 뒤 임시 파일 정리용).
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
