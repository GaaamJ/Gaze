using System;
using Yarn.Unity;
using Gaze.Save;

namespace Gaze.Dialogue
{
    // 플레이어 이름을 Yarn에 넘기는 함수. Yarn에서 {pc_user_name()}, {player_name()}으로 부름.
    // Yarn 변수는 죽으면 지워지므로(CoreEntry가 ResetVariables를 부름) 변수에 담지 않고 부를 때마다 읽음.
    // [YarnFunction]은 Yarn Spinner가 자동으로 등록함. Yarn 파일을 가져올 때도 이 선언을 보고 함수를 앎.
    public static class PlayerNameFunctions
    {
        // 이 PC의 Windows 사용자 이름.
        [YarnFunction("pc_user_name")]
        public static string PcUserName() => Environment.UserName;

        // 정한 이름. 아직 없으면 PC 사용자 이름.
        [YarnFunction("player_name")]
        public static string Name() => PlayerName.HasName ? PlayerName.Saved : PcUserName();
    }
}
