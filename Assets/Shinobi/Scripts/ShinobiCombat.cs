using System.Collections.Generic;
using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent]
    public sealed class ShinobiCombat : MonoBehaviour
    {
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
        private float attackFacing = 1f;
        private readonly HashSet<ShinobiHealth> damaged = new HashSet<ShinobiHealth>();
        public float Windup => Mathf.Max(0f, windup);
        public float ActiveDuration => Mathf.Max(0.01f, activeDuration);
        public float Recovery => Mathf.Max(0.01f, recovery);
        public float ComboBuffer => Mathf.Clamp(comboBuffer, 0.01f, 0.5f);
        public int ComboLength => 1 + (followUpAttacks == null ? 0 : followUpAttacks.Length);
        public ShinobiAttack GetAttack(int index)
        {
            if (index < 0 || index >= ComboLength) throw new System.ArgumentOutOfRangeException(nameof(index));
            var first = new ShinobiAttack { damage = damage, windup = windup, activeDuration = activeDuration, recovery = recovery, reach = reach };
            return (index == 0 ? first : followUpAttacks[index - 1] ?? first).Snapshot();
        }
        private Vector3 HalfExtents => new Vector3(currentAttack == null ? Mathf.Max(0.1f, reach) : currentAttack.reach, Mathf.Max(0.1f, hitHeight), Mathf.Max(0.1f, hitDepth)) * 0.5f;
        private Vector3 HitCenter => transform.position + Vector3.up * hitCenterHeight + Vector3.right * attackFacing * HalfExtents.x;

        private void Awake() { ShowActive(false); }
        private void OnDisable() { EndAttack(); }
        public void BeginAttack(float facing, ShinobiAttack attack = null)
        {
            currentAttack = (attack ?? GetAttack(0)).Snapshot();
            attackFacing = facing < 0f ? -1f : 1f;
            damaged.Clear();
            ShowActive(false);
        }
        public void ResolveHit()
        {
            // One sample at the start of Active, one damage application per health owner.
            foreach (var collider in Physics.OverlapBox(HitCenter, HalfExtents, Quaternion.identity,
                targetMask, QueryTriggerInteraction.Ignore))
            {
                var health = collider.GetComponentInParent<ShinobiHealth>();
                if (health == null || health.transform == transform || health.transform.IsChildOf(transform)) continue;
                if (damaged.Add(health)) health.ApplyDamage(currentAttack == null ? Mathf.Max(1f, damage) : currentAttack.damage);
            }
        }
        public void ShowActive(bool visible)
        {
            if (hitPreview == null) return;
            hitPreview.position = HitCenter;
            hitPreview.rotation = Quaternion.identity;
            hitPreview.localScale = HalfExtents * 2f;
            hitPreview.gameObject.SetActive(visible);
        }
        public void EndAttack() { ShowActive(false); damaged.Clear(); currentAttack = null; }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(HitCenter, HalfExtents * 2f);
        }
    }
}
