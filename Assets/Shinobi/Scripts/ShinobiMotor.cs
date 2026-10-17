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
        private readonly float standingHeight;
        private readonly Vector3 standingCenter, standingVisualScale;
        private readonly Collider[] standingOverlaps = new Collider[32];
        private float crouchSpeedMultiplier = 0.4f;
        private float dashRemaining, dashSpeed, dashDirection;
        public bool Dashing => dashRemaining > 0f;
        public bool Crouching { get; private set; }
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
            standingHeight = controller.height;
            standingCenter = controller.center;
            standingVisualScale = visual != null ? visual.localScale : Vector3.one;
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
        public void Push(float distance) {
            float next = Mathf.Clamp(transform.position.x + distance, -arenaHalfWidth, arenaHalfWidth);
            controller.Move(Vector3.right * (next - transform.position.x));
        }
        public void Jump() { VerticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity); }
        public void ApplyGravity(float dt) { VerticalSpeed += gravity * dt; }
        public void StartDash(float direction, float distance, float duration)
        {
            dashDirection = direction > 0f ? 1f : -1f;
            dashRemaining = Mathf.Max(0.01f, duration);
            dashSpeed = Mathf.Max(0.1f, distance) / dashRemaining;
            Facing = dashDirection;
            if (visual != null) visual.localRotation = Quaternion.Euler(0, Facing > 0 ? 90 : -90, 0);
        }
        public void Face(float direction)
        {
            Facing = direction < 0 ? -1 : 1;
            if (visual != null) visual.localRotation = Quaternion.Euler(0, Facing > 0 ? 90 : -90, 0);
        }
        public void CancelDash() { dashRemaining = 0f; }
        public void UpdateCrouch(bool requested, float heightRatio, float speedMultiplier)
        {
            crouchSpeedMultiplier = Mathf.Clamp01(speedMultiplier);
            bool crouch = requested && Grounded;
            if (!crouch && Crouching && !CanStand()) crouch = true;
            if (crouch == Crouching) return;
            Crouching = crouch;
            float height = crouch ? Mathf.Max(controller.radius * 2f, standingHeight * Mathf.Clamp(heightRatio, 0.4f, 0.9f)) : standingHeight;
            controller.height = height;
            controller.center = standingCenter + Vector3.down * ((standingHeight - height) * 0.5f);
            if (visual != null)
                visual.localScale = new Vector3(standingVisualScale.x, standingVisualScale.y * height / standingHeight, standingVisualScale.z);
        }
        private bool CanStand()
        {
            var scale = transform.lossyScale;
            float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float half = Mathf.Max(0f, standingHeight * Mathf.Abs(scale.y) * 0.5f - radius);
            var center = transform.TransformPoint(standingCenter);
            int count = Physics.OverlapCapsuleNonAlloc(center - transform.up * half, center + transform.up * half,
                Mathf.Max(0.01f, radius - 0.01f), standingOverlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == standingOverlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var other = standingOverlaps[i];
                if (other == controller || other.transform.IsChildOf(transform)) continue;
                if (Physics.GetIgnoreLayerCollision(controller.gameObject.layer, other.gameObject.layer) || Physics.GetIgnoreCollision(controller, other)) continue;
                return false;
            }
            return true;
        }
        public void RestoreStanding()
        {
            Crouching = false;
            controller.height = standingHeight;
            controller.center = standingCenter;
            if (visual != null) visual.localScale = standingVisualScale;
        }
        public void Move(float dt)
        {
            bool dashing = Dashing;
            float horizontal = dashing ? dashDirection * dashSpeed * Mathf.Min(dt, dashRemaining)
                : (AllowMovement ? Input.Move : 0f) * moveSpeed * (Crouching ? crouchSpeedMultiplier : 1f) * dt;
            float requestedX = transform.position.x + horizontal;
            float nextX = Mathf.Clamp(requestedX, -arenaHalfWidth, arenaHalfWidth);
            var collisions = controller.Move(new Vector3(nextX - transform.position.x,
                VerticalSpeed * dt, spawnPosition.z - transform.position.z));
            if ((collisions & CollisionFlags.Above) != 0 && VerticalSpeed > 0f) VerticalSpeed = 0f;
            if ((collisions & CollisionFlags.Below) != 0 && VerticalSpeed < 0f) StickToGround();
            if (dashing)
            {
                dashRemaining = Mathf.Max(0, dashRemaining - dt);
                if ((collisions & CollisionFlags.Sides) != 0 || nextX != requestedX) CancelDash();
            }
            if (!dashing && AllowMovement && Moving)
            {
                Facing = Input.Move > 0f ? 1f : -1f;
                if (visual != null) visual.localRotation = Quaternion.Euler(0f, Facing > 0f ? 90f : -90f, 0f);
            }
        }
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            CancelDash();
            RestoreStanding();
            VerticalSpeed = 0f;
            Input = default;
            AllowMovement = true;
            controller.enabled = true;
        }
    }
}

