using System;
using Yarn.Unity;
using Gaze.Save;

namespace Gaze.Dialogue
{
    // 플레이어 이름을 Yarn에 넘기는 함수. Yarn에서 {pc_user_name()}, {player_name()}으로 부름.
    // Yarn 변수는 죽으면 지워지므로(CoreEntry가 ResetVariables를 부름) 변수에 담지 않고 부를 때마다 읽음.
    // [YarnFunction]은 Yarn Spinner가 자동으로 등록함. Yarn 파일을 가져올 때도 이 선언을 보고 함수를 앎.
    // Yarn에 넘기는 값은 이름 색 태그로 감쌈. 이름이 나오는 모든 대사가 손으로 태그를 달지 않아도 같은 색이 됨.
    public static class PlayerNameFunctions
    {
        // 대사 속 이름 색. TMP <color> 태그에 그대로 들어감.
        const string NameColor = "#FFD27A";

        // 이 PC의 Windows 사용자 이름. 색 태그 없는 원본. 저장할 때는 이걸 씀.
        public static string PcUserName() => Environment.UserName;

        // 대사에 찍을 이름. 색 태그로 감쌈.
        static string Colored(string name) => $"<color={NameColor}>{name}</color>";

        // 이 PC의 Windows 사용자 이름.
        [YarnFunction("pc_user_name")]
        public static string PcUserNameForLine() => Colored(PcUserName());

        // 정한 이름. 아직 없으면 PC 사용자 이름.
        [YarnFunction("player_name")]
        public static string Name() => Colored(PlayerName.HasName ? PlayerName.Saved : PcUserName());
    }
}
