using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Shinobi.Editor
{
    public static class ShinobiWeaponModelsSetup
    {
        public const string Root = "Assets/Shinobi/Art/Weapons";
        public static readonly string[] Names = { "Shuriken", "Kunai", "Kunai_Seal" };
        static readonly string[] Labels = { "Shuriken", "Kunai", "Kunai z wybuchową notką" };
        const string Definitions = "Assets/Shinobi/Configuration/Weapons/";

        [MenuItem("Shinobi/Modele/Dodaj shuriken i kunai")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zatrzymaj Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Icons");
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Brak shadera URP/Lit.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Seal_BaseColor.png");
            if (texture == null) throw new InvalidOperationException("Brak tekstury notki.");
            var source = AssetDatabase.LoadAssetAtPath<ShinobiWeaponDefinition>(Definitions + "TrainingWeapon1.asset");
            if (source == null || !source.IsUsable) throw new InvalidOperationException("Brak poprawnej broni treningowej.");
            var definitions = new ShinobiWeaponDefinition[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                string path = Root + "/Models/" + Names[i] + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Brak modelu: " + path);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var wrapper = new GameObject(Names[i]);
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, wrapper.transform);
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                    if (renderers.Length == 0) throw new InvalidOperationException("Pusty FBX: " + path);
                    // Import materials independently of FBX to keep their colors editable.
                    // Assign by mesh role; the faceted blades use dark faces and silver bevels.
                    foreach (var renderer in renderers)
                    {
                        string role = renderer.name;
                        Material main;
                        if (role.StartsWith("Paper seal")) main = Material("Paper", new Color(.87f,.79f,.61f),0,.15f,texture,shader);
                        else if (role.StartsWith("Seal cord") || role.StartsWith("Cord knot")) main = Material("Cord",new Color(.36f,.075f,.035f),0,.1f,null,shader);
                        else if (role.StartsWith("Cloth wrap")) main = Material("ClothSeam",new Color(.42f,.36f,.25f),0,.1f,null,shader);
                        else if (role.StartsWith("Grip core")) main = Material("Cloth",new Color(.72f,.64f,.46f),0,.1f,null,shader);
                        else main = Material("Steel",new Color(.11f,.14f,.18f),.7f,.55f,null,shader);
                        int count = renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
                        var materials = Enumerable.Repeat(main,count).ToArray();
                        if (count > 1 && (role.StartsWith("Blade") || role.StartsWith("Four point")))
                            materials[1] = Material("Bevel",new Color(.42f,.47f,.54f),.8f,.65f,null,shader);
                        renderer.sharedMaterials = materials;
                    }
                    var blade = renderers.First(r => i == 0 || r.name == "Blade" || r.name.StartsWith("Blade."));
                    Bounds b = blade.bounds;
                    if (b.size.y < .1f || b.size.y > .4f) throw new InvalidOperationException("Niepoprawna skala/osi FBX " + Names[i] + ": " + b);
                    Vector3 grip = i == 0 ? b.center : new Vector3(b.center.x,b.min.y + .222f,b.center.z);
                    instance.transform.position -= grip;
                    // Unpack the nested model instance so material remaps persist in a plain game prefab.
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper,Root + "/Prefabs/" + Names[i] + ".prefab");
                    string definitionPath = Definitions + Names[i] + ".asset";
                    var definition = AssetDatabase.LoadAssetAtPath<ShinobiWeaponDefinition>(definitionPath);
                    if (definition == null)
                    {
                        definition = ScriptableObject.CreateInstance<ShinobiWeaponDefinition>();
                        AssetDatabase.CreateAsset(definition,definitionPath);
                        definition.displayName = Labels[i];
                        definition.comboWindow = source.comboWindow;
                        definition.combo = new ShinobiAttackDefinition[source.combo.Length];
                        for (int c = 0; c < source.combo.Length; c++)
                        {
                            var attack = Object.Instantiate(source.combo[c]);
                            attack.name = "Cios " + (c+1);
                            AssetDatabase.AddObjectToAsset(attack,definition);
                            definition.combo[c] = attack;
                        }
                        definition.localEulerAngles = new Vector3(0,0,90);
                        definition.localScale = Vector3.one;
                    }
                    definition.visualPrefab = prefab;
                    definitions[i] = definition;
                    EditorUtility.SetDirty(definition);
                }
                finally { Object.DestroyImmediate(wrapper); }
                CreateIcon(definitions[i],Names[i]);
            }
            var prefabRoot = PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
            try { Configure(prefabRoot,definitions); PrefabUtility.SaveAsPrefabAsset(prefabRoot,ShinobiCombatIntegration.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
            var scene = SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiWeaponInventory>(true))
                    .Single(g=>g.GetComponent<ShinobiTrainingOpponent>() == null);
                Configure(player.gameObject,definitions);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Nie zapisano sceny.");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
            AssetDatabase.SaveAssets();
            Debug.Log("WEAPON_MODELS_SETUP_PASS: trzy modele, materialy, ikony, scena i prefab.");
        }

        static Material Material(string name,Color color,float metallic,float gloss,Texture2D texture,Shader shader)
        {
            string path=Root+"/Materials/"+name+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result!=null && result.shader==shader) return result;
            bool created=result==null;
            if(created) result=new Material(shader) { name=name }; else result.shader=shader;
            result.SetColor("_BaseColor",color);result.SetTexture("_BaseMap",texture);
            result.SetFloat("_Metallic",metallic);result.SetFloat("_Smoothness",gloss);
            if(created) AssetDatabase.CreateAsset(result,path); else EditorUtility.SetDirty(result);
            return result;
        }

        static void Configure(GameObject player,ShinobiWeaponDefinition[] added)
        {
            var inventory=player.GetComponent<ShinobiWeaponInventory>();
            if(inventory==null) throw new InvalidOperationException("Brak ekwipunku: "+player.name);
            var data=new SerializedObject(inventory);
            var socket=data.FindProperty("weaponSocket").objectReferenceValue as Transform;
            if(socket==null) throw new InvalidOperationException("Brak Weapon Socket: "+player.name);
            var animator=player.GetComponentInChildren<Animator>();
            var hand=animator!=null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if(hand==null || !socket.IsChildOf(hand)) throw new InvalidOperationException("Socket musi byc w prawej dloni: "+player.name);
            var list=data.FindProperty("weapons");
            foreach(var weapon in added)
            {
                // Defaults are in hand space; leave later custom nonzero offsets editable.
                if(weapon.localPosition==Vector3.zero)
                {
                    bool star=weapon.name=="Shuriken";
                    weapon.localPosition=socket.InverseTransformPoint(hand.TransformPoint(star ? new Vector3(-.035f,.105f,.025f) : new Vector3(-.035f,.065f,.005f)));
                    weapon.localEulerAngles=star ? new Vector3(0,90,0) : new Vector3(0,70,200);
                    EditorUtility.SetDirty(weapon);
                }
                bool exists=false;
                for(int j=0;j<list.arraySize;j++) if(list.GetArrayElementAtIndex(j).objectReferenceValue==weapon) exists=true;
                if(!exists) { int j=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(j).objectReferenceValue=weapon; }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(inventory);
        }

        static void CreateIcon(ShinobiWeaponDefinition definition,string name)
        {
            var preview=new PreviewRenderUtility();
            var instance=Object.Instantiate(definition.visualPrefab);
            try
            {
                preview.AddSingleGO(instance);
                var all=instance.GetComponentsInChildren<Renderer>();var bounds=all[0].bounds;
                foreach(var r in all.Skip(1)) bounds.Encapsulate(r.bounds);
                preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.65f;
                preview.camera.transform.position=bounds.center+new Vector3(.15f,.08f,-1);
                preview.camera.transform.LookAt(bounds.center);preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=10;
                preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.08f,.10f,.14f,1);
                preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);
                preview.lights[1].intensity=.8f;
                preview.ambientColor=new Color(.4f,.4f,.4f,1);
                preview.BeginStaticPreview(new Rect(0,0,256,256));preview.Render(true);
                var image=preview.EndStaticPreview();
                string path=Root+"/Icons/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                definition.hudIcon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(definition);
            }
            finally { preview.Cleanup(); }
        }
    }
}
