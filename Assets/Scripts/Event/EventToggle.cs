using UnityEngine;

namespace Gaze.Events
{
    // 이벤트가 일어났으면 target을 끔. 주운 열쇠가 사라지고, 방에 다시 들어와도 나오지 않게 함.
    // 켜질 때 지금 상태를 먼저 보고, 켜져 있는 동안 일어나는 이벤트는 EventManager.Changed로 받음.
    public class EventToggle : MonoBehaviour
    {
        [Tooltip("이 이벤트가 일어나면 target을 끈다.")]
        [SerializeField] EventFlag flag;
        [Tooltip("끌 오브젝트. 비우면 이 오브젝트.")]
        [SerializeField] GameObject target;

        GameObject Target => target != null ? target : gameObject;

        // 켜질 때(Room이 불러와질 때) 이미 일어난 이벤트면 바로 끔.
        void OnEnable()
        {
            EventManager.Changed += OnChanged;
            Refresh();
        }

        void OnDisable() => EventManager.Changed -= OnChanged;

        // flag가 일어났으면 target을 끔. 에디트 모드 테스트가 OnEnable 대신 직접 부름.
        internal void Refresh()
        {
            if (flag == null)
            {
                Debug.LogWarning($"[EventToggle] '{name}': 'flag'가 비어 있어 아무것도 끄지 않는다.", this);
                return;
            }

            if (EventManager.Has(flag))
                Target.SetActive(false);
        }

        // 일어난 이벤트가 이 부품의 flag면 Refresh. 다른 이벤트는 무시함.
        internal void OnChanged(EventFlag changed)
        {
            if (changed == flag)
                Refresh();
        }
    }
}
