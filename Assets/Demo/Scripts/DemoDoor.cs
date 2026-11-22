using UnityEngine;

namespace Hakaton.Demo
{
    public sealed class DemoDoor : DemoInteractable
    {
        public enum LockMode { None, Permanent, Item, Puzzle }
        public bool isOpen;
        public bool kitchenDoor;
        public float openAngle = 100;
        public LockMode lockMode;
        public string requirement;
        [TextArea] public string lockedMessage = "Дверь заперта.";
        [TextArea] public string unlockedMessage;
        float currentAngle;
        bool accessConfirmed;
        Renderer doorRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;
        void Awake()
        {
            currentAngle = Mathf.DeltaAngle(0, transform.localEulerAngles.y);
            doorRenderer = GetComponentInChildren<Renderer>();
            if (doorRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                doorRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = doorRenderer.material.color;
            }
        }
        public override string Prompt => (isOpen ? "Закрыть · " : lockMode != LockMode.None ? "Заперто · " : "Открыть · ") + label;

        public bool HasAccess(DemoGame game)
        {
            switch (lockMode)
            {
                case LockMode.None: return true;
                case LockMode.Item: return !string.IsNullOrWhiteSpace(requirement) && game.HasItem(requirement);
                case LockMode.Puzzle: return !string.IsNullOrWhiteSpace(requirement) && game.GetPuzzleState(requirement) == PuzzleState.Solved;
                default: return false;
            }
        }

        public override void Highlight(bool on)
        {
            if (doorRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            doorRenderer.SetPropertyBlock(propBlock);
        }

        void Update()
        {
            currentAngle = Mathf.MoveTowards(currentAngle, isOpen ? openAngle : 0, 160 * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        }

        public override void Interact(DemoGame game)
        {
            if (!isOpen && !HasAccess(game))
            {
                game.sound.Knock(transform.position);
                game.hud.Notify(lockedMessage);
                return;
            }
            bool unlocking = !isOpen && lockMode != LockMode.None && !accessConfirmed;
            accessConfirmed |= unlocking;
            isOpen = !isOpen;
            game.sound.Creak(transform.position);
            if (unlocking && !string.IsNullOrWhiteSpace(unlockedMessage)) game.hud.Notify(unlockedMessage);
            if (!isOpen && kitchenDoor && game.Phase == DemoGame.Stage.Chase && game.InKitchen)
                game.Escape();
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            currentAngle = open ? openAngle : 0;
            transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        }
    }
}
