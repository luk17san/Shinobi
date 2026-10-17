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
        [SerializeField, Range(0.4f, 0.9f)] private float crouchHeightRatio = 0.6f;
        [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = 0.4f;
        [SerializeField, Min(0.1f)] private float dashDistance = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float dashDuration = 0.15f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.7f;
        [SerializeField, Min(0f)] private float dashStaminaCost = 20f;
        [SerializeField, Min(0.1f)] private float slideDistance = 2f;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float longJumpDistance = 6f;
        public AnimationClip slideAnimation, longJumpAnimation;
        private float nextDashTime = float.NegativeInfinity;
        private float lastDashTapTime = float.NegativeInfinity;
        private int lastDashTapDirection;
        private ShinobiMotor motor;
        private PlayerMovementState state;
        private ShinobiTechniqueController techniques;
        private ShinobiHealth health;
        private ShinobiWeaponInventory weapons;
        private ShinobiDefense defense;
        private ShinobiThrowController throwing;
        private bool interrupted;
        public event System.Action<float> DashStarted;

        public string ActiveState => state == null ? "Inactive" : "Player/" + state.ActivePath;
        public bool IsCrouching => motor != null && motor.Crouching;
        public bool IsGrounded => motor != null && motor.Grounded;
        public bool IsMoving => motor != null && motor.Moving;
        public float MoveAmount => motor != null ? Mathf.Abs(motor.Input.Move) : 0f;
        public float Facing => motor != null ? motor.Facing : 1f;
        public bool IsDashing => motor != null && motor.Dashing;
        public bool IsSliding => motor != null && motor.Sliding;
        public bool IsLongJumping => motor != null && motor.LongJumping;
        public float VerticalSpeed => motor != null ? motor.VerticalSpeed : 0;
        public bool TrySlide() {
            if (!ShinobiGameSession.GameplayInputAllowed || motor == null || !motor.Dashing || !motor.Grounded) return false;
            motor.StartSlide(slideDistance, slideDuration); return true;
        }
        public bool TryLongJump() { return ShinobiGameSession.GameplayInputAllowed && motor != null && motor.StartLongJump(longJumpDistance); }
        public float DashStaminaCost => Mathf.Max(0, dashStaminaCost);
        public bool DashReady => CanStartDash && (defense == null || defense.Stamina >= DashStaminaCost);
        public float DashCooldownRemaining => Mathf.Max(0, nextDashTime - Time.time);
        private bool CanStartDash => ShinobiGameSession.GameplayInputAllowed && isActiveAndEnabled && motor != null && !motor.Dashing && !motor.Sliding && motor.Grounded && !motor.Crouching
            && (throwing == null || !throwing.IsThrowing)
            && Time.time >= nextDashTime && (health == null || !health.Defeated) && (motor.Combat == null || !motor.Combat.IsAttacking)
            && (techniques == null || !techniques.Busy) && (defense == null || !defense.GuardBroken);
        public bool TryDash(float direction = 0)
        {
            if (!CanStartDash) return false;
            if (defense != null && !defense.TrySpendStamina(dashStaminaCost)) return false;
            motor.StartDash(Mathf.Abs(direction) > 0.01f ? direction : motor.Facing, dashDistance, dashDuration);
            nextDashTime = Time.time + Mathf.Max(0, dashCooldown);
            DashStarted?.Invoke(motor.Facing);
            return true;
        }
        private void ClearDashTap() { lastDashTapDirection = 0; lastDashTapTime = float.NegativeInfinity; }
        private int ReadDashGesture(ShinobiInput input, bool busy)
        {
            if (busy || !CanStartDash || input.Crouch)
            {
                ClearDashTap();
                return 0;
            }
            var profile = motor.Combat != null ? motor.Combat.profile : null;
            float window = profile != null ? Mathf.Clamp(profile.dashDoubleTapWindow, 0.1f, 0.5f) : 0.25f;
            if (Time.time - lastDashTapTime > window) ClearDashTap();
            int direction = input.DirectionTap;
            if (direction == 0) return 0;
            if (direction != -1 && direction != 1) { ClearDashTap(); return 0; }
            if (lastDashTapDirection == direction)
            {
                ClearDashTap();
                return direction;
            }
            lastDashTapDirection = direction;
            lastDashTapTime = Time.time;
            return 0;
        }
        public void Push(float distance) {
            if (motor != null) motor.Push(distance);
        }
        public void Face(float direction) { if (motor != null) motor.Face(direction); }
        public void ClearInputGestures() { ClearDashTap(); }
        public void ResetTrainingState()
        {
            state.Exit(); motor.ResetPosition();
            if (techniques != null) techniques.ResetTechnique();
            if (defense != null) defense.ResetDefense();
            if (throwing != null) throwing.ResetThrowState();
            nextDashTime = float.NegativeInfinity; ClearDashTap(); interrupted = false;
            state.Enter();
        }

        private void Awake()
        {
            motor = new ShinobiMotor(GetComponent<CharacterController>(), transform, visual,
                moveSpeed, jumpHeight, gravity, arenaHalfWidth, GetComponent<ShinobiCombat>());
            state = new PlayerMovementState(motor);
            techniques = GetComponent<ShinobiTechniqueController>();
            weapons = GetComponent<ShinobiWeaponInventory>();
            defense = GetComponent<ShinobiDefense>();
            throwing = GetComponent<ShinobiThrowController>();
        }

        private void OnEnable() {
            ClearDashTap();
            if (state != null) state.Enter();
            health = GetComponent<ShinobiHealth>();
            if (health != null) health.Damaged += OnDamaged;
        }
        private void OnDisable() {
            if (state != null) state.Exit();
            if (motor != null) motor.RestoreStanding();
            if (motor != null) motor.CancelSpecialMotion();
            ClearDashTap();
            if (health != null) health.Damaged -= OnDamaged;
        }
        private void OnDamaged() { InterruptAction(); }
        public void InterruptAction() { interrupted = true; ClearDashTap(); if (motor != null) motor.CancelSpecialMotion(); if (throwing != null) throwing.CancelThrow(); }

        private void Update()
        {
            if (!ShinobiGameSession.GameplayInputAllowed) return;
            if (throwing != null) throwing.TickThrow(Time.deltaTime);
            var opponent = GetComponent<ShinobiTrainingOpponent>();
            var input = opponent != null ? opponent.ReadInput() : ShinobiInput.Read(motor.Combat != null ? motor.Combat.profile : null);
            if (input.Reset && !ShinobiGameSession.DuelActive || transform.position.y < -10f)
            {
                ResetTrainingState();
                return;
            }
            motor.Configure(moveSpeed, jumpHeight, gravity, arenaHalfWidth);
            if (interrupted) { state.Exit(); state.Enter(); interrupted = false; }
            bool busy = opponent == null && techniques != null && techniques.TickTechnique(Time.deltaTime, motor.Facing,
                motor.Combat != null && !motor.Combat.IsAttacking && !motor.Dashing && !motor.Sliding && (throwing == null || !throwing.IsThrowing) && (health == null || !health.Defeated) && (defense == null || !defense.GuardBroken));
            busy |= throwing != null && throwing.IsThrowing;
            if (busy || health != null && health.Defeated || defense != null && defense.GuardBroken) {
                bool airborneThrow = throwing != null && throwing.IsThrowing && !motor.Grounded
                    && (health == null || !health.Defeated) && (defense == null || !defense.GuardBroken);
                input = new ShinobiInput(airborneThrow ? input.Move : 0f, false, false);
            }
            int dashDirection = ReadDashGesture(input, busy);
            if (dashDirection != 0) TryDash(dashDirection);
            if (motor.Dashing) {
                if (input.CrouchPressed) TrySlide();
                else if (input.Jump) TryLongJump();
            }
            if (motor.Dashing || motor.Sliding) input = new ShinobiInput(0f, false, false);
            if (input.NextWeapon && weapons != null) weapons.TryEquipNext();
            motor.UpdateCrouch((input.Crouch || throwing != null && throwing.IsThrowing && throwing.CrouchedThrow
                || motor.Combat != null && motor.Combat.IsAttacking && motor.Combat.Context == ShinobiAttackContext.Crouching)
                && (motor.Combat == null || !motor.Combat.IsAttacking || motor.Crouching),
                crouchHeightRatio, crouchSpeedMultiplier);
            if (!input.Attack && input.ThrowWeapon && throwing != null && throwing.TryThrow())
                input = new ShinobiInput(input.Move,input.Jump,input.Reset,false,input.Crouch);
            motor.Input = input;
            state.Tick(Time.deltaTime);
            motor.Move(Time.deltaTime);
            state.SynchronizeAfterMove();
        }
    }
}


