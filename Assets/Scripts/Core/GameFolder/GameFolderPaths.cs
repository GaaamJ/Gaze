using System;
using System.IO;

namespace ProjectT.Core.GameFolder
{
    public sealed class GameFolderPaths
    {
        public const string RootFolderName = "Salbyeol";
        public const string AreasFolderName = "Areas";
        public const string CorpsesFolderName = "Corpses";
        public const string SaveFileName = "save.json";
        public const string ElevatorFileName = "elevator.ev";

        public static string DefaultRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), RootFolderName);

        public static GameFolderPaths CreateDefault() => new GameFolderPaths(DefaultRoot);

        public string Root { get; }
        public string Areas { get; }
        public string Corpses { get; }
        public string SaveFile { get; }

        public GameFolderPaths(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Root path is empty.", nameof(root));

            Root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Areas = Path.Combine(Root, AreasFolderName);
            Corpses = Path.Combine(Root, CorpsesFolderName);
            SaveFile = Path.Combine(Root, SaveFileName);
        }

        public string FloorFolder(string folderName)
        {
            if (!TryNormalizeFolderName(folderName, out var name))
                throw new ArgumentException($"Invalid floor folder name: '{folderName}'.", nameof(folderName));
            return Path.Combine(Areas, name);
        }

        public string ElevatorFilePath(string floorFolderName) =>
            Path.Combine(FloorFolder(floorFolderName), ElevatorFileName);

        public static bool TryNormalizeFolderName(string raw, out string name)
        {
            name = null;
            if (raw == null) return false;

            var trimmed = raw.Trim().TrimEnd('\\', '/').Trim();
            if (trimmed.Length == 0 || trimmed == "." || trimmed == "..") return false;
            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            // GetInvalidFileNameChars는 플랫폼마다 달라 Windows 기준 구분자를 따로 막는다.
            if (trimmed.IndexOf('\\') >= 0 || trimmed.IndexOf('/') >= 0) return false;

            name = trimmed;
            return true;
        }
    }
}
