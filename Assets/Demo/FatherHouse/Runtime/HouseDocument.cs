using UnityEngine;
namespace Hakaton.Demo.FatherHouse
{
    public sealed class HouseDocument : DemoInteractable
    {
        public string heading;
        [TextArea(4,12)]public string body;
        public override void Interact(DemoGame game)=>game.hud.ShowDocument(heading,body);

        Renderer docRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;

        void Awake()
        {
            docRenderer = GetComponentInChildren<Renderer>();
            if (docRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                docRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = docRenderer.material.color;
            }
        }

        public override void Highlight(bool on)
        {
            if (docRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            docRenderer.SetPropertyBlock(propBlock);
        }
    }
}
