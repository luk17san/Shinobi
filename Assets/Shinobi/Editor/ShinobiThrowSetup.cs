using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Shinobi.Editor
{
    public static class ShinobiThrowSetup
    {
        public const string ClipPath="Assets/Shinobi/Animation/YBot/Fight/Y Bot@Throw Object.fbx";
        public const string EffectPath="Assets/Shinobi/Art/Weapons/Prefabs/SealExplosion.prefab";
        [MenuItem("Shinobi/Modele/Przygotuj rzut bronia H")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var importer=AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            if(importer==null) throw new InvalidOperationException("Brak animacji Throw Object.");
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects=false;
            var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
            foreach(var c in clips) {c.loopTime=false;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var animation=AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
            if(!animation.humanMotion) throw new InvalidOperationException("Animacja rzutu nie jest Humanoid.");
            var effect=Effect();
            string folder="Assets/Shinobi/Configuration/Weapons/";
            for(int i=0;i<3;i++)
            {
                string name=ShinobiWeaponModelsSetup.Names[i];
                var weapon=AssetDatabase.LoadAssetAtPath<ShinobiWeaponDefinition>(folder+name+".asset");
                if(weapon==null) throw new InvalidOperationException("Brak broni "+name);
                // First setup supplies defaults. Repeating the menu preserves custom throw parameters.
                if(!weapon.throwable)
                {
                    weapon.throwable=true;
                    weapon.throwing=new ShinobiThrowSettings {damage=i==0?20:25,spin=i==0,
                        cooldown=i==2?1.1f:.7f,explosive=i==2,animation=animation,explosionEffect=i==2?effect:null};
                }
                else
                {
                    if(weapon.throwing==null) weapon.throwing=new ShinobiThrowSettings();
                    if(weapon.throwing.animation==null) weapon.throwing.animation=animation;
                    if(weapon.throwing.explosive && weapon.throwing.explosionEffect==null) weapon.throwing.explosionEffect=effect;
                }
                EditorUtility.SetDirty(weapon);
            }
            var prefab=PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
            try
            {
                if(prefab.GetComponent<ShinobiThrowController>()==null) prefab.AddComponent<ShinobiThrowController>();
                var profile=prefab.GetComponent<ShinobiCombat>().profile;
                if(profile==null)throw new InvalidOperationException("Brak profilu gracza.");
                EditorUtility.SetDirty(profile);
                PrefabUtility.SaveAsPrefabAsset(prefab,ShinobiCombatIntegration.PrefabPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(prefab);}
            var scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Single);
            var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiMovement>(true))
                .Single(p=>p.GetComponent<ShinobiTrainingOpponent>()==null);
            if(player.GetComponent<ShinobiThrowController>()==null)
            {
                player.gameObject.AddComponent<ShinobiThrowController>();
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Nie zapisano sceny.");
            }
            AssetDatabase.SaveAssets();Debug.Log("THROW_SETUP_PASS");
        }
        static GameObject Effect()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath);
            const string texturePath="Assets/Shinobi/Art/Weapons/Textures/SealSpark.png";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(texture==null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(texturePath));
                var image=new Texture2D(64,64,TextureFormat.RGBA32,false);
                var pixels=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float dx=(x-31.5f)/31.5f,dy=(y-31.5f)/31.5f;
                    float alpha=Mathf.Clamp01(1-dx*dx-dy*dy);alpha*=alpha;
                    pixels[y*64+x]=new Color(1,1,1,alpha);
                }
                image.SetPixels(pixels);image.Apply();File.WriteAllBytes(texturePath,image.EncodeToPNG());Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
                texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            string materialPath="Assets/Shinobi/Art/Weapons/Materials/SealExplosion.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetColor("_BaseColor",new Color(1,.45f,.04f,1));
                material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);
                material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
                AssetDatabase.CreateAsset(material,materialPath);
            }
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
            if(existing!=null)return existing;
            var root=new GameObject("Seal explosion");
            try
            {
                var particles=root.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.duration=.5f;main.loop=false;main.startLifetime=.4f;
                main.startSpeed=2;main.startSize=.12f;main.startColor=new Color(1,.6f,.08f,1);
                main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,24)});
                var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
                var color=particles.colorOverLifetime;color.enabled=true;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.yellow,0),new GradientColorKey(new Color(1,.2f,.01f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
                root.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
                return PrefabUtility.SaveAsPrefabAsset(root,EffectPath);
            }
            finally {Object.DestroyImmediate(root);}
        }
    }
}
