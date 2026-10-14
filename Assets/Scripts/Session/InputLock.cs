using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Session
{
    public class InputLock : MonoBehaviour
    {
        readonly Dictionary<InputLockReason, int> counts = new Dictionary<InputLockReason, int>();
        int total;

        // 씬을 다시 불러와도 사망 잠금이 이어지도록 정적으로 둔다. 사망 처리(#52)가 켜고 끌 예정이며 지금은 켜는 곳이 없다.
        public static bool KeepDeathLock { get; internal set; }

        public bool IsLocked => total > 0;

        void Awake()
        {
            if (KeepDeathLock)
                Lock(InputLockReason.Death);
        }

        public void Lock(InputLockReason reason)
        {
            counts.TryGetValue(reason, out int count);
            counts[reason] = count + 1;
            total++;
        }

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
    }
}
