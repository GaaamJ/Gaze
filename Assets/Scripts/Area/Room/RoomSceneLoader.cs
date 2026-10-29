using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gaze.Player;
using Gaze.Session;
using Gaze.UI;

namespace Gaze.Area
{
    // Core 씬에서 Room 씬 하나를 additive로 불러오고 내리고, 문으로 Room을 옮기는 흐름을 맡음.
    // 어느 Room을 불러올지는 정하지 않음. 호출하는 쪽이 이름을 넘김.
    // Load, Unload, Enter는 CoreEntry가 시작·리스폰 때 부르고, BeginMove는 RoomSceneTransition이 부름.
    // RoomSceneTransition이 FindAnyObjectByType으로 찾으므로 Core 씬에 하나만 둠.
    public class RoomSceneLoader : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [Tooltip("RoomSceneDoor로 넘어갈 때 도착 문으로 옮길 라리스")]
        [SerializeField] PlayerMovement laris;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;

        // 지금 불러와 있는 Room 씬.
        Scene room;
        InputLock moveLock;
        ScreenFader moveFader;

        internal bool IsMovingRoom { get; private set; }

        void Awake()
        {
            if (inputLock == null)
                Debug.LogError("[RoomSceneLoader] 'inputLock' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (laris == null)
                Debug.LogError("[RoomSceneLoader] 'laris' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
        }

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금을 되돌림.
        void OnDisable()
        {
            if (!IsMovingRoom)
                return;
            StopAllCoroutines();
            EndMove();
        }

        // Room 씬을 additive로 불러와 활성 씬으로 지정. 코루틴이라 호출하는 쪽이 끝까지 돌려야 함.
        internal IEnumerator Load(string name)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[RoomSceneLoader] Room 씬 '{name}'을 불러오지 못했다. 빌드 설정을 확인해야 한다.", this);
                yield break;
            }
            while (!load.isDone)
                yield return null;

            room = SceneManager.GetSceneByName(name);
            SceneManager.SetActiveScene(room);
        }

        // 불러온 Room 씬의 RoomSceneEntry를 찾아 Enter를 부름. 불러온 Room이 없으면 아무것도 안 함.
        internal void Enter()
        {
            if (!room.IsValid() || !room.isLoaded)
                return;
            var entry = FindRoomEntry(room);
            if (entry == null)
                Debug.LogWarning($"[RoomSceneLoader] Room 씬 '{room.name}'에 RoomSceneEntry가 없다.", this);
            else
                entry.Enter();
        }

        // 불러온 Room 씬을 내림. 불러온 Room이 없으면 바로 끝남. 코루틴이라 호출하는 쪽이 끝까지 돌려야 함.
        internal IEnumerator Unload()
        {
            if (!room.IsValid() || !room.isLoaded)
                yield break;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(room);
            while (unload != null && !unload.isDone)
                yield return null;
        }

        // 씬의 루트 아래(꺼진 오브젝트 포함)에서 처음 나오는 RoomSceneEntry. 없으면 null.
        static RoomSceneEntry FindRoomEntry(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RoomSceneEntry entry = root.GetComponentInChildren<RoomSceneEntry>(true);
                if (entry != null)
                    return entry;
            }
            return null;
        }

        // 문으로 Room을 옮기는 이동 시작. 이동 중이거나 대상 씬이 빌드 설정에 없으면 false.
        // 입력을 잠근 뒤 RunMove를 돌림. Room 사이 이동은 세이브를 쓰지 않음.
        internal bool BeginMove(string targetRoom, string targetDoorId)
        {
            if (IsMovingRoom || !isActiveAndEnabled)
                return false;
            if (string.IsNullOrWhiteSpace(targetRoom) || !Application.CanStreamedLevelBeLoaded(targetRoom))
            {
                Debug.LogError($"[RoomSceneLoader] Room 씬 '{targetRoom}'이 빌드 설정에 없어 넘어가지 못했다.", this);
                return false;
            }

            IsMovingRoom = true;
            if (inputLock != null)
            {
                inputLock.Lock(InputLockReason.RoomMove);
                moveLock = inputLock;
            }
            StartCoroutine(RunMove(targetRoom, targetDoorId));
            return true;
        }

        // 페이드 아웃 → 현재 Room 내림 → 대상 Room 불러옴 → 라리스를 도착 문으로 옮김 → Enter → 페이드 인.
        // 어디서 끊기든 finally에서 EndMove로 잠금과 페이드를 되돌림.
        IEnumerator RunMove(string targetRoom, string targetDoorId)
        {
            try
            {
                moveFader = ScreenFader.FindOrCreate(faderPrefab, this);
                if (moveFader != null)
                {
                    IEnumerator fadeOut = moveFader.FadeOut();
                    while (fadeOut.MoveNext())
                        yield return fadeOut.Current;
                }

                IEnumerator unload = Unload();
                while (unload.MoveNext())
                    yield return unload.Current;

                IEnumerator load = Load(targetRoom);
                while (load.MoveNext())
                    yield return load.Current;

                var door = FindDoor(room, targetDoorId);
                Rigidbody2D body = laris != null ? laris.GetComponent<Rigidbody2D>() : null;
                if (door == null)
                {
                    Debug.LogError($"[RoomSceneLoader] Room '{targetRoom}'에 id '{targetDoorId}'인 RoomSceneDoor가 없어 라리스를 옮기지 못했다.", this);
                }
                else if (body != null)
                {
                    PlayerMovement.MoveTo(body, door.transform.position);
                    door.WaitingForExit = true;
                    CinemachineCore.ResetCameraState();
                }

                Enter();

                // 옮긴 위치의 트리거 접촉이 전환 중에 들어오도록 물리 단계를 한 번 기다림.
                yield return new WaitForFixedUpdate();

                // 도착 위치가 트리거 밖이면 나가는 이벤트가 오지 않으므로 바로 다시 쓸 수 있게 함.
                if (door != null && body != null && !OverlapsBody(door.GetComponent<Collider2D>(), body))
                    door.WaitingForExit = false;

                if (moveFader != null)
                {
                    IEnumerator fadeIn = moveFader.HoldThenFadeIn();
                    while (fadeIn.MoveNext())
                        yield return fadeIn.Current;
                }
            }
            finally
            {
                EndMove();
            }
        }

        // 트리거가 몸체의 Trigger가 아닌 콜라이더와 실제로 겹치는지.
        static bool OverlapsBody(Collider2D trigger, Rigidbody2D body)
        {
            var colliders = new List<Collider2D>();
            body.GetAttachedColliders(colliders);
            foreach (Collider2D collider in colliders)
            {
                if (!collider.isTrigger && Physics2D.Distance(trigger, collider).isOverlapped)
                    return true;
            }
            return false;
        }

        // 이동 상태, 입력 잠금, 검은 화면을 되돌림. RunMove의 finally와 OnDisable이 부름.
        void EndMove()
        {
            if (moveLock != null)
                moveLock.Unlock(InputLockReason.RoomMove);
            moveLock = null;
            IsMovingRoom = false;

            if (moveFader != null)
                moveFader.SetAlpha(0f);
            moveFader = null;
        }

        // 씬 안에서 id가 같은 RoomSceneDoor를 찾음. 씬이 불러와 있지 않거나 없으면 null.
        static RoomSceneDoor FindDoor(Scene scene, string doorId)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RoomSceneDoor door in root.GetComponentsInChildren<RoomSceneDoor>(true))
                {
                    if (door.DoorId == doorId)
                        return door;
                }
            }
            return null;
        }
    }
}
