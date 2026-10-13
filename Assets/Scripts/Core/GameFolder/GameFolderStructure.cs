using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectT.Core.GameFolder
{
    public readonly struct FolderFailure
    {
        public readonly string Path;
        public readonly string Reason;

        public FolderFailure(string path, string reason)
        {
            Path = path;
            Reason = reason;
        }
    }

    public sealed class EnsureResult
    {
        public List<string> Created { get; } = new List<string>();
        public List<FolderFailure> Failed { get; } = new List<FolderFailure>();
        public List<string> InvalidFloorNames { get; } = new List<string>();

        public bool Succeeded => Failed.Count == 0 && InvalidFloorNames.Count == 0;
    }

    public static class GameFolderStructure
    {
        public static EnsureResult EnsureStructure(GameFolderPaths paths, IEnumerable<string> floorFolderNames)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));

            var result = new EnsureResult();
            var targets = new List<string> { paths.Root, paths.Areas };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (floorFolderNames != null)
            {
                foreach (var raw in floorFolderNames)
                {
                    if (!GameFolderPaths.TryNormalizeFolderName(raw, out var name))
                    {
                        result.InvalidFloorNames.Add(raw);
                        continue;
                    }
                    if (seen.Add(name))
                        targets.Add(paths.FloorFolder(name));
                }
            }

            targets.Add(paths.Corpses);

            foreach (var target in targets)
                EnsureDirectory(target, result);

            return result;
        }

        static void EnsureDirectory(string path, EnsureResult result)
        {
            if (Directory.Exists(path)) return;

            if (File.Exists(path))
            {
                result.Failed.Add(new FolderFailure(path, "A file with the same name exists."));
                return;
            }

            try
            {
                Directory.CreateDirectory(path);
                result.Created.Add(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                result.Failed.Add(new FolderFailure(path, e.Message));
            }
        }
    }
}
