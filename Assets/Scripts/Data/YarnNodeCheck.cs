using System.Collections.Generic;
using UnityEngine;

namespace Gaze.Data
{
    // 인스펙터에 적은 Yarn 노드 이름이 실제 Yarn 프로젝트에 있는지 검사함.
    // DialogueInteractable, DeathZone, DeathInteractable, DeathRefusalZone이 OnValidate에서 WarnMissingDeferred를 부름.
    public static class YarnNodeCheck
    {
        // nodes 중 existing에 없는 이름을 중복 없이 모아 돌려줌. 빈 이름은 건너뜀.
        public static List<string> FindMissing(IEnumerable<string> nodes, ICollection<string> existing)
        {
            var missing = new List<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node) || existing.Contains(node) || missing.Contains(node))
                    continue;
                missing.Add(node);
            }
            return missing;
        }

#if UNITY_EDITOR
        // 없는 노드마다 owner를 가리키는 경고를 남김. 에디터 전용.
        // OnValidate는 임포트 도중에도 불리고 그때 다른 에셋을 읽으면 안 되므로 한 틱 미룸.
        public static void WarnMissingDeferred(Object owner, string tag, params string[] nodes)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (owner == null)
                    return;
                foreach (var node in FindMissing(nodes, LoadProjectNodeNames()))
                    Debug.LogWarning($"[{tag}] '{owner.name}': 노드 '{node}'가 Yarn에 없다.", owner);
            };
        }

        // 프로젝트의 모든 YarnProject 에셋에서 노드 이름을 모음. 읽기 실패한 에셋은 경고 후 건너뜀.
        public static HashSet<string> LoadProjectNodeNames()
        {
            var names = new HashSet<string>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:YarnProject"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var project = UnityEditor.AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>(path);
                if (project == null)
                    continue;
                try
                {
                    names.UnionWith(project.NodeNames);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[YarnNodeCheck] '{path}'의 노드 목록을 읽지 못했다: {e.Message}", project);
                }
            }
            return names;
        }
#endif
    }
}
