using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
namespace Shinobi.Editor
{
    public static class ShinobiSessionTrainingSetup
    {
        [MenuItem("Shinobi/Etap 12/Przygotuj menu i pojedynek")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            const string panelPath="Assets/Shinobi/Configuration/GamePanel.asset";
            var panel=AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if(panel==null)
            {
                panel=ScriptableObject.CreateInstance<PanelSettings>(); panel.name="GamePanel";
                panel.scaleMode=PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution=new Vector2Int(1280,720); panel.match=0.5f;
                panel.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Shinobi/UI/GameTheme.tss");
                panel.sortingOrder=100; AssetDatabase.CreateAsset(panel,panelPath);
            }
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath); bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try
            {
                var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiMovement>(true)).ToArray();
                var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="GameSession");
                if(root==null) { root=new GameObject("GameSession"); SceneManager.MoveGameObjectToScene(root,scene); }
                var document=root.GetComponent<UIDocument>(); if(document==null) document=root.AddComponent<UIDocument>();
                document.panelSettings=panel; document.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Shinobi/UI/GameMenu.uxml");
                var session=root.GetComponent<ShinobiGameSession>(); if(session==null) session=root.AddComponent<ShinobiGameSession>();
                session.player=actors.Single(m=>m.GetComponent<ShinobiTrainingOpponent>()==null);
                session.opponent=actors.Select(m=>m.GetComponent<ShinobiTrainingOpponent>()).First(o=>o!=null);
                session.trainingDummy=scene.GetRootGameObjects().First(g=>g.name=="TrainingDummy");
                EditorUtility.SetDirty(session); EditorUtility.SetDirty(document);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("SESSION_SETUP_PASS");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
