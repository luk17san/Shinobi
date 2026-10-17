using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Shinobi.Editor
{
    public static class ShinobiHudTrainingSetup
    {
        const string Art="Assets/Shinobi/Art/Hud/";
        [MenuItem("Shinobi/Etap 12/Przygotuj kompaktowy HUD")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Art); AssetDatabase.Refresh();
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath); bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try
            {
                var session=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiGameSession>(true)).Single();
                var hud=session.GetComponent<ShinobiCombatHud>(); if(hud==null) hud=session.gameObject.AddComponent<ShinobiCombatHud>();
                hud.legacyHud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiTrainingHud>(true)).Single(); hud.legacyHud.enabled=false;
                if(hud.playerPortrait==null) hud.playerPortrait=RenderIcon(session.player.gameObject,"PlayerPortrait",true,false);
                if(hud.dummyPortrait==null) hud.dummyPortrait=RenderIcon(session.trainingDummy,"DummyPortrait",false,false);
                var inventory=session.player.GetComponent<ShinobiWeaponInventory>();
                for(int i=0;i<inventory.Weapons.Count;i++)
                {
                    var weapon=inventory.Weapons[i]; if(weapon==null) continue;
                    if(weapon.hudIcon==null && weapon.visualPrefab!=null) weapon.hudIcon=RenderIcon(weapon.visualPrefab,"Weapon"+i,false,true);
                    if(weapon.displayName=="Bron treningowa "+(i+1)) weapon.displayName=i==0?"Miecz treningowy":"Kij treningowy";
                    EditorUtility.SetDirty(weapon);
                }
                EditorUtility.SetDirty(hud); EditorUtility.SetDirty(hud.legacyHud);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("HUD_SETUP_PASS");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        static Texture2D RenderIcon(GameObject source,string name,bool portrait,bool weapon)
        {
            var path=Art+name+".png";
            if(!File.Exists(path))
            {
                var clone=Object.Instantiate(source); clone.name="HUD icon render"; clone.transform.position=new Vector3(1000,1000,1000);
                foreach(var transform in clone.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer=31;
                if(portrait)
                {
                    var visual=clone.transform.Find("Visual"); if(visual!=null) visual.localRotation=Quaternion.Euler(0,180,0);
                    var animator=clone.GetComponentInChildren<Animator>(); if(animator!=null) { animator.Rebind(); animator.Update(0); }
                }
                if(weapon) clone.transform.rotation=Quaternion.Euler(0,0,-30);
                var cameraObject=new GameObject("HUD icon camera"); var camera=cameraObject.AddComponent<Camera>();
                float height=portrait?1.6f:weapon?0.35f:1.2f;
                camera.transform.position=clone.transform.position+new Vector3(0,height,-4); camera.orthographic=true; camera.orthographicSize=portrait?0.65f:weapon?1f:1.2f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear; camera.cullingMask=1<<31; camera.farClipPlane=10;
                var texture=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);
                var previous=RenderTexture.active;
                try
                {
                    camera.targetTexture=texture; camera.Render(); RenderTexture.active=texture;
                    pixels.ReadPixels(new Rect(0,0,256,256),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG());
                }
                finally { RenderTexture.active=previous; camera.targetTexture=null; Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(clone); }
            }
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
