using System.Collections.Generic;

namespace ProjectT.Area
{
    public class MarkerIndex<T>
    {
        readonly Dictionary<(string areaId, string markerId), T> entries = new();

        public int Count => entries.Count;

        public bool TryAdd(string areaId, string markerId, T value)
        {
            if (string.IsNullOrWhiteSpace(areaId) || string.IsNullOrWhiteSpace(markerId))
                return false;
            return entries.TryAdd((areaId, markerId), value);
        }

        public bool TryGet(string areaId, string markerId, out T value)
        {
            if (areaId == null || markerId == null)
            {
                value = default;
                return false;
            }
            return entries.TryGetValue((areaId, markerId), out value);
        }

        public void Clear() => entries.Clear();
    }
}
