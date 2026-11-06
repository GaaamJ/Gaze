using UnityEngine;
using Gaze.Player;
using Gaze.Session;

namespace Gaze.Area
{
    // Room 씬에 놓는 문의 공통 부분. 라리스가 들어가면 행선지 문으로 Room 이동을 시작함.
    // 이 오브젝트의 위치가 다른 Room에서 올 때의 도착 위치가 됨.
    // 행선지를 정하는 방식은 자식 클래스가 ResolveTarget으로, 들어갈 수 있는지는 CanEnter로 정함.
    // Core의 RoomSceneLoader를 인스펙터로 참조할 수 없어 정적 진입점 RoomSceneTransition.Begin으로 넘김.
    [RequireComponent(typeof(Collider2D))]
    public abstract class Door : MonoBehaviour
    {
        [Tooltip("이 문의 id. 다른 Room의 문이 targetDoorId로 가리킨다.")]
        [SerializeField] string doorId;

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

        // 다른 Room의 문을 지나 이 문에 도착했을 때 RoomSceneLoader가 부름.
        internal void Arrive(string fromScene, string fromDoorId) => OnArrived(fromScene, fromDoorId);

        // 도착했을 때. fromScene·fromDoorId는 떠나온 Room과 들어간 문의 id. 바뀌는 문처럼 어디서 왔는지 쓰는 문이 덮어씀.
        protected virtual void OnArrived(string fromScene, string fromDoorId) { }

        // 지금 들어갈 수 있는지. 잠긴 문처럼 조건이 있는 문이 덮어씀.
        protected virtual bool CanEnter() => true;

        // 넘어갈 Scene과 도착할 문의 id를 정함. 넘어갈 곳이 없으면 false.
        protected abstract bool ResolveTarget(out string targetScene, out string targetDoorId);

        // Room 이동이 시작된 뒤 한 번. 들어갈 때마다 바뀌는 문처럼 들어간 것을 세는 문이 덮어씀.
        protected virtual void OnEntered() { }

        // 라리스인지 → 문이 켜져 있고 도착 직후가 아닌지 → 입력이 잠겨 있지 않은지 → CanEnter → 행선지가 있는지 확인한 뒤
        // Room 이동을 시작하고 OnEntered를 부름. 시작했으면 true.
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

            if (!CanEnter() || !ResolveTarget(out string targetScene, out string targetDoorId))
                return false;

            if (!RoomSceneTransition.Begin(targetScene, targetDoorId, doorId))
                return false;
            OnEntered();
            return true;
        }
    }
}
