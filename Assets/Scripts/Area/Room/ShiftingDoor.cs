using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gaze.Area
{
    // 들어갈 때마다 다음 방이 나오는 문. 기본 행선지 → 목록 1번째 → 2번째 → … → 다시 기본 행선지 순서로 돎.
    // 빛·방 밝기와 상관없음.
    // 몇 번 들어갔는지는 Room을 다시 불러와도 남도록 정적 기록(Entries)에 둠. 세이브에는 남기지 않아 게임을 끄면 처음부터.
    public class ShiftingDoor : Door
    {
        [Serializable]
        public struct Target
        {
            [Tooltip("넘어갈 Scene 이름")]
            public string scene;
            [Tooltip("넘어간 Scene에서 도착할 문의 id")]
            public string doorId;
        }

        [Tooltip("처음 들어갈 때의 행선지 Scene 이름")]
        [SerializeField] string defaultTargetScene;
        [Tooltip("처음 행선지 Scene에서 도착할 문의 id")]
        [SerializeField] string defaultTargetDoorId;
        [Tooltip("들어갈 때마다 차례로 나오는 다음 행선지. 끝 다음은 기본 행선지.")]
        [SerializeField] List<Target> shiftingTargets = new List<Target>();

        // 문마다 들어간 횟수. 키는 "Scene 이름/문 id".
        // Room 씬이 내려가면 문 오브젝트도 사라지므로 정적으로 둠.
        static readonly Dictionary<string, int> Entries = new Dictionary<string, int>();

        // 도메인 리로드를 끈 플레이에서도 플레이마다 처음부터 시작하게 비움.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetEntries() => Entries.Clear();

        string Key => gameObject.scene.name + "/" + DoorId;

        // 0이면 기본 행선지, 1부터는 shiftingTargets[Index - 1].
        internal int Index
        {
            get
            {
                Entries.TryGetValue(Key, out int count);
                return count % (shiftingTargets.Count + 1);
            }
        }

        protected override void OnEntered()
        {
            Entries.TryGetValue(Key, out int count);
            Entries[Key] = count + 1;
        }

        protected override bool ResolveTarget(out string scene, out string doorId)
        {
            int index = Index;
            if (index == 0)
            {
                scene = defaultTargetScene;
                doorId = defaultTargetDoorId;
            }
            else
            {
                scene = shiftingTargets[index - 1].scene;
                doorId = shiftingTargets[index - 1].doorId;
            }
            return true;
        }
    }
}
