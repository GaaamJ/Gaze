using Yarn.Unity;
using ProjectT.Player;

namespace ProjectT.Death
{
    // DeathRefusal이 거부 대사 뒤 같은 오브젝트에서 GetComponents로 모아 차례로 기다린다.
    public interface IRefusalReaction
    {
        YarnTask Run(PlayerMovement laris);
    }
}
