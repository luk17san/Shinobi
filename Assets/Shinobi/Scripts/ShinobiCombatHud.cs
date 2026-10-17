using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Shinobi
{
    [DefaultExecutionOrder(12000), RequireComponent(typeof(UIDocument),typeof(ShinobiGameSession))]
    public sealed class ShinobiCombatHud : MonoBehaviour
    {
        [Range(0.7f,1.2f), Tooltip("1 = compact approved HUD at 1280x720 reference resolution.")]
        public float hudScale=1;
        public Texture2D playerPortrait, dummyPortrait;
        public ShinobiTrainingHud legacyHud;
        private static ShinobiCombatHud active;
        private UIDocument document;
        private ShinobiGameSession session;
        private ShinobiHealth health;
        private ShinobiDefense defense;
        private ShinobiCombat combat;
        private ShinobiTechniqueController techniques;
        private ShinobiWeaponInventory inventory;
        private VisualElement root,hud;
        private readonly Dictionary<string,VisualElement> elements=new Dictionary<string,VisualElement>();
        private readonly List<VisualElement> slots=new List<VisualElement>();
        private ShinobiJutsuDefinition displayedJutsu;
        private ShinobiSeal[] displayedSequence;
        private bool drawerOpen;
        public static bool IsActive => active!=null && active.isActiveAndEnabled && active.hud!=null && ShinobiGameSession.ShowGameplayHud;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { active=null; }
        private void Awake()
        {
            session=GetComponent<ShinobiGameSession>(); document=GetComponent<UIDocument>();
            if(session.player==null) return;
            health=session.player.GetComponent<ShinobiHealth>(); defense=session.player.GetComponent<ShinobiDefense>();
            combat=session.player.GetComponent<ShinobiCombat>(); techniques=session.player.GetComponent<ShinobiTechniqueController>(); inventory=session.player.GetComponent<ShinobiWeaponInventory>();
        }
        private void OnEnable() { active=this; if(legacyHud!=null) legacyHud.enabled=false; }
        private void OnDisable()
        {
            if(hud!=null) hud.style.display=DisplayStyle.None;
            if(active==this) active=null;
            if(legacyHud!=null) legacyHud.enabled=true;
        }
        private VisualElement Element(string name)
        {
            if(elements.TryGetValue(name,out var element)) return element;
            element=root.Q(name); elements[name]=element; return element;
        }
        private void Text(string name,string value) { if(Element(name) is Label label && label.text!=value) label.text=value; }
        private void Visible(string name,bool value) { var element=Element(name); if(element!=null) element.style.display=value?DisplayStyle.Flex:DisplayStyle.None; }
        private void Value(string fill,string label,float value,float max)
        {
            var element=Element(fill); if(element!=null) element.style.width=Length.Percent(max>0?Mathf.Clamp01(value/max)*100:0);
            Text(label,$"{Mathf.Max(0,value):0} / {Mathf.Max(0,max):0}");
        }
        private void Bind(string name,Action callback) { if(Element(name) is Button button) button.clicked+=callback; }
        private void BindTree()
        {
            root=document.rootVisualElement; elements.Clear(); hud=Element("CombatHud"); if(hud==null) return;
            root.Query<VisualElement>(className:"hud-frame").ForEach(e=>{ e.generateVisualContent+=ShinobiHudDrawing.Frame; });
            hud.Query<VisualElement>(className:"hud-icon").ForEach(e=>{
                string kind=e.ClassListContains("heart")?"heart":e.ClassListContains("chakra")?"chakra":e.ClassListContains("stamina")?"stamina":"speed";
                e.generateVisualContent+=c=>ShinobiHudDrawing.Icon(c,kind);
            });
            if(Element("PlayerPortrait")!=null && playerPortrait!=null) Element("PlayerPortrait").style.backgroundImage=new StyleBackground(playerPortrait);
            Bind("TrainingToolsToggle",()=>{drawerOpen=!drawerOpen;});
            Bind("ToggleSparring",()=>{ if(TrainingAllowed) session.opponent.SetRunning(!session.opponent.Running); });
            Bind("ResetPractice",()=>{ if(TrainingAllowed) session.RestartRound(); });
            Bind("TestFront",()=>TestHit(1,true)); Bind("TestRear",()=>TestHit(-1,true)); Bind("TestJutsu",()=>TestHit(1,false));
            displayedJutsu=null; displayedSequence=null;
        }
        private bool TrainingAllowed => session.Phase==ShinobiSessionPhase.Playing && !session.IsDuel;
        private void TestHit(float side,bool blockable)
        {
            if(TrainingAllowed && defense!=null) defense.ReceiveHit(25,session.player.transform.position+Vector3.right*session.player.Facing*side,blockable);
        }
        private void LateUpdate()
        {
            if(document==null || session==null || health==null || document.rootVisualElement==null) return;
            if(root!=document.rootVisualElement) BindTree();
            if(hud==null) return;
            bool playing=session.Phase==ShinobiSessionPhase.Playing;
            hud.style.display=playing?DisplayStyle.Flex:DisplayStyle.None;
            if(!playing) { drawerOpen=false; return; }
            ApplyScale();
            Value("HpFill","HpValue",health.CurrentHealth,health.MaxHealth);
            var profile=combat!=null?combat.profile:null;
            Value("ChakraFill","ChakraValue",techniques!=null?techniques.Chakra:0,profile!=null?profile.maxChakra:0);
            Value("StaminaFill","StaminaValue",defense!=null?defense.Stamina:0,defense!=null?defense.MaxStamina:0);
            var weapon=inventory!=null?inventory.CurrentWeapon:null;
            Text("WeaponName",inventory!=null?inventory.CurrentWeaponName.ToUpperInvariant():"WALKA WRĘCZ");
            Text("WeaponKey",profile!=null?profile.nextWeaponKey.ToString():"Y");
            var throwing=session.player.GetComponent<ShinobiThrowController>();
            string throwKey=profile!=null?profile.throwKey.ToString():"H";
            Text("WeaponActionHint",weapon!=null && weapon.throwable
                ? throwing!=null && throwing.CooldownRemaining>0 ? $"ZMIEŃ · {throwKey} {throwing.CooldownRemaining:0.0}s" : $"ZMIEŃ · {throwKey} RZUT"
                : "ZMIEŃ BROŃ");
            if(Element("WeaponIcon")!=null) Element("WeaponIcon").style.backgroundImage=weapon!=null && weapon.hudIcon!=null?new StyleBackground(weapon.hudIcon):StyleKeyword.None;
            UpdateTarget(); UpdateJutsu(profile);
            var movement=session.player;
            string dash=movement.IsDashing?"DASH":movement.DashCooldownRemaining>0?$"{movement.DashCooldownRemaining:0.0} s":defense!=null && defense.Stamina<movement.DashStaminaCost?"BRAK WYTRZ.":movement.DashReady?"GOTOWY":"NIEDOSTĘPNY";
            Text("DashStatus",dash); Text("DashCost",movement.DashStaminaCost.ToString("0.#"));
            Element("DashStatus")?.EnableInClassList("unavailable",dash!="GOTOWY");
            Visible("TrainingToolsToggle",!session.IsDuel); Visible("TrainingDrawer",drawerOpen && !session.IsDuel);
            if(Element("ToggleSparring") is Button sparring) sparring.text=session.opponent.Running?"Zatrzymaj sparing":"Rozpocznij sparing";
        }
        private void UpdateTarget()
        {
            bool rival=session.IsDuel || session.opponent!=null && session.opponent.Running;
            var target=rival && session.opponent!=null?session.opponent.GetComponent<ShinobiHealth>():session.trainingDummy!=null && session.trainingDummy.activeInHierarchy?session.trainingDummy.GetComponent<ShinobiHealth>():null;
            Visible("TargetCard",target!=null); if(target==null) return;
            Text("TargetName",rival?"RYWAL":"MANEKIN"); Value("TargetHpFill","TargetHpValue",target.CurrentHealth,target.MaxHealth);
            Text("TargetStatus",rival?session.opponent.Status:"");
            var texture=rival?playerPortrait:dummyPortrait;
            if(texture!=null && Element("TargetPortrait")!=null) { Element("TargetPortrait").style.backgroundImage=new StyleBackground(texture); Element("TargetPortrait").style.unityBackgroundImageTintColor=rival?new Color(0.5f,0.86f,1):Color.white; }
        }
        private static bool SameSequence(ShinobiSeal[] a,ShinobiSeal[] b)
        {
            if(a==null || b==null) return a==b;
            if(a.Length!=b.Length) return false;
            for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) return false;
            return true;
        }
        private static string Key(ShinobiSeal seal)=>seal==ShinobiSeal.Up?"W":seal==ShinobiSeal.Down?"S":seal==ShinobiSeal.Left?"A":"D";
        private void UpdateJutsu(ShinobiCombatProfile profile)
        {
            var jutsu=profile!=null && profile.jutsu!=null && profile.jutsu.Length>0?profile.jutsu[0]:null;
            Visible("JutsuCard",jutsu!=null && techniques!=null); if(jutsu==null || techniques==null) return;
            if(displayedJutsu!=jutsu || !SameSequence(displayedSequence,jutsu.sequence))
            {
                displayedJutsu=jutsu; displayedSequence=jutsu.sequence!=null?(ShinobiSeal[])jutsu.sequence.Clone():null;
                var container=Element("SealSlots"); container.Clear(); slots.Clear();
                int count=displayedSequence!=null?Mathf.Min(6,displayedSequence.Length):0;
                for(int i=0;i<count;i++)
                {
                    var slot=new VisualElement(); slot.AddToClassList("seal-slot"); slot.style.width=Mathf.Min(45,210f/Mathf.Max(1,count));
                    var icon=new VisualElement(); icon.AddToClassList("hud-icon"); icon.generateVisualContent+=c=>ShinobiHudDrawing.Icon(c,"seal"); slot.Add(icon);
                    var key=new Label(Key(displayedSequence[i])); key.AddToClassList("keycap"); slot.Add(key); container.Add(slot); slots.Add(slot);
                }
                if(displayedSequence!=null && displayedSequence.Length>6) container.Add(new Label("…"));
            }
            Text("JutsuName",jutsu.name.ToUpperInvariant()); Text("JutsuCost",Mathf.Max(0,jutsu.chakraCost).ToString("0.#"));
            string focus=profile.focusKey.ToString();
            Text("JutsuHint",techniques.Busy?techniques.Focus?$"PIECZĘCIE: {techniques.EnteredSeals.Count} / {jutsu.sequence?.Length??0} · PUŚĆ {focus}":"WYKONYWANIE JUTSU":techniques.Chakra<jutsu.chakraCost?"BRAK CHAKRY":$"PRZYTRZYMAJ {focus} · WPROWADŹ PIECZĘCIE · PUŚĆ {focus}");
            for(int i=0;i<slots.Count;i++)
            {
                bool entered=techniques.Focus && techniques.EnteredSeals.Count>i;
                slots[i].EnableInClassList("entered",entered && techniques.EnteredSeals[i]==displayedSequence[i]);
                slots[i].EnableInClassList("wrong",entered && techniques.EnteredSeals[i]!=displayedSequence[i]);
            }
        }
        private void ApplyScale()
        {
            float width=root.resolvedStyle.width;
            float scale=Mathf.Clamp(hudScale,0.7f,1.2f)*(!float.IsNaN(width)&&width>0?Mathf.Min(1,width/1100f):1);
            foreach(var name in new[]{"PlayerCard","TargetCard","WeaponCard","JutsuCard","DashCard"})
            {
                var e=Element(name); if(e==null) continue;
                float x=name=="TargetCard"||name=="DashCard"?100:name=="JutsuCard"?50:0;
                float y=name=="PlayerCard"||name=="TargetCard"?0:100;
                e.style.transformOrigin=new TransformOrigin(Length.Percent(x),Length.Percent(y),0);
                e.style.scale=new Scale(new Vector3(scale,scale,1));
            }
        }
    }
}

