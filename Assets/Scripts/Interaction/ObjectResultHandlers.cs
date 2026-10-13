using System;
using System.Collections.Generic;
using ProjectT.Data;

namespace ProjectT.Interaction
{
    public class ObjectResultHandlers
    {
        readonly Dictionary<ObjectKind, Action<Interactable, InteractContext>> handlers = new();

        // 종류마다 하나만 둔다. 등록된 핸들러가 기본 결과(대사 재생)를 대신한다.
        public void Register(ObjectKind kind, Action<Interactable, InteractContext> handler)
        {
            handlers[kind] = handler;
        }

        public void Unregister(ObjectKind kind, Action<Interactable, InteractContext> handler)
        {
            if (handlers.TryGetValue(kind, out var current) && current == handler)
                handlers.Remove(kind);
        }

        public bool TryGet(ObjectKind kind, out Action<Interactable, InteractContext> handler) =>
            handlers.TryGetValue(kind, out handler);
    }
}
