using UnityEngine;
using Gaze.Dialogue;
using Gaze.Interaction;
using Gaze.Session;

namespace Gaze.Player
{
    // 조사 키가 눌리면 DetectZone이 고른 대상을 조사시킴.
    // 조사 입력 → 대상 확인 → Interact(InteractContext).
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerInteractInput input;
        [SerializeField] DetectZone detectZone;
        [SerializeField] DialogueService dialogueService;

        void Awake()
        {
            if (input == null)
                Debug.LogError("[PlayerInteractor] 'input' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (detectZone == null)
                Debug.LogError("[PlayerInteractor] 'detectZone' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (dialogueService == null)
                Debug.LogError("[PlayerInteractor] 'dialogueService' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (input == null || detectZone == null || dialogueService == null)
                enabled = false;
        }

        void OnEnable()
        {
            input.InteractPressed += OnInteractPressed;
        }

        void OnDisable()
        {
            input.InteractPressed -= OnInteractPressed;
        }

        void OnInteractPressed()
        {
            IInteractable target = detectZone.CurrentTarget;
            if (!target.IsAlive() || !target.CanInteract)
                return;

            target.Interact(new InteractContext(dialogueService, GameSession.Investigated));
        }
    }
}
