using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shinobi.Editor
{
    public static class ShinobiCombatTrainingBuilder
    {
        private const string Root = "Assets/Shinobi";
        private const string ScenePath = Root + "/Scenes/TrainingArena.unity";

        [MenuItem("Shinobi/Etap 2/Przygotuj trening walki")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Zatrzymaj Play Mode przed przygotowaniem treningu.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath))
                throw new System.InvalidOperationException("Najpierw utworz arene przez menu Shinobi/Etap 1.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            var players = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ShinobiMovement>(true)).ToArray();
            if (players.Length != 1)
                throw new System.InvalidOperationException("Arena musi zawierac dokladnie jeden ShinobiMovement.");
            var player = players[0].gameObject;
            PreparePlayer(player, true);

            string prefabPath = Root + "/Prefabs/Player.prefab";
            if (File.Exists(prefabPath))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    PreparePlayer(prefabRoot, false);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
            }

            if (!scene.GetRootGameObjects().Any(x => x.name == "TrainingDummy"))
            {
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummy.name = "TrainingDummy";
                Undo.RegisterCreatedObjectUndo(dummy, "Create training dummy");
                // Collider capsule is centered on the local origin; root sits at floor level.
                dummy.GetComponent<CapsuleCollider>().center = Vector3.up;
                dummy.transform.position = new Vector3(4f, 0f, 0f);
                var renderer = dummy.GetComponent<Renderer>();
                Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(dummy.GetComponent<MeshFilter>());
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Visual";
                body.transform.SetParent(dummy.transform, false);
                body.transform.localPosition = Vector3.up;
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.GetComponent<Renderer>().sharedMaterial = GetMaterial("Dummy", new Color(0.75f, 0.25f, 0.18f));
                var health = dummy.AddComponent<ShinobiHealth>();
                var serialized = new SerializedObject(health);
                serialized.FindProperty("visual").objectReferenceValue = body.GetComponent<Renderer>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!File.Exists(Root + "/Prefabs/TrainingDummy.prefab"))
                    PrefabUtility.SaveAsPrefabAsset(dummy, Root + "/Prefabs/TrainingDummy.prefab");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Nie zapisano areny walki.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Shinobi: trening walki gotowy. Domyslnie G lub zachodni przycisk pada: cios. Profil i jutsu: menu Shinobi/Etap 4.");
        }

        private static void PreparePlayer(GameObject player, bool recordUndo)
        {
            var combat = player.GetComponent<ShinobiCombat>();
            if (combat == null) combat = recordUndo ? Undo.AddComponent<ShinobiCombat>(player) : player.AddComponent<ShinobiCombat>();
            var serialized = new SerializedObject(combat);
            var previewProperty = serialized.FindProperty("hitPreview");
            if (previewProperty.objectReferenceValue != null) return;
            var preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "AttackPreview";
            if (recordUndo) Undo.RegisterCreatedObjectUndo(preview, "Create attack preview");
            preview.transform.SetParent(player.transform, false);
            Object.DestroyImmediate(preview.GetComponent<Collider>());
            preview.GetComponent<Renderer>().sharedMaterial = GetMaterial("AttackPreview", new Color(1f, 0.78f, 0.1f));
            preview.SetActive(false);
            if (recordUndo) Undo.RecordObject(combat, "Assign attack preview");
            previewProperty.objectReferenceValue = preview.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetMaterial(string name, Color color)
        {
            Directory.CreateDirectory(Root + "/Materials");
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("Brak URP/Lit.");
            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
