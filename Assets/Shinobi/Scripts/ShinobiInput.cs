using UnityEngine;
using UnityEngine.InputSystem;

namespace Shinobi
{
    public readonly struct ShinobiInput
    {
        public float Move { get; }
        public bool Jump { get; }
        public bool Reset { get; }
        public bool Attack { get; }
        public ShinobiInput(float move, bool jump, bool reset, bool attack = false)
        {
            Move = Mathf.Clamp(move, -1f, 1f);
            Jump = jump;
            Reset = reset;
            Attack = attack;
        }
        public static ShinobiInput Read()
        {
            float move = 0f;
            bool jump = false;
            bool reset = false;
            bool attack = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                jump = keyboard.spaceKey.wasPressedThisFrame;
                reset = keyboard.rKey.wasPressedThisFrame;
                attack |= keyboard.jKey.wasPressedThisFrame;
            }
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stick = gamepad.leftStick.ReadValue().x;
                if (Mathf.Abs(stick) > 0.15f) move = stick;
                jump |= gamepad.buttonSouth.wasPressedThisFrame;
                attack |= gamepad.buttonWest.wasPressedThisFrame;
            }
            return new ShinobiInput(move, jump, reset, attack);
        }
    }
}
