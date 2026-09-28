using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Story;
using BookTower.UI;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class PaperCutsceneBuilder
    {
        const string Root = "Assets/_BookTower/Art/Cutscenes";
        public const string ArtPath = Root + "/OpeningCutsceneArt.asset";
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        [MenuItem("Book Tower/스토리/2D 종이 컷신 적용")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play before applying cutscene art.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene first.");
            Directory.CreateDirectory("Logs/StoryBackups");
            EditorSceneManager.SaveScene(scene,"Logs/StoryBackups/InGame-before-2D-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            AssetDatabase.Refresh();
            var art=AssetDatabase.LoadAssetAtPath<PaperCutsceneArt>(ArtPath);
            if(!art){art=ScriptableObject.CreateInstance<PaperCutsceneArt>();AssetDatabase.CreateAsset(art,ArtPath);}
            art.spirit=Import("Characters/Spirit_Paper2D_v1.png",true);
            art.moa=Import("Characters/Moa_Paper2D_v1.png",true);
            art.foyer=Import("Backgrounds/Foyer_Paper2D_v1.png",false);
            art.theatre=Import("Backgrounds/Theatre_Paper2D_v1.png",false);
            art.memory=Import("Backgrounds/Memory_Paper2D_v1.png",false);
            if(!art.IsReady)throw new Exception("Cutscene art is incomplete.");
            EditorUtility.SetDirty(art);
            var ui=Object.FindFirstObjectByType<PaperUI>();
            if(!ui)throw new Exception("PaperUI missing from scene.");
            ui.cutsceneArt=art;EditorUtility.SetDirty(ui);
            // Remove the former in-world story-only tableaux; ordinary world props and companion stay playable.
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(t=>t.name=="M01_돌아올자리"||t.name=="M02_이름").ToArray())Object.DestroyImmediate(t.gameObject);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BookTower/UI/PaperUI.prefab");
            if(prefab){var prefabUI=prefab.GetComponent<PaperUI>();if(prefabUI){prefabUI.cutsceneArt=art;EditorUtility.SetDirty(prefabUI);}}
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("[PaperCutscene] Five sprites wired; 3D memory tableaux removed; dialogue is press-to-reveal/advance.");
        }
        static Sprite Import(string relative,bool alpha)
        {
            string path=Root+"/"+relative;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new FileNotFoundException(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=alpha;importer.mipmapEnabled=false;importer.sRGBTexture=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
