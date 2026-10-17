using System.Collections.Generic;
using UnityEngine;

namespace Shinobi
{
    // Collect both players' active attacks before applying damage, independent of Update order.
    [DefaultExecutionOrder(10000)]
    public sealed class ShinobiClashResolver : MonoBehaviour
    {
        public sealed class Strike
        {
            public ShinobiCombat owner;
            public int level;
            public float damage, push, facing;
            public bool cancelled;
            public readonly HashSet<ShinobiHealth> hit = new HashSet<ShinobiHealth>();
            public readonly HashSet<Strike> clashed = new HashSet<Strike>();
        }
        private sealed class Pulse
        {
            public Strike strike;
            public Vector3 center, half;
            public float radius;
            public LayerMask mask;
            public readonly List<ShinobiHealth> clashTargets = new List<ShinobiHealth>();
        }
        private struct Damage
        {
            public ShinobiHealth target;
            public float amount, push;
            public Vector3 source;
            public bool blockable;
        }
        private static ShinobiClashResolver instance;
        private readonly List<Pulse> pending = new List<Pulse>();
        public static void ClearPending() { if (instance != null) instance.pending.Clear(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { instance = null; }
        public static void Submit(Strike strike, Vector3 center, Vector3 half, float radius, LayerMask mask)
        {
            if (strike == null || strike.cancelled || strike.owner == null) return;
            if (instance == null) {
                var go = new GameObject("Shinobi Clash Resolver");
                instance = go.AddComponent<ShinobiClashResolver>();
            }
            var existing = instance.pending.Find(x => ReferenceEquals(x.strike, strike));
            if (existing == null) { existing = new Pulse { strike = strike }; instance.pending.Add(existing); }
            existing.center = center; existing.half = half; existing.radius = radius; existing.mask = mask;
        }
        private static bool Overlaps(Pulse a, Pulse b)
        {
            if (a.radius > 0f && b.radius > 0f) return (a.center - b.center).sqrMagnitude <= (a.radius + b.radius) * (a.radius + b.radius);
            if (a.radius > 0f) return (new Bounds(b.center, b.half * 2f).ClosestPoint(a.center) - a.center).sqrMagnitude <= a.radius * a.radius;
            if (b.radius > 0f) return (new Bounds(a.center, a.half * 2f).ClosestPoint(b.center) - b.center).sqrMagnitude <= b.radius * b.radius;
            return new Bounds(a.center, a.half * 2f).Intersects(new Bounds(b.center, b.half * 2f));
        }
        private static bool Valid(Strike strike) => !strike.cancelled && strike.owner != null && strike.owner.isActiveAndEnabled;
        private void LateUpdate()
        {
            if (!ShinobiGameSession.ShowGameplayHud) { pending.Clear(); return; }
            pending.RemoveAll(x => !Valid(x.strike));
            // Stable pair order also makes three-way encounters reproducible for the same actors.
            pending.Sort((a, b) => a.strike.owner.ClashOrder.CompareTo(b.strike.owner.ClashOrder));
            for (int i = 0; i < pending.Count; i++) for (int j = i + 1; j < pending.Count; j++) {
                var a = pending[i]; var b = pending[j];
                if (!Valid(a.strike) || !Valid(b.strike) || a.strike.owner == b.strike.owner || a.strike.clashed.Contains(b.strike)) continue;
                if ((a.mask.value & (1 << b.strike.owner.gameObject.layer)) == 0 || (b.mask.value & (1 << a.strike.owner.gameObject.layer)) == 0 || !Overlaps(a, b)) continue;
                a.strike.clashed.Add(b.strike); b.strike.clashed.Add(a.strike);
                a.strike.owner.ReportClash((a.center + b.center) * 0.5f);
                var stronger = a.strike.level > b.strike.level ? a.strike : b.strike;
                var profile = stronger.owner.profile;
                float chance = ShinobiClashRules.Chance(System.Math.Abs(a.strike.level - b.strike.level),
                    profile != null ? profile.interruptChance : 60f, profile != null ? profile.chancePerLevel : 15f,
                    profile != null ? profile.maximumInterruptChance : 95f);
                // Random.value can include 1; keep the roll below 100 for a true 100% setting.
                var result = ShinobiClashRules.Resolve(a.strike.level, b.strike.level, chance, Mathf.Min(Random.value * 100f, 99.99999f));
                if (result == ShinobiClashResult.Draw) {
                    a.strike.cancelled = b.strike.cancelled = true;
                    a.strike.owner.InterruptAction(); b.strike.owner.InterruptAction();
                    Separate(a.strike.owner, b.strike.owner);
                } else if (result != ShinobiClashResult.Trade) {
                    var loser = result == ShinobiClashResult.FirstWins ? b.strike : a.strike;
                    loser.cancelled = true; loser.owner.InterruptAction();
                    var winner = result == ShinobiClashResult.FirstWins ? a : b;
                    winner.clashTargets.Add(loser.owner.GetComponent<ShinobiHealth>());
                } else {
                    a.clashTargets.Add(b.strike.owner.GetComponent<ShinobiHealth>());
                    b.clashTargets.Add(a.strike.owner.GetComponent<ShinobiHealth>());
                }
            }
            // Snapshot accepted damage first: taking damage must not erase a successful trade.
            var damage = new List<Damage>();
            foreach (var pulse in pending) {
                var strike = pulse.strike;
                if (!Valid(strike)) continue;
                foreach (var health in pulse.clashTargets) AddDamage(damage, strike, health, false);
                var colliders = pulse.radius > 0f
                    ? Physics.OverlapSphere(pulse.center, pulse.radius, pulse.mask, QueryTriggerInteraction.Ignore)
                    : Physics.OverlapBox(pulse.center, pulse.half, Quaternion.identity, pulse.mask, QueryTriggerInteraction.Ignore);
                foreach (var collider in colliders) {
                    var health = collider.GetComponentInParent<ShinobiHealth>();
                    AddDamage(damage, strike, health, pulse.radius <= 0f);
                }
            }
            pending.Clear();
            foreach (var hit in damage) if (hit.target != null) {
                var defense = hit.target.GetComponent<ShinobiDefense>();
                bool damaged = defense != null && defense.isActiveAndEnabled
                    ? defense.ReceiveHit(hit.amount, hit.source, hit.blockable) : hit.target.ApplyDamage(hit.amount);
                if (!damaged) continue;
                var movement = hit.target.GetComponent<ShinobiMovement>();
                if (movement != null) movement.Push(hit.push);
            }
        }
        private static void AddDamage(List<Damage> damage, Strike strike, ShinobiHealth health, bool blockable)
        {
            if (health == null || health.Defeated || health.transform == strike.owner.transform || health.transform.IsChildOf(strike.owner.transform) || !strike.hit.Add(health)) return;
            damage.Add(new Damage { target = health, amount = strike.damage, push = strike.facing * strike.push,
                source = strike.owner.transform.position, blockable = blockable });
        }
        private static void Separate(ShinobiCombat first, ShinobiCombat second)
        {
            float gap = Mathf.Max(first.profile != null ? first.profile.drawSeparation : 2f, second.profile != null ? second.profile.drawSeparation : 2f);
            float difference = first.transform.position.x - second.transform.position.x;
            float direction = difference == 0f ? (first.ClashOrder < second.ClashOrder ? -1f : 1f) : Mathf.Sign(difference);
            float distance = Mathf.Max(0f, gap - Mathf.Abs(difference)) * 0.5f;
            var a = first.GetComponent<ShinobiMovement>(); var b = second.GetComponent<ShinobiMovement>();
            if (a != null) a.Push(direction * distance);
            if (b != null) b.Push(-direction * distance);
        }
    }
}
