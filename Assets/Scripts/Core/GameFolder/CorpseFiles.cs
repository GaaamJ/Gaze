using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ProjectT.Core.GameFolder
{
    public static class CorpseFiles
    {
        const int MaxAttempts = 100;

        static readonly Regex NamePattern = new Regex(@"^Corpse(\d+)\.txt$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static string FileName(int number) => $"Corpse{number:D3}.txt";

        public static bool TryCreate(GameFolderPaths paths, out string filePath, out string error)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));

            filePath = null;
            error = null;
            string folder = paths.Corpses;

            try
            {
                if (File.Exists(folder))
                {
                    error = $"A file with the same name as the corpse folder exists: {folder}";
                    return false;
                }

                Directory.CreateDirectory(folder);

                long next = MaxNumber(folder) + 1;
                for (int attempt = 0; attempt < MaxAttempts; attempt++, next++)
                {
                    if (next > int.MaxValue)
                        break;

                    string path = Path.Combine(folder, FileName((int)next));
                    if (TryCreateEmpty(path))
                    {
                        filePath = path;
                        return true;
                    }
                }

                error = $"Could not find a free corpse file name in: {folder}";
                return false;
            }
            catch (Exception e) when (IsFileError(e))
            {
                error = e.Message;
                return false;
            }
        }

        static int MaxNumber(string folder)
        {
            int max = 0;
            foreach (string file in Directory.EnumerateFiles(folder))
            {
                Match match = NamePattern.Match(Path.GetFileName(file));
                // int 범위를 넘는 번호는 다음 번호를 만들 수 없어 세지 않는다.
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number) && number > max)
                    max = number;
            }
            return max;
        }

        // 같은 이름이 이미 있으면(다른 프로세스가 먼저 만들었거나 같은 이름의 폴더) false를 돌려 다음 번호로 넘어간다.
        static bool TryCreateEmpty(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                }
                return true;
            }
            catch (Exception e) when ((e is IOException || e is UnauthorizedAccessException) && (File.Exists(path) || Directory.Exists(path)))
            {
                return false;
            }
        }

        static bool IsFileError(Exception e) =>
            e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException;
    }
}
