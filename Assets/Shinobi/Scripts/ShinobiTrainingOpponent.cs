using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiMovement), typeof(ShinobiCombat), typeof(ShinobiHealth))]
    public sealed class ShinobiTrainingOpponent : MonoBehaviour
    {
        public ShinobiMovement target;
        [SerializeField, Range(0.1f, 1f)] private float approachSpeed = 0.45f;
        [SerializeField, Range(0.8f, 2f)] private float attackDistance = 1.15f;
        [SerializeField, Min(0.1f)] private float telegraphDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.5f;
        private bool running;
        private float readyAt, warningAt = -1f;
        private ShinobiMovement movement;
        private ShinobiCombat combat;
        private ShinobiHealth health;
        private ShinobiDefense defense;
        public bool Running => running;
        public string Status { get; private set; } = "Pauza";
        private void Awake() {
            movement=GetComponent<ShinobiMovement>(); combat=GetComponent<ShinobiCombat>();
            health=GetComponent<ShinobiHealth>(); defense=GetComponent<ShinobiDefense>();
        }
        private void OnEnable() { if(health!=null) health.Damaged+=CancelWarning; }
        private void OnDisable() { if(health!=null) health.Damaged-=CancelWarning; SetRunning(false); }
        private void CancelWarning() { warningAt=-1; readyAt=Time.time+Mathf.Max(0.1f,attackInterval); }
        public void SetRunning(bool value) {
            running=value; CancelWarning();
            if(combat!=null) combat.InterruptAction();
            Status=value ? "Gotowy" : "Pauza";
        }
        public void ResetTraining() {
            SetRunning(false);
            health.ResetHealth(); if(defense!=null) defense.ResetDefense();
            // The movement state owns the original spawn position and action reset.
            resetRequested=true;
        }
        public void ResetSessionState() { resetRequested=false; SetRunning(false); }
        private bool resetRequested;
        public ShinobiInput ReadInput() {
            if(resetRequested) { resetRequested=false; return new ShinobiInput(0,false,true); }
            if(!isActiveAndEnabled || !running || target==null || !target.isActiveAndEnabled) { Status="Pauza"; warningAt=-1; return default; }
            var targetHealth=target.GetComponent<ShinobiHealth>();
            if(health.Defeated || targetHealth==null || targetHealth.Defeated || defense!=null && defense.GuardBroken) {
                Status="Przerwa"; CancelWarning(); return default;
            }
            if(combat.IsAttacking) { Status="Atak"; return default; }
            float delta=target.transform.position.x-transform.position.x;
            float direction=delta<0 ? -1 : 1;
            float distance=Mathf.Abs(delta);
            if(!movement.IsGrounded) { Status="Ladowanie"; warningAt=-1; return default; }
            if(distance>Mathf.Max(0.8f,attackDistance) || movement.Facing!=direction) {
                Status="Podejscie"; warningAt=-1;
                return new ShinobiInput(direction*Mathf.Clamp(approachSpeed,0.1f,1f),false,false);
            }
            if(!target.IsGrounded) { Status="Czekam na ladowanie"; warningAt=-1; return default; }
            if(Time.time<readyAt) { Status="Gotowy"; return default; }
            if(warningAt<0) warningAt=Time.time;
            Status="UWAGA: CIOS";
            if(Time.time-warningAt<Mathf.Max(0.1f,telegraphDuration)) return default;
            warningAt=-1; readyAt=Time.time+Mathf.Max(0.1f,attackInterval);
            return new ShinobiInput(0,false,false,true);
        }
        private void OnGUI() {
            if(!ShinobiGameSession.ShowGameplayHud || ShinobiCombatHud.IsActive) return;
            var camera=Camera.main; if(camera==null) return;
            var point=camera.WorldToScreenPoint(transform.position+Vector3.up*3.1f);
            if(point.z>0) GUI.Box(new Rect(point.x-85,Screen.height-point.y,170,24),"Rywal: "+Status);
        }
    }
}

