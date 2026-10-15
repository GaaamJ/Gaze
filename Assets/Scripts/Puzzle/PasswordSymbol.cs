using System;
using UnityEngine;
using ProjectT.Interaction;

namespace ProjectT.Puzzle
{
    public class PasswordSymbol : Interactable
    {
        [Tooltip("켜짐/꺼짐에 따라 색을 바꿀 자식 스프라이트.")]
        [SerializeField] SpriteRenderer[] symbolRenderers = Array.Empty<SpriteRenderer>();
        [SerializeField] Color unlitColor = Color.white;
        [SerializeField] Color litColor = new Color(1f, 0.9f, 0.2f, 1f);

        public event Action<PasswordSymbol> Pressed;

        public bool IsLit { get; private set; }

        public override void Interact(InteractContext context)
        {
            Pressed?.Invoke(this);
        }

        public void SetLit(bool lit)
        {
            IsLit = lit;
            Color color = lit ? litColor : unlitColor;
            foreach (SpriteRenderer symbolRenderer in symbolRenderers)
            {
                if (symbolRenderer != null)
                    symbolRenderer.color = color;
            }
        }
    }
}
