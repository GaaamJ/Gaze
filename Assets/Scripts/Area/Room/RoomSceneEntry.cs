using System.Collections.Generic;
using UnityEngine;
using ProjectT.Death;

namespace ProjectT.Area
{
    // Room 씬마다 하나 두는 진입 지점. Room이 불러와지면 그 Room의 DeathAftermath를 모두 적용함.
    // Core 오브젝트를 인스펙터로 참조하지 않음. RoomSceneLoader.Enter가 Room 씬을 불러온 뒤 찾아 부름.
    public class RoomSceneEntry : MonoBehaviour
    {
        // 이 Room 씬의 모든 DeathAftermath(꺼진 오브젝트 포함)를 모은 뒤 하나씩 Apply.
        internal void Enter()
        {
            // Apply가 다른 오브젝트를 끄기 전에 목록을 다 모아야 꺼진 쪽의 DeathAftermath도 빠지지 않음.
            var aftermaths = new List<DeathAftermath>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                aftermaths.AddRange(root.GetComponentsInChildren<DeathAftermath>(true));

            foreach (DeathAftermath aftermath in aftermaths)
                aftermath.Apply();
        }
    }
}
