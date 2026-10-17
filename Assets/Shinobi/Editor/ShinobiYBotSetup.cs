using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Shinobi.Editor
{
    public static class ShinobiYBotSetup
    {
        public const string ModelPath = "Assets/Shinobi/Animation/YBot/Y Bot@Fight Idle.fbx";
        public const string ControllerPath = "Assets/Shinobi/Animation/YBot/YBotTraining.controller";
        public const string ScenePath = "Assets/Shinobi/Scenes/TrainingArena.unity";
        public const string PrefabPath = "Assets/Shinobi/Prefabs/Player.prefab";

        [MenuItem("Shinobi/Model testowy/Uzyj YBot w TrainingArena")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Zatrzymaj Play Mode przed podmiana modelu.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Brak modelu YBot: " + ModelPath);
            var clips = importer.clipAnimations;
            if (clips.Length == 0) clips = importer.defaultClipAnimations;
            var idleSettings = clips.Single(c => c.name == "Fight Idle");
            idleSettings.loopTime = true;
            idleSettings.loopPose = true;
            idleSettings.lockRootRotation = true;
            idleSettings.lockRootHeightY = true;
            idleSettings.lockRootPositionXZ = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Single(c => c.name == "Fight Idle");
            var avatar = model.GetComponent<Animator>()?.avatar;
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("YBot wymaga poprawnego avatara Humanoid.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                var state = controller.layers[0].stateMachine.AddState("Fight Idle");
                state.motion = clip;
                controller.layers[0].stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
            }

            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShinobiMovement>(true))
                    .Single().gameObject;
                ConfigurePlayer(player, model, controller);
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    ConfigurePlayer(prefab, model, controller);
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Nie zapisano TrainingArena.");
                AssetDatabase.SaveAssets();
                Debug.Log("YBOT_SETUP_PASS: TrainingArena i Player.prefab uzywaja YBot z Fight Idle.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigurePlayer(GameObject player, GameObject model, RuntimeAnimatorController controller)
        {
            var movement = new SerializedObject(player.GetComponent<ShinobiMovement>());
            var visual = movement.FindProperty("visual").objectReferenceValue as Transform;
            if (visual == null || visual.parent != player.transform)
                throw new InvalidOperationException("Player musi miec bezposrednie dziecko Visual przypisane do ruchu.");
            // Keep the existing visual reference and combat/controller components intact.
            var placeholder = visual.GetComponent<MeshRenderer>();
            if (placeholder != null) placeholder.enabled = false;
            var marker = visual.Find("FacingMarker");
            if (marker != null) marker.gameObject.SetActive(false);
            // CharacterController rests above the floor by its skin width.
            visual.localPosition = Vector3.down * player.GetComponent<CharacterController>().skinWidth;
            visual.localRotation = Quaternion.Euler(0, 90, 0);
            visual.localScale = Vector3.one;

            var modelTransform = visual.Find("YBot");
            if (modelTransform == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
                instance.name = "YBot";
                modelTransform = instance.transform;
                modelTransform.localPosition = Vector3.zero;
                modelTransform.localRotation = Quaternion.identity;
                modelTransform.localScale = Vector3.one;
                var bounds = MeshBounds(modelTransform);
                float scale = player.GetComponent<CharacterController>().height / bounds.size.y;
                modelTransform.localScale = Vector3.one * scale;
                modelTransform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            }
            var animator = modelTransform.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(modelTransform.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(modelTransform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            Debug.Log($"YBot {player.name}: scale={modelTransform.localScale.x:F4}, position={modelTransform.localPosition}");
        }

        public static Bounds MeshBounds(Transform root)
        {
            bool found = false;
            var bounds = new Bounds();
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh);
                    foreach (var vertex in mesh.vertices)
                    {
                        Vector3 local = root.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                        if (!found) { bounds = new Bounds(local, Vector3.zero); found = true; }
                        else bounds.Encapsulate(local);
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            if (!found || bounds.size.y < 0.01f) throw new InvalidOperationException("YBot nie ma poprawnej siatki.");
            return bounds;
        }
    }
}
