using UnityEngine;
namespace Shinobi
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiHealth))]
    public sealed class ShinobiCombatFeedback : MonoBehaviour
    {
        public ShinobiFeedbackProfile profile;
        private ShinobiCombatEffect lastBlockEffect;
        private ShinobiHealth health;
        private ShinobiDefense defense;
        private ShinobiMovement movement;
        private ShinobiCombat combat;
        private ShinobiTechniqueController techniques;
        private void Awake()
        {
            health=GetComponent<ShinobiHealth>(); defense=GetComponent<ShinobiDefense>(); movement=GetComponent<ShinobiMovement>();
            combat=GetComponent<ShinobiCombat>(); techniques=GetComponent<ShinobiTechniqueController>();
        }
        private void OnEnable()
        {
            if(health!=null) health.DamageApplied+=OnDamage;
            if(defense!=null) { defense.Blocked+=OnBlock; defense.GuardBreakStarted+=OnGuardBreak; }
            if(movement!=null) movement.DashStarted+=OnDash;
            if(combat!=null) { combat.SwingActivated+=OnSwing; combat.Clashed+=OnClash; }
            if(techniques!=null) techniques.TechniqueReleased+=OnJutsu;
        }
        private void OnDisable()
        {
            if(health!=null) health.DamageApplied-=OnDamage;
            if(defense!=null) { defense.Blocked-=OnBlock; defense.GuardBreakStarted-=OnGuardBreak; }
            if(movement!=null) movement.DashStarted-=OnDash;
            if(combat!=null) { combat.SwingActivated-=OnSwing; combat.Clashed-=OnClash; }
            if(techniques!=null) techniques.TechniqueReleased-=OnJutsu;
        }
        private Vector3 Center => transform.position+Vector3.up*1.2f;
        private void OnDamage(float amount) { if(profile!=null) Spawn(ShinobiEffectKind.Hit,Center,profile.hitColor,0.4f,"-"+amount.ToString("0.#")); }
        private void OnBlock() { if(profile!=null) lastBlockEffect=Spawn(ShinobiEffectKind.Block,Center,profile.blockColor,0.65f,"BLOK"); }
        private void OnGuardBreak() { if(lastBlockEffect!=null) Destroy(lastBlockEffect.gameObject); if(profile!=null) Spawn(ShinobiEffectKind.GuardBreak,Center+Vector3.up*0.6f,profile.guardBreakColor,0.8f,"PRZELAMANIE"); }
        private void OnDash(float facing) { if(profile!=null) Spawn(ShinobiEffectKind.Dash,transform.position+Vector3.up*0.2f-Vector3.right*facing*0.4f,profile.dashColor,0.55f,null); }
        private void OnSwing(float facing) { if(profile!=null) Spawn(ShinobiEffectKind.Swing,Center+Vector3.right*facing*0.75f,profile.swingColor,0.6f,null,facing); }
        private void OnClash(Vector3 point) { if(profile!=null) Spawn(ShinobiEffectKind.Clash,point,profile.guardBreakColor,0.85f,"ZDERZENIE"); }
        private void OnJutsu(Vector3 point,float radius) { if(profile!=null) Spawn(ShinobiEffectKind.Jutsu,point,profile.jutsuColor,radius,null); }
        private ShinobiCombatEffect Spawn(ShinobiEffectKind kind,Vector3 point,Color tint,float radius,string text,float facing=1)
        {
            if(!profile.showEffects && (!profile.showLabels || string.IsNullOrEmpty(text))) return null;
            var go=new GameObject("CombatFX "+kind); go.transform.position=new Vector3(point.x,point.y,transform.position.z-0.7f);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
            var points=new Vector3[33];
            for(int i=0;i<points.Length;i++)
            {
                float angle=(float)i/(points.Length-1)*Mathf.PI*2;
                float r=(kind==ShinobiEffectKind.Hit || kind==ShinobiEffectKind.GuardBreak || kind==ShinobiEffectKind.Clash) && i%2==0 ? radius*0.45f : radius;
                points[i]=new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0);
                if(kind==ShinobiEffectKind.Dash) points[i]=new Vector3(points[i].x*1.5f,points[i].y*0.2f,0);
                if(kind==ShinobiEffectKind.Swing) { float arc=-Mathf.PI*0.7f+(float)i/(points.Length-1)*Mathf.PI*1.4f; points[i]=new Vector3(Mathf.Cos(arc)*radius*facing,Mathf.Sin(arc)*radius,0); }
            }
            var effect=go.AddComponent<ShinobiCombatEffect>(); effect.Initialize(kind,points,tint,profile,text); return effect;
        }
    }
}


