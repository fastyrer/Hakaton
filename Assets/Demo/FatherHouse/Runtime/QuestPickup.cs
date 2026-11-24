using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    public sealed class QuestPickup : DemoInteractable
    {
        public string itemId;
        [TextArea] public string successMessage = "Предмет получен.";
        [TextArea] public string monolog;
        public override string Prompt => "Взять · " + label;
        Renderer pickupRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;

        void Awake()
        {
            pickupRenderer = GetComponentInChildren<Renderer>();
            if (pickupRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                pickupRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = pickupRenderer.material.color;
            }
        }

        public override void Highlight(bool on)
        {
            if (pickupRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            pickupRenderer.SetPropertyBlock(propBlock);
        }

        public override void Interact(DemoGame game)
        {
            if (!game.GrantItem(itemId))
            {
                game.hud.Notify("Этот предмет уже у вас.");
                gameObject.SetActive(false);
                return;
            }
            game.sound.Creak(transform.position);
            game.hud.Notify(successMessage, 4);
            if (!string.IsNullOrWhiteSpace(monolog))
            {
                var manager = FindAnyObjectByType<SubtitleManager>();
                if (manager == null) manager = new GameObject("Subtitles").AddComponent<SubtitleManager>();
                manager.ShowSubtitle(monolog, Mathf.Clamp(2f + monolog.Length * 0.055f, 3f, 8f));
            }
            gameObject.SetActive(false);
        }
    }
}
