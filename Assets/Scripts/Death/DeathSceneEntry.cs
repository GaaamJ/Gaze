using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using ProjectT.Area;
using ProjectT.Dialogue;
using ProjectT.Player;
using ProjectT.Save;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Death
{
    // DeathSequence가 FindAnyObjectByType으로 찾으므로 씬에 하나만 둔다.
    public class DeathSceneEntry : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [SerializeField] DialogueService dialogueService;
        [Tooltip("세이브에 리스폰 위치가 있으면 씬 시작 때 이 라리스를 옮긴다.")]
        [SerializeField] PlayerMovement laris;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;

        bool enteringAfterDeath;
        bool holdingLock;

        internal InputLock InputLock => inputLock;
        internal DialogueService Dialogue => dialogueService;
        internal PlayerMovement Laris => laris;

        void Awake()
        {
            if (inputLock == null)
                Debug.LogError("[DeathSceneEntry] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (dialogueService == null)
                Debug.LogError("[DeathSceneEntry] 'dialogueService' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (laris == null)
                Debug.LogError("[DeathSceneEntry] 'laris' 참조가 비어 있다. 씬에서 연결해야 한다.", this);

            EnterScene();
            RestoreRespawn(SaveManager.Data);
        }

        IEnumerator Start()
        {
            IEnumerator entry = RunEntry();
            while (entry.MoveNext())
                yield return entry.Current;
        }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금과 사망 상태를 되돌린다.
        void OnDisable()
        {
            if (!enteringAfterDeath)
                return;
            StopAllCoroutines();
            FinishEntry();
        }

        // 다른 컴포넌트의 Update보다 먼저 잠가야 리로드 첫 프레임에 방향키가 먹지 않는다.
        internal void EnterScene()
        {
            if (!DeathSequence.ConsumeReload())
                return;

            enteringAfterDeath = true;
            if (inputLock != null)
            {
                inputLock.Lock(InputLockReason.Death);
                holdingLock = true;
            }
        }

        // 사망 리로드뿐 아니라 첫 시작에도 복원한다. 잠금 해제·페이드 인보다 먼저여야 이동이 보이지 않는다.
        internal void RestoreRespawn(SaveData data)
        {
            if (laris == null || !RespawnPoint.TryGet(data, out Vector2 position))
                return;

            Teleport.MoveTo(laris.GetComponent<Rigidbody2D>(), position);
            CinemachineCore.ResetCameraState();
            Debug.Log($"[DeathSceneEntry] 리스폰 위치로 시작: {position}", this);
        }

        internal IEnumerator RunEntry()
        {
            if (!enteringAfterDeath)
                yield break;

            // 반복기 안에서는 yield를 catch로 감쌀 수 없어 finally로 정리만 보장하고, 예외는 Unity 코루틴 실행기가 로그로 남긴다.
            try
            {
                // 씬에 놓인 페이더의 Awake가 알파를 0으로 되돌리므로 Awake가 아닌 여기서 검게 만든다.
                ScreenFader fader = FindOrCreateFader();
                if (fader != null)
                    fader.SetAlpha(1f);

                yield return null;

                FinishEntry();

                if (fader != null)
                {
                    IEnumerator fadeIn = fader.FadeIn();
                    while (fadeIn.MoveNext())
                        yield return fadeIn.Current;
                }
            }
            finally
            {
                // 페이드 인 중 새 사망이 시작됐을 수 있어, 이미 끝낸 진입은 다시 끝내지 않는다.
                if (enteringAfterDeath)
                    FinishEntry();
            }
        }

        void FinishEntry()
        {
            if (holdingLock && inputLock != null)
                inputLock.Unlock(InputLockReason.Death);
            holdingLock = false;
            enteringAfterDeath = false;
            DeathSequence.CompleteReload();
        }

        // 만든 페이더는 씬 루트에 두어 Teleport도 FindAnyObjectByType으로 같은 것을 쓰게 한다.
        internal ScreenFader FindOrCreateFader()
        {
            ScreenFader fader = FindAnyObjectByType<ScreenFader>();
            if (fader != null)
                return fader;
            if (faderPrefab != null)
                return Instantiate(faderPrefab);
            Debug.LogWarning("[DeathSceneEntry] 씬에 ScreenFader가 없고 'faderPrefab'도 비어 있어 페이드 없이 진행한다.", this);
            return null;
        }
    }
}
