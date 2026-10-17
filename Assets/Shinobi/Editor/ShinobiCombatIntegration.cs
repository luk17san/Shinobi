using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinobi.Editor
{
    public static class ShinobiCombatIntegration
    {
        public const string ScenePath = "Assets/Shinobi/Scenes/TrainingArena.unity";
        public const string PrefabPath = "Assets/Shinobi/Prefabs/Player.prefab";
        public const string ProfilePath = "Assets/Shinobi/Configuration/TrainingCombat.asset";

        [MenuItem("Shinobi/Etap 4/Polacz walke i jutsu z arena")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Zatrzymaj Play Mode przed konfiguracja areny.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShinobiMovement>(true)).Single(g => g.GetComponent<ShinobiTrainingOpponent>() == null).gameObject;
                var combat = player.GetComponent<ShinobiCombat>();
                if (combat == null) throw new InvalidOperationException("Najpierw przygotuj trening walki etapu 2.");
                var profile = combat.profile != null ? combat.profile : GetOrCreateProfile(combat);
                Configure(player, profile);
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    Configure(prefab, profile);
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }

                var hud = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShinobiTrainingHud>(true)).FirstOrDefault();
                if (hud == null)
                {
                    var go = new GameObject("TrainingHUD");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    hud = go.AddComponent<ShinobiTrainingHud>();
                }
                var hudData = new SerializedObject(hud);
                hudData.FindProperty("player").objectReferenceValue = player.GetComponent<ShinobiMovement>();
                hudData.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Nie zapisano TrainingArena.");
                AssetDatabase.SaveAssets();
                Debug.Log("COMBAT_INTEGRATION_PASS: profil walki, jutsu, HP i panel treningowy zapisane; model zachowany.");
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }

        private static ShinobiCombatProfile GetOrCreateProfile(ShinobiCombat combat)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ShinobiCombatProfile>(ProfilePath);
            if (profile != null) return profile;
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));
            AssetDatabase.Refresh();
            profile = ScriptableObject.CreateInstance<ShinobiCombatProfile>();
            profile.combo = new ShinobiAttackDefinition[combat.ComboLength];
            AssetDatabase.CreateAsset(profile, ProfilePath);
            var legacy = new SerializedObject(combat);
            for (int i = 0; i < profile.combo.Length; i++)
            {
                var attack = ScriptableObject.CreateInstance<ShinobiAttackDefinition>();
                attack.name = "Uderzenie " + (i + 1);
                attack.attack = combat.GetAttack(i);
                // These dimensions were shared by all attacks in the previous controller.
                attack.attack.hitHeight = legacy.FindProperty("hitHeight").floatValue;
                attack.attack.hitDepth = legacy.FindProperty("hitDepth").floatValue;
                attack.attack.hitCenterHeight = legacy.FindProperty("hitCenterHeight").floatValue;
                attack.attack.pushDistance = i == 2 ? 1.5f : 0f;
                AssetDatabase.AddObjectToAsset(attack, profile);
                profile.combo[i] = attack;
            }
            var jutsu = ScriptableObject.CreateInstance<ShinobiJutsuDefinition>();
            jutsu.name = "Fala treningowa";
            AssetDatabase.AddObjectToAsset(jutsu, profile);
            profile.jutsu = new[] { jutsu };
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void Configure(GameObject player, ShinobiCombatProfile profile)
        {
            var combat = player.GetComponent<ShinobiCombat>();
            combat.profile = profile;
            var health = player.GetComponent<ShinobiHealth>();
            if (health == null) health = player.AddComponent<ShinobiHealth>();
            var healthData = new SerializedObject(health);
            if (healthData.FindProperty("visual").objectReferenceValue == null)
            {
                var renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
                var body = renderers.FirstOrDefault(r => r.name.Contains("Surface")) ?? renderers.FirstOrDefault();
                if (body != null) healthData.FindProperty("visual").objectReferenceValue = body;
                healthData.ApplyModifiedPropertiesWithoutUndo();
            }
            if (player.GetComponent<ShinobiTechniqueController>() == null)
            {
                var techniques = player.AddComponent<ShinobiTechniqueController>();
                var data = new SerializedObject(techniques);
                data.FindProperty("targetMask").intValue = new SerializedObject(combat).FindProperty("targetMask").intValue;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(combat);
        }
    }
}

