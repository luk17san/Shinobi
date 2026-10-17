using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shinobi
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShinobiCombat))]
    public sealed class ShinobiTechniqueController : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask = ~0;
        private readonly List<ShinobiSeal> seals = new List<ShinobiSeal>();
        private ShinobiCombat combat;
        private ShinobiHealth health;
        private ShinobiJutsuDefinition casting;
        private ShinobiClashResolver.Strike strike;
        private float elapsed, facing;
        private bool resolved, waitForRelease;
        public event System.Action<Vector3, float> TechniqueReleased;
        public bool Focus { get; private set; }
        public bool Busy => Focus || casting != null;
        public AnimationClip ActiveAnimation => casting != null ? casting.animation : null;
        public float Chakra { get; private set; }
        public IReadOnlyList<ShinobiSeal> EnteredSeals => seals;
        public string Sequence => string.Join(", ", seals);
        private void Awake() { combat = GetComponent<ShinobiCombat>(); ResetTechnique(); }
        private void OnEnable() {
            health = GetComponent<ShinobiHealth>();
            if (health != null) health.Damaged += Interrupt;
        }
        private void OnDisable() {
            if (health != null) health.Damaged -= Interrupt;
            Interrupt();
        }
        public void ResetTechnique() {
            Interrupt(); waitForRelease = false;
            Chakra = combat != null && combat.profile != null ? combat.profile.maxChakra : 100f;
        }
        public void Interrupt() {
            if (strike != null) strike.cancelled = true;
            Focus = false; casting = null; seals.Clear(); waitForRelease = true;
        }
        public bool TickTechnique(float dt, float direction, bool canStart) {
            var profile = combat.profile;
            if (profile == null) return false;
            var keyboard = Keyboard.current;
            bool held = keyboard != null && keyboard[profile.focusKey].isPressed;
            if (!held) waitForRelease = false;
            if (casting != null) {
                elapsed += dt;
                if (!resolved && elapsed >= casting.windup) { resolved = true; Resolve(); }
                if (casting != null && elapsed >= casting.windup + casting.recovery) casting = null;
                return true;
            }
            if (!Focus && canStart && !waitForRelease && keyboard != null && keyboard[profile.focusKey].wasPressedThisFrame) {
                Focus = true; seals.Clear(); facing = direction;
            }
            if (!Focus) return false;
            if (held) {
                // Simultaneous seals are ambiguous: reject the sequence until Focus is released.
                int count = 0; ShinobiSeal seal = ShinobiSeal.Up;
                if (keyboard.wKey.wasPressedThisFrame) { count++; seal = ShinobiSeal.Up; }
                if (keyboard.sKey.wasPressedThisFrame) { count++; seal = ShinobiSeal.Down; }
                if (keyboard.aKey.wasPressedThisFrame) { count++; seal = ShinobiSeal.Left; }
                if (keyboard.dKey.wasPressedThisFrame) { count++; seal = ShinobiSeal.Right; }
                if (count > 1 || seals.Count >= 32 && count > 0) { Interrupt(); return true; }
                if (count == 1) seals.Add(seal);
            } else {
                Focus = false;
                ShinobiJutsuDefinition match = null;
                if (profile.jutsu != null) foreach (var technique in profile.jutsu) {
                    if (!Matches(technique)) continue;
                    if (match != null) { match = null; break; }
                    match = technique;
                }
                if (match != null && Chakra >= match.chakraCost) {
                    casting = match; Chakra -= Mathf.Max(0f, match.chakraCost); elapsed = 0f; resolved = false;
                    strike = new ShinobiClashResolver.Strike { owner = combat, level = match.level, damage = match.damage, facing = facing };
                }
                seals.Clear();
            }
            return true;
        }
        private bool Matches(ShinobiJutsuDefinition technique) {
            if (technique == null || technique.sequence == null || seals.Count == 0 || technique.sequence.Length != seals.Count) return false;
            for (int i = 0; i < seals.Count; i++) if (technique.sequence[i] != seals[i]) return false;
            return true;
        }
        private void Resolve() {
            var technique = casting;
            Vector3 center = transform.position + Vector3.up + Vector3.right * facing * technique.forwardOffset;
            if (technique.effect != null) Destroy(Instantiate(technique.effect, center, Quaternion.identity), 5f);
            else TechniqueReleased?.Invoke(center, Mathf.Max(0.1f, technique.radius));
            ShinobiClashResolver.Submit(strike, center, Vector3.zero, Mathf.Max(0.1f, technique.radius), targetMask);
        }
    }
}

