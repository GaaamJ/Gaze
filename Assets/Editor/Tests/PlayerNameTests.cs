using NUnit.Framework;
using Gaze.Dialogue;
using Gaze.Save;
using Gaze.Tests;

namespace Gaze.Save.Tests
{
    // 플레이어 이름이 공백을 지운 채 바로 저장되고, 없을 때는 PC 사용자 이름을 쓰는지 본다.
    public class PlayerNameTests
    {
        TempSaveSandbox save;

        [SetUp]
        public void SetUp() => save = new TempSaveSandbox();

        [TearDown]
        public void TearDown() => save.Dispose();

        [Test]
        public void Set_TrimsAndSaves()
        {
            Assert.IsTrue(PlayerName.Set("  별  "));

            Assert.AreEqual("별", PlayerName.Saved);
            SaveManager.Load();
            Assert.AreEqual("별", SaveManager.Data.playerName);
        }

        [Test]
        public void Set_Blank_IsRejected()
        {
            Assert.IsFalse(PlayerName.Set("   "));
            Assert.IsFalse(PlayerName.Set(null));
            Assert.IsFalse(PlayerName.HasName);
        }

        [Test]
        public void Name_WithoutSavedName_IsPcUserName()
        {
            Assert.AreEqual(PlayerNameFunctions.PcUserName(), PlayerNameFunctions.Name());
        }

        [Test]
        public void Name_WithSavedName_IsSavedName()
        {
            PlayerName.Set("별");

            Assert.AreEqual("별", PlayerNameFunctions.Name());
        }
    }
}
