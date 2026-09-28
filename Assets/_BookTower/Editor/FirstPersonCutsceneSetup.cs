using System;
using UnityEditor;
using UnityEngine;
using BookTower.Story;

namespace BookTower.EditorTools
{
    public static class FirstPersonCutsceneSetup
    {
        public static void RunBatch()
        {
            var art=AssetDatabase.LoadAssetAtPath<PaperCutsceneArt>(PaperCutsceneBuilder.ArtPath);
            if(!art)throw new Exception("Opening cutscene art missing");
            var names=new[]{"01-dark","02-offering","03-received","04-awake"};
            art.firstPersonAwakening=new Sprite[4];
            for(int i=0;i<4;i++)
            {
                string path="Assets/_BookTower/Art/Cutscenes/FirstPersonV1/"+names[i]+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)throw new Exception("Missing "+path);
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.sRGBTexture=true;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
                art.firstPersonAwakening[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            if(!art.HasFirstPerson)throw new Exception("Incomplete first-person art");
            art.continuousBird=Layer("rest","WholeBirdV1");art.continuousOffering=Layer("release","WholeBirdV1");
            art.continuousHands=Layer("hands");art.continuousSeed=Layer("seed");
            art.spiritPortrait=Layer("spirit","PortraitsV1");
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            ContinuousAwakeningPreview.Render(art);
            OpeningStoryVerifier.RunBatch();
        }
        static Sprite Layer(string name,string folder="ContinuousV1")
        {
            string path="Assets/_BookTower/Art/Cutscenes/"+folder+"/"+name+".png";
            var i=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!i)throw new Exception(path);
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
            i.alphaIsTransparency=true;i.mipmapEnabled=false;i.maxTextureSize=2048;
            i.textureCompression=TextureImporterCompression.Uncompressed;i.wrapMode=TextureWrapMode.Clamp;
            var s=new TextureImporterSettings();i.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteGenerateFallbackPhysicsShape=false;i.SetTextureSettings(s);
            i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
