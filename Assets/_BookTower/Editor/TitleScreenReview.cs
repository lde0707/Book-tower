using System;
using System.IO;
using System.Linq;
using BookTower.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BookTower.EditorTools
{
    [InitializeOnLoad]
    public static class TitleScreenReview
    {
        const string Flag = "TitleScreenReview";
        static TitleScreenReview() { EditorApplication.playModeStateChanged += Changed; }
        public static void Prepare()
        {
            const string quitPlugin = "Assets/_BookTower/Plugins/x86_64/BookTowerWindowsQuit.dll";
            AssetDatabase.ImportAsset(quitPlugin, ImportAssetOptions.ForceUpdate);
            var plugin = (PluginImporter)AssetImporter.GetAtPath(quitPlugin);
            plugin.SetCompatibleWithAnyPlatform(false); plugin.SetCompatibleWithEditor(false);
            plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
            plugin.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "x86_64");
            plugin.SaveAndReimport();
            const string artPath = "Assets/_BookTower/UI/Title/FoldOfLight_Title_Characters_v2_Background.png";
            AssetDatabase.ImportAsset(artPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(artPath);
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.isReadable = false; importer.alphaSource = TextureImporterAlphaSource.None;
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            Directory.CreateDirectory("Assets/_BookTower/Resources");
            const string path = "Assets/_BookTower/Resources/TitleScreen.asset";
            var assets = AssetDatabase.LoadAssetAtPath<PaperTitleAssets>(path);
            if (!assets) { assets = ScriptableObject.CreateInstance<PaperTitleAssets>(); AssetDatabase.CreateAsset(assets, path); }
            assets.background = AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);
            const string buttonsPath = "Assets/_BookTower/UI/Title/TitleButtons_v2.png";
            AssetDatabase.ImportAsset(buttonsPath, ImportAssetOptions.ForceUpdate);
            var buttons = (TextureImporter)AssetImporter.GetAtPath(buttonsPath);
            buttons.textureType = TextureImporterType.Default; buttons.mipmapEnabled = false; buttons.isReadable = false;
            buttons.alphaSource = TextureImporterAlphaSource.FromInput; buttons.alphaIsTransparency = true;
            buttons.npotScale = TextureImporterNPOTScale.None; buttons.wrapMode = TextureWrapMode.Clamp;
            buttons.maxTextureSize = 2048; buttons.textureCompression = TextureImporterCompression.CompressedHQ;
            buttons.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(buttonsPath);
            Sprite Slice(string name, Rect rect)
            {
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
                if (sprite) return sprite;
                sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                sprite.name = name; AssetDatabase.AddObjectToAsset(sprite, assets); return sprite;
            }
            assets.menuPaper = Slice("Title neutral paper", new Rect(0, 644, 1536, 350));
            assets.menuSelected = Slice("Title gold selected paper", new Rect(0, 312, 1536, 350));
            assets.menuLeaf = Slice("Title glowing leaf", new Rect(600, 0, 340, 280));
            assets.motionShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_BookTower/UI/Title/PaperTitleMotion.shader");
            assets.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Audio/FantasyLibraryV4/04_MainMenu.wav");
            assets.titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_BookTower/UI/Fonts/NanumMyeongjo-ExtraBold.ttf");
            if (!assets.background || !assets.music || !assets.motionShader || !assets.menuPaper || !assets.menuSelected || !assets.menuLeaf) throw new Exception("Title illustration, menu artwork, shader or main-menu music missing.");
            EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
        }
        public static void Run()
        {
            Prepare(); EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Storage()
        {
            if (SessionState.GetBool(Flag, false)) PaperSettingsRuntime.TestStoragePath = Path.GetFullPath("Logs/TitleScreen/qa-settings.json");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!SessionState.GetBool(Flag, false)) return;
            PaperTitleBuildProbe.Finished = success => { SessionState.SetInt(Flag + "exit", success ? 0 : 1); EditorApplication.ExitPlaymode(); };
            if (!UnityEngine.Object.FindFirstObjectByType<PaperTitleBuildProbe>())
                new GameObject("Title QA").AddComponent<PaperTitleBuildProbe>();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Flag, false)) return;
            SessionState.EraseBool(Flag); EditorApplication.Exit(SessionState.GetInt(Flag + "exit", 1));
        }
        public static void Build() { Prepare(); GravityPageReview.Build(); }
    }
}
