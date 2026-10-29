using UnityEngine;
using UnityEngine.Serialization;
using ProjectT.Player;
using ProjectT.Session;

namespace ProjectT.Area
{
    // Room 씬에 놓는 문. 라리스가 들어가면 targetScene의 targetDoorId 문으로 Room 이동을 시작함.
    // 이 오브젝트의 위치가 다른 Room에서 올 때의 도착 위치가 됨.
    // Core의 RoomSceneLoader를 인스펙터로 참조할 수 없어 정적 진입점 RoomSceneTransition.Begin으로 넘김.
    [RequireComponent(typeof(Collider2D))]
    public class RoomSceneDoor : MonoBehaviour
    {
        [Tooltip("이 문의 id. 다른 Room의 문이 targetDoorId로 가리킨다.")]
        [SerializeField] string doorId;
        [Tooltip("넘어갈 Scene 이름")]
        [FormerlySerializedAs("targetRoom")]
        [SerializeField] string targetScene;
        [Tooltip("넘어간 Scene에서 도착할 문의 id")]
        [SerializeField] string targetDoorId;

        public string DoorId => doorId;

        // 라리스가 이 문에 도착한 뒤 트리거 밖으로 나가기 전까지 참.
        // 도착하자마자 다시 이 문으로 넘어가는 것을 막음. RoomSceneLoader가 켜고 끔.
        public bool WaitingForExit { get; internal set; }

        void OnTriggerEnter2D(Collider2D other) => TryEnter(other);

        void OnTriggerExit2D(Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                WaitingForExit = false;
        }

        // 라리스인지 → 문이 켜져 있고 도착 직후가 아닌지 → 입력이 잠겨 있지 않은지 확인한 뒤 Room 이동을 시작함.
        // 시작했으면 true.
        internal bool TryEnter(Collider2D other)
        {
            if (!PlayerMovement.TryFrom(other, out PlayerMovement laris))
                return false;
            if (!isActiveAndEnabled || WaitingForExit)
                return false;

            InputLock inputLock = laris.GetComponent<InputLock>();
            // 다른 이동 중이거나 대화·사망 중이면 넘어가지 않음.
            if (inputLock != null && inputLock.IsLocked)
                return false;

            return RoomSceneTransition.Begin(targetScene, targetDoorId);
        }
    }
}
