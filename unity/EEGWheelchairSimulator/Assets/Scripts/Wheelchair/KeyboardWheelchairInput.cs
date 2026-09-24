using UnityEngine;
using UnityEngine.InputSystem;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WheelchairMovement))]
    public sealed class KeyboardWheelchairInput : MonoBehaviour
    {
        private WheelchairMovement movement;

        private void Awake()
        {
            movement = GetComponent<WheelchairMovement>();
        }

        private void Update()
        {
            if (movement.ControlSource != WheelchairControlSource.Keyboard) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            float turn = (right ? 1f : 0f) - (left ? 1f : 0f);

            movement.ApplyInput(forward, turn, Time.deltaTime);
        }
    }
}
