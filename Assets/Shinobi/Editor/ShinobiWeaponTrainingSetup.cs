using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinobi.Editor
{
    public static class ShinobiWeaponTrainingSetup
    {
        private const string Folder = "Assets/Shinobi/Configuration/Weapons";
        [MenuItem("Shinobi/Etap 5/Przygotuj ekwipunek broni")]
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
                var profile = player.GetComponent<ShinobiCombat>().profile;
                if (profile == null) throw new System.InvalidOperationException("Najpierw przygotuj profil etapu 4.");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var weapons = new[] { CreateWeapon(profile, 1), CreateWeapon(profile, 2) };
                Configure(player, weapons);
                var prefab = PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
                try { Configure(prefab, weapons); PrefabUtility.SaveAsPrefabAsset(prefab, ShinobiCombatIntegration.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                // Save new key defaults without changing any existing custom bindings.
                EditorUtility.SetDirty(profile);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Nie zapisano TrainingArena.");
                AssetDatabase.SaveAssets();
                Debug.Log("WEAPON_SETUP_PASS: ekwipunek sceny i prefabu gotowy; Y wybiera kolejna bron.");
            }
            finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
        }
        private static ShinobiWeaponDefinition CreateWeapon(ShinobiCombatProfile profile, int number)
        {
            string path = Folder + "/TrainingWeapon" + number + ".asset";
            var weapon = AssetDatabase.LoadAssetAtPath<ShinobiWeaponDefinition>(path);
            if (weapon != null) return weapon;
            weapon = ScriptableObject.CreateInstance<ShinobiWeaponDefinition>();
            weapon.displayName = "Bron treningowa " + number;
            weapon.comboWindow = profile.comboWindow;
            weapon.combo = new ShinobiAttackDefinition[profile.combo.Length];
            AssetDatabase.CreateAsset(weapon, path);
            for (int i = 0; i < weapon.combo.Length; i++)
            {
                var attack = Object.Instantiate(profile.combo[i]);
                attack.name = "Cios " + (i + 1);
                if (number == 2) { attack.attack.damage += 5; attack.attack.reach += 0.3f; }
                AssetDatabase.AddObjectToAsset(attack, weapon);
                weapon.combo[i] = attack;
            }
            EditorUtility.SetDirty(weapon);
            return weapon;
        }
        private static void Configure(GameObject player, ShinobiWeaponDefinition[] weapons)
        {
            if (player.GetComponent<ShinobiWeaponInventory>() != null) return;
            var inventory = player.AddComponent<ShinobiWeaponInventory>();
            var data = new SerializedObject(inventory);
            var items = data.FindProperty("weapons"); items.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++) items.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

