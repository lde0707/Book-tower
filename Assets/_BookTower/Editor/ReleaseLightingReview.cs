using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ReleaseLightingReview
 {
  public const string Output="Docs/ArtReview/ReleaseArt/03-Lighting";
  const string Flag="ReleaseLightingReview";static IEnumerator routine;static double start,next;static string folder;static readonly StringBuilder log=new();
  static PlayerController player;static PaperCamera follow;static Room[] rooms;
  static ReleaseLightingReview(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/lighting-review-settings.json");}
  public static bool Target(Room r)=>r&&(r.RoomId=="F0"||r.RoomId=="1A"||r.RoomId=="1B");
  public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  public static HashSet<Light> PuzzleLights()
  {
   var set=new HashSet<Light>();
   foreach(var p in Object.FindObjectsByType<FoldBridgePuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var l in p.GetComponentsInChildren<Light>(true))set.Add(l);
   foreach(var p in Object.FindObjectsByType<ReadingRoomPuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None)){set.Add(p.lanternLight);set.Add(p.windowLight);set.Add(p.recordLight);}
   foreach(var p in Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None))set.Add(p.lantern);
   foreach(var d in Object.FindObjectsByType<ShadowDuet>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var l in d.lamps)set.Add(l);
   foreach(var t in Object.FindObjectsByType<StorybookShadowTheatre>(FindObjectsInactive.Include,FindObjectsSortMode.None))set.Add(t.rewardLight);
   set.Remove(null);return set;
  }
  public static void Inventory(string stage)
  {
   string dir=Output+"/"+stage;Directory.CreateDirectory(dir);var owned=PuzzleLights();string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
   var rows=new StringBuilder("path,room,type,intensity,range,color,shadows,bias,normal_bias,enabled,puzzle_owned,bake_type\n");
   foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
   {var r=l.GetComponentInParent<Room>();if(r&&!Target(r))continue;rows.AppendLine($"{Q(PathOf(l.transform))},{r?.RoomId},{l.type},{l.intensity},{l.range},{Q(l.color.ToString())},{l.shadows},{l.shadowBias},{l.shadowNormalBias},{l.enabled},{owned.Contains(l)},{l.lightmapBakeType}");}
   File.WriteAllText(dir+"/lights.csv",rows.ToString());
   var info=new StringBuilder();foreach(var r in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(Target))info.AppendLine($"{r.RoomId} pos={r.transform.position}, floor={r.FloorY}, bounds={r.WorldBounds}, sun={r.SunIntensity}, ambient={r.Ambient}, fog={r.Fog}, shadow={r.ShadowStrength}");
   info.AppendLine($"lightmaps={LightmapSettings.lightmaps.Length}; lightProbes={LightmapSettings.lightProbes?.count??0}; reflectionProbes={Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length}; reflectionIntensity={RenderSettings.reflectionIntensity}");
   foreach(var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)){info.AppendLine($"Volume {PathOf(v.transform)} priority={v.priority} weight={v.weight} profile={AssetDatabase.GetAssetPath(v.sharedProfile)}");if(v.sharedProfile)foreach(var c in v.sharedProfile.components)info.AppendLine(JsonUtility.ToJson(c));}
   File.WriteAllText(dir+"/environment.txt",info.ToString());
  }
  public static void Before(){Begin("Before");}
  public static void After(){Begin("After");}
  public static void PuzzleQualities(){Begin("PuzzleQualities");}
  static void Begin(string stage){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Inventory(stage);SessionState.SetString(Flag+"stage",stage);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){folder=Output+"/"+SessionState.GetString(Flag+"stage","");log.Clear();routine=Review();start=EditorApplication.timeSinceStartup;next=start+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-start>200)throw new Exception("Review timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float delay?delay:.02f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception error){EditorApplication.update-=Tick;File.WriteAllText(folder+"/review.txt",log+(error==null?"PASS: review captures completed; visual assessment separate\n":"FAIL: "+error));SessionState.SetInt(Flag+"exit",error==null?0:1);EditorApplication.ExitPlaymode();}
  static void Place(Room room,Vector3 local){player.Controller.enabled=false;player.transform.position=room.transform.TransformPoint(local);player.Controller.enabled=true;Physics.SyncTransforms();}
  static IEnumerator Shot(string name,Room room,Vector3 at,float yaw,float pitch,bool comparison=false)
  {
   Place(room,at);follow.SetAim(yaw,pitch);yield return 2f;Capture(name);
   if(!comparison)yield break;
   var owned=PuzzleLights();var states=owned.ToDictionary(l=>l,l=>l.enabled);foreach(var l in owned)l.enabled=false;
   var fx=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r is ParticleSystemRenderer||r.sharedMaterials.Any(m=>m&&m.shader.name=="BookTower/WindowHaze")).ToDictionary(r=>r,r=>r.enabled);
   foreach(var r in fx.Keys)r.enabled=false;var data=Camera.main.GetUniversalAdditionalCameraData();bool post=data.renderPostProcessing;data.renderPostProcessing=false;
   Capture(name+"-environment");
   var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.5f,.5f,.5f));material.SetFloat("_Smoothness",.15f);
   var rs=room.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r is MeshRenderer).ToArray();var saved=rs.Select(r=>r.sharedMaterials).ToArray();
   for(int i=0;i<rs.Length;i++)rs[i].sharedMaterials=Enumerable.Repeat(material,saved[i].Length).ToArray();
   Capture(name+"-neutral");for(int i=0;i<rs.Length;i++)rs[i].sharedMaterials=saved[i];Object.Destroy(material);data.renderPostProcessing=post;
   foreach(var p in states)p.Key.enabled=p.Value;foreach(var p in fx)p.Key.enabled=p.Value;
  }
  static IEnumerable<object> Steps(IEnumerator e){while(e.MoveNext())yield return e.Current;}
  static void Capture(string name)
  {
   var c=Camera.main;F0PaperIdentityVerifier.Capture(c,folder+"/"+name+".png");var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.enabled&&l.intensity>0).ToArray();var stack=VolumeManager.instance.stack.GetComponent<ColorAdjustments>();
   log.AppendLine($"{name}: camera={c.transform.position:F3} rotation={c.transform.eulerAngles:F2}, FOV={c.fieldOfView}; gameQuality={PaperSettingsRuntime.Values.quality}, scale={pipeline?.renderScale}, shadows={pipeline?.shadowDistance}; exposure={stack?.postExposure.value}; liveLights={lights.Length}, shadowLights={lights.Count(l=>l.shadows!=LightShadows.None)}, ambient={RenderSettings.ambientLight}, probes={Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length}");
  }
  static IEnumerator Review()
  {
   var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story){if(story.companion)story.companion.transform.SetParent(null,true);story.gameObject.SetActive(false);}
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;follow=Object.FindFirstObjectByType<PaperCamera>();rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);Room R(string id)=>rooms.First(r=>r.RoomId==id);
   var settings=PaperSettingsRuntime.Instance;settings.Apply(new PaperSettings(),false);
   if(SessionState.GetString(Flag+"stage","")=="PuzzleQualities"){foreach(var s in Steps(QualityPuzzles(R("1A"))))yield return s;yield break;}
   foreach(var s in Steps(Shot("01-F0-start",R("F0"),new(0,3.1f,-10.5f),0,5,true)))yield return s;
   foreach(var s in Steps(Shot("02-F0-face",R("F0"),new(0,3.1f,-10),180,5)))yield return s;
   foreach(var s in Steps(Shot("03-1A-stage",R("1A"),new(-4,.2f,14),0,4,true)))yield return s;
   foreach(var s in Steps(Shot("04-1A-entry",R("1A"),new(-8,.2f,-21),0,8)))yield return s;
   foreach(var s in Steps(Shot("05-1B-entry",R("1B"),new(0,.2f,-11),0,-6,true)))yield return s;
   foreach(var s in Steps(Shot("06-1B-lantern",R("1B"),new(-8,.2f,-19),0,12)))yield return s;
   foreach(var s in Steps(Shot("07-1B-rear",R("1B"),new(0,.2f,14),180,-8)))yield return s;
   var p=R("1B").GetComponentInChildren<ReadingRoomPuzzle>();p.Use(ReadingRoomControl.Action.Charge);yield return 1f;Capture("08-1B-charge");
   p.Use(ReadingRoomControl.Action.LeftRing);p.Use(ReadingRoomControl.Action.RightRing);p.Use(ReadingRoomControl.Action.Pickup);p.Use(ReadingRoomControl.Action.RecordDock);p.Use(ReadingRoomControl.Action.LeftRing);p.Use(ReadingRoomControl.Action.RightRing);yield return 2f;
   foreach(var s in Steps(Shot("09-1B-record",R("1B"),new(-9,.2f,-10),270,3)))yield return s;
   p.Use(ReadingRoomControl.Action.TakeCrank);p.Use(ReadingRoomControl.Action.Crank);p.Use(ReadingRoomControl.Action.Latch);yield return 3f;
   foreach(var s in Steps(Shot("10-1B-window",R("1B"),new(0,.2f,11),0,-12)))yield return s;
   foreach(int q in new[]{0,2}){var option=settings.Current.Copy();option.quality=q;settings.Apply(option,false);foreach(var s in Steps(Shot($"11-1A-quality-{q}",R("1A"),new(-4,.2f,14),0,4)))yield return s;foreach(var s in Steps(Shot($"12-1B-quality-{q}",R("1B"),new(0,.2f,-11),0,-6)))yield return s;}
   Object.FindFirstObjectByType<RoomManager>().enabled=false;
   foreach(var r in rooms.Where(Target)){r.SetVisible(false);yield return .1f;if(r.GetComponentsInChildren<Light>().Any(l=>l.enabled))throw new Exception("Hidden room light leak "+r.RoomId);r.SetVisible(true);yield return .2f;}
   log.AppendLine("PASS: all three room light groups disable when hidden; default/low/high game settings captured.");
  }
  static IEnumerator QualityPuzzles(Room room)
  {
   Place(room,new Vector3(-4,.2f,14));follow.SetAim(0,4);yield return 2f;var p=room.GetComponentInChildren<StorybookLightPuzzle>();var d=p.Duet;
   p.DragPiece(0,new Vector3(-1.8f,1,12.5f/1.5f));p.DragPiece(1,new Vector3(-.9f,1,12.5f/3));if(!p.Flipped)p.TurnPiece(1);
   p.DragPiece(2,ShadowDuet.FoxBodySolution);p.DragPiece(3,ShadowDuet.FoxTailSolution);if(!d.TailFlipped)p.TurnPiece(3);
   // Freeze the settled physical arrangement before recording; recorded paper now parks in the wings.
   // This test compares actual projection quality without advancing the puzzle between quality levels.
   p.enabled=false;p.body.localPosition=p.BodyPosition;p.ears.localPosition=p.EarPosition;p.ears.localScale=Vector3.one;
   d.foxBody.localPosition=ShadowDuet.FoxBodySolution;d.foxTail.localPosition=ShadowDuet.FoxTailSolution;d.foxTail.localScale=Vector3.one;
   foreach(int q in new[]{0,1,2})
   {
    var settings=PaperSettingsRuntime.Values.Copy();settings.quality=q;PaperSettingsRuntime.Instance.Apply(settings,false);
    p.SelectLight(1);yield return 2f;Capture($"quality-{q}-rabbit");
    p.SelectLight(2);yield return .8f;if(d.FoxAligned)throw new Exception("Wrong light solves fox at quality "+q);Capture($"quality-{q}-wrong-fox");
    p.SelectLight(0);yield return 1f;if(!d.FoxAligned)throw new Exception("Left projection cannot align fox at quality "+q);Capture($"quality-{q}-correct-fox");
   }
   foreach(int q in new[]{0,2}){var settings=PaperSettingsRuntime.Values.Copy();settings.quality=q;PaperSettingsRuntime.Instance.Apply(settings,false);foreach(var s in Steps(Shot($"quality-{q}-F0",rooms.First(r=>r.RoomId=="F0"),new Vector3(0,3.1f,-10.5f),0,5)))yield return s;}
   log.AppendLine("PASS: wrong/right fox geometric projection at all three game qualities; rabbit/fox rendered evidence requires visual inspection.");
  }
 }
}
