using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent]
    public sealed class ShinobiCombat : MonoBehaviour
    {
        private static int nextClashOrder;
        internal readonly int ClashOrder = System.Threading.Interlocked.Increment(ref nextClashOrder);
        public ShinobiCombatProfile profile;
        [SerializeField, Min(1f)] private float damage = 20f;
        [SerializeField, Min(0f)] private float windup = 0.12f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float recovery = 0.25f;
        [SerializeField, Min(0.1f)] private float reach = 1.3f;
        [SerializeField, Min(0.1f)] private float hitHeight = 1.2f;
        [SerializeField, Min(0.1f)] private float hitDepth = 0.8f;
        [SerializeField] private float hitCenterHeight = 1f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private Transform hitPreview;
        [SerializeField, Range(0.01f, 0.5f)] private float comboBuffer = 0.18f;
        [SerializeField] private ShinobiAttack[] followUpAttacks = {
            new ShinobiAttack { damage = 25f, windup = 0.1f, recovery = 0.22f, reach = 1.4f },
            new ShinobiAttack { damage = 35f, windup = 0.18f, recovery = 0.4f, reach = 1.6f }
        };
        private ShinobiAttack currentAttack;
        public event System.Action<float> SwingActivated;
        public event System.Action<Vector3> Clashed;
        public void ReportClash(Vector3 point) { Clashed?.Invoke(point); }
        private ShinobiClashResolver.Strike strike;
        public void InterruptAction() {
            if (strike != null) strike.cancelled = true;
            EndAttack();
            var techniques = GetComponent<ShinobiTechniqueController>();
            if (techniques != null) techniques.Interrupt();
            var movement = GetComponent<ShinobiMovement>();
            if (movement != null) movement.InterruptAction();
        }
        public bool IsAttacking => currentAttack != null;
        public AnimationClip ActiveAnimation { get; private set; }
        public float ActiveAnimationDuration { get; private set; }
        public int AnimationSequence { get; private set; }
        public AnimationClip GetAnimation(int index)
        {
            var combo = CurrentCombo;
            if (combo != null && index >= 0 && index < combo.Length && combo[index] != null && combo[index].animation != null)
                return combo[index].animation;
            // Missing context/weapon clips inherit the character's standing punches.
            var fallback = profile != null ? profile.combo : null;
            if (fallback == null || fallback.Length == 0 || index < 0) return null;
            int source = Context == ShinobiAttackContext.LongJump ? fallback.Length - 1 : index % fallback.Length;
            return fallback[source] != null ? fallback[source].animation : null;
        }
        private float attackFacing = 1f;
        public float Windup => Mathf.Max(0f, windup);
        public float ActiveDuration => Mathf.Max(0.01f, activeDuration);
        public float Recovery => Mathf.Max(0.01f, recovery);
        private ShinobiWeaponDefinition EquippedWeapon
        {
            get { var inventory = GetComponent<ShinobiWeaponInventory>(); return inventory != null && inventory.isActiveAndEnabled ? inventory.CurrentWeapon : null; }
        }
        public ShinobiAttackContext Context { get; private set; }
        public void SelectContext(ShinobiAttackContext context) { Context = context; }
        public ShinobiComboDefinition ResolveCombo(ShinobiAttackContext context)
        {
            var definition = EquippedWeapon != null ? EquippedWeapon.contextCombos?.Get(context) : null;
            if (definition != null && definition.IsUsable) return definition;
            if (context == ShinobiAttackContext.Standing && EquippedWeapon != null && EquippedWeapon.combo != null && EquippedWeapon.combo.Length > 0) return null;
            definition = profile != null ? profile.contextCombos?.Get(context) : null;
            return definition != null && definition.IsUsable ? definition : null;
        }
        private ShinobiAttackDefinition[] LegacyCombo => EquippedWeapon != null ? EquippedWeapon.combo : profile != null ? profile.combo : null;
        private ShinobiAttackDefinition[] CurrentCombo => ResolveCombo(Context) != null ? ResolveCombo(Context).attacks : Context == ShinobiAttackContext.Standing ? LegacyCombo : null;
        public float ComboBuffer => ResolveCombo(Context) != null ? Mathf.Max(0.01f, ResolveCombo(Context).comboWindow) : EquippedWeapon != null ? Mathf.Max(0.01f, EquippedWeapon.comboWindow)
            : profile != null ? Mathf.Max(0.01f, profile.comboWindow) : Mathf.Clamp(comboBuffer, 0.01f, 0.5f);
        public int ComboLength => CurrentCombo != null && CurrentCombo.Length > 0
            ? CurrentCombo.Length : Context != ShinobiAttackContext.Standing ? 1 : 1 + (followUpAttacks == null ? 0 : followUpAttacks.Length);
        public ShinobiAttack GetAttack(int index)
        {
            if (index < 0 || index >= ComboLength) throw new System.ArgumentOutOfRangeException(nameof(index));
            if (Context != ShinobiAttackContext.Standing && CurrentCombo == null)
            {
                var legacy = LegacyCombo;
                int source = Context == ShinobiAttackContext.LongJump && legacy != null ? legacy.Length - 1 : 0;
                var value = legacy != null && source >= 0 && source < legacy.Length && legacy[source] != null
                    ? legacy[source].attack.Snapshot() : new ShinobiAttack();
                if (Context == ShinobiAttackContext.Crouching) { value.hitCenterHeight = 0.6f; value.hitHeight = 0.6f; }
                if (Context == ShinobiAttackContext.LongJump) { value.pushDistance = 1.5f; if (legacy == null || legacy.Length == 0) value.damage = 35; }
                return value;
            }
            var combo = CurrentCombo;
            if (combo != null && combo.Length > 0) {
                var definition = combo[index];
                return (definition != null && definition.attack != null ? definition.attack : new ShinobiAttack()).Snapshot();
            }
            var first = new ShinobiAttack { damage = damage, windup = windup, activeDuration = activeDuration, recovery = recovery, reach = reach,
                hitHeight = hitHeight, hitDepth = hitDepth, hitCenterHeight = hitCenterHeight };
            return (index == 0 ? first : followUpAttacks[index - 1] ?? first).Snapshot();
        }
        private Vector3 HalfExtents => new Vector3(currentAttack == null ? Mathf.Max(0.1f, reach) : currentAttack.reach,
            currentAttack == null ? Mathf.Max(0.1f, hitHeight) : currentAttack.hitHeight,
            currentAttack == null ? Mathf.Max(0.1f, hitDepth) : currentAttack.hitDepth) * 0.5f;
        private Vector3 HitCenter => transform.position + Vector3.up * (currentAttack == null ? hitCenterHeight : currentAttack.hitCenterHeight) + Vector3.right * attackFacing * HalfExtents.x;
        public void PlayAttackEffect(int index) {
            var combo = CurrentCombo;
            var effect = combo != null && index >= 0 && index < combo.Length && combo[index] != null ? combo[index].effect : null;
            if (effect != null) Destroy(Instantiate(effect, HitCenter, Quaternion.identity), 5f);
            else SwingActivated?.Invoke(attackFacing);
        }

        private void Awake() { ShowActive(false); }
        private void OnDisable() { EndAttack(); }
        public void BeginAttack(float facing, ShinobiAttack attack = null, AnimationClip animation = null)
        {
            currentAttack = (attack ?? GetAttack(0)).Snapshot();
            ActiveAnimation = animation;
            ActiveAnimationDuration = currentAttack.windup + currentAttack.activeDuration + currentAttack.recovery;
            AnimationSequence++;
            strike = new ShinobiClashResolver.Strike { owner = this, level = currentAttack.level,
                damage = currentAttack.damage, push = currentAttack.pushDistance, facing = facing < 0f ? -1f : 1f };
            attackFacing = facing < 0f ? -1f : 1f;
            ShowActive(false);
        }
        public void ResolveHit()
        {
            ShinobiClashResolver.Submit(strike, HitCenter, HalfExtents, 0f, targetMask);
        }
        public void ShowActive(bool visible)
        {
            if (hitPreview == null) return;
            hitPreview.position = HitCenter;
            hitPreview.rotation = Quaternion.identity;
            hitPreview.localScale = HalfExtents * 2f;
            hitPreview.gameObject.SetActive(visible);
        }
        public void EndAttack() { ShowActive(false); currentAttack = null; }
        public void CancelAttack() { if (strike != null) strike.cancelled = true; EndAttack(); }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(HitCenter, HalfExtents * 2f);
        }
    }
}
