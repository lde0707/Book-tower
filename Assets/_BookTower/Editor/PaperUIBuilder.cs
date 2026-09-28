using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.UI;
using BookTower.Rooms;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class PaperUIBuilder
 {
  [MenuItem("Book Tower/종이 UI 적용")]
  public static void Apply()
  {
   Directory.CreateDirectory("Logs/UIBackups");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Logs/UIBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   foreach(string n in new[]{"Notebook","Leaf"}){var imp=(TextureImporter)AssetImporter.GetAtPath("Assets/_BookTower/UI/"+n+".png");imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.SaveAndReimport();}
   var atlasPath="Assets/_BookTower/UI/Extracted/Settings_NoText.png";var atlasImporter=(TextureImporter)AssetImporter.GetAtPath(atlasPath);atlasImporter.textureType=TextureImporterType.Default;atlasImporter.npotScale=TextureImporterNPOTScale.None;atlasImporter.mipmapEnabled=false;atlasImporter.alphaIsTransparency=true;atlasImporter.textureCompression=TextureImporterCompression.Uncompressed;atlasImporter.maxTextureSize=2048;atlasImporter.SaveAndReimport();
   var storyPath="Assets/_BookTower/UI/Extracted/Resources/StoryPanels.png";var storyImporter=(TextureImporter)AssetImporter.GetAtPath(storyPath);storyImporter.textureType=TextureImporterType.Default;storyImporter.npotScale=TextureImporterNPOTScale.None;storyImporter.mipmapEnabled=false;storyImporter.alphaIsTransparency=true;storyImporter.textureCompression=TextureImporterCompression.Uncompressed;storyImporter.maxTextureSize=2048;storyImporter.SaveAndReimport();
   var old=Object.FindFirstObjectByType<PaperUI>();if(old)Object.DestroyImmediate(old.gameObject);var go=new GameObject("Paper UI · State Machine");go.AddComponent<PaperSettingsRuntime>();var ui=go.AddComponent<PaperUI>();ui.cutsceneArt=AssetDatabase.LoadAssetAtPath<BookTower.Story.PaperCutsceneArt>(PaperCutsceneBuilder.ArtPath);ui.storyAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(storyPath);ui.skinAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);var inkPath="Assets/_BookTower/UI/Extracted/InkOnly.mat";var inkMaterial=AssetDatabase.LoadAssetAtPath<Material>(inkPath);if(!inkMaterial){inkMaterial=new Material(Shader.Find("BookTower/UI/PaperInk"));AssetDatabase.CreateAsset(inkMaterial,inkPath);}ui.inkOnlyMaterial=inkMaterial;ui.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/_BookTower/UI/Fonts/NanumGothic-Bold.ttf");ui.headingFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/_BookTower/UI/Fonts/NanumGothic-ExtraBold.ttf");ui.notebookSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BookTower/UI/Notebook.png");ui.leafSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BookTower/UI/Leaf.png");ui.previewClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Audio/BindingPlay/PaperRustle.wav");ui.musicClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/UI/QuietPages.wav");ui.ambienceClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/UI/PaperAir.wav");
   foreach(var hud in Object.FindObjectsByType<BookTower.Presentation.PrototypeHud>(FindObjectsInactive.Include,FindObjectsSortMode.None))hud.enabled=false;
   var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");var previous=room.transform.Find("제본사의 읽는 기록");if(previous)Object.DestroyImmediate(previous.gameObject);
   var note=GameObject.CreatePrimitive(PrimitiveType.Cube);note.name="제본사의 읽는 기록";note.transform.SetParent(room.transform,false);note.transform.localPosition=new Vector3(-3.2f,4,-7);note.transform.localScale=new Vector3(1.3f,1.1f,.12f);note.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0Reference/IvoryPaper.mat");var it=note.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue="제본사의 기록";so.FindProperty("verb").stringValue="읽기";so.FindProperty("lift").floatValue=.025f;so.ApplyModifiedPropertiesWithoutUndo();note.AddComponent<ReadableNote>();
   RefreshSymbols();
   PrefabUtility.SaveAsPrefabAsset(go,"Assets/_BookTower/UI/PaperUI.prefab");EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  }
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();PaperUIVerifier.Run();}
  public static void RefreshSymbols(){var ui=Object.FindFirstObjectByType<PaperUI>();if(!ui)return;foreach(var target in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))if(target.GetComponent<FoldBridgeControl>()||target.GetComponent<BookTower.Fold.FoldPanel>()){var symbol=target.GetComponent<PaperWorldSymbol>();if(!symbol)symbol=target.gameObject.AddComponent<PaperWorldSymbol>();symbol.font=ui.font;symbol.symbol=PaperUI.Symbol(target);}}
 }
}
