using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shinobi.Editor
{
    [InitializeOnLoad]
    public static class ShinobiTrainingBuilder
    {
        private const string Root = "Assets/Shinobi";
        private const string ScenePath = Root + "/Scenes/TrainingArena.unity";

        static ShinobiTrainingBuilder() { EditorApplication.delayCall += GenerateIfMissing; }

        private static void GenerateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(ScenePath)) Build();
        }

        [MenuItem("Shinobi/Etap 1/Otworz arene treningowa")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("Brak shadera URP/Lit.");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        private static void Build()
        {
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var ground = Material("Ground", new Color(0.35f, 0.43f, 0.31f));
                var wood = Material("Wood", new Color(0.31f, 0.21f, 0.14f));
                var body = Material("Player", new Color(0.2f, 0.45f, 0.8f));
                Box("Ground", new Vector3(0, -0.5f, 0), new Vector3(28, 1, 6), ground);
                Box("LeftBoundary", new Vector3(-13, 1.5f, 0), new Vector3(1, 3, 6), wood);
                Box("RightBoundary", new Vector3(13, 1.5f, 0), new Vector3(1, 3, 6), wood);
                var player = new GameObject("Player");
                player.transform.position = new Vector3(0, 0.1f, 0);
                var controller = player.AddComponent<CharacterController>();
                controller.height = 2f;
                controller.radius = 0.4f;
                controller.center = Vector3.up;
                controller.stepOffset = 0.25f;
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(player.transform, false);
                visual.transform.localPosition = Vector3.up;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = body;
                var marker = Box("FacingMarker", Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f), wood);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.transform.SetParent(visual.transform, false);
                marker.transform.localPosition = new Vector3(0, 0.25f, 0.5f);
                var movement = player.AddComponent<ShinobiMovement>();
                var settings = new SerializedObject(movement);
                settings.FindProperty("visual").objectReferenceValue = visual.transform;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, Root + "/Prefabs/Player.prefab");
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 3, -25);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.16f, 0.23f);
                var lightObject = new GameObject("Directional Light", typeof(Light));
                lightObject.transform.rotation = Quaternion.Euler(45, -30, 0);
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new System.IO.IOException("Nie zapisano sceny treningowej.");
                AssetDatabase.SaveAssets();
                Debug.Log("Shinobi: utworzono TrainingArena. Otworz przez menu Shinobi/Etap 1.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
