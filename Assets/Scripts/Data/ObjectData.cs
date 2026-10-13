using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    // 에셋에 숫자로 저장되므로 기존 값은 바꾸지 않고 새 종류는 새 값으로 추가한다.
    public enum ObjectKind
    {
        Basic = 0,
        Item = 1,
        Death = 2,
        Puzzle = 3,
        Respawn = 4,
        Clear = 5,
        ElevatorDoor = 6,
        Corpse = 7
    }

    [CreateAssetMenu(fileName = "ObjectData", menuName = "ProjectT/Object Data")]
    public class ObjectData : ScriptableObject
    {
        [Tooltip("오브젝트 고유 이름. 이후 사망·퍼즐 데이터가 이 값으로 연결된다.")]
        [SerializeField] string id;

        [Tooltip("조사 결과 종류. 기본·아이템·사망·퍼즐·리스폰·클리어·엘리베이터 문·시체")]
        [SerializeField] ObjectKind kind;

        [Tooltip("첫 조사 때 재생할 Yarn 노드 이름. 기본은 필수, 아이템은 비워도 된다.")]
        [SerializeField] string dialogueNode;

        [Tooltip("다시 조사하면 재생할 짧은 대사 노드 이름. 아이템은 비워도 된다.")]
        [SerializeField] string recheckDialogueNode;

        [Tooltip("사망: 사망 트리거 id / 퍼즐: 퍼즐 id. 다른 종류는 비운다.")]
        [SerializeField] string link;

        public string Id => id;
        public ObjectKind Kind => kind;
        public string DialogueNode => dialogueNode;
        public string RecheckDialogueNode => recheckDialogueNode;
        public string Link => link;

        public List<string> CollectRuleWarnings()
        {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
                warnings.Add("id가 비어 있다.");

            bool usesLink = kind == ObjectKind.Death || kind == ObjectKind.Puzzle;
            if (!usesLink && !string.IsNullOrWhiteSpace(link))
                warnings.Add($"{kind} 종류에는 연결을 두지 않는다. (사망·퍼즐만)");

            if (kind == ObjectKind.Basic)
            {
                if (string.IsNullOrWhiteSpace(dialogueNode))
                    warnings.Add("기본 오브젝트인데 대사 노드가 비어 있다.");
                if (string.IsNullOrWhiteSpace(recheckDialogueNode))
                    warnings.Add("기본 오브젝트인데 재조사 대사 노드가 비어 있다.");
            }
            return warnings;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            foreach (var warning in CollectRuleWarnings())
                Debug.LogWarning($"[ObjectData] '{name}': {warning}", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(ObjectData), dialogueNode, recheckDialogueNode);
        }
#endif
    }
}
