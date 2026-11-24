using UnityEngine;

namespace Hakaton.Demo
{
    // Evidence prop with hover highlight (MaterialPropertyBlock tint) for runtime-spawned
    // study props that are not part of the kit clue prefabs.
    public class HighlightableEvidence : DemoInteractable
    {
        Renderer propRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;

        void Awake()
        {
            kind = Interaction.Evidence;
            propRenderer = GetComponentInChildren<Renderer>();
            if (propRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                propRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = propRenderer.sharedMaterial != null ? propRenderer.sharedMaterial.color : Color.white;
            }
        }

        public override void Highlight(bool on)
        {
            if (propRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            propRenderer.SetPropertyBlock(propBlock);
        }
    }
}
