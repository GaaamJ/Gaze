using System.Collections.Generic;
using ProjectT.Core.GameFolder;

namespace ProjectT.Data
{
    public static class AreaFloorFolders
    {
        public static List<string> Names(IEnumerable<AreaData> areas)
        {
            var names = new List<string>();
            if (areas == null) return names;

            foreach (var area in areas)
            {
                if (area != null)
                    names.Add(area.FloorFolderName);
            }
            return names;
        }

        public static string FloorFolder(GameFolderPaths paths, AreaData area) =>
            paths.FloorFolder(area.FloorFolderName);

        public static string ElevatorFile(GameFolderPaths paths, AreaData area) =>
            paths.ElevatorFilePath(area.FloorFolderName);

        public static EnsureResult EnsureStructure(GameFolderPaths paths, IEnumerable<AreaData> areas) =>
            GameFolderStructure.EnsureStructure(paths, Names(areas));
    }
}
