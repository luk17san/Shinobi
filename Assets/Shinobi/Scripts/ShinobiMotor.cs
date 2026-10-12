using UnityEngine;

namespace Shinobi
{
    // Executes movement independently of input devices and state decisions.
    public sealed class ShinobiMotor
    {
        private readonly CharacterController controller;
        private readonly Transform transform;
        private readonly Transform visual;
        private readonly Vector3 spawnPosition;
        private float moveSpeed, jumpHeight, gravity, arenaHalfWidth;
        public ShinobiInput Input { get; set; }
        public float VerticalSpeed { get; private set; }
        public bool Grounded => controller.isGrounded && VerticalSpeed <= 0f;
        public bool Moving => Mathf.Abs(Input.Move) > 0.01f;
        public bool AllowMovement { get; set; } = true;
        public float Facing { get; private set; } = 1f;
        public ShinobiCombat Combat { get; }
        public ShinobiMotor(CharacterController controller, Transform transform, Transform visual,
            float moveSpeed, float jumpHeight, float gravity, float arenaHalfWidth, ShinobiCombat combat = null)
        {
            this.controller = controller;
            this.transform = transform;
            this.visual = visual;
            Combat = combat;
            spawnPosition = transform.position;
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Configure(moveSpeed, jumpHeight, gravity, arenaHalfWidth);
        }
        public void Configure(float speed, float height, float acceleration, float halfWidth)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
            jumpHeight = Mathf.Max(0.1f, height);
            gravity = Mathf.Clamp(acceleration, -60f, -1f);
            arenaHalfWidth = Mathf.Max(1f, halfWidth);
        }
        public void StickToGround() { VerticalSpeed = -2f; }
        public void Jump() { VerticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity); }
        public void ApplyGravity(float dt) { VerticalSpeed += gravity * dt; }
        public void Move(float dt)
        {
            float nextX = Mathf.Clamp(transform.position.x + (AllowMovement ? Input.Move : 0f) * moveSpeed * dt,
                -arenaHalfWidth, arenaHalfWidth);
            var collisions = controller.Move(new Vector3(nextX - transform.position.x,
                VerticalSpeed * dt, spawnPosition.z - transform.position.z));
            if ((collisions & CollisionFlags.Above) != 0 && VerticalSpeed > 0f) VerticalSpeed = 0f;
            if ((collisions & CollisionFlags.Below) != 0 && VerticalSpeed < 0f) StickToGround();
            if (AllowMovement && Moving)
            {
                Facing = Input.Move > 0f ? 1f : -1f;
                if (visual != null) visual.localRotation = Quaternion.Euler(0f, Facing > 0f ? 90f : -90f, 0f);
            }
        }
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            VerticalSpeed = 0f;
            Input = default;
            AllowMovement = true;
            controller.enabled = true;
        }
    }
}
