using System.Collections.Generic;
using UnityEngine;
using ProjectT.Dialogue;

namespace ProjectT.Interaction
{
    // 조사할 때 PlayerInteractor가 대상에게 넘기는 값 묶음.
    // Dialogue는 대사 재생용, Investigated는 이번 실행에서 이미 조사한 노드 이름 집합(GameSession.Investigated).
    public readonly struct InteractContext
    {
        public readonly DialogueService Dialogue;
        public readonly HashSet<string> Investigated;

        public InteractContext(DialogueService dialogue, HashSet<string> investigated)
        {
            Dialogue = dialogue;
            Investigated = investigated;
        }
    }

    // 조사 키(Z)로 조사할 수 있는 대상. DetectZone이 후보로 모으고 PlayerInteractor가 Interact를 부름.
    // CanInteract가 거짓이면 대상으로 뽑히지 않음.
    public interface IInteractable
    {
        bool CanInteract { get; }
        void Interact(InteractContext context);
    }

    public static class InteractableExtensions
    {
        // 파괴되지 않았으면 참. DetectZone과 PlayerInteractor가 대상을 쓰기 전에 확인함.
        // 인터페이스로 받으면 파괴된 오브젝트도 == null이 false라서 UnityEngine.Object로 비교함.
        public static bool IsAlive(this IInteractable interactable)
        {
            return interactable is Object unityObject ? unityObject != null : interactable != null;
        }
    }
}
