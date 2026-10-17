using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;

namespace AzureShadow.EditorTools
{
public static class AzureShadowSetup
{
    static string Root;
    [MenuItem("Tools/Azure Shadow/Build or refresh prefab")]
    public static void Build()
    {
        Root = ResolveAssetRoot();
        ConfigureTexture("AzureShadow_BaseColor.jpg", false);
        ConfigureTexture("AzureShadow_Normal.jpg", true);
        var mainPath = Root + "/Models/AzureShadow_LOD0.fbx";
        var mi = (ModelImporter)AssetImporter.GetAtPath(mainPath);
        if (mi == null) throw new Exception("Missing model: " + mainPath);
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.importAnimation = false;
        mi.globalScale = 1;
        mi.useFileScale = true;
        mi.optimizeGameObjects = false;
        mi.importNormals = ModelImporterNormals.Import;
        mi.importTangents = ModelImporterTangents.CalculateMikk;
        mi.meshCompression = ModelImporterMeshCompression.Off;
        mi.SaveAndReimport();
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(mainPath);
        var transforms = asset.GetComponentsInChildren<Transform>(true);
        var names = new HashSet<string>(transforms.Select(t => t.name));
        var humans = new List<HumanBone>();
        foreach (var hn in HumanTrait.BoneName)
        {
            var bn = hn.Replace(" ", "");
            if (names.Contains(bn)) humans.Add(new HumanBone { humanName = hn, boneName = bn, limit = new HumanLimit { useDefaultValues = true } });
        }
        mi.animationType = ModelImporterAnimationType.Human;
        mi.humanDescription = new HumanDescription {
            human = humans.ToArray(),
            skeleton = transforms.Select(t => new SkeletonBone { name=t.name, position=t.localPosition, rotation=t.localRotation, scale=t.localScale }).ToArray(),
            armStretch = .05f, legStretch = .05f, upperArmTwist = .5f, lowerArmTwist = .5f, upperLegTwist = .5f, lowerLegTwist = .5f, feetSpacing = 0
        };
        mi.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(mainPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new Exception("Humanoid Avatar validation failed.");
        for (int i=1;i<3;i++)
        {
            var imp=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/AzureShadow_LOD"+i+".fbx");
            imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;imp.sourceAvatar=avatar;
            imp.importAnimation=false;imp.optimizeGameObjects=false;imp.globalScale=1;imp.useFileScale=true;
            imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.SaveAndReimport();
        }
        var animPath=Root+"/Models/AzureShadow_RigCheck.fbx";
        var animImp=(ModelImporter)AssetImporter.GetAtPath(animPath);
        animImp.animationType=ModelImporterAnimationType.Human;animImp.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;animImp.sourceAvatar=avatar;animImp.importAnimation=true;animImp.materialImportMode=ModelImporterMaterialImportMode.None;animImp.SaveAndReimport();
        var clips=animImp.defaultClipAnimations;
        foreach (var c in clips) { c.name="Rig_Check_NOT_Walk";c.loopTime=true; }
        animImp.clipAnimations=clips;animImp.SaveAndReimport();
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Animation");AssetDatabase.Refresh();
        string shaderName=GraphicsSettings.currentRenderPipeline==null?"Standard":(GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HD")?"HDRP/Lit":"Universal Render Pipeline/Lit");
        var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Shader unavailable: "+shaderName);
        var matPath=Root+"/Materials/AzureShadow.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}else mat.shader=shader;
        var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/AzureShadow_BaseColor.jpg");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/AzureShadow_Normal.jpg");
        foreach(var n in new[]{"_MainTex","_BaseMap","_BaseColorMap"})if(mat.HasProperty(n))mat.SetTexture(n,color);
        foreach(var n in new[]{"_Color","_BaseColor"})if(mat.HasProperty(n))mat.SetColor(n,Color.white);
        foreach(var n in new[]{"_BumpMap","_NormalMap"})if(mat.HasProperty(n))mat.SetTexture(n,normal);
        foreach(var n in new[]{"_Glossiness","_Smoothness"})if(mat.HasProperty(n))mat.SetFloat(n,.35f);
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",0);
        if(mat.HasProperty("_BumpScale"))mat.SetFloat("_BumpScale",.5f);
        mat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(mat);
        // Remap material on the source models too, so direct FBX instances are textured.
        for(int i=0;i<3;i++)
        {
            var path=Root+"/Models/AzureShadow_LOD"+i+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"AzureShadow_Material"),mat);
            importer.SaveAndReimport();
        }
        var root=new GameObject("AzureShadow");
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(mainPath));model.transform.SetParent(root.transform,false);
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var bones=model.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            var renderers=new List<SkinnedMeshRenderer>();
            var first=model.GetComponentInChildren<SkinnedMeshRenderer>();first.sharedMaterial=mat;first.updateWhenOffscreen=false;renderers.Add(first);
            for(int i=1;i<3;i++)
            {
                var temp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/AzureShadow_LOD"+i+".fbx"));temp.transform.SetParent(root.transform,false);
                PrefabUtility.UnpackPrefabInstance(temp,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var smr=temp.GetComponentInChildren<SkinnedMeshRenderer>();
                smr.bones=smr.bones.Select(b=>bones[b.name]).ToArray();smr.rootBone=first.rootBone;smr.sharedMaterial=mat;
                smr.transform.SetParent(first.transform.parent,true);smr.localBounds=first.localBounds;renderers.Add(smr);UnityEngine.Object.DestroyImmediate(temp);
            }
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.55f,new Renderer[]{renderers[0]}),new LOD(.25f,new Renderer[]{renderers[1]}),new LOD(.06f,new Renderer[]{renderers[2]})});lod.RecalculateBounds();
            var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;animator.runtimeAnimatorController=null;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/AzureShadow.prefab");
            var clip=AssetDatabase.LoadAllAssetsAtPath(animPath).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
            if(clip==null)throw new Exception("Rig check animation did not import.");
            var controllerPath=Root+"/Animation/RigCheck.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;
            foreach(var state in sm.states)sm.RemoveState(state.state);
            var check=sm.AddState("RigCheck");check.motion=clip;sm.defaultState=check;
            animator.runtimeAnimatorController=controller;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/AzureShadow_RigCheck.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("Azure Shadow: humanoid avatar, 3 LODs, materials and prefabs are ready.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static string ResolveAssetRoot([CallerFilePath] string sourceFile = "")
    {
        var editorFolder = Path.GetDirectoryName(Path.GetFullPath(sourceFile));
        var assetFolder = Path.GetFullPath(Path.Combine(editorFolder, "..")).Replace('\\', '/').TrimEnd('/');
        var assetsFolder = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
        if (!assetFolder.StartsWith(assetsFolder + "/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Keep AzureShadowSetup.cs inside the asset's Editor folder under Assets.");
        var assetRoot = "Assets" + assetFolder.Substring(assetsFolder.Length);
        if (!AssetDatabase.IsValidFolder(assetRoot + "/Models") || !AssetDatabase.IsValidFolder(assetRoot + "/Textures"))
            throw new InvalidOperationException("Missing Models or Textures next to Editor in " + assetRoot);
        return assetRoot;
    }
    static void ConfigureTexture(string name,bool normal)
    {
        var imp=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+name);
        if(imp==null)throw new Exception("Missing texture "+name);
        imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=!normal;imp.convertToNormalmap=false;imp.maxTextureSize=2048;imp.SaveAndReimport();
    }
}
}
