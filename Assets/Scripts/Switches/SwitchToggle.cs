using UnityEngine;
using UnityEngine.Serialization;

namespace Gaze.Switches
{
    // 스위치가 켜졌으면 target을 끔. 주운 열쇠가 사라지고, 방에 다시 들어와도 나오지 않게 함.
    // 켜질 때 지금 상태를 먼저 보고, 켜져 있는 동안 켜지는 스위치는 SwitchRecords.Changed로 받음.
    public class SwitchToggle : MonoBehaviour
    {
        [Tooltip("이 스위치가 켜지면 target을 끈다.")]
        [FormerlySerializedAs("flag")]
        [SerializeField] Switch watchedSwitch;
        [Tooltip("끌 오브젝트. 비우면 이 오브젝트.")]
        [SerializeField] GameObject target;

        GameObject Target => target != null ? target : gameObject;

        // 켜질 때(Room이 불러와질 때) 이미 켜진 스위치면 바로 끔.
        void OnEnable()
        {
            SwitchRecords.Changed += OnChanged;
            Refresh();
        }

        void OnDisable() => SwitchRecords.Changed -= OnChanged;

        // watchedSwitch가 켜져 있으면 target을 끔. 에디트 모드 테스트가 OnEnable 대신 직접 부름.
        internal void Refresh()
        {
            if (watchedSwitch == null)
            {
                Debug.LogWarning($"[SwitchToggle] '{name}': 'watchedSwitch'가 비어 있어 아무것도 끄지 않는다.", this);
                return;
            }

            if (SwitchRecords.IsOn(watchedSwitch))
                Target.SetActive(false);
        }

        // 켜진 스위치가 이 부품의 watchedSwitch면 Refresh. 다른 스위치는 무시함.
        internal void OnChanged(Switch changed)
        {
            if (changed == watchedSwitch)
                Refresh();
        }
    }
}
