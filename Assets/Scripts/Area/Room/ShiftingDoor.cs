using System;
using System.Collections.Generic;
using UnityEngine;
using Gaze.Lighting;

namespace Gaze.Area
{
    // 안 볼 때 바뀌는 문. 기본 행선지와 바뀌는 행선지 목록을 차례로 돎.
    // 라리스의 빛 범위가 이 문에서 처음 벗어날 때 다음 행선지로 한 칸 넘어감. 목록 끝 다음은 기본 행선지.
    // 넘어간 뒤로는 문에 들어가기 전까지 다시 바뀌지 않음. 들어가야 그 방을 보게 되므로.
    // 방이 밝으면 바뀌지 않고 기본 행선지로 고정됨.
    // 상태는 저장하지 않음. 문으로 도착하면 떠나온 Room을 가리키는 행선지부터, 아니면 기본 행선지부터 시작.
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

        [Tooltip("처음 행선지 Scene 이름. 방이 밝으면 늘 여기로 감.")]
        [SerializeField] string defaultTargetScene;
        [Tooltip("처음 행선지 Scene에서 도착할 문의 id")]
        [SerializeField] string defaultTargetDoorId;
        [Tooltip("빛 밖으로 나가면 한 칸씩 차례로 바뀌는 행선지. 끝 다음은 기본 행선지.")]
        [SerializeField] List<Target> shiftingTargets = new List<Target>();

        // 0이면 기본 행선지, 1부터는 shiftingTargets[Index - 1].
        internal int Index { get; private set; }

        // 이번 방문에서 이미 한 칸 넘어갔으면 참. 방을 다시 불러오면 거짓으로 시작.
        internal bool Shifted { get; private set; }

        bool RoomLit => RoomLight.IsSceneLit(gameObject.scene);

        // 떠나온 Room의 문을 가리키는 행선지로 맞춤. 보고 있는 동안 문이 바뀌지 않도록.
        // 목록에 없는 곳에서 왔으면 기본 행선지.
        protected override void OnArrived(string fromScene, string fromDoorId)
        {
            Index = 0;
            for (int i = 0; i < shiftingTargets.Count; i++)
            {
                if (shiftingTargets[i].scene == fromScene && shiftingTargets[i].doorId == fromDoorId)
                {
                    Index = i + 1;
                    return;
                }
            }
        }

        protected override void OnColliderExit(Collider2D other)
        {
            if (Shifted || !LarisLight.Is(other) || RoomLit)
                return;
            Index = (Index + 1) % (shiftingTargets.Count + 1);
            Shifted = true;
        }

        protected override bool ResolveTarget(out string scene, out string doorId)
        {
            int index = RoomLit ? 0 : Index;
            if (index == 0 || index > shiftingTargets.Count)
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
