using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Session
{
    // 입력 잠금을 사유별 횟수로 셈. 하나라도 걸려 있으면 IsLocked가 참.
    // 사유마다 Lock과 Unlock을 짝지어 부르고, 씬을 다시 시작할 때는 CoreEntry가 Clear로 비움.
    public class InputLock : MonoBehaviour
    {
        readonly Dictionary<InputLockReason, int> counts = new Dictionary<InputLockReason, int>();
        int total;

        public bool IsLocked => total > 0;

        public void Lock(InputLockReason reason)
        {
            counts.TryGetValue(reason, out int count);
            counts[reason] = count + 1;
            total++;
        }

        // 걸지 않은 사유를 풀면 에러 로그만 남기고 무시함.
        public void Unlock(InputLockReason reason)
        {
            counts.TryGetValue(reason, out int count);
            if (count == 0)
            {
                Debug.LogError($"[InputLock] 걸지 않은 잠금 사유({reason})를 풀려고 했다.", this);
                return;
            }

            counts[reason] = count - 1;
            total--;
        }

        // 모든 사유의 잠금을 한꺼번에 비움.
        public void Clear()
        {
            counts.Clear();
            total = 0;
        }
    }
}
