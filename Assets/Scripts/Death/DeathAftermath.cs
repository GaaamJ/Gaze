using System;
using UnityEngine;
using ProjectT.Save;

namespace ProjectT.Death
{
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

        [Tooltip("IDeathSource를 구현한 트리거 컴포넌트 (DeathInteractable, DeathZone, PasswordPuzzle)")]
        [SerializeField] MonoBehaviour source;
        [SerializeField] Spawn[] spawns;
        [Tooltip("사망 기록이 있으면 끌 오브젝트")]
        [SerializeField] GameObject[] deactivate;

        bool applied;

        // 세이브는 GameSession.Begin(BeforeSceneLoad)에서 이미 불러와 있다.
        void Awake()
        {
            Apply();
        }

        internal void Apply()
        {
            if (!(source is IDeathSource death))
            {
                Debug.LogWarning($"[DeathAftermath] '{name}': source가 비었거나 IDeathSource가 아니다.", this);
                return;
            }

            Apply(DeathRecords.Has(death.DeathNodeId));
        }

        internal void Apply(bool died)
        {
            if (!died || applied)
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
