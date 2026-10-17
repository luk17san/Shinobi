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
        public bool Crouch { get; }
        public bool NextWeapon { get; }
        public bool ThrowWeapon { get; }
        public int DirectionTap { get; }
        public ShinobiInput(float move, bool jump, bool reset, bool attack = false, bool crouch = false, bool nextWeapon = false, int directionTap = 0, bool throwWeapon = false)
        {
            Move = Mathf.Clamp(move, -1f, 1f);
            Jump = jump;
            Reset = reset;
            Attack = attack;
            Crouch = crouch;
            NextWeapon = nextWeapon;
            DirectionTap = directionTap;
            ThrowWeapon = throwWeapon;
        }
        public static ShinobiInput Read(ShinobiCombatProfile profile = null)
        {
            float move = 0f;
            bool jump = false;
            bool reset = false;
            bool attack = false;
            bool crouch = false;
            bool nextWeapon = false;
            bool throwWeapon = false;
            int directionTap = 0;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                move = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                jump = keyboard[profile != null ? profile.jumpKey : Key.W].wasPressedThisFrame;
                crouch = keyboard[profile != null ? profile.crouchKey : Key.S].isPressed;
                reset = keyboard.rKey.wasPressedThisFrame;
                attack = keyboard[profile != null ? profile.meleeKey : Key.G].wasPressedThisFrame;
                nextWeapon = keyboard[profile != null ? profile.nextWeaponKey : Key.Y].wasPressedThisFrame;
                throwWeapon = keyboard[profile != null ? profile.throwKey : Key.H].wasPressedThisFrame;
                // A held key is not a new tap. Both directions together invalidate the gesture.
                directionTap = keyboard.aKey.isPressed && keyboard.dKey.isPressed ? 2
                    : keyboard.aKey.wasPressedThisFrame ? -1 : keyboard.dKey.wasPressedThisFrame ? 1 : 0;
            }
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stick = gamepad.leftStick.ReadValue().x;
                if (Mathf.Abs(stick) > 0.15f) move = stick;
                jump |= gamepad.buttonSouth.wasPressedThisFrame;
                attack |= gamepad.buttonWest.wasPressedThisFrame;
            }
            return new ShinobiInput(move, jump, reset, attack, crouch, nextWeapon, directionTap, throwWeapon);
        }
    }
}
