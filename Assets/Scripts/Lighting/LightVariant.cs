using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gaze.Lighting
{
    // 방이 밝은지에 따라 오브젝트 둘 중 하나만 켬. 밝을 때 모습(lit)과 어두울 때 모습(dark)을 나눠 둠.
    // Ex. 어두울 땐 DeathResponse가 붙은 칼, 밝아지면 DialogueResponse만 붙은 평범한 칼.
    // 같은 Room 씬의 RoomLight를 봄. 들어올 때 한 번 맞추고, 방 안에서 불이 켜지면 바로 밝은 쪽으로 바꿈.
    // lit·dark는 이 오브젝트의 자식(또는 다른 오브젝트)이어야 함. 이 오브젝트 자신이나 부모를 넣으면 꺼질 때 이 부품도 꺼짐.
    public class LightVariant : MonoBehaviour
    {
        [Tooltip("방이 밝을 때만 켜는 오브젝트. 비우면 밝을 땐 아무것도 없음.")]
        [SerializeField] GameObject lit;
        [Tooltip("방이 어두울 때만 켜는 오브젝트. 비우면 어두울 땐 아무것도 없음.")]
        [SerializeField] GameObject dark;

        // Start 전에는 같은 씬의 RoomLight가 등록됐는지 알 수 없어 Start에서 처음 맞춤.
        bool started;

        void Start()
        {
            started = true;
            Apply(RoomLight.IsSceneLit(gameObject.scene));
        }

        // 꺼졌다 다시 켜졌을 때(부모가 꺼졌다 켜짐 등) 그 사이에 바뀐 밝기를 다시 맞춤.
        void OnEnable()
        {
            RoomLight.SceneLit += OnSceneLit;
            if (started)
                Apply(RoomLight.IsSceneLit(gameObject.scene));
        }

        void OnDisable() => RoomLight.SceneLit -= OnSceneLit;

        // 이 부품이 있는 Room이 밝아지면 밝은 쪽으로 바꿈. 다른 Room은 무시함.
        internal void OnSceneLit(Scene scene)
        {
            if (scene == gameObject.scene)
                Apply(true);
        }

        // roomLit이면 lit만, 아니면 dark만 켬. 에디트 모드 테스트가 직접 부름.
        internal void Apply(bool roomLit)
        {
            if (lit == null && dark == null)
            {
                Debug.LogWarning($"[LightVariant] '{name}': 'lit'과 'dark'가 모두 비어 있어 아무것도 바꾸지 않는다.", this);
                return;
            }

            if (lit != null)
                lit.SetActive(roomLit);
            if (dark != null)
                dark.SetActive(!roomLit);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            WarnIfSelfOrAncestor(lit, nameof(lit));
            WarnIfSelfOrAncestor(dark, nameof(dark));
        }

        void WarnIfSelfOrAncestor(GameObject target, string field)
        {
            if (target != null && transform.IsChildOf(target.transform))
                Debug.LogWarning($"[LightVariant] '{name}': {field}의 '{target.name}'을 끄면 이 컴포넌트도 꺼진다. 자식 오브젝트를 넣어야 한다.", this);
        }
#endif
    }
}
