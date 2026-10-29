using System;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Death
{
    // 사망 기록이 있으면 Room에 흔적을 남김. source의 사망 노드가 기록돼 있으면 프리팹(시체 등)을 놓고 지정한 오브젝트를 끔.
    // RoomSceneEntry.Enter가 Room 진입 때 Apply를 부름.
    public class DeathAftermath : MonoBehaviour
    {
        [Serializable]
        public struct Spawn
        {
            [Tooltip("사망 기록이 있으면 놓을 프리팹 (시체 등)")]
            public GameObject prefab;
            [Tooltip("놓을 위치·회전. 비우면 이 오브젝트의 위치·회전을 쓴다.")]
            public Transform point;
        }

        [Tooltip("IDeathSource를 구현한 트리거 컴포넌트 (DeathInteractable, DeathZone)")]
        [SerializeField] MonoBehaviour source;
        [SerializeField] Spawn[] spawns;
        [Tooltip("사망 기록이 있으면 끌 오브젝트")]
        [SerializeField] GameObject[] deactivate;

        // 다시 불러도 생성물이 겹치지 않게 막음.
        bool applied;

        // source의 사망 기록 확인 → spawns 프리팹 생성 → deactivate 오브젝트 끔.
        // 기록이 없거나 이미 적용했으면 아무것도 안 함.
        // Awake에서 부르지 않음. RoomSceneEntry.Enter()가 CoreEntry의 리셋·리스폰 뒤에 부름.
        internal void Apply()
        {
            if (!(source is IDeathSource death))
            {
                Debug.LogWarning($"[DeathAftermath] '{name}': source가 비었거나 IDeathSource가 아니다.", this);
                return;
            }

            if (applied || !DeathRecords.Has(death.DeathNodeId))
                return;
            applied = true;

            if (spawns != null && spawns.Length > 0)
            {
                WarnIfParentScaled();
                foreach (Spawn spawn in spawns)
                    Place(spawn);
            }

            if (deactivate != null)
            {
                foreach (GameObject target in deactivate)
                {
                    if (target == null)
                    {
                        Debug.LogWarning($"[DeathAftermath] '{name}': deactivate에 빈 항목이 있어 건너뛴다.", this);
                        continue;
                    }
                    target.SetActive(false);
                }
            }
        }

        // 프리팹 하나를 point(없으면 이 오브젝트)의 위치·회전에 이 오브젝트의 부모 아래로 생성.
        void Place(Spawn spawn)
        {
            if (spawn.prefab == null)
            {
                Debug.LogWarning($"[DeathAftermath] '{name}': spawns에 prefab이 빈 항목이 있어 건너뛴다.", this);
                return;
            }

            Transform at = spawn.point != null ? spawn.point : transform;
            Instantiate(spawn.prefab, at.position, at.rotation, transform.parent);
        }

        // 생성물이 부모 아래에 놓이므로 부모 스케일이 1이 아니면 크기가 프리팹과 달라짐. 그때 경고.
        void WarnIfParentScaled()
        {
            Transform parent = transform.parent;
            if (parent != null && parent.lossyScale != Vector3.one)
                Debug.LogWarning($"[DeathAftermath] '{name}': 부모 '{parent.name}'의 스케일이 {parent.lossyScale}라 생성물 크기가 프리팹과 달라진다.", this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (source != null && !(source is IDeathSource))
                Debug.LogWarning($"[DeathAftermath] '{name}': source '{source.GetType().Name}'는 IDeathSource가 아니다.", this);

            if (deactivate == null)
                return;
            foreach (GameObject target in deactivate)
            {
                if (target != null && transform.IsChildOf(target.transform))
                    Debug.LogWarning($"[DeathAftermath] '{name}': deactivate의 '{target.name}'를 끄면 이 컴포넌트도 꺼진다.", this);
            }
        }
#endif
    }
}
