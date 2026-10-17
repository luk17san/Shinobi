using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Shinobi
{
    [DefaultExecutionOrder(11000), DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class ShinobiMainMenu : MonoBehaviour
    {
        public const string MenuScene = "Assets/Shinobi/Scenes/MainMenu.unity";
        public const string ArenaScene = "Assets/Shinobi/Scenes/TrainingArena.unity";
        private static bool? requestedDuel;
        private bool loading;
        private UIDocument document;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRequest() { requestedDuel=null; }
        public static bool TryConsumeRequest(out bool duel)
        {
            duel=requestedDuel.GetValueOrDefault();
            bool exists=requestedDuel.HasValue; requestedDuel=null; return exists;
        }
        private void OnEnable()
        {
            Time.timeScale=1;
            document=GetComponent<UIDocument>();
            document.rootVisualElement.Q<Button>("StartTraining").clicked+=StartTraining;
            document.rootVisualElement.Q<Button>("StartDuel").clicked+=StartDuel;
            document.rootVisualElement.Q<Button>("Quit").clicked+=Quit;
        }
        private void Start() { document.rootVisualElement.Q<Button>("StartTraining").Focus(); }
        private void OnDisable()
        {
            if(document==null || document.rootVisualElement==null) return;
            var root=document.rootVisualElement;
            var training=root.Q<Button>("StartTraining"); if(training!=null) training.clicked-=StartTraining;
            var duel=root.Q<Button>("StartDuel"); if(duel!=null) duel.clicked-=StartDuel;
            var quit=root.Q<Button>("Quit"); if(quit!=null) quit.clicked-=Quit;
        }
        public void StartTraining() { LoadArena(false); }
        public void StartDuel() { LoadArena(true); }
        private void LoadArena(bool duel)
        {
            if(loading) return;
            if(!Application.CanStreamedLevelBeLoaded(ArenaScene))
            {
                document.rootVisualElement.Q<Label>("Description").text="Brak TrainingArena w Build Settings. Uruchom przygotowanie scen menu.";
                Debug.LogError("TrainingArena is missing from Build Settings.",this); return;
            }
            loading=true; requestedDuel=duel; Time.timeScale=1;
            document.rootVisualElement.Q("MenuPanel").SetEnabled(false);
            document.rootVisualElement.Q<Label>("Description").text="Wczytywanie areny…";
            SceneManager.LoadSceneAsync(ArenaScene,LoadSceneMode.Single);
        }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
