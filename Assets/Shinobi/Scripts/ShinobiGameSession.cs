using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Shinobi
{
    public enum ShinobiSessionPhase { Menu, Playing, Paused, Finished }
    [DefaultExecutionOrder(11000), DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class ShinobiGameSession : MonoBehaviour
    {
        public ShinobiMovement player;
        public ShinobiTrainingOpponent opponent;
        public GameObject trainingDummy;
        private static ShinobiGameSession active;
        private UIDocument document;
        private ShinobiHealth playerHealth, opponentHealth;
        private bool originalPlayerAutoReset, originalOpponentAutoReset, originalDummyActive;
        private float originalTimeScale=1, runningTimeScale=1;
        private int blockedUntilFrame;
        private bool started;
        public ShinobiSessionPhase Phase { get; private set; } = ShinobiSessionPhase.Menu;
        public bool IsDuel { get; private set; }
        public string Result { get; private set; } = "";
        public static bool GameplayInputAllowed => active==null || active.Phase==ShinobiSessionPhase.Playing && Time.frameCount>active.blockedUntilFrame;
        public static bool ShowGameplayHud => active==null || active.Phase==ShinobiSessionPhase.Playing;
        public static bool DuelActive => active!=null && active.IsDuel;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { active=null; }
        private void Awake()
        {
            if(active!=null && active!=this) { enabled=false; return; }
            active=this; originalTimeScale=Time.timeScale; runningTimeScale=originalTimeScale>0?originalTimeScale:1;
            document=GetComponent<UIDocument>();
            if(player!=null) { playerHealth=player.GetComponent<ShinobiHealth>(); originalPlayerAutoReset=playerHealth.TrainingAutoReset; }
            if(opponent!=null) { opponentHealth=opponent.GetComponent<ShinobiHealth>(); originalOpponentAutoReset=opponentHealth.TrainingAutoReset; }
            if(trainingDummy!=null) originalDummyActive=trainingDummy.activeSelf;
        }
        private void Start()
        {
            started=true;
            Bind("StartTraining",StartTraining); Bind("StartDuel",StartDuel); Bind("Resume",Resume);
            Bind("Restart",RestartRound); Bind("ToMenu",ShowMenu); Bind("Quit",Quit); Bind("PauseButton",Pause);
            ShowMenu();
        }
        private void OnEnable()
        {
            if(active!=null || document==null) return;
            active=this; originalTimeScale=Time.timeScale; runningTimeScale=originalTimeScale>0?originalTimeScale:1;
            document.rootVisualElement.style.display=DisplayStyle.Flex;
            if(started) ShowMenu();
        }
        private void Bind(string name,System.Action action) { var button=document.rootVisualElement.Q<Button>(name); if(button!=null) button.clicked+=action; }
        private void Update()
        {
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if(Phase==ShinobiSessionPhase.Playing) Pause();
                else if(Phase==ShinobiSessionPhase.Paused) Resume();
            }
        }
        private void LateUpdate()
        {
            if(Phase!=ShinobiSessionPhase.Playing || !IsDuel || playerHealth==null || opponentHealth==null) return;
            if(!playerHealth.Defeated && !opponentHealth.Defeated) return;
            Result=playerHealth.Defeated && opponentHealth.Defeated ? "REMIS" : playerHealth.Defeated ? "PORAŻKA" : "ZWYCIĘSTWO";
            Phase=ShinobiSessionPhase.Finished;
            CancelActions(); Time.timeScale=0; RefreshUi();
        }
        public void StartTraining() { StartRound(false); }
        public void StartDuel() { StartRound(true); }
        public void RestartRound() { StartRound(IsDuel); }
        private void StartRound(bool duel)
        {
            if(player==null || opponent==null || playerHealth==null || opponentHealth==null) return;
            IsDuel=duel; Result=""; CancelActions();
            opponent.ResetSessionState();
            playerHealth.TrainingAutoReset=!duel; opponentHealth.TrainingAutoReset=!duel;
            playerHealth.ResetHealth(); opponentHealth.ResetHealth();
            player.ResetTrainingState(); opponent.GetComponent<ShinobiMovement>().ResetTrainingState();
            player.Face(duel?-1:1); opponent.GetComponent<ShinobiMovement>().Face(1);
            var inventory=player.GetComponent<ShinobiWeaponInventory>(); if(inventory!=null) inventory.ResetEquipment();
            if(trainingDummy!=null)
            {
                trainingDummy.SetActive(!duel && originalDummyActive);
                var health=trainingDummy.GetComponent<ShinobiHealth>(); if(health!=null) health.ResetHealth();
            }
            foreach(var effect in FindObjectsByType<ShinobiCombatEffect>(FindObjectsSortMode.None)) Destroy(effect.gameObject);
            Phase=ShinobiSessionPhase.Playing; blockedUntilFrame=Time.frameCount+1;
            Time.timeScale=runningTimeScale; opponent.SetRunning(duel); RefreshUi();
        }
        public void Pause()
        {
            if(Phase!=ShinobiSessionPhase.Playing) return;
            runningTimeScale=Time.timeScale>0?Time.timeScale:runningTimeScale;
            ClearGestures(); ShinobiClashResolver.ClearPending();
            Phase=ShinobiSessionPhase.Paused; Time.timeScale=0; RefreshUi();
        }
        public void Resume()
        {
            if(Phase!=ShinobiSessionPhase.Paused) return;
            ClearGestures(); blockedUntilFrame=Time.frameCount+1;
            Phase=ShinobiSessionPhase.Playing; Time.timeScale=runningTimeScale; RefreshUi();
        }
        public void ShowMenu()
        {
            CancelActions(); IsDuel=false; Result=""; Phase=ShinobiSessionPhase.Menu;
            Time.timeScale=0; RefreshUi();
        }
        private void ClearGestures()
        {
            if(player!=null)
            {
                player.ClearInputGestures(); var techniques=player.GetComponent<ShinobiTechniqueController>();
                if(techniques!=null && techniques.Focus) techniques.Interrupt();
            }
            if(opponent!=null) opponent.GetComponent<ShinobiMovement>().ClearInputGestures();
        }
        private void CancelActions()
        {
            ShinobiThrownWeapon.ClearAll();
            ClearGestures(); ShinobiClashResolver.ClearPending();
            if(player!=null) player.GetComponent<ShinobiCombat>()?.InterruptAction();
            if(opponent!=null) opponent.GetComponent<ShinobiCombat>()?.InterruptAction();
        }
        private void RefreshUi()
        {
            if(document==null) return;
            var root=document.rootVisualElement;
            bool menu=Phase==ShinobiSessionPhase.Menu;
            var overlay=root.Q("Overlay"); if(overlay!=null) overlay.style.display=Phase==ShinobiSessionPhase.Playing?DisplayStyle.None:DisplayStyle.Flex;
            var bar=root.Q("GameBar"); if(bar!=null) bar.style.display=Phase==ShinobiSessionPhase.Playing?DisplayStyle.Flex:DisplayStyle.None;
            SetVisible("StartTraining",menu); SetVisible("StartDuel",menu); SetVisible("Resume",Phase==ShinobiSessionPhase.Paused);
            SetVisible("Restart",!menu); SetVisible("ToMenu",!menu);
            var title=root.Q<Label>("Title"); if(title!=null) title.text=menu?"SHINOBI":Phase==ShinobiSessionPhase.Paused?"PAUZA":Result;
            var description=root.Q<Label>("Description"); if(description!=null) description.text=menu?"Arena treningowa\nWybierz swobodny trening lub pojedynek z AI.":Phase==ShinobiSessionPhase.Paused?"Walka jest wstrzymana.":"Pojedynek zakończony. Zagraj ponownie lub wróć do menu.";
            var mode=root.Q<Label>("Mode"); if(mode!=null) mode.text=IsDuel?"POJEDYNEK Z AI":"SWOBODNY TRENING";
            var restart=root.Q<Button>("Restart"); if(restart!=null) restart.text=IsDuel?"Rewanż":"Restart treningu";
            var first=root.Q<Button>(menu?"StartTraining":Phase==ShinobiSessionPhase.Paused?"Resume":"Restart"); first?.Focus();
        }
        private void SetVisible(string name,bool visible) { var element=document.rootVisualElement.Q(name); if(element!=null) element.style.display=visible?DisplayStyle.Flex:DisplayStyle.None; }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable()
        {
            if(active!=this) return;
            ShinobiThrownWeapon.ClearAll();
            if(playerHealth!=null) playerHealth.TrainingAutoReset=originalPlayerAutoReset;
            if(opponentHealth!=null) opponentHealth.TrainingAutoReset=originalOpponentAutoReset;
            if(trainingDummy!=null) trainingDummy.SetActive(originalDummyActive);
            if(document!=null && document.rootVisualElement!=null) document.rootVisualElement.style.display=DisplayStyle.None;
            Time.timeScale=originalTimeScale; active=null;
        }
    }
}



