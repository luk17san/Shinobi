using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shinobi.Editor
{
    public static class ShinobiModelRepair
    {
        const string Prefab = "Assets/Shinobi/Prefabs/Player.prefab";
        const string Scene = "Assets/Shinobi/Scenes/TrainingArena.unity";
        const string Controller = "Assets/Shinobi/Animation/YBot/YBotGameplay.controller";
        static void Reference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            if (owner == null) return;
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }
        static AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));

        [MenuItem("Shinobi/Naprawa/Polacz model Azure Shadow")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zatrzymaj Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Animation-only FBXs do not need an optimized render hierarchy or an Avatar instance.
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[]{"Assets/Shinobi/Animation"}))
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
                if (importer != null && importer.optimizeGameObjects) { importer.optimizeGameObjects = false; importer.SaveAndReimport(); }
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            var idle = Clip("Assets/Shinobi/Animation/YBot/Move/Y Bot@Idle.fbx");
            foreach (var child in controller.layers[0].stateMachine.states)
                if (child.state.motion == null) { child.state.motion = idle; EditorUtility.SetDirty(child.state); }
            foreach (var guid in AssetDatabase.FindAssets("t:ShinobiAnimationSet"))
            {
                var set = AssetDatabase.LoadAssetAtPath<ShinobiAnimationSet>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(set);
                foreach (var field in new[]{"idle","walk","run","jump","fall","crouch","crouchWalk","dash","hit","block","guardBreak","focus"})
                    if (serialized.FindProperty(field).objectReferenceValue == null) serialized.FindProperty(field).objectReferenceValue = idle;
                serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set);
            }
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try { Configure(root, null, controller); PrefabUtility.SaveAsPrefabAsset(root, Prefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).transform.Find("Visual/AzureShadow");
            EditorSceneManager.OpenScene(Scene);
            foreach (var driver in UnityEngine.Object.FindObjectsByType<ShinobiAnimationDriver>(FindObjectsSortMode.None))
                Configure(driver.gameObject, template, controller);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Verify();
        }
        static void Configure(GameObject actor, Transform template, RuntimeAnimatorController controller)
        {
            var visual = actor.transform.Find("Visual");
            if (visual == null) throw new InvalidOperationException(actor.name + ": brak Visual");
            var animator = visual.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a.avatar != null && a.avatar.isValid && a.avatar.isHuman);
            var brokenMesh = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r.sharedMesh == null);
            if ((animator == null || brokenMesh) && template != null)
            {
                foreach (Transform child in visual.Cast<Transform>().ToArray())
                    if (child.name == "YBot" || child.name == "AzureShadow" || child.name.Contains("Missing Prefab")) UnityEngine.Object.DestroyImmediate(child.gameObject);
                var model = UnityEngine.Object.Instantiate(template.gameObject, visual);
                model.name = "AzureShadow";
                animator = model.GetComponentsInChildren<Animator>(true).First(a => a.avatar != null && a.avatar.isValid && a.avatar.isHuman);
            }
            if (animator == null) throw new InvalidOperationException(actor.name + ": brak prawidlowego Humanoid Avatar");
            var modelRoot = animator.transform;
            while (modelRoot.parent != visual) modelRoot = modelRoot.parent;
            modelRoot.localPosition = Vector3.zero;
            foreach (var other in visual.GetComponentsInChildren<Animator>(true))
                if (other != animator) UnityEngine.Object.DestroyImmediate(other);
            animator.enabled = true; animator.applyRootMotion = false; animator.runtimeAnimatorController = controller;
            animator.Rebind();
            var driver = actor.GetComponent<ShinobiAnimationDriver>();
            Reference(driver, "animator", animator);
            Reference(actor.GetComponent<ShinobiMovement>(), "visual", visual);
            Reference(actor.GetComponent<ShinobiHealth>(), "visual", animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.sharedMesh != null));
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) throw new InvalidOperationException("Brak prawej dloni");
            var socket = hand.Find("WeaponSocket");
            if (socket == null) { socket = new GameObject("WeaponSocket").transform; socket.SetParent(hand, false); }
            Reference(actor.GetComponent<ShinobiWeaponInventory>(), "weaponSocket", socket);
            EditorUtility.SetDirty(animator);
        }
        public static void Verify()
        {
            foreach (var driver in UnityEngine.Object.FindObjectsByType<ShinobiAnimationDriver>(FindObjectsSortMode.None))
            {
                var a = driver.animator;
                if (a == null || a.avatar == null || !a.avatar.isValid || !a.avatar.isHuman || a.runtimeAnimatorController == null) throw new Exception("Animator: " + driver.name);
                if (driver.GetComponentsInChildren<Animator>(true).Length != 1) throw new Exception("Duplicate Animator: " + driver.name);
                if (a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r.sharedMesh == null)) throw new Exception("Missing mesh: " + driver.name);
                var bone = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                var idle = Clip("Assets/Shinobi/Animation/YBot/Move/Y Bot@Idle.fbx");
                var run = Clip("Assets/Shinobi/Animation/YBot/Move/Y Bot@Running.fbx");
                idle.SampleAnimation(a.gameObject, 0.1f); var before = bone.localRotation;
                run.SampleAnimation(a.gameObject, 0.2f);
                if (Quaternion.Angle(before, bone.localRotation) < 0.1f) throw new Exception("Retargeting does not move leg: " + driver.name);
                a.Rebind();
                Debug.Log("MODEL_REPAIR PASS " + driver.name + ": Humanoid, mesh, single Animator, retargeted leg motion");
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (controller.layers[0].stateMachine.states.Any(s => s.state.motion == null)) throw new Exception("Missing state motion");
            Debug.Log("MODEL_REPAIR PASS all controller state motions assigned");
        }
    }
}
