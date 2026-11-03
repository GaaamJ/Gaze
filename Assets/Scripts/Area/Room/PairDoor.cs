using UnityEngine;
using UnityEngine.Serialization;

namespace Gaze.Area
{
    // 행선지가 하나인 문. targetScene의 targetDoorId 문으로 이어짐.
    // 양쪽 Room의 문이 서로를 가리키는 쌍으로 쓰임.
    public class PairDoor : Door
    {
        [Tooltip("넘어갈 Scene 이름")]
        [FormerlySerializedAs("targetRoom")]
        [SerializeField] string targetScene;
        [Tooltip("넘어간 Scene에서 도착할 문의 id")]
        [SerializeField] string targetDoorId;

        protected override bool ResolveTarget(out string scene, out string doorId)
        {
            scene = targetScene;
            doorId = targetDoorId;
            return true;
        }
    }
}
