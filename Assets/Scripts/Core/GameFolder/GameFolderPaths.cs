using System;
using System.IO;

namespace ProjectT.Core.GameFolder
{
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
