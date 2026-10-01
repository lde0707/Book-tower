using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ReleaseMaterialReview
 {
  const string Flag="ReleaseMaterialReview";static IEnumerator routine;static double next,started;static string folder;static readonly StringBuilder log=new();
  static PlayerController player;static PaperCamera camera;static Room f0,a1;
  static ReleaseMaterialReview(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Storage(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/material-review-settings.json");}
  public static void Before(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ReleaseMaterialAudit.Write("Before");MemoryBefore();Begin("Before");}
  public static void CaptureAfter(){Begin("After");}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ReleasePaperMaps.Generate();ReleaseMaterialBuilder.Apply();Board();Begin("After");}
  static void Begin(string stage){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetString(Flag+"stage",stage);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Flag,false))return;if(state==PlayModeStateChange.EnteredPlayMode){folder=ReleaseMaterialAudit.Output+"/"+SessionState.GetString(Flag+"stage","");Directory.CreateDirectory(folder);log.Clear();started=EditorApplication.timeSinceStartup;next=started+1;routine=Review();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float delay?delay:.02f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception error){EditorApplication.update-=Tick;File.WriteAllText(folder+"/review.txt",log+(error==null?"PASS: saved-scene capture complete; visual findings recorded separately\n":"FAIL: "+error));SessionState.SetInt(Flag+"exit",error==null?0:1);EditorApplication.ExitPlaymode();}
  static void Place(Vector3 at){player.Controller.enabled=false;player.transform.position=at;player.Controller.enabled=true;Physics.SyncTransforms();}
  static IEnumerator Shot(string name,Room room,Vector3 at,float yaw,float pitch){Place(room.transform.TransformPoint(at));camera.SetAim(yaw,pitch);yield return 2f;F0PaperIdentityVerifier.Capture(Camera.main,folder+"/"+name+".png");var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;log.AppendLine($"{name}: player={player.transform.position:F4} camera={Camera.main.transform.position:F4} rotation={Camera.main.transform.eulerAngles:F4} FOV={Camera.main.fieldOfView}, gameQuality={PaperSettingsRuntime.Values.quality}, UnityQuality={QualitySettings.GetQualityLevel()}, renderScale={pipeline?.renderScale}, shadowDistance={pipeline?.shadowDistance}");}
  static System.Collections.Generic.IEnumerable<object> Steps(IEnumerator e){while(e.MoveNext())yield return e.Current;}
  static IEnumerator Review()
  {
   var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story){var bird=story.companion;if(bird)bird.transform.SetParent(null,true);story.gameObject.SetActive(false);}
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;camera=Object.FindFirstObjectByType<PaperCamera>();var rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);f0=rooms.First(r=>r.RoomId=="F0");a1=rooms.First(r=>r.RoomId=="1A");
   foreach(var s in Steps(Shot("01-F0-start",f0,new Vector3(0,3.1f,-10.5f),0,5)))yield return s;
   foreach(var s in Steps(Shot("02-F0-grazing-floor",f0,new Vector3(0,3.1f,-12),0,26)))yield return s;
   foreach(var s in Steps(Shot("03-F0-wall",f0,new Vector3(-11,3.1f,-10),270,5)))yield return s;
   foreach(var s in Steps(Shot("04-spirit-front",f0,new Vector3(0,3.1f,-10),180,5)))yield return s;
   var renderers=ReleaseMaterialAudit.Renderers();var saved=renderers.Select(r=>r.sharedMaterials).ToArray();
   for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=saved[i].Select(m=>{if(!m)return m;var n=new Material(m);if(n.HasProperty("_BumpScale")){n.SetFloat("_BumpScale",0);n.DisableKeyword("_NORMALMAP");}return n;}).ToArray();
   foreach(var s in Steps(Shot("05-spirit-normal-off",f0,new Vector3(0,3.1f,-10),180,5)))yield return s;
   for(int i=0;i<renderers.Length;i++){foreach(var m in renderers[i].sharedMaterials)if(m)Object.Destroy(m);renderers[i].sharedMaterials=saved[i];}
   foreach(var s in Steps(Shot("06-1A-entry",a1,new Vector3(-8,.2f,-21),0,8)))yield return s;
   foreach(var s in Steps(Shot("07-1A-stage",a1,new Vector3(-4,.2f,14),0,4)))yield return s;
   var settings=PaperSettingsRuntime.Instance;var savedSettings=settings.Current.Copy();var low=savedSettings.Copy();low.quality=0;settings.Apply(low,false);yield return .8f;
   foreach(var s in Steps(Shot("08-1A-low",a1,new Vector3(-4,.2f,14),0,4)))yield return s;
   foreach(var s in Steps(Shot("09-F0-low",f0,new Vector3(0,3.1f,-10.5f),0,5)))yield return s;
   settings.Apply(savedSettings,false);
   if(SessionState.GetString(Flag+"stage","")=="After")Validate();
  }
  static void Validate()
  {
   int checkedMaps=0;foreach(string path in Directory.GetFiles(ReleasePaperMaps.Folder,"*.png")){var i=(TextureImporter)AssetImporter.GetAtPath(path);if(i.sRGBTexture!=path.Contains("Color")||i.convertToNormalmap||!i.mipmapEnabled||i.anisoLevel!=8)throw new Exception("Texture import contract: "+path);checkedMaps++;}
   if(checkedMaps!=24)throw new Exception("Expected 24 material maps, got "+checkedMaps);
   var r=ReleaseMaterialAudit.Renderers();if(r.SelectMany(x=>x.sharedMaterials).Any(m=>!m||m.shader.name=="Hidden/InternalErrorShader"))throw new Exception("Missing/error material");
   log.AppendLine("PASS: 24 maps have explicit colour/data spaces, authored normals, mipmaps and anisotropic filtering; no missing/error material");
  }
  static void MemoryBefore(){var ts=ReleaseMaterialAudit.Renderers().SelectMany(r=>r.sharedMaterials).Where(m=>m).SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t=>t).Distinct();File.WriteAllText(ReleaseMaterialAudit.Output+"/memory-before.txt","Referenced texture runtime bytes="+ts.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)));}
  static void Board()
  {
   Directory.CreateDirectory(ReleaseMaterialAudit.Output+"/Swatches");var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);foreach(var l in lights)l.enabled=false;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.32f,.32f);RenderSettings.fog=false;
   var root=new GameObject("Temporary neutral material laboratory");root.transform.position=new Vector3(500,500,500);var cam=new GameObject("Swatch camera").AddComponent<Camera>();cam.transform.position=root.transform.position+new Vector3(0,2,-9);cam.transform.LookAt(root.transform.position+Vector3.up*.1f);cam.orthographic=true;cam.orthographicSize=1.7f;cam.aspect=3.2f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.16f,.18f);cam.cullingMask=1<<31;var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.volumeLayerMask=0;
   foreach(var (pos,intensity) in new[]{(new Vector3(-3,4,-4),2f),(new Vector3(3,2,-1),.7f)}){var light=new GameObject("Neutral studio light").AddComponent<Light>();light.type=LightType.Directional;light.color=Color.white;light.intensity=intensity;light.transform.rotation=Quaternion.LookRotation(-pos);light.cullingMask=1<<31;light.transform.SetParent(root.transform);}
   var pairs=ReleaseMaterialBuilder.Originals.GroupBy(p=>ReleaseMaterialBuilder.Family(p.Key)).Where(g=>new[]{"Paper","Cover","Cotton","Edge","Brass","Foil"}.Contains(g.Key));
   foreach(var group in pairs)
   {var pair=group.First();var original=pair.Value;string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pair.Key));var variantsForSource=AssetDatabase.FindAssets("t:Material",new[]{ReleaseMaterialBuilder.Folder+"/Materials"}).Select(id=>AssetDatabase.GUIDToAssetPath(id)).Where(p=>Path.GetFileName(p).StartsWith(guid+"_")).ToArray();var preferred=variantsForSource.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==guid+"_1")??variantsForSource.First();var replacement=AssetDatabase.LoadAssetAtPath<Material>(preferred);
    var no=new Material(original);no.SetFloat("_BumpScale",0);no.DisableKeyword("_NORMALMAP");
    var normal=new Material(original);normal.SetTexture("_BumpMap",replacement.GetTexture("_BumpMap"));normal.SetFloat("_BumpScale",replacement.GetFloat("_BumpScale"));
    var rough=new Material(original);rough.SetTexture("_MetallicGlossMap",replacement.GetTexture("_MetallicGlossMap"));rough.SetFloat("_Smoothness",1);rough.EnableKeyword("_METALLICSPECGLOSSMAP");
    var variants=new[]{original,no,normal,rough,replacement};var objects=new GameObject[5];
    for(int i=0;i<5;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.layer=31;go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3((i-2)*2.05f,0,0);go.transform.localScale=Vector3.one*1.65f;go.GetComponent<Renderer>().sharedMaterial=variants[i];objects[i]=go;}
    CaptureBoard(cam,ReleaseMaterialAudit.Output+"/Swatches/"+group.Key+".png");foreach(var go in objects)Object.DestroyImmediate(go);Object.DestroyImmediate(no);Object.DestroyImmediate(normal);Object.DestroyImmediate(rough);
   }
   Object.DestroyImmediate(root);Object.DestroyImmediate(cam.gameObject);
  }
  static void CaptureBoard(Camera c,string path){var rt=RenderTexture.GetTemporary(1920,640,24);var previous=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1920,640,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1920,640),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(t);}
 }
}
