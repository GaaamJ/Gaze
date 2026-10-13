using System.Collections;
using UnityEngine;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Area
{
    // 라리스에 붙인다.
    public class ZoneTransition : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [SerializeField] ScreenFader fader;
        [SerializeField] CameraBounds cameraBounds;

        bool lockHeld;

        public bool IsTransitioning { get; private set; }

        void Awake()
        {
            if (inputLock == null)
                Debug.LogError("[ZoneTransition] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (fader == null)
                Debug.LogError("[ZoneTransition] 'fader' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (cameraBounds == null)
                Debug.LogError("[ZoneTransition] 'cameraBounds' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
        }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금을 푼다.
        void OnDisable()
        {
            if (!IsTransitioning)
                return;
            StopAllCoroutines();
            End();
            if (fader != null)
                fader.SetAlpha(0f);
        }

        public bool TryBegin(ZoneExit exit)
        {
            IEnumerator routine = Prepare(exit);
            if (routine == null)
                return false;
            StartCoroutine(routine);
            return true;
        }

        internal IEnumerator Prepare(ZoneExit exit)
        {
            if (IsTransitioning || !isActiveAndEnabled)
                return null;

            AreaMarker arrival = exit != null ? exit.Marker.Pair : null;
            if (arrival == null)
            {
                Debug.LogWarning($"[ZoneTransition] 출구 '{(exit != null ? exit.name : "(없음)")}'에 짝이 되는 도착 지점이 없어 전환하지 않는다.", exit);
                return null;
            }
            if (inputLock == null || fader == null || cameraBounds == null)
            {
                Debug.LogError("[ZoneTransition] 참조가 비어 있어 전환하지 않는다.", this);
                return null;
            }

            IsTransitioning = true;
            inputLock.Lock(InputLockReason.ZoneTransition);
            lockHeld = true;
            return Run(arrival, exit.ArrivalCameraBoundsId);
        }

        IEnumerator Run(AreaMarker arrival, string boundsMarkerId)
        {
            try
            {
                // 하위 페이드를 Unity에 넘기면 예외 때 이 코루틴의 finally가 돌지 않아 직접 돌린다.
                IEnumerator fadeOut = fader.FadeOut();
                while (fadeOut.MoveNext())
                    yield return fadeOut.Current;

                MoveTo(arrival.Position);
                cameraBounds.TrySwitch(arrival.Area, boundsMarkerId);

                // 도착 지점이 다른 출구 안이면 그 트리거가 전환 중에 들어오도록 물리 단계를 한 번 기다린다.
                yield return new WaitForFixedUpdate();
                yield return null;

                IEnumerator fadeIn = fader.FadeIn();
                while (fadeIn.MoveNext())
                    yield return fadeIn.Current;
            }
            finally
            {
                End();
            }
        }

        void End()
        {
            if (lockHeld)
            {
                lockHeld = false;
                inputLock.Unlock(InputLockReason.ZoneTransition);
            }
            IsTransitioning = false;
        }

        void MoveTo(Vector3 position)
        {
            position.z = transform.position.z;
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            transform.position = position;
        }
    }
}
