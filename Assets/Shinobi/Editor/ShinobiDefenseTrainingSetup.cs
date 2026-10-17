using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinobi.Editor
{
    public static class ShinobiDefenseTrainingSetup
    {
        [MenuItem("Shinobi/Etap 6/Przygotuj blok i wytrzymalosc")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShinobiMovement>(true)).Single(g => g.GetComponent<ShinobiTrainingOpponent>() == null).gameObject;
                Configure(player);
                var prefab = PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
                try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, ShinobiCombatIntegration.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                var profile = player.GetComponent<ShinobiCombat>().profile;
                EditorUtility.SetDirty(profile);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Nie zapisano TrainingArena.");
                AssetDatabase.SaveAssets();
                Debug.Log("DEFENSE_SETUP_PASS: autoblok i wytrzymalosc gracza oraz prefabu gotowe.");
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }
        private static void Configure(GameObject player)
        {
            if (player.GetComponent<ShinobiDefense>() == null) player.AddComponent<ShinobiDefense>();
        }
    }
}

