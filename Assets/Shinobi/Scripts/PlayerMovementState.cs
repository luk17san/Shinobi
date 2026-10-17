namespace Shinobi
{
    public sealed class PlayerMovementState : HierarchicalState
    {
        private readonly ShinobiMotor motor;
        private readonly GroundedState grounded;
        private readonly AirborneState airborne;
        private readonly DashState dash;
        private readonly SlideState slide;
        public PlayerMovementState(ShinobiMotor motor) : base("Movement")
        {
            this.motor = motor;
            grounded = new GroundedState(motor);
            airborne = new AirborneState(motor);
            dash = new DashState(motor);
            slide = new SlideState(motor);
        }
        protected override void OnEnter() { SelectBranch(); }
        protected override void OnTick(float dt)
        {
            SelectBranch();
            if (ReferenceEquals(Child, grounded) && !grounded.Attacking && !motor.Crouching && motor.Input.Jump)
            {
                motor.Jump();
                SetChild(airborne);
            }
        }
        private void SelectBranch() { SetChild(motor.Sliding ? (HierarchicalState)slide : motor.Dashing ? (HierarchicalState)dash : motor.Grounded ? (HierarchicalState)grounded : airborne); }
        public void SynchronizeAfterMove()
        {
            SelectBranch();
            if (ReferenceEquals(Child, airborne)) airborne.SelectPhase();
        }
    }
    internal sealed class GroundedState : HierarchicalState
    {
        private readonly ShinobiMotor motor;
        private readonly IdleState idle = new IdleState();
        private readonly RunState run = new RunState();
        private readonly CrouchState crouch = new CrouchState();
        private readonly AttackState attack;
        public bool Attacking => ReferenceEquals(Child, attack);
        public GroundedState(ShinobiMotor motor) : base("Grounded")
        {
            this.motor = motor;
            attack = new AttackState(motor);
        }
        protected override void OnEnter() { SelectMotion(); }
        protected override void OnTick(float dt)
        {
            motor.StickToGround();
            if (Attacking && !attack.Complete) return;
            if (motor.Combat != null && motor.Input.Attack)
            {
                // Releasing a completed attack permits a new press to start another.
                if (Attacking) SetChild(null);
                SetChild(attack);
                return;
            }
            SelectMotion();
        }
        private void SelectMotion() { SetChild(motor.Crouching ? (HierarchicalState)crouch : motor.Moving ? (HierarchicalState)run : idle); }
    }
    internal sealed class AirborneState : HierarchicalState
    {
        private readonly ShinobiMotor motor;
        private readonly JumpState jump = new JumpState();
        private readonly FallState fall = new FallState();
        private readonly AttackState attack;
        private readonly LongJumpState longJump = new LongJumpState();
        public AirborneState(ShinobiMotor motor) : base("Airborne") { this.motor = motor; attack = new AttackState(motor); }
        protected override void OnEnter() { SelectPhase(); }
        protected override void OnTick(float dt)
        {
            motor.ApplyGravity(dt);
            if (ReferenceEquals(Child, attack) && !attack.Complete) return;
            if (motor.Combat != null && motor.Input.Attack)
            {
                SetChild(null); SetChild(attack); return;
            }
            SelectPhase();
        }
        public void SelectPhase() {
            if (ReferenceEquals(Child, attack) && !attack.Complete) return;
            SetChild(motor.LongJumping ? (HierarchicalState)longJump : motor.VerticalSpeed > 0f ? (HierarchicalState)jump : fall);
        }
    }
    internal sealed class IdleState : HierarchicalState { public IdleState() : base("Idle") { } }
    internal sealed class DashState : HierarchicalState
    {
        private readonly ShinobiMotor motor;
        public DashState(ShinobiMotor motor) : base("Dash") { this.motor = motor; }
        protected override void OnTick(float dt)
        {
            if (motor.Grounded) motor.StickToGround();
            else motor.ApplyGravity(dt);
        }
    }
    internal sealed class CrouchState : HierarchicalState { public CrouchState() : base("Crouch") { } }
    internal sealed class SlideState : HierarchicalState {
        private readonly ShinobiMotor motor;
        public SlideState(ShinobiMotor motor) : base("Slide") { this.motor = motor; }
        protected override void OnTick(float dt) { if (motor.Grounded) motor.StickToGround(); else motor.ApplyGravity(dt); }
    }
    internal sealed class LongJumpState : HierarchicalState { public LongJumpState() : base("LongJump") { } }
    internal sealed class RunState : HierarchicalState { public RunState() : base("Run") { } }
    internal sealed class JumpState : HierarchicalState { public JumpState() : base("Jump") { } }
    internal sealed class FallState : HierarchicalState { public FallState() : base("Fall") { } }
}
