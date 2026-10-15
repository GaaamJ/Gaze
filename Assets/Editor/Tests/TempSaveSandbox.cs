using System;
using System.IO;
using ProjectT.Core.GameFolder;
using ProjectT.Save;
using ProjectT.Session;

namespace ProjectT.Tests
{
    // 세이브를 읽거나 쓰는 테스트는 반드시 이것을 거친다. 빠뜨리면 실제 Salbyeol\save.json에 쓴다.
    // Tests/EditMode(ProjectT.Core.Tests)는 이 어셈블리를 참조할 수 없어 같은 이름의 헬퍼를 따로 둔다.
    internal sealed class TempSaveSandbox : IDisposable
    {
        public string Base { get; }
        public GameFolderPaths Paths { get; }

        public TempSaveSandbox()
        {
            Base = Path.Combine(Path.GetTempPath(), "ProjectT.Tests." + Guid.NewGuid().ToString("N"));
            Paths = new GameFolderPaths(Path.Combine(Base, GameFolderPaths.RootFolderName));
            SaveManager.Paths = Paths;
            GameSession.Begin();
        }

        public void Dispose()
        {
            SaveManager.Paths = null;
            // SaveManager.Data와 GameSession.Investigated를 실제 세이브 기준으로 되돌린다(Begin이 Load를 포함한다).
            GameSession.Begin();
            if (Directory.Exists(Base))
                Directory.Delete(Base, true);
        }
    }
}
