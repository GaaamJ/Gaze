using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using ProjectT.Player;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Area
{
    // 두 끝 중 하나에 라리스가 닿으면 반대쪽 끝으로 옮긴다. ScreenFader·CinemachineConfiner2D는 있으면 쓰고 없으면 건너뛴다.
    public class Passage : MonoBehaviour
    {
        [SerializeField] PassageEnd endA;
        [SerializeField] PassageEnd endB;

        InputLock heldLock;
        ScreenFader activeFader;

        public PassageEnd EndA => endA;
        public PassageEnd EndB => endB;
        public bool IsTransitioning { get; private set; }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금과 페이드를 되돌린다.
        void OnDisable()
        {
            if (!IsTransitioning)
                return;
            StopAllCoroutines();
            End();
        }

        internal void OnEndEntered(PassageEnd end, Collider2D other)
        {
            IEnumerator routine = Prepare(end, other);
            if (routine != null)
                StartCoroutine(routine);
        }

        internal void OnEndExited(PassageEnd end, Collider2D other)
        {
            if (TryGetLaris(other, out _))
                end.WaitingForExit = false;
        }

        internal IEnumerator Prepare(PassageEnd from, Collider2D body)
        {
            if (!TryGetLaris(body, out PlayerMovement laris))
                return null;
            if (IsTransitioning || !isActiveAndEnabled || from == null || from.WaitingForExit)
                return null;

            InputLock inputLock = laris.GetComponent<InputLock>();
            // 다른 통로로 옮겨지는 중이거나 대화 중이면 넘어가지 않는다.
            if (inputLock != null && inputLock.IsLocked)
                return null;

            PassageEnd to = from == endA ? endB : from == endB ? endA : null;
            if (to == null)
            {
                Debug.LogWarning($"[Passage] '{name}': '{from.name}'의 반대쪽 끝이 없어 옮기지 않는다.", this);
                return null;
            }
            if (to.CameraBounds != null && !IsConfinerShape(to.CameraBounds))
            {
                Debug.LogWarning($"[Passage] '{name}': '{to.name}'의 카메라 경계 '{to.CameraBounds.name}'는 BoxCollider2D·PolygonCollider2D·CompositeCollider2D가 아니어서 옮기지 않는다.", this);
                return null;
            }

            IsTransitioning = true;
            if (inputLock != null)
            {
                inputLock.Lock(InputLockReason.RoomTransition);
                heldLock = inputLock;
            }
            return Run(to, laris, body);
        }

        IEnumerator Run(PassageEnd to, PlayerMovement laris, Collider2D body)
        {
            try
            {
                activeFader = FindAnyObjectByType<ScreenFader>();
                // 하위 페이드를 Unity에 넘기면 예외 때 이 코루틴의 finally가 돌지 않아 직접 돌린다.
                if (activeFader != null)
                {
                    IEnumerator fadeOut = activeFader.FadeOut();
                    while (fadeOut.MoveNext())
                        yield return fadeOut.Current;
                }

                MoveTo(body.attachedRigidbody, to.transform.position);
                to.WaitingForExit = true;
                if (to.TryGetFacing(out Vector2Int facing))
                    laris.Face(facing);
                if (to.CameraBounds != null)
                    SwitchCameraBounds(to.CameraBounds);
                // 이전 위치에서 감쇠로 따라오면 페이드 인 동안 화면이 미끄러지므로 다음 프레임에 바로 붙인다.
                CinemachineCore.ResetCameraState();

                // 옮긴 위치의 트리거 접촉이 전환 중에 들어오도록 물리 단계를 한 번 기다린다.
                yield return new WaitForFixedUpdate();

                // 도착 위치가 트리거 밖이면 나가는 이벤트가 오지 않으므로 바로 다시 쓸 수 있게 한다.
                if (to.TryGetComponent(out Collider2D trigger) && !Physics2D.Distance(trigger, body).isOverlapped)
                    to.WaitingForExit = false;

                if (activeFader != null)
                {
                    IEnumerator fadeIn = activeFader.FadeIn();
                    while (fadeIn.MoveNext())
                        yield return fadeIn.Current;
                }
            }
            finally
            {
                End();
            }
        }

        void End()
        {
            if (heldLock != null)
            {
                InputLock released = heldLock;
                heldLock = null;
                released.Unlock(InputLockReason.RoomTransition);
            }
            IsTransitioning = false;

            ScreenFader fader = activeFader;
            activeFader = null;
            if (fader != null)
                fader.SetAlpha(0f);
        }

        static bool TryGetLaris(Collider2D other, out PlayerMovement laris)
        {
            laris = null;
            // 라리스의 조사 감지 원(트리거)이 먼저 닿아도 넘어가지 않도록 몸 콜라이더만 본다.
            if (other == null || other.isTrigger || other.attachedRigidbody == null)
                return false;
            return other.attachedRigidbody.TryGetComponent(out laris);
        }

        static bool IsConfinerShape(Collider2D shape) =>
            shape is BoxCollider2D || shape is PolygonCollider2D || shape is CompositeCollider2D;

        static void MoveTo(Rigidbody2D body, Vector3 position)
        {
            position.z = body.transform.position.z;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.transform.position = position;
        }

        static void SwitchCameraBounds(Collider2D shape)
        {
            var confiner = FindAnyObjectByType<CinemachineConfiner2D>();
            if (confiner == null)
                return;
            confiner.BoundingShape2D = shape;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
