using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Shinobi.Editor
{
    public static class ShinobiMainMenuSetup
    {
        [MenuItem("Shinobi/Menu glowne/Przygotuj sceny i przejscia")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ShinobiSessionTrainingSetup.Apply();
            const string panelPath="Assets/Shinobi/Configuration/MainMenuPanel.asset";
            var panel=AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if(panel==null)
            {
                panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Shinobi/Configuration/GamePanel.asset"));
                panel.name="MainMenuPanel";
                panel.targetTexture=null;
                AssetDatabase.CreateAsset(panel,panelPath);
            }
            var previous=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(ShinobiMainMenu.MenuScene);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=System.IO.File.Exists(ShinobiMainMenu.MenuScene)
                ? EditorSceneManager.OpenScene(ShinobiMainMenu.MenuScene,OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    string.IsNullOrEmpty(previous.path) && !previous.isDirty ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="MainMenu");
                if(root==null) { root=new GameObject("MainMenu"); SceneManager.MoveGameObjectToScene(root,scene); }
                var document=root.GetComponent<UIDocument>(); if(document==null) document=root.AddComponent<UIDocument>();
                document.panelSettings=panel;
                document.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Shinobi/UI/MainMenu.uxml");
                if(root.GetComponent<ShinobiMainMenu>()==null) root.AddComponent<ShinobiMainMenu>();
                if(!scene.GetRootGameObjects().Any(g=>g.GetComponent<Camera>()!=null))
                {
                    var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
                    SceneManager.MoveGameObjectToScene(cameraObject,scene);
                    var camera=cameraObject.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor;
                    camera.backgroundColor=new Color(0.06f,0.1f,0.15f); camera.cullingMask=0; cameraObject.tag="MainCamera";
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ShinobiMainMenu.MenuScene);
                var required=new[]{ShinobiMainMenu.MenuScene,ShinobiMainMenu.ArenaScene};
                EditorBuildSettings.scenes=required.Select(p=>new EditorBuildSettingsScene(p,true))
                    .Concat(EditorBuildSettings.scenes.Where(s=>!required.Contains(s.path))).ToArray();
                AssetDatabase.SaveAssets(); Debug.Log("MAIN_MENU_SETUP_PASS");
            }
            finally
            {
                if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if(opened) EditorSceneManager.CloseScene(scene,true);
            }
        }
        [MenuItem("Shinobi/Menu glowne/Otworz menu glowne")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!System.IO.File.Exists(ShinobiMainMenu.MenuScene)) Apply();
            EditorSceneManager.OpenScene(ShinobiMainMenu.MenuScene);
        }
    }
}
