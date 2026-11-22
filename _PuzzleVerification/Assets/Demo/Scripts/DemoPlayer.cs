using UnityEngine;
using UnityEngine.InputSystem;

namespace Hakaton.Demo
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoPlayer : MonoBehaviour
    {
        public Camera view;
        public Light flashlight;
        [Range(0.03f, 0.3f)] public float sensitivity = 0.11f;
        public float walkSpeed = 2.25f;
        public float runSpeed = 4.1f;
        public DemoGame game;
        public DemoInteractable Focused { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        CharacterController controller;
        float pitch, fallSpeed, stepTime;
        bool interactHeld, flashlightHeld, clickHeld;

        void Awake()
        {
            controller=GetComponent<CharacterController>();
            if(PlayerPrefs.HasKey(DemoPreferences.MouseKey))sensitivity=DemoPreferences.MouseSensitivity;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool interact = keyboard != null && keyboard.eKey.isPressed;
            bool light = keyboard != null && keyboard.fKey.isPressed;
            bool click = mouse != null && mouse.leftButton.isPressed;
            // Read edges once in the player loop, including when an overlay blocks movement.
            bool interactPressed = interact && !interactHeld;
            bool flashlightPressed = light && !flashlightHeld;
            bool clickPressed = click && !clickHeld;
            interactHeld = interact;
            flashlightHeld = light;
            clickHeld = click;
            if (keyboard == null || game == null || game.Blocked)
            {
                IsMoving = IsRunning = false;
                Focused = null;
                return;
            }

            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Mouse.current.delta.ReadValue() * sensitivity;
                transform.Rotate(0, look.x, 0);
                pitch = Mathf.Clamp(pitch - look.y, -78, 78);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            IsMoving = input.sqrMagnitude > 0.01f;
            IsRunning = IsMoving && keyboard.leftShiftKey.isPressed;
            float speed = IsRunning ? runSpeed : walkSpeed;
            if (controller.isGrounded && fallSpeed < 0) fallSpeed = -2;
            fallSpeed -= 16 * Time.deltaTime;
            Vector3 movement = (transform.right * input.x + transform.forward * input.y) * speed;
            movement.y = fallSpeed;
            controller.Move(movement * Time.deltaTime);
            if (IsMoving && controller.isGrounded)
            {
                stepTime -= Time.deltaTime;
                if (stepTime <= 0)
                {
                    game.sound.Footstep(IsRunning);
                    stepTime = IsRunning ? 0.31f : 0.49f;
                }
            }

            Focused = null;
            if (Physics.Raycast(view.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out var hit, 2.7f,
                    ~0, QueryTriggerInteraction.Ignore))
                Focused = hit.collider.GetComponentInParent<DemoInteractable>();
            if (interactPressed && Focused != null) Focused.Interact(game);
            else if (Focused != null && Focused.kind == DemoInteractable.Interaction.Car &&
                clickPressed)
                Focused.Interact(game);
            if (flashlightPressed) flashlight.enabled = !flashlight.enabled;
        }

        public void Teleport(Vector3 position, float yaw)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            pitch = fallSpeed = 0;
            view.transform.localRotation = Quaternion.identity;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        public void AimAt(Vector3 position)
        {
            Vector3 direction = position - view.transform.position;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -78, 78);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
    }
}
