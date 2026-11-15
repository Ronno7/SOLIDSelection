using UnityEngine;
using UnityEngine.InputSystem;

namespace SolidSelection
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [Min(0.1f), SerializeField] private float moveSpeed = 4f;
        [Min(0.01f), SerializeField] private float mouseSensitivity = 0.12f;
        private CharacterController controller;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float spawnPitch;
        private float pitch;
        private float verticalSpeed;
        public Transform View { get => view; set => view = value; }

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnPitch = view != null ? Mathf.DeltaAngle(0f, view.localEulerAngles.x) : 0f;
            pitch = spawnPitch;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ReleaseCursor();
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) ResetPosition();
            if (Cursor.lockState != CursorLockMode.Locked || view == null) return;

            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            }
            input = Vector2.ClampMagnitude(input, 1f);
            float speed = moveSpeed * (keyboard != null && keyboard.leftShiftKey.isPressed ? 1.7f : 1f);
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var velocity = (transform.right * input.x + transform.forward * input.y) * speed;
            velocity.y = verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
            if (transform.position.y < -20f) ResetPosition();
        }

        public void ResetPosition()
        {
            if (controller == null) return;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            pitch = spawnPitch;
            if (view != null) view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            verticalSpeed = 0f;
            controller.enabled = true;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnDisable() => ReleaseCursor();
        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
