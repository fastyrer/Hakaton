using UnityEngine;

namespace Hakaton.Demo
{
    public sealed class DemoDoor : DemoInteractable
    {
        public bool isOpen;
        public bool kitchenDoor;
        public float openAngle = 100;
        float currentAngle;
        void Awake() => currentAngle = Mathf.DeltaAngle(0, transform.localEulerAngles.y);
        public override string Prompt => (isOpen ? "Закрыть · " : "Открыть · ") + label;

        void Update()
        {
            currentAngle = Mathf.MoveTowards(currentAngle, isOpen ? openAngle : 0, 160 * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        }

        public override void Interact(DemoGame game)
        {
            isOpen = !isOpen;
            game.sound.Creak(transform.position);
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
