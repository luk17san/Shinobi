using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiHealth), typeof(ShinobiCombat))]
    public sealed class ShinobiDefense : MonoBehaviour
    {
        private ShinobiHealth health;
        private ShinobiCombat combat;
        private ShinobiMovement movement;
        private ShinobiTechniqueController techniques;
        private float lastHit = float.NegativeInfinity;
        private float guardBrokenUntil = float.NegativeInfinity;
        private float lastBlock = float.NegativeInfinity;
        public float Stamina { get; private set; }
        public event System.Action Blocked;
        public event System.Action GuardBreakStarted;
        private ShinobiCombatProfile Profile => combat != null ? combat.profile : null;
        public float MaxStamina => Profile != null ? Mathf.Max(1, Profile.maxStamina) : 100;
        public bool GuardBroken => Time.time < guardBrokenUntil;
        public bool BlockedRecently => Time.time - lastBlock < 0.25f;

        private void Awake()
        {
            health = GetComponent<ShinobiHealth>();
            combat = GetComponent<ShinobiCombat>();
            movement = GetComponent<ShinobiMovement>();
            techniques = GetComponent<ShinobiTechniqueController>();
            ResetDefense();
        }
        private void OnEnable() { if (health != null) health.Damaged += PauseRegeneration; }
        private void OnDisable() { if (health != null) health.Damaged -= PauseRegeneration; }
        private void PauseRegeneration() { lastHit = Time.time; }
        private void Update()
        {
            Stamina = Mathf.Min(Stamina, MaxStamina);
            if (Profile == null || health.Defeated || GuardBroken || combat.IsAttacking || movement != null && movement.IsDashing || techniques != null && techniques.Busy) return;
            if (Time.time - lastHit >= Mathf.Max(0, Profile.staminaRegenDelay))
                Stamina = Mathf.Min(MaxStamina, Stamina + Mathf.Max(0, Profile.staminaRegenRate) * Time.deltaTime);
        }
        public void ResetDefense()
        {
            Stamina = MaxStamina;
            guardBrokenUntil = lastHit = lastBlock = float.NegativeInfinity;
        }
        public bool TrySpendStamina(float amount)
        {
            amount = Mathf.Max(0, amount);
            if (!isActiveAndEnabled || health.Defeated || GuardBroken || Stamina < amount) return false;
            Stamina -= amount;
            lastHit = Time.time;
            return true;
        }
        private bool CanBlock(Vector3 source)
        {
            if (Profile == null || !Profile.autoBlock || health.Defeated || GuardBroken || movement == null || !movement.IsGrounded || movement.IsDashing || movement.IsSliding
                || combat.IsAttacking || techniques != null && techniques.Busy
                || GetComponent<ShinobiThrowController>() != null && GetComponent<ShinobiThrowController>().IsThrowing) return false;
            Vector3 direction = Vector3.ProjectOnPlane(source - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f) return false;
            float threshold = Mathf.Cos(Mathf.Clamp(Profile.blockHalfAngle, 0, 90) * Mathf.Deg2Rad);
            return Vector3.Dot(Vector3.right * movement.Facing, direction.normalized) >= threshold;
        }
        // Raw Health.ApplyDamage remains available for hazards. Melee and direct projectiles can be blocked.
        public bool ReceiveHit(float amount, Vector3 source, bool blockable)
        {
            if (health.Defeated || amount <= 0) return false;
            lastHit = Time.time;
            if (blockable && isActiveAndEnabled && CanBlock(source))
            {
                float cost = amount * Mathf.Max(0, Profile.blockCostMultiplier);
                if (Stamina >= cost)
                {
                    Stamina = Mathf.Max(0, Stamina - cost);
                    lastBlock = Time.time;
                    Blocked?.Invoke();
                    return false;
                }
                Stamina = 0;
                guardBrokenUntil = Time.time + Mathf.Max(0.01f, Profile.guardBreakDuration);
                combat.InterruptAction();
                GuardBreakStarted?.Invoke();
            }
            return health.ApplyDamage(amount);
        }
    }
}
