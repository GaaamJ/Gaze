using System;
using System.IO;
using Gaze.Core.GameFolder;
using Gaze.Save;
using Gaze.Session;

namespace Gaze.Tests
{
    // 세이브를 읽거나 쓰는 테스트는 반드시 이것을 거친다. 빠뜨리면 실제 Salbyeol\save.json에 쓴다.
    // Tests/EditMode(Gaze.Core.Tests)는 이 어셈블리를 참조할 수 없어 같은 이름의 헬퍼를 따로 둔다.
    internal sealed class TempSaveSandbox : IDisposable
    {
        public string Base { get; }
        public GameFolderPaths Paths { get; }

        public TempSaveSandbox()
        {
            Base = Path.Combine(Path.GetTempPath(), "Gaze.Tests." + Guid.NewGuid().ToString("N"));
            Paths = new GameFolderPaths(Path.Combine(Base, GameFolderPaths.RootFolderName));
            SaveManager.Paths = Paths;
            GameSession.Reload();
        }

        public void Dispose()
        {
            SaveManager.Paths = null;
            // 실제 세이브 기준으로 되돌리되 Begin을 부르지 않는다. Begin은 실제 세이브의 종료 상태를 None으로 덮어쓴다.
            GameSession.Reload();
            if (Directory.Exists(Base))
                Directory.Delete(Base, true);
        }
    }
}
