using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shinobi
{
    // Sweeps the entire flight segment, including at high speed; visuals have no physical body.
    public sealed class ShinobiThrownWeapon : MonoBehaviour
    {
        private Transform owner;
        private Vector3 direction;
        private ShinobiThrowSettings settings;
        private LayerMask mask;
        private Transform visual;
        private float travelled;
        private bool finished;
        private static readonly List<GameObject> effects = new List<GameObject>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { effects.Clear(); }
        public Transform Owner => owner;
        public float Travelled => travelled;
        public void Initialize(Transform source,Vector3 origin,Vector3 flight,GameObject prefab,Vector3 scale,ShinobiThrowSettings config,LayerMask collisionMask)
        {
            owner=source;transform.position=origin;direction=flight.normalized;settings=config.Snapshot();mask=collisionMask;
            var instance=Instantiate(prefab,transform,false);visual=instance.transform;
            visual.localPosition=Vector3.zero;
            visual.localRotation=settings.spin?Quaternion.identity:Quaternion.FromToRotation(Vector3.down,direction);
            visual.localScale=scale;
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic=true;
        }
        private bool Ignore(Collider collider) => collider==null || owner!=null && (collider.transform==owner || collider.transform.IsChildOf(owner));
        private void FixedUpdate()
        {
            if(finished || settings==null) return;
            float radius=Mathf.Clamp(settings.collisionRadius,.01f,.3f);
            // Also detect targets already touching the release point.
            var overlaps=Physics.OverlapSphere(transform.position,radius,mask,QueryTriggerInteraction.Ignore);
            foreach(var collider in overlaps) if(!Ignore(collider)) { Impact(collider,transform.position);return; }
            float step=Mathf.Min(Mathf.Max(1,settings.speed)*Time.fixedDeltaTime,Mathf.Max(.1f,settings.range)-travelled);
            if(step<=0) { Finish();return; }
            var hits=Physics.SphereCastAll(transform.position,radius,direction,step,mask,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(Ignore(hit.collider)) continue;
                transform.position+=direction*hit.distance;travelled+=hit.distance;
                Impact(hit.collider,hit.point+hit.normal*.02f);return;
            }
            transform.position+=direction*step;travelled+=step;
            if(settings.spin && visual!=null) visual.Rotate(Vector3.forward,1200*Time.fixedDeltaTime,Space.Self);
            if(travelled>=Mathf.Max(.1f,settings.range)-.0001f) Finish();
        }
        private void Impact(Collider collider,Vector3 point)
        {
            if(finished) return;
            if(settings.explosive) Explode(point);
            else Damage(collider.GetComponentInParent<ShinobiHealth>(),Mathf.Max(0,settings.damage),point-direction,true);
            Finish();
        }
        private void Explode(Vector3 point)
        {
            float radius=Mathf.Max(.1f,settings.explosionRadius);
            if(settings.explosionEffect!=null)
            {
                var effect=Instantiate(settings.explosionEffect,point,Quaternion.identity);
                effects.RemoveAll(e=>e==null);effects.Add(effect);
                effect.transform.localScale=Vector3.one*radius;
                Destroy(effect,1f);
            }
            var damaged=new HashSet<ShinobiHealth>();
            foreach(var collider in Physics.OverlapSphere(point,radius,mask,QueryTriggerInteraction.Ignore))
            {
                if(Ignore(collider)) continue;
                var health=collider.GetComponentInParent<ShinobiHealth>();
                if(health==null || damaged.Contains(health) || !Visible(point,collider)) continue;
                damaged.Add(health);Damage(health,Mathf.Max(0,settings.explosionDamage),point,false);
            }
        }
        private bool Visible(Vector3 point,Collider target)
        {
            Vector3 delta=target.ClosestPoint(point)-point;
            if(delta.sqrMagnitude<.0001f) return true;
            var hits=Physics.RaycastAll(point,delta.normalized,delta.magnitude,mask,QueryTriggerInteraction.Ignore);
            foreach(var hit in hits)
                if(!Ignore(hit.collider) && hit.collider.GetComponentInParent<ShinobiHealth>()==null) return false;
            return true;
        }
        private void Damage(ShinobiHealth health,float amount,Vector3 source,bool blockable)
        {
            if(health==null || health.Defeated || owner!=null && (health.transform==owner || health.transform.IsChildOf(owner))) return;
            var defense=health.GetComponent<ShinobiDefense>();
            if(defense!=null && defense.isActiveAndEnabled) defense.ReceiveHit(amount,source,blockable);
            else health.ApplyDamage(amount);
        }
        private void Finish() { if(finished)return;finished=true;gameObject.SetActive(false);Destroy(gameObject); }
        public static void ClearAll()
        {
            foreach(var effect in effects) if(effect!=null) Destroy(effect);
            effects.Clear();
            foreach(var projectile in FindObjectsByType<ShinobiThrownWeapon>(FindObjectsSortMode.None)) projectile.Finish();
        }
        public static void ClearForOwner(Transform source)
        {
            foreach(var projectile in FindObjectsByType<ShinobiThrownWeapon>(FindObjectsSortMode.None))
                if(projectile.owner==source) projectile.Finish();
        }
    }
}
