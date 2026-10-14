using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectT.Data
{
    // 에셋에 숫자로 저장되므로 기존 값은 바꾸지 않고 새 값은 뒤에 추가한다.
    public enum DeathTriggerKind
    {
        Location = 0,
        Investigate = 1,
        Metafiction = 2
    }

    public enum DeathCondition
    {
        EnterArea = 0,
        InvestigateObject = 1,
        CloseWindow = 2,
        ElevatorFileTamper = 3
    }

    public enum CorpseLocation
    {
        DeathPlace = 0,
        CorpseFolder = 1
    }

    [Serializable]
    public struct ResultSignNode
    {
        [Tooltip("판정 결과 이름")]
        public string resultId;

        [Tooltip("이 결과일 때 게임 오버 사인 문구 노드")]
        public string node;
    }

    [CreateAssetMenu(fileName = "DeathData", menuName = "ProjectT/Death Data")]
    public class DeathData : ScriptableObject
    {
        [Tooltip("사망 트리거 고유 이름. 노드 이름과 다른 데이터의 연결에 쓴다.")]
        [SerializeField] string id;

        [Tooltip("장소 / 조사 / 메타픽션")]
        [SerializeField] DeathTriggerKind triggerKind;

        [Tooltip("발동 조건")]
        [SerializeField] DeathCondition condition;

        [Tooltip("조사 조건일 때 조사할 오브젝트 id")]
        [SerializeField] string conditionObjectId;

        [Tooltip("사망 연출 컷씬 id")]
        [SerializeField] string cutsceneId;

        [Tooltip("사망 대사 노드. 규칙: Death_{id}")]
        [SerializeField] string deathNode;

        [Tooltip("게임 오버 사인 문구 노드. 규칙: Death_{id}_Sign")]
        [SerializeField] string gameOverSignNode;

        [Tooltip("판정 결과별 사인 문구 노드. 엘리베이터 파일 비정상 조작만. 규칙: Death_{id}_Sign_{결과}")]
        [SerializeField] List<ResultSignNode> resultSignNodes = new();

        [Tooltip("시체가 남는 곳")]
        [SerializeField] CorpseLocation corpseLocation;

        [Tooltip("재방문 때 거부하는지. 메타픽션과 비밀번호 문 오답은 거부하지 않는다.")]
        [SerializeField] bool refuses;

        [Tooltip("거부 대사 노드. 장소·조사만. 규칙: Death_{id}_Refuse")]
        [SerializeField] string refuseNode;

        [Tooltip("재발동 대사 노드. 거부하지 않는 트리거만. 규칙: Death_{id}_Retrigger")]
        [SerializeField] string retriggerNode;

        public string Id => id;
        public DeathTriggerKind TriggerKind => triggerKind;
        public DeathCondition Condition => condition;
        public string ConditionObjectId => conditionObjectId;
        public string CutsceneId => cutsceneId;
        public string DeathNode => deathNode;
        public string GameOverSignNode => gameOverSignNode;
        public IReadOnlyList<ResultSignNode> ResultSignNodes => resultSignNodes;
        public CorpseLocation CorpseLocation => corpseLocation;
        public bool Refuses => refuses;
        public string RefuseNode => refuseNode;
        public string RetriggerNode => retriggerNode;

        public List<string> CollectRuleWarnings()
        {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
                warnings.Add("id가 비어 있다.");

            bool metafiction = triggerKind == DeathTriggerKind.Metafiction;
            if (metafiction && refuses)
                warnings.Add("메타픽션 트리거는 거부할 수 없다.");
            if (metafiction && !string.IsNullOrWhiteSpace(refuseNode))
                warnings.Add("메타픽션 트리거에는 거부 대사 노드를 두지 않는다.");
            if (refuses && string.IsNullOrWhiteSpace(refuseNode))
                warnings.Add("거부하는 트리거인데 거부 대사 노드가 비어 있다.");
            if (refuses && !string.IsNullOrWhiteSpace(retriggerNode))
                warnings.Add("거부하는 트리거에는 재발동 대사 노드를 두지 않는다.");
            if (!refuses && string.IsNullOrWhiteSpace(retriggerNode))
                warnings.Add("거부하지 않는 트리거인데 재발동 대사 노드가 비어 있다.");
            if (resultSignNodes.Count > 0 && condition != DeathCondition.ElevatorFileTamper)
                warnings.Add("판정 결과별 사인 문구는 엘리베이터 파일 비정상 조작만 쓴다.");
            if (!ConditionMatchesKind())
                warnings.Add($"조건 {condition}은 트리거 종류 {triggerKind}와 맞지 않는다.");
            return warnings;
        }

        bool ConditionMatchesKind()
        {
            return triggerKind switch
            {
                DeathTriggerKind.Location => condition == DeathCondition.EnterArea,
                DeathTriggerKind.Investigate => condition == DeathCondition.InvestigateObject,
                DeathTriggerKind.Metafiction => condition == DeathCondition.CloseWindow || condition == DeathCondition.ElevatorFileTamper,
                _ => false
            };
        }

        IEnumerable<string> AllNodes()
        {
            yield return deathNode;
            yield return gameOverSignNode;
            yield return refuseNode;
            yield return retriggerNode;
            foreach (var entry in resultSignNodes)
                yield return entry.node;
        }

#if UNITY_EDITOR
        [ContextMenu("노드 이름 규칙으로 채우기")]
        void FillNodeNamesFromId()
        {
            UnityEditor.Undo.RecordObject(this, "Fill Death Node Names");
            deathNode = DialogueNodeNames.Death(id);
            gameOverSignNode = DialogueNodeNames.DeathSign(id);
            refuseNode = refuses ? DialogueNodeNames.DeathRefuse(id) : "";
            retriggerNode = refuses ? "" : DialogueNodeNames.DeathRetrigger(id);
            for (int i = 0; i < resultSignNodes.Count; i++)
            {
                var entry = resultSignNodes[i];
                entry.node = DialogueNodeNames.DeathSign(id, entry.resultId);
                resultSignNodes[i] = entry;
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void OnValidate()
        {
            foreach (var warning in CollectRuleWarnings())
                Debug.LogWarning($"[DeathData] '{name}': {warning}", this);
            YarnNodeCheck.WarnMissingDeferred(this, nameof(DeathData), AllNodes().ToArray());
        }
#endif
    }
}
