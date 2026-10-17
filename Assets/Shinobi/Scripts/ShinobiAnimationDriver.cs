using System.Collections.Generic;
using UnityEngine;

namespace Shinobi
{
    // Sends gameplay parameters to the editable Animator graph. It does not move the character.
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiMovement))]
    public sealed class ShinobiAnimationDriver : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Defaults for controller creation and missing action clips. Edit normal state motions in YBotGameplay.controller.")]
        public ShinobiAnimationSet animations;
        [Tooltip("Unique placeholder clips used by Attack A, Attack B and Jutsu in the controller.")]
        public AnimationClip attackSlotA, attackSlotB, jutsuSlot;
        private AnimatorOverrideController overrides;
        private RuntimeAnimatorController originalController;
        private ShinobiMovement movement;
        private ShinobiCombat combat;
        private ShinobiDefense defense;
        private ShinobiHealth health;
        private ShinobiTechniqueController techniques;
        private ShinobiThrowController throwing;
        private int throwSequence = -1;
        private int actionSlot;
        private float hitUntil;
        private int attackSequence = -1;
        private AnimationClip attackClip, jutsuClip;
        private static readonly int State = Animator.StringToHash("State");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        public AnimationClip CurrentClip
        {
            get
            {
                if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return null;
                var clips = animator.IsInTransition(0) ? animator.GetNextAnimatorClipInfo(0) : animator.GetCurrentAnimatorClipInfo(0);
                return clips.Length > 0 ? clips[0].clip : null;
            }
        }
        private void Awake()
        {
            movement = GetComponent<ShinobiMovement>(); combat = GetComponent<ShinobiCombat>();
            defense = GetComponent<ShinobiDefense>(); health = GetComponent<ShinobiHealth>();
            techniques = GetComponent<ShinobiTechniqueController>();
            throwing = GetComponent<ShinobiThrowController>();
        }
        private void OnEnable()
        {
            if (health != null) health.Damaged += OnHit;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.applyRootMotion = false;
            originalController = animator.runtimeAnimatorController;
            overrides = new AnimatorOverrideController(originalController);
            overrides.name = originalController.name + " (" + name + ")";
            animator.runtimeAnimatorController = overrides;
            attackSequence = throwSequence = -1; actionSlot = 0; attackClip = jutsuClip = null;
        }
        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnHit;
            if (animator != null && animator.runtimeAnimatorController == overrides) animator.runtimeAnimatorController = originalController;
            if (overrides != null) Destroy(overrides);
        }
        private void OnHit() { hitUntil = Time.time + 0.2f; }
        private void LateUpdate()
        {
            if (animator == null || overrides == null) return;
            int state = 0;
            if (health != null && health.Defeated) state = 10;
            else if (Time.time < hitUntil) state = 8;
            else if (defense != null && defense.GuardBroken) state = 10;
            else if (throwing != null && throwing.IsThrowing && throwing.ActiveAnimation != null)
            {
                state = 12 + actionSlot;
                if (throwSequence != throwing.AnimationSequence)
                {
                    throwSequence = throwing.AnimationSequence;
                    state = StartAction(throwing.ActiveAnimation, throwing.AnimationDuration);
                    attackSequence = -1; attackClip = null;
                }
                animator.SetFloat(AttackSpeed,throwing.ActiveAnimation.length / throwing.AnimationDuration);
            }
            else if (combat != null && combat.IsAttacking)
            {
                state = 12 + actionSlot;
                if (attackSequence != combat.AnimationSequence || attackClip != combat.ActiveAnimation)
                {
                    attackSequence = combat.AnimationSequence; attackClip = combat.ActiveAnimation;
                    throwSequence = -1;
                    state = StartAction(attackClip, combat.ActiveAnimationDuration);
                }
                animator.SetFloat(AttackSpeed, combat.ActiveAnimationDuration > 0 && attackClip != null
                    ? attackClip.length / combat.ActiveAnimationDuration : 1);
            }
            else if (techniques != null && techniques.Busy)
            {
                state = techniques.Focus ? 11 : 14;
                if (jutsuClip != techniques.ActiveAnimation) { jutsuClip = techniques.ActiveAnimation; Override(jutsuSlot, jutsuClip); }
            }
            else if (movement.IsSliding || movement.IsLongJumping) {
                var clip = movement.IsSliding ? movement.slideAnimation : movement.longJumpAnimation;
                if (clip != null) { Override(attackSlotA, clip); state = 12; attackSequence = -1; throwSequence = -1; }
                else state = movement.IsSliding ? 5 : movement.VerticalSpeed > 0 ? 3 : 4;
                animator.SetFloat(AttackSpeed, 1);
            }
            else if (movement.IsDashing) state = 7;
            else if (defense != null && defense.BlockedRecently) state = 9;
            else if (!movement.IsGrounded) state = movement.VerticalSpeed > 0 ? 3 : 4;
            else if (movement.IsCrouching) state = movement.IsMoving ? 6 : 5;
            else if (movement.IsMoving) state = movement.MoveAmount < 0.6f ? 1 : 2;
            animator.SetInteger(State, state);
        }
        // Preserve the outgoing clip and restart each new action, including melee -> throw.
        private int StartAction(AnimationClip clip, float duration)
        {
            actionSlot = 1 - actionSlot;
            Override(actionSlot == 0 ? attackSlotA : attackSlotB, clip);
            animator.SetFloat(AttackSpeed, clip != null ? clip.length / Mathf.Max(0.01f, duration) : 1f);
            int state = 12 + actionSlot;
            animator.SetInteger(State, state);
            int hash = Animator.StringToHash("Base Layer.Attack " + (actionSlot == 0 ? "A" : "B"));
            if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
            return state;
        }
        private void Override(AnimationClip slot, AnimationClip value)
        {
            if (slot == null) return;
            overrides.ApplyOverrides(new List<KeyValuePair<AnimationClip, AnimationClip>> {
                new KeyValuePair<AnimationClip, AnimationClip>(slot, value != null ? value : animations != null ? animations.idle : slot)
            });
        }
    }
}

