using System;
using System.IO;
using System.Text;

namespace ProjectT.Core.GameFolder
{
    public static class ElevatorFile
    {
        public static string NewId() => Guid.NewGuid().ToString("D");

        public static bool TryNormalizeId(string raw, out string id)
        {
            id = null;
            if (raw == null) return false;
            if (!Guid.TryParseExact(raw.Trim(), "D", out var guid)) return false;
            id = guid.ToString("D");
            return true;
        }

        public static bool Write(string filePath, string id)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (!TryNormalizeId(id, out var normalized)) return false;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(normalized);
                    stream.Write(bytes, 0, bytes.Length);
                    // 발급 직후 강제 종료돼도 파일 내용이 디스크에 남도록 OS 캐시까지 비운다.
                    stream.Flush(true);
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
            {
                return false;
            }
        }

        public static bool TryReadId(string filePath, out string id)
        {
            id = null;
            if (string.IsNullOrEmpty(filePath)) return false;

            string text;
            try
            {
                if (!File.Exists(filePath)) return false;
                // UTF8 디코딩은 BOM을 제거한다. 메모장 저장으로 BOM이 붙어도 같은 값으로 읽힌다.
                text = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
            {
                return false;
            }

            return TryNormalizeId(text, out id);
        }
    }
}
