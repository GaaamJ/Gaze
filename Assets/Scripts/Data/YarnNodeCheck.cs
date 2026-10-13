using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    public static class YarnNodeCheck
    {
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
        // OnValidate는 임포트 도중에도 불리고 그때 다른 에셋을 읽으면 안 되므로 한 틱 미룬다.
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
