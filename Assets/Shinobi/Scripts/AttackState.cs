namespace Shinobi
{
    internal sealed class AttackState : HierarchicalState
    {
        private readonly ShinobiMotor motor;
        private StrikeState strike;
        private float elapsed, windupTime, activeEnd, end;
        private float bufferTime;
        private int index, comboLength;
        private bool hitResolved, queued, firstTick;
        public bool Complete { get; private set; }

        public AttackState(ShinobiMotor motor) : base("Attack") { this.motor = motor; }

        protected override void OnEnter()
        {
            comboLength = motor.Combat.ComboLength;
            motor.AllowMovement = false;
            StartStrike(0);
        }

        private void StartStrike(int nextIndex)
        {
            index = nextIndex;
            elapsed = 0f;
            Complete = false;
            hitResolved = false;
            queued = false;
            firstTick = true;
            var attack = motor.Combat.GetAttack(index);
            windupTime = attack.windup;
            activeEnd = windupTime + attack.activeDuration;
            end = activeEnd + attack.recovery;
            bufferTime = motor.Combat.ComboBuffer;
            motor.Combat.BeginAttack(motor.Facing, attack, motor.Combat.GetAnimation(index));
            strike = new StrikeState(index + 1);
            SetChild(strike);
        }

        protected override void OnTick(float dt)
        {
            if (!motor.Combat.IsAttacking) { Complete = true; return; }
            elapsed += dt;
            // The press starting this strike must not also queue the next one.
            if (!firstTick && index + 1 < comboLength && motor.Input.Attack && elapsed <= bufferTime)
                queued = true;
            firstTick = false;
            // Even a long frame crossing several phases resolves the strike once.
            if (!hitResolved && elapsed >= windupTime)
            {
                hitResolved = true;
                motor.Combat.ResolveHit();
                motor.Combat.PlayAttackEffect(index);
            }
            else if (elapsed >= windupTime && elapsed < activeEnd) motor.Combat.ResolveHit();
            Complete = elapsed >= System.Math.Max(end, index + 1 < comboLength ? bufferTime : end);
            if (elapsed >= end && queued)
            {
                StartStrike(index + 1);
                return;
            }
            strike.SetPhase(elapsed < windupTime ? 0 : elapsed < activeEnd ? 1 : 2);
            motor.Combat.ShowActive(!Complete && elapsed >= windupTime && elapsed < activeEnd);
        }

        protected override void OnExit()
        {
            motor.AllowMovement = true;
            motor.Combat.EndAttack();
        }

        private sealed class StrikeState : HierarchicalState
        {
            private readonly AttackPhase[] phases = {
                new AttackPhase("Windup"), new AttackPhase("Active"), new AttackPhase("Recovery")
            };
            public StrikeState(int number) : base("Hit" + number) { }
            protected override void OnEnter() { SetPhase(0); }
            public void SetPhase(int phase) { SetChild(phases[phase]); }
        }

        private sealed class AttackPhase : HierarchicalState
        {
            public AttackPhase(string name) : base(name) { }
        }
    }
}
