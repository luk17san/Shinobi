using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Shinobi.Editor {
    public static class ShinobiAnimationTrainingSetup {
        const string Folder="Assets/Shinobi/Animation/Imported/";
        static AnimationClip Import(string file,bool loop) {
            var path=Folder+file+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips) {
                clip.loopTime=loop; clip.loopPose=loop;
                clip.lockRootRotation=true; clip.lockRootPositionXZ=true; clip.lockRootHeightY=true;
                clip.keepOriginalPositionXZ=false; clip.keepOriginalOrientation=true; clip.heightFromFeet=true;
            }
            importer.clipAnimations=clips; importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        }
        [MenuItem("Shinobi/Etap 9/Przygotuj animacje i modele treningowe")]
        public static void Apply() {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Zatrzymaj Play Mode.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            var bodyImporter=(ModelImporter)AssetImporter.GetAtPath("Assets/Shinobi/Animation/YBot/Y Bot@Fight Idle.fbx");
            if(bodyImporter.optimizeGameObjects) { bodyImporter.optimizeGameObjects=false; bodyImporter.SaveAndReimport(); }
            var walking=Import("Walking",true); var running=Import("Running",true); var jumping=Import("Jumping",false); var block=Import("BodyBlock",false); Import("OutwardBlock",false);
            const string setPath="Assets/Shinobi/Configuration/YBotAnimations.asset";
            var set=AssetDatabase.LoadAssetAtPath<ShinobiAnimationSet>(setPath);
            if(set==null) { set=ScriptableObject.CreateInstance<ShinobiAnimationSet>(); AssetDatabase.CreateAsset(set,setPath); }
            if(set.idle==null) set.idle=AssetDatabase.LoadAllAssetsAtPath("Assets/Shinobi/Animation/YBot/Y Bot@Fight Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
            if(set.walk==null) set.walk=walking; if(set.run==null) set.run=running; if(set.jump==null) set.jump=jumping; if(set.fall==null) set.fall=jumping; if(set.block==null) set.block=block;
            EditorUtility.SetDirty(set);
            var scene=SceneManager.GetSceneByPath(ShinobiCombatIntegration.ScenePath); bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(ShinobiCombatIntegration.ScenePath,OpenSceneMode.Additive);
            try {
                foreach(var movement in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShinobiMovement>(true))) Configure(movement.gameObject,set);
                var prefab=PrefabUtility.LoadPrefabContents(ShinobiCombatIntegration.PrefabPath);
                try { Configure(prefab,set); PrefabUtility.SaveAsPrefabAsset(prefab,ShinobiCombatIntegration.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("ANIMATION_SETUP_PASS");
            } finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        static void Configure(GameObject character,ShinobiAnimationSet set) {
            var driver=character.GetComponent<ShinobiAnimationDriver>(); if(driver==null) driver=character.AddComponent<ShinobiAnimationDriver>();
            driver.animator=character.GetComponentInChildren<Animator>(); driver.animations=set; driver.animator.applyRootMotion=false; ShinobiAnimatorControllerSetup.Configure(driver);
            var inventory=character.GetComponent<ShinobiWeaponInventory>(); if(inventory==null) return;
            AnimatorUtility.DeoptimizeTransformHierarchy(driver.animator.gameObject);
            driver.animator.Rebind();
            var bone=driver.animator.GetBoneTransform(HumanBodyBones.RightHand);
            if(bone==null) {
                var old=driver.animator;
                var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shinobi/Animation/YBot/Y Bot@Fight Idle.fbx"),old.transform.parent);
                model.name=old.gameObject.name; model.transform.localPosition=old.transform.localPosition; model.transform.localRotation=old.transform.localRotation; model.transform.localScale=old.transform.localScale;
                var replacement=model.GetComponent<Animator>(); replacement.runtimeAnimatorController=old.runtimeAnimatorController; replacement.applyRootMotion=false;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()) {
                    var original=old.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name==renderer.name);
                    if(original!=null) renderer.sharedMaterials=original.sharedMaterials;
                }
                Object.DestroyImmediate(old.gameObject); driver.animator=replacement; replacement.Rebind();
                var health=character.GetComponent<ShinobiHealth>(); if(health!=null) { var healthData=new SerializedObject(health); healthData.FindProperty("visual").objectReferenceValue=model.GetComponentInChildren<Renderer>(); healthData.ApplyModifiedPropertiesWithoutUndo(); }
                bone=replacement.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if(bone==null) throw new System.InvalidOperationException("Brak prawej dloni Humanoid.");
            var socket=bone.Find("WeaponSocket"); if(socket==null) { socket=new GameObject("WeaponSocket").transform; socket.SetParent(bone,false); }
            var data=new SerializedObject(inventory); data.FindProperty("weaponSocket").objectReferenceValue=socket; data.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<inventory.Weapons.Count;i++) {
                var weapon=inventory.Weapons[i]; if(weapon==null || weapon.visualPrefab!=null) continue;
                weapon.visualPrefab=BuildWeapon(i); weapon.localPosition=Vector3.zero; weapon.localEulerAngles=new Vector3(0,0,90); weapon.localScale=Vector3.one;
                EditorUtility.SetDirty(weapon);
            }
        }
        static GameObject BuildWeapon(int index) {
            string path="Assets/Shinobi/Prefabs/TrainingWeaponModel"+(index+1)+".prefab";
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(saved!=null) return saved;
            string matPath="Assets/Shinobi/Configuration/TrainingWeaponWood.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color=new Color(0.35f,0.16f,0.06f); AssetDatabase.CreateAsset(material,matPath); }
            var root=new GameObject(index==0?"Drewniany miecz treningowy":"Kij treningowy");
            try {
                Part(root,"Uchwyt",new Vector3(0,0,0),new Vector3(0.04f,0.25f,0.05f),material);
                Part(root,"Ostrze lub trzon",new Vector3(0,index==0?0.42f:0.35f,0),new Vector3(index==0?0.07f:0.035f,index==0?0.65f:1.4f,0.035f),material);
                if(index==0) Part(root,"Jelec",new Vector3(0,0.12f,0),new Vector3(0.22f,0.04f,0.06f),material);
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            } finally { Object.DestroyImmediate(root); }
        }
        static void Part(GameObject root,string name,Vector3 position,Vector3 scale,Material material) {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube); part.name=name; part.transform.SetParent(root.transform,false); part.transform.localPosition=position; part.transform.localScale=scale;
            Object.DestroyImmediate(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}

