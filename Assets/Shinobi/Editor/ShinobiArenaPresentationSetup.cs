using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Shinobi.Editor
{
    public static class ShinobiArenaPresentationSetup
    {
        private const string Art="Assets/Shinobi/Art/TrainingArena/";
        [MenuItem("Shinobi/Etap 10/Przygotuj ilustrowana arene")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            foreach(var name in new[]{"Mountains","Village","Courtyard","Foreground"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");
                if(importer==null) throw new System.IO.FileNotFoundException("Brak warstwy "+name);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=1672f/28f; importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048; importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath); bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="ArenaPresentation");
                if(root==null)
                {
                    root=new GameObject("ArenaPresentation"); SceneManager.MoveGameObjectToScene(root,scene);
                    Layer(root,"Mountains",8); Layer(root,"Village",6); Layer(root,"Courtyard",3); Layer(root,"Foreground",-4);
                    var obstacles=new GameObject("TrainingObstacles"); obstacles.transform.SetParent(root.transform,false);
                    var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Shinobi/Configuration/TrainingWeaponWood.mat");
                    Box(obstacles,"JumpBlock",new Vector3(10,0.45f,0),new Vector3(1.2f,0.9f,1.5f),wood);
                    var beam=Box(obstacles,"CrouchBeam",new Vector3(-10,1.6f,0),new Vector3(2.4f,0.5f,1.5f),wood);
                    Box(obstacles,"CrouchSupportLeft",new Vector3(-11,0.8f,1.1f),new Vector3(0.18f,1.6f,0.18f),wood);
                    Box(obstacles,"CrouchSupportRight",new Vector3(-9,0.8f,1.1f),new Vector3(0.18f,1.6f,0.18f),wood);
                }
                var dummy=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="TrainingDummy");
                if(dummy!=null)
                {
                    var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Shinobi/Configuration/TrainingWeaponWood.mat");
                    var body=dummy.GetComponentInChildren<Renderer>(); if(body!=null && wood!=null) body.sharedMaterial=wood;
                    if(dummy.transform.Find("WoodenTarget")==null)
                    {
                        var target=new GameObject("WoodenTarget"); target.transform.SetParent(dummy.transform,false);
                        var crossbar=Box(target,"Crossbar",new Vector3(0,1.2f,0),new Vector3(1.4f,0.18f,0.2f),wood);
                        Object.DestroyImmediate(crossbar.GetComponent<Collider>());
                        var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder); disk.name="TargetRing";
                        disk.transform.SetParent(target.transform,false); disk.transform.localPosition=new Vector3(0,1.25f,-0.51f);
                        disk.transform.localRotation=Quaternion.Euler(90,0,0); disk.transform.localScale=new Vector3(0.5f,0.01f,0.5f);
                        Object.DestroyImmediate(disk.GetComponent<Collider>());
                        string ringPath=Art+"TargetRing.mat"; var ring=AssetDatabase.LoadAssetAtPath<Material>(ringPath);
                        if(ring==null) { ring=new Material(Shader.Find("Universal Render Pipeline/Unlit")); ring.SetColor("_BaseColor",new Color(0.9f,0.78f,0.5f)); AssetDatabase.CreateAsset(ring,ringPath); }
                        disk.GetComponent<Renderer>().sharedMaterial=ring;
                    }
                }
                foreach(var name in new[]{"Ground","LeftBoundary","RightBoundary"})
                {
                    var item=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==name);
                    if(item!=null && item.TryGetComponent<Renderer>(out var renderer)) renderer.enabled=false;
                }
                var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(c=>c.CompareTag("MainCamera"));
                camera.transform.position=new Vector3(0,3.84f,-25); camera.transform.rotation=Quaternion.identity;
                camera.orthographic=true; camera.orthographicSize=28f*941f/1672f/2f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.21f,0.55f,0.88f);
                EditorUtility.SetDirty(camera); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("ARENA_PRESENTATION_SETUP_PASS");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        private static void Layer(GameObject root,string name,float z)
        {
            var go=new GameObject(name); go.transform.SetParent(root.transform,false); go.transform.localPosition=new Vector3(0,3.84f,z);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
            var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
            string path=Art+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.name=name;
                material.SetTexture("_BaseMap",sprite.texture); material.SetFloat("_Surface",1);
                material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite",0); material.SetFloat("_Cull",(float)CullMode.Off);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=(int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(material,path);
            }
            renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        private static GameObject Box(GameObject root,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(root.transform,false); go.transform.localPosition=position; go.transform.localScale=scale;
            if(material!=null) go.GetComponent<Renderer>().sharedMaterial=material;
            return go;
        }
    }
}

