using UnityEditor;
using UnityEditor.SceneManagement;

// Room 씬만 열고 Play해도 Core부터 시작하게 한다. Room은 Core가 additive로 불러온다.
[InitializeOnLoad]
static class CoreScenePlayStart
{
    const string CoreScenePath = "Assets/Scenes/Core.unity";

    static CoreScenePlayStart()
    {
        // 도메인 리로드 직후에는 애셋 DB가 준비되지 않았을 수 있어 한 틱 미룬다.
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreScenePath);
    }
}
