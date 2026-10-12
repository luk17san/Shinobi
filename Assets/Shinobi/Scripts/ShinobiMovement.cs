using UnityEngine;

namespace Shinobi
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ShinobiMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 2f;
        [SerializeField, Range(-60f, -1f)] private float gravity = -20f;
        [SerializeField, Min(1f)] private float arenaHalfWidth = 12f;
        [SerializeField] private Transform visual;
        private ShinobiMotor motor;
        private PlayerMovementState state;

        public string ActiveState => state == null ? "Inactive" : "Player/" + state.ActivePath;

        private void Awake()
        {
            motor = new ShinobiMotor(GetComponent<CharacterController>(), transform, visual,
                moveSpeed, jumpHeight, gravity, arenaHalfWidth, GetComponent<ShinobiCombat>());
            state = new PlayerMovementState(motor);
        }

        private void OnEnable() { if (state != null) state.Enter(); }
        private void OnDisable() { if (state != null) state.Exit(); }

        private void Update()
        {
            var input = ShinobiInput.Read();
            if (input.Reset || transform.position.y < -10f)
            {
                state.Exit();
                motor.ResetPosition();
                state.Enter();
                return;
            }
            motor.Configure(moveSpeed, jumpHeight, gravity, arenaHalfWidth);
            motor.Input = input;
            state.Tick(Time.deltaTime);
            motor.Move(Time.deltaTime);
            state.SynchronizeAfterMove();
        }
    }
}
