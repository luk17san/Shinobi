using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shinobi.Editor
{
    public static class ShinobiAnimatorControllerSetup
    {
        public const string ControllerPath = "Assets/Shinobi/Animation/YBot/YBotGameplay.controller";
        private static readonly string[] Names = { "Idle", "Walk", "Run", "Jump", "Fall", "Crouch", "Crouch Walk", "Dash", "Hit", "Block", "Guard Break", "Focus", "Attack A", "Attack B", "Jutsu" };
        private static AnimationClip Slot(string name, ShinobiAnimationSet set)
        {
            string path = "Assets/Shinobi/Animation/YBot/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;
            clip = Object.Instantiate(set.idle); clip.name = name;
            AssetDatabase.CreateAsset(clip, path); return clip;
        }
        public static void Configure(ShinobiAnimationDriver driver)
        {
            var set = driver.animations;
            if (set == null || set.idle == null) throw new System.InvalidOperationException("Przypisz zestaw animacji z klipem Idle.");
            driver.attackSlotA = Slot("AttackSlotA", set); driver.attackSlotB = Slot("AttackSlotB", set); driver.jutsuSlot = Slot("JutsuSlot", set);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                controller.AddParameter("State", AnimatorControllerParameterType.Int);
                controller.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
                var clips = new[] { set.idle, set.walk, set.run, set.jump, set.fall, set.crouch, set.crouchWalk, set.dash, set.hit, set.block, set.guardBreak, set.focus, driver.attackSlotA, driver.attackSlotB, driver.jutsuSlot };
                var machine = controller.layers[0].stateMachine;
                machine.anyStatePosition = new Vector3(-300, 160); machine.entryPosition = new Vector3(-300, 0);
                for (int i = 0; i < Names.Length; i++)
                {
                    var state = machine.AddState(Names[i], new Vector3((i % 5) * 240, (i / 5) * 160));
                    state.motion = clips[i] != null ? clips[i] : set.idle;
                    state.writeDefaultValues = true;
                    if (i == 12 || i == 13) { state.speedParameterActive = true; state.speedParameter = "AttackSpeed"; }
                    if (i == 0) machine.defaultState = state;
                    var transition = machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false; transition.hasFixedDuration = true;
                    transition.duration = Mathf.Clamp(set.crossFade, 0.01f, 0.3f);
                    transition.canTransitionToSelf = false;
                    transition.interruptionSource = TransitionInterruptionSource.None;
                    transition.orderedInterruption = true;
                    transition.AddCondition(AnimatorConditionMode.Equals, i, "State");
                }
                EditorUtility.SetDirty(controller);
            }
            driver.animator.runtimeAnimatorController = controller;
            driver.animator.applyRootMotion = false;
            EditorUtility.SetDirty(driver); EditorUtility.SetDirty(driver.animator);
        }
        [MenuItem("Shinobi/Etap 9/Przygotuj Animator Controller")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var driver in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShinobiAnimationDriver>(true))) Configure(driver);
                var prefab = PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
                try { Configure(prefab.GetComponent<ShinobiAnimationDriver>()); PrefabUtility.SaveAsPrefabAsset(prefab, ShinobiCombatIntegration.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("ANIMATOR_CONTROLLER_SETUP_PASS");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}

