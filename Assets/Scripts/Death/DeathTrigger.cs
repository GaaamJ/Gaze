// 무엇: 장소 사망 트리거. 라리스의 몸이 이 영역에 들어오면 DeathSequence.Begin(deathData)를 부른다.
// 어떻게 쓰나: 사망 장소 오브젝트에 Collider2D(Is Trigger)와 이 컴포넌트를 붙이고 'deathData'에 DeathData 에셋을 연결한다.
//             컴포넌트를 붙이면 콜라이더의 Is Trigger가 자동으로 켜진다.
// 주의:
//   - 조사 감지 원(트리거)이나 다른 물체는 무시한다. 판별 규칙은 PlayerMovement.TryFrom 한 곳에 있다.
//   - 이미 사망 중이면 DeathSequence가 무시하므로 여러 트리거가 겹쳐도 사망은 한 번만 시작된다.
//   - 재방문 거부(#55)는 하지 않는다.
using UnityEngine;
using ProjectT.Data;
using ProjectT.Player;

namespace ProjectT.Death
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathTrigger : MonoBehaviour
    {
        [SerializeField] DeathData deathData;

        void Reset()
        {
            if (TryGetComponent(out Collider2D area))
                area.isTrigger = true;
        }

        void Awake()
        {
            if (deathData == null)
                Debug.LogError($"[DeathTrigger] '{name}': 'deathData' 참조가 비어 있다. 씬에서 연결해야 한다.", this);
            if (!GetComponent<Collider2D>().isTrigger)
                Debug.LogError($"[DeathTrigger] '{name}': 콜라이더의 Is Trigger가 꺼져 있어 들어와도 반응하지 않는다.", this);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (deathData != null && PlayerMovement.TryFrom(other, out _))
                DeathSequence.Begin(deathData);
        }
    }
}
