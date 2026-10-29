using System;
using System.IO;

namespace ProjectT.Core.GameFolder
{
    // 게임이 파일을 두는 폴더의 경로 모음. 루트(사용자 폴더 아래 Salbyeol) 아래에 세이브 파일과 시체 폴더가 있음.
    // SaveManager.Paths가 들고 있음.
    public sealed class GameFolderPaths
    {
        public const string RootFolderName = "Salbyeol";
        public const string CorpsesFolderName = "Corpses";
        public const string SaveFileName = "save.json";

        public static string DefaultRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), RootFolderName);

        public static GameFolderPaths CreateDefault() => new GameFolderPaths(DefaultRoot);

        public string Root { get; }
        public string Corpses { get; }
        public string SaveFile { get; }

        public GameFolderPaths(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Root path is empty.", nameof(root));

            Root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Corpses = Path.Combine(Root, CorpsesFolderName);
            SaveFile = Path.Combine(Root, SaveFileName);
        }
    }
}
