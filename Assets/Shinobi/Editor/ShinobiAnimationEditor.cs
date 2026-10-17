using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Shinobi.Editor
{
    public sealed class ShinobiAnimationEditor : EditorWindow
    {
        private AnimatorController controller;
        private ShinobiAnimationDriver character;
        private ShinobiWeaponDefinition weapon;
        private Vector2 scroll;
        private static readonly string[] States = { "Idle", "Walk", "Run", "Jump", "Fall", "Crouch", "Crouch Walk", "Dash", "Hit", "Block", "Guard Break", "Focus" };
        private static readonly string[] Labels = { "Spoczynek", "Chodzenie", "Bieg", "Skok", "Opadanie", "Kucanie", "Chod w kucki", "Dash", "Trafienie", "Blok", "Przelamanie gardy", "Pieczecie jutsu" };
        [MenuItem("Shinobi/Edytor animacji")]
        public static void Open() { Open(null); }
        public static void Open(ShinobiAnimationDriver driver)
        {
            var window = GetWindow<ShinobiAnimationEditor>("Animacje Shinobi");
            window.minSize = new Vector2(430, 400);
            if (driver != null)
            {
                window.character = driver;
                var assigned = driver.animator != null ? driver.animator.runtimeAnimatorController : null;
                if (assigned is AnimatorOverrideController overrides) assigned = overrides.runtimeAnimatorController;
                if (assigned is AnimatorController selected) window.controller = selected;
            }
            if (window.controller == null) window.controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ShinobiAnimatorControllerSetup.ControllerPath);
            window.Show();
        }
        public static bool SetMotion(AnimatorController controller, string stateName, AnimationClip clip)
        {
            if (controller == null || controller.layers.Length == 0) return false;
            var state = controller.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);
            if (state == null) return false;
            Undo.RecordObject(state, "Przypisz animacje " + stateName);
            state.motion = clip;
            EditorUtility.SetDirty(state);
            AssetDatabase.SaveAssetIfDirty(state);
            return true;
        }
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            character = (ShinobiAnimationDriver)EditorGUILayout.ObjectField("Postac (opcjonalnie)", character, typeof(ShinobiAnimationDriver), true);
            controller = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", controller, typeof(AnimatorController), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (controller == null || controller.layers.Length == 0) EditorGUILayout.HelpBox("Najpierw przygotuj Animator Controller w menu Shinobi > Etap 9.", MessageType.Info);
                else
                {
                    if (GUILayout.Button("Otworz graf w Animatorze")) AssetDatabase.OpenAsset(controller);
                    EditorGUILayout.LabelField("Ruch i reakcje", EditorStyles.boldLabel);
                    var machine = controller.layers[0].stateMachine;
                    var idle = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle");
                    int missing = 0;
                    for (int i = 0; i < States.Length; i++)
                    {
                        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == States[i]);
                        if (state == null) { EditorGUILayout.HelpBox("Brak stanu: " + States[i], MessageType.Warning); missing++; continue; }
                        if (state.motion != null && !(state.motion is AnimationClip)) { EditorGUILayout.LabelField(Labels[i], "Blend Tree — edytuj w Animatorze"); continue; }
                        var clip = state.motion as AnimationClip;
                        EditorGUI.BeginChangeCheck();
                        var next = (AnimationClip)EditorGUILayout.ObjectField(Labels[i] + " (" + States[i] + ")", clip, typeof(AnimationClip), false);
                        if (EditorGUI.EndChangeCheck()) SetMotion(controller, States[i], next);
                        if (clip == null || i > 0 && idle != null && clip == idle.motion) { missing++; EditorGUILayout.LabelField("    Do przypisania — brak klipu lub tymczasowy Idle", EditorStyles.miniLabel); }
                        ValidateClip(clip, i == 0 || i == 1 || i == 2 || i == 6);
                    }
                    EditorGUILayout.HelpBox("Stany bez wlasnego klipu: " + missing + ". Zmiany zapisuja Motion stanow, zachowujac przejscia i parametry grafu. Tego samego klipu mozna celowo uzyc w kilku stanach.", MessageType.Info);
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animacje ciosow", EditorStyles.boldLabel);
                weapon = (ShinobiWeaponDefinition)EditorGUILayout.ObjectField("Bron", weapon, typeof(ShinobiWeaponDefinition), false);
                var combat = character != null ? character.GetComponent<ShinobiCombat>() : null;
                if (weapon != null) DrawAttacks(weapon.combo);
                else if (combat != null && character.GetComponent<ShinobiWeaponInventory>() != null)
                {
                    foreach (var entry in character.GetComponent<ShinobiWeaponInventory>().Weapons)
                        if (entry != null && GUILayout.Button("Edytuj ciosy: " + entry.displayName)) weapon = entry;
                }
                else if (combat != null && combat.profile != null) DrawAttacks(combat.profile.combo);
                else EditorGUILayout.HelpBox("Wybierz bron lub postac, aby przypisac klipy ciosow.", MessageType.Info);
                if (combat != null && combat.profile != null && combat.profile.jutsu != null)
                {
                    EditorGUILayout.LabelField("Animacje jutsu", EditorStyles.boldLabel);
                    foreach (var definition in combat.profile.jutsu)
                        if (definition != null) DrawClip(definition, definition.name);
                }
                EditorGUILayout.HelpBox("Attack A/B oraz Jutsu w grafie zachowuja klipy zastepcze. Sterownik podmienia je osobno dla kazdej postaci na klipy z definicji. Edytuj tutaj definicje, aby zachowac zmiane broni po Y.", MessageType.Info);
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorGUILayout.HelpBox("Zatrzymaj Play Mode przed zmiana przypisan. Aktywne stany obserwuj w oknie Animator.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        private static void DrawAttacks(ShinobiAttackDefinition[] definitions)
        {
            if (definitions == null) return;
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i] != null) DrawClip(definitions[i], "Cios " + (i + 1) + ": " + definitions[i].name);
        }
        private static void DrawClip(ScriptableObject definition, string label)
        {
            var data = new SerializedObject(definition); data.Update();
            var property = data.FindProperty("animation");
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            if (data.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(definition);
            var clip = property.objectReferenceValue as AnimationClip;
            if (clip == null) EditorGUILayout.LabelField("    Do przypisania — aktualnie zapasowy Idle", EditorStyles.miniLabel);
            ValidateClip(clip, false);
        }
        private static void ValidateClip(AnimationClip clip, bool loopExpected)
        {
            if (clip == null) return;
            if (!clip.humanMotion) EditorGUILayout.HelpBox(clip.name + ": klip nie jest Humanoid. Sprawdz Rig pliku FBX przed uzyciem na YBocie.", MessageType.Warning);
            if (loopExpected && !clip.isLooping) EditorGUILayout.HelpBox(clip.name + ": ten stan zwykle wymaga Loop Time. Ustaw petle w imporcie klipu, jesli ruch ma trwac ciagle.", MessageType.Info);
        }
    }
}

