using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Shinobi.Editor {
    public static class ShinobiOpponentTrainingSetup {
        [MenuItem("Shinobi/Etap 8/Przygotuj przeciwnika treningowego")]
        public static void Apply() {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try {
                var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiMovement>(true)).Single(g=>g.GetComponent<ShinobiTrainingOpponent>()==null);
                var opponent=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiTrainingOpponent>(true)).FirstOrDefault();
                if(opponent==null) {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ShinobiCombatIntegration.PrefabPath),scene);
                    PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    instance.name="TrainingOpponent"; instance.transform.position=new Vector3(-6,0.08f,0);
                    Object.DestroyImmediate(instance.GetComponent<ShinobiWeaponInventory>());
                    opponent=instance.AddComponent<ShinobiTrainingOpponent>();
                }
                opponent.target=player;
                const string profilePath="Assets/Shinobi/Configuration/TrainingOpponentCombat.asset";
                var profile=AssetDatabase.LoadAssetAtPath<ShinobiCombatProfile>(profilePath);
                if(profile==null) { profile=Object.Instantiate(player.GetComponent<ShinobiCombat>().profile); profile.name="TrainingOpponentCombat"; AssetDatabase.CreateAsset(profile,profilePath); }
                const string attackPath="Assets/Shinobi/Configuration/TrainingOpponentStrike.asset";
                var attack=AssetDatabase.LoadAssetAtPath<ShinobiAttackDefinition>(attackPath);
                if(attack==null) { attack=Object.Instantiate(player.GetComponent<ShinobiCombat>().profile.combo[0]); attack.name="TrainingOpponentStrike"; AssetDatabase.CreateAsset(attack,attackPath); }
                profile.combo=new[] { attack }; EditorUtility.SetDirty(profile);
                opponent.GetComponent<ShinobiCombat>().profile=profile;
                EditorUtility.SetDirty(opponent);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("OPPONENT_SETUP_PASS");
            } finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
