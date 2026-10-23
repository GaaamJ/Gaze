using UnityEngine;
using ProjectT.Player;
using ProjectT.Session;

namespace ProjectT.Area
{
    // 도착 위치는 이 오브젝트의 위치다. Core를 인스펙터로 참조할 수 없어 정적 진입점 RoomTransition.Begin으로 넘긴다.
    [RequireComponent(typeof(Collider2D))]
    public class RoomDoor : MonoBehaviour
    {
        [Tooltip("이 문의 id. 다른 Room의 문이 targetDoorId로 가리킨다.")]
        [SerializeField] string doorId;
        [Tooltip("넘어갈 Room 씬 이름")]
        [SerializeField] string targetRoom;
        [Tooltip("넘어간 Room에서 도착할 문의 id")]
        [SerializeField] string targetDoorId;

        public string DoorId => doorId;

        // 라리스가 이 문에 도착한 뒤 트리거 밖으로 나가기 전까지 true다.
        public bool WaitingForExit { get; internal set; }

        void OnTriggerEnter2D(Collider2D other) => TryEnter(other);

        void OnTriggerExit2D(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                WaitingForExit = false;
        }

        internal bool TryEnter(Collider2D other)
        {
            if (!PlayerMovement.TryFrom(other, out PlayerMovement laris))
                return false;
            if (!isActiveAndEnabled || WaitingForExit)
                return false;

            InputLock inputLock = laris.GetComponent<InputLock>();
            // 다른 이동 중이거나 대화·사망 중이면 넘어가지 않는다.
            if (inputLock != null && inputLock.IsLocked)
                return false;

            return RoomTransition.Begin(targetRoom, targetDoorId);
        }
    }
}
