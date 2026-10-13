using UnityEngine;

namespace ProjectT.Area
{
    // 도착 위치는 이 오브젝트의 위치다.
    [RequireComponent(typeof(Collider2D))]
    public class TeleportEnd : MonoBehaviour
    {
        Teleport teleport;

        // 라리스가 이 끝에 도착한 뒤 트리거 밖으로 나가기 전까지 true다.
        public bool WaitingForExit { get; internal set; }

        void Awake()
        {
            teleport = GetComponentInParent<Teleport>();
            if (teleport == null)
                Debug.LogError("[TeleportEnd] 부모에서 Teleport를 찾지 못했다.", this);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (teleport != null)
                teleport.OnEndEntered(this, other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (teleport != null)
                teleport.OnEndExited(this, other);
        }
    }
}
