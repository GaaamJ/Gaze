using System.Collections.Generic;
using UnityEngine;
using ProjectT.Death;

namespace ProjectT.Area
{
    // Core 오브젝트를 인스펙터로 참조하지 않는다. SceneLoader가 Scene 씬을 불러온 뒤 찾아 부른다.
    public class RoomSceneEntry : MonoBehaviour
    {
        internal void Enter()
        {
            // Apply가 다른 오브젝트를 끄기 전에 목록을 다 모아야 꺼진 쪽의 DeathAftermath도 빠지지 않는다.
            var aftermaths = new List<DeathAftermath>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                aftermaths.AddRange(root.GetComponentsInChildren<DeathAftermath>(true));

            foreach (DeathAftermath aftermath in aftermaths)
                aftermath.Apply();
        }
    }
}
