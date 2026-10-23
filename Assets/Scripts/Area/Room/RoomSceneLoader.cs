using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectT.Player;
using ProjectT.Session;
using ProjectT.UI;

namespace ProjectT.Area
{
    // Sceneransition이 FindAnyObjectByType으로 찾으므로 Core 씬에 하나만 둔다. 어느 Scene을 불러올지는 정하지 않는다.
    public class RoomSceneLoader : MonoBehaviour
    {
        [SerializeField] InputLock inputLock;
        [Tooltip("SceneTransitionDoor로 넘어갈 때 도착 문으로 옮길 라리스")]
        [SerializeField] PlayerMovement laris;
        [Tooltip("씬에 ScreenFader가 없을 때 만들어 쓸 페이더")]
        [SerializeField] ScreenFader faderPrefab;

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

        // Unity는 멈춘 코루틴의 finally를 실행하지 않으므로 여기서도 잠금을 되돌린다.
        void OnDisable()
        {
            if (!IsMovingRoom)
                return;
            StopAllCoroutines();
            EndMove();
        }

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

        internal IEnumerator Unload()
        {
            if (!room.IsValid() || !room.isLoaded)
                yield break;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(room);
            while (unload != null && !unload.isDone)
                yield return null;
        }

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

        // Room 사이 이동은 세이브를 쓰지 않는다.
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

                // 옮긴 위치의 트리거 접촉이 전환 중에 들어오도록 물리 단계를 한 번 기다린다.
                yield return new WaitForFixedUpdate();

                // 도착 위치가 트리거 밖이면 나가는 이벤트가 오지 않으므로 바로 다시 쓸 수 있게 한다.
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
