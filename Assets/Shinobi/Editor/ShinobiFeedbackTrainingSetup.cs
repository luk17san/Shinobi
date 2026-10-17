using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Shinobi.Editor
{
    public static class ShinobiFeedbackTrainingSetup
    {
        [MenuItem("Shinobi/Etap 11/Przygotuj efekty walki")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path="Assets/Shinobi/Configuration/TrainingFeedback.asset";
            var profile=AssetDatabase.LoadAssetAtPath<ShinobiFeedbackProfile>(path);
            if(profile==null) { profile=ScriptableObject.CreateInstance<ShinobiFeedbackProfile>(); AssetDatabase.CreateAsset(profile,path); }
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath); bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try
            {
                foreach(var health in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiHealth>(true))) Configure(health.gameObject,profile);
                var prefab=PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
                try { Configure(prefab,profile); PrefabUtility.SaveAsPrefabAsset(prefab,ShinobiCombatIntegration.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("FEEDBACK_SETUP_PASS");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        private static void Configure(GameObject actor,ShinobiFeedbackProfile profile)
        {
            var feedback=actor.GetComponent<ShinobiCombatFeedback>(); if(feedback==null) feedback=actor.AddComponent<ShinobiCombatFeedback>();
            if(feedback.profile==null) feedback.profile=profile;
            EditorUtility.SetDirty(feedback);
        }
    }
}
