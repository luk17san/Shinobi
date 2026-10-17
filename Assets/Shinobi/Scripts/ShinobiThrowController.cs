using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiWeaponInventory),typeof(ShinobiMovement))]
    public sealed class ShinobiThrowController : MonoBehaviour
    {
        [SerializeField] private LayerMask collisionMask = ~0;
        private ShinobiWeaponInventory inventory;
        private ShinobiMovement movement;
        private ShinobiCombat combat;
        private ShinobiHealth health;
        private ShinobiDefense defense;
        private ShinobiTechniqueController techniques;
        private ShinobiThrowSettings pending;
        private GameObject pendingVisual;
        private Vector3 pendingScale;
        private float elapsed, facing, nextThrowTime = float.NegativeInfinity;
        private bool released;
        public bool CrouchedThrow { get; private set; }
        public bool IsThrowing => pending != null;
        public float CooldownRemaining => Mathf.Max(0,nextThrowTime-Time.time);
        public AnimationClip ActiveAnimation => pending != null ? pending.animation : null;
        public float AnimationDuration => pending != null ? Mathf.Max(.01f,pending.windup+pending.recovery) : 0;
        public int AnimationSequence { get; private set; }
        public event System.Action<ShinobiThrownWeapon> Thrown;
        public bool CanThrow => isActiveAndEnabled && ShinobiGameSession.GameplayInputAllowed && !IsThrowing
            && Time.time >= nextThrowTime && inventory != null && inventory.isActiveAndEnabled
            && inventory.CurrentWeapon != null && inventory.CurrentWeapon.throwable
            && inventory.CurrentWeapon.throwing != null && inventory.CurrentWeapon.visualPrefab != null
            && combat != null && !combat.IsAttacking && (health == null || !health.Defeated)
            && (movement == null || !movement.IsDashing && !movement.IsSliding) && (defense == null || !defense.GuardBroken)
            && (techniques == null || !techniques.Busy);
        private void Awake()
        {
            inventory=GetComponent<ShinobiWeaponInventory>();movement=GetComponent<ShinobiMovement>();
            combat=GetComponent<ShinobiCombat>();health=GetComponent<ShinobiHealth>();
            defense=GetComponent<ShinobiDefense>();techniques=GetComponent<ShinobiTechniqueController>();
        }
        public bool TryThrow()
        {
            if(!CanThrow) return false;
            var weapon=inventory.CurrentWeapon;
            pending=weapon.throwing.Snapshot();pendingVisual=weapon.visualPrefab;pendingScale=weapon.localScale;
            elapsed=0;released=false;facing=movement!=null?movement.Facing:1;
            CrouchedThrow = movement != null && movement.IsCrouching;
            nextThrowTime=Time.time+Mathf.Max(.05f,pending.cooldown,pending.windup+pending.recovery);
            AnimationSequence++;
            return true;
        }
        // Called from the same input/movement update as melee, Focus and dash.
        public void TickThrow(float dt)
        {
            if(pending==null) return;
            if(health!=null && health.Defeated || defense!=null && defense.GuardBroken || techniques!=null && techniques.Busy)
            { CancelThrow();return; }
            elapsed+=Mathf.Max(0,dt);
            if(!released && elapsed>=Mathf.Max(0,pending.windup))
            {
                released=true;
                var origin=inventory.WeaponSocket!=null
                    ? inventory.WeaponSocket.TransformPoint(inventory.CurrentWeapon.localPosition)
                    : transform.position+Vector3.up*(movement!=null && movement.IsCrouching?.7f:1.1f);
                var projectile=new GameObject("Rzut - "+inventory.CurrentWeaponName).AddComponent<ShinobiThrownWeapon>();
                if (CrouchedThrow) origin.y = Mathf.Min(origin.y, transform.position.y + 0.7f);
                var direction = Vector3.right * (facing < 0 ? -1 : 1);
                var target = ShinobiGameSession.TargetFor(transform);
                if (target != null && target.gameObject.activeInHierarchy && !target.Defeated) {
                    var collider = target.GetComponentInChildren<Collider>();
                    var point = collider != null ? collider.bounds.center : target.transform.position + Vector3.up;
                    point.z = origin.z;
                    var offset = point - origin;
                    if (offset.sqrMagnitude > 0.0001f && offset.magnitude <= pending.range) direction = offset.normalized;
                }
                projectile.Initialize(transform,origin,direction,pendingVisual,pendingScale,pending,collisionMask);
                inventory.SetThrownVisualHidden(true);
                Thrown?.Invoke(projectile);
            }
            if(elapsed>=Mathf.Max(0,pending.windup)+Mathf.Max(.01f,pending.recovery)) CancelThrow();
        }
        public void CancelThrow()
        {
            pending=null;pendingVisual=null;CrouchedThrow=false;
            if(inventory!=null) inventory.SetThrownVisualHidden(false);
        }
        public void ResetThrowState()
        {
            CancelThrow();nextThrowTime=float.NegativeInfinity;
            ShinobiThrownWeapon.ClearForOwner(transform);
        }
        private void OnDisable() { CancelThrow(); }
    }
}
