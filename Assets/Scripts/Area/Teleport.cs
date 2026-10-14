using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using ProjectT.Player;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Area
{
    // 두 끝 중 하나에 라리스가 닿으면 반대쪽 끝으로 옮긴다. 씬에 ScreenFader가 없으면 faderPrefab을 꺼내 쓴다.
    public class Teleport : MonoBehaviour
    {
        [SerializeField] TeleportEnd endA;
        [SerializeField] TeleportEnd endB;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;

        InputLock heldLock;
        ScreenFader activeFader;
        bool warnedNoFader;

        public TeleportEnd EndA => endA;
        public TeleportEnd EndB => endB;
        public bool IsTransitioning { get; private set; }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금과 페이드를 되돌린다.
        void OnDisable()
        {
            if (!IsTransitioning)
                return;
            StopAllCoroutines();
            End();
        }

        internal void OnEndEntered(TeleportEnd end, Collider2D other)
        {
            IEnumerator routine = Prepare(end, other);
            if (routine != null)
                StartCoroutine(routine);
        }

        internal void OnEndExited(TeleportEnd end, Collider2D other)
        {
            if (PlayerMovement.TryFrom(other, out _))
                end.WaitingForExit = false;
        }

        internal IEnumerator Prepare(TeleportEnd from, Collider2D body)
        {
            if (!PlayerMovement.TryFrom(body, out PlayerMovement laris))
                return null;
            if (IsTransitioning || !isActiveAndEnabled || from == null || from.WaitingForExit)
                return null;

            InputLock inputLock = laris.GetComponent<InputLock>();
            // 다른 통로로 옮겨지는 중이거나 대화 중이면 넘어가지 않는다.
            if (inputLock != null && inputLock.IsLocked)
                return null;

            TeleportEnd to = from == endA ? endB : from == endB ? endA : null;
            if (to == null)
            {
                Debug.LogWarning($"[Teleport] '{name}': '{from.name}'의 반대쪽 끝이 없어 옮기지 않는다.", this);
                return null;
            }

            IsTransitioning = true;
            if (inputLock != null)
            {
                inputLock.Lock(InputLockReason.RoomTransition);
                heldLock = inputLock;
            }
            return Run(to, body);
        }

        IEnumerator Run(TeleportEnd to, Collider2D body)
        {
            try
            {
                activeFader = FindOrCreateFader();
                // 하위 페이드를 Unity에 넘기면 예외 때 이 코루틴의 finally가 돌지 않아 직접 돌린다.
                if (activeFader != null)
                {
                    IEnumerator fadeOut = activeFader.FadeOut();
                    while (fadeOut.MoveNext())
                        yield return fadeOut.Current;
                }

                MoveTo(body.attachedRigidbody, to.transform.position);
                to.WaitingForExit = true;
                // 이전 위치에서 감쇠로 따라오면 페이드 인 동안 화면이 미끄러지므로 다음 프레임에 바로 붙인다.
                CinemachineCore.ResetCameraState();

                // 옮긴 위치의 트리거 접촉이 전환 중에 들어오도록 물리 단계를 한 번 기다린다.
                yield return new WaitForFixedUpdate();

                // 도착 위치가 트리거 밖이면 나가는 이벤트가 오지 않으므로 바로 다시 쓸 수 있게 한다.
                if (to.TryGetComponent(out Collider2D trigger) && !Physics2D.Distance(trigger, body).isOverlapped)
                    to.WaitingForExit = false;

                if (activeFader != null)
                {
                    IEnumerator fadeIn = activeFader.HoldThenFadeIn();
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

        // 만든 페이더는 씬 루트에 두어 다음 전환·다른 통로가 FindAnyObjectByType으로 다시 쓰고, 씬과 함께 정리되게 한다.
        ScreenFader FindOrCreateFader()
        {
            ScreenFader fader = FindAnyObjectByType<ScreenFader>();
            if (fader != null)
                return fader;
            if (faderPrefab != null)
                return Instantiate(faderPrefab);
            if (!warnedNoFader)
            {
                warnedNoFader = true;
                Debug.LogWarning($"[Teleport] '{name}': 씬에 ScreenFader가 없고 'faderPrefab'도 비어 있어 페이드 없이 옮긴다.", this);
            }
            return null;
        }

        static void MoveTo(Rigidbody2D body, Vector3 position)
        {
            position.z = body.transform.position.z;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.transform.position = position;
        }
    }
}
