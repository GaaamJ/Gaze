using System;
using System.IO;
using Gaze.Core.GameFolder;
using Gaze.Save;

namespace Gaze.Core.Tests
{
    // 세이브를 읽거나 쓰는 테스트는 반드시 이것을 거친다. 빠뜨리면 실제 Salbyeol\save.json에 쓴다.
    // 이 asmdef는 Assembly-CSharp(GameSession)와 Assets/Editor/Tests의 같은 이름 헬퍼를 참조할 수 없어 따로 둔다.
    internal sealed class TempSaveSandbox : IDisposable
    {
        public string Base { get; }
        public GameFolderPaths Paths { get; }

        public TempSaveSandbox()
        {
            Base = Path.Combine(Path.GetTempPath(), "Gaze.Tests." + Guid.NewGuid().ToString("N"));
            Paths = new GameFolderPaths(Path.Combine(Base, GameFolderPaths.RootFolderName));
            SaveManager.Paths = Paths;
            SaveManager.Load();
        }

        public void Dispose()
        {
            SaveManager.Paths = null;
            SaveManager.Load();
            if (Directory.Exists(Base))
                Directory.Delete(Base, true);
        }
    }
}
