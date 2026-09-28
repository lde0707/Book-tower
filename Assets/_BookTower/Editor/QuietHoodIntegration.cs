using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Interaction;
using BookTower.Puzzles;
using BookTower.Story;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class QuietHoodIntegration
    {
        static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Resources/Sfx/"+name+".wav");
        public static void Run()
        {
            try
            {
                AssetDatabase.Refresh();
                foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/_BookTower/Resources/Sfx"}))
                {
                    var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    importer.forceToMono=true;
                    var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;
                    sample.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
                }
                HoodSpiritBuilder.Build();
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                HoodSpiritBuilder.Apply();
                int assigned=0;
                foreach(var p in Object.FindObjectsByType<FoldBridgePuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {p.foldSound=Clip("fold_step");EditorUtility.SetDirty(p);assigned++;}
                foreach(var p in Object.FindObjectsByType<BindingTouch>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {p.sound=Clip(p.kind==BindingTouch.TouchKind.TapBook?"puzzle_solve":p.kind==BindingTouch.TouchKind.FlutterPage?"fold_step":"hint_tremble");EditorUtility.SetDirty(p);assigned++;}
                foreach(var p in Object.FindObjectsByType<OpeningStoryDirector>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {p.rustle=Clip("ui_page");EditorUtility.SetDirty(p);assigned++;}
                foreach(var p in Object.FindObjectsByType<PaperUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {p.previewClip=Clip("ui_click");EditorUtility.SetDirty(p);assigned++;}
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                HoodSpiritBuilder.Verify();
                File.WriteAllText("Logs/quiet-hood-integration.txt","PASS: new hood prefab and 9 animation clips; quiet audio importer, "+assigned+" scene sound references assigned. BGM unchanged.");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/quiet-hood-integration.txt",e.ToString());EditorApplication.Exit(1);}
        }
    }
}
