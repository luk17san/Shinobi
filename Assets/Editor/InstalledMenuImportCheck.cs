using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Shinobi;

[InitializeOnLoad]
public static class InstalledMenuImportCheck
{
    const string Report="C:/Users/Użytkownik/Documents/ChatGPT/Shinobi/.verification/MenuInstall/ImportResult.txt";
    static InstalledMenuImportCheck() { EditorApplication.update+=Check; }
    static void Check()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update-=Check;
        try
        {
            var required=new[]{ShinobiMainMenu.MenuScene,ShinobiMainMenu.ArenaScene};
            foreach(var path in required) if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null) throw new Exception("Missing scene: "+path);
            var tree=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Shinobi/UI/MainMenu.uxml");
            if(tree==null) throw new Exception("MainMenu UXML missing");
            var root=tree.CloneTree();
            foreach(var name in new[]{"StartTraining","StartDuel","Quit"}) if(root.Q<Button>(name)==null) throw new Exception("Missing button: "+name);
            var panel=AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Shinobi/Configuration/MainMenuPanel.asset");
            if(panel==null || panel.themeStyleSheet==null) throw new Exception("Panel or theme missing");
            foreach(var name in new[]{"background","panel","logo","button-normal","button-selected"})
                if(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shinobi/UI/MenuArt/"+name+".png")==null) throw new Exception("Missing texture: "+name);
            EditorBuildSettings.scenes=required.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!required.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Report,"PASS: runtime/editor compilation, MainMenu/TrainingArena scene imports, UXML buttons, panel/theme and 5 textures. Build Settings updated: MainMenu first, TrainingArena second. Active scene preserved. No live Play Mode test performed.");
            Debug.Log("INSTALLED_MAIN_MENU_IMPORT_PASS");
        }
        catch(Exception error) { File.WriteAllText(Report,"FAIL: "+error); Debug.LogException(error); }
    }
}
