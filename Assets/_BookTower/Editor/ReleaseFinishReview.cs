using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Puzzles;
using BookTower.Presentation;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ReleaseFinishReview
 {
  public const string Output="Docs/ArtReview/ReleaseArt/08-Final";
  const string Flag="ReleaseFinishReview";static IEnumerator routine;static double start,next;static readonly StringBuilder log=new();
  static PlayerController player;static PaperCamera camera;static Room[] rooms;
  static ReleaseFinishReview(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/finish-review-settings.json");}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory(Output);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  public static void CoverageOnly(){SessionState.SetBool(Flag+"coverage",true);Run();}
  public static void FoyerOnly(){SessionState.SetBool(Flag+"foyer",true);Run();}
  static void Changed(PlayModeStateChange state)
  {
   if(!SessionState.GetBool(Flag,false))return;
   if(state==PlayModeStateChange.EnteredPlayMode){log.Clear();routine=Review();start=next=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);SessionState.EraseBool(Flag+"coverage");SessionState.EraseBool(Flag+"foyer");EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}
  }
  static void Tick()
  {
   if(EditorApplication.timeSinceStartup<next)return;
   try{if(EditorApplication.timeSinceStartup-start>420)throw new Exception("Finish review timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float delay?delay:.02f);}catch(Exception e){Finish(e);}
  }
  static void Finish(Exception error){EditorApplication.update-=Tick;File.WriteAllText(Output+(SessionState.GetBool(Flag+"coverage",false)?"/coverage-diagnostic.txt":SessionState.GetBool(Flag+"foyer",false)?"/foyer-contact-checks.txt":"/review.txt"),log+(error==null?"PASS: automated finish checks; visual judgment recorded separately.\n":"FAIL: "+error));SessionState.SetInt(Flag+"exit",error==null?0:1);EditorApplication.ExitPlaymode();}
  static Room R(string id)=>rooms.First(r=>r.RoomId==id);
  static void Place(Room room,Vector3 local,float yaw,float pitch)
  {player.Controller.enabled=false;player.transform.position=room.transform.TransformPoint(local);player.Controller.enabled=true;Physics.SyncTransforms();camera.SetAim(yaw,pitch);}
  public static void Capture(string name,int width=1600,int height=900)
  {
   var c=Camera.main;var rt=RenderTexture.GetTemporary(width,height,24);var old=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;Texture2D tex=null;
   try{c.targetTexture=rt;c.aspect=(float)width/height;c.Render();RenderTexture.active=rt;tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());}
   finally{c.targetTexture=old;c.aspect=aspect;RenderTexture.active=active;if(tex)Object.DestroyImmediate(tex);RenderTexture.ReleaseTemporary(rt);}
   log.AppendLine($"{name}: {width}x{height}, camera {c.transform.position:F2}, euler {c.transform.eulerAngles:F1}, FOV {c.fieldOfView}, q={PaperSettingsRuntime.Values.quality}");
  }
  static IEnumerator Review()
  {
   yield return 1f;
   var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story){if(story.companion)story.companion.transform.SetParent(null,true);story.gameObject.SetActive(false);}
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;camera=Object.FindFirstObjectByType<PaperCamera>();rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
   if(SessionState.GetBool(Flag+"coverage",false))
   {foreach(float yaw in new[]{0,180}){Place(R("1B"),new(0,.2f,-11),yaw,-6);yield return 2f;Capture("1B-"+yaw);Coverage("1B-"+yaw);CheckRenderedEnvelope("1B-"+yaw);}yield break;}
   PaperSettingsRuntime.Instance.Apply(new PaperSettings(),false);
   yield return .5f;
   if(SessionState.GetBool(Flag+"foyer",false))
   {
    foreach(float yaw in new[]{0,90,180,270}){Place(R("F0"),new(0,3.1f,-10.5f),yaw,5);yield return 2f;Capture("F0-"+yaw);Coverage("F0-"+yaw);}
    foreach(float x in new[]{-10.8f,0,10.8f})
    {
     Place(R("F0"),new(x,3.2f,-17.5f),180,5);yield return .2f;player.Controller.Move(Vector3.back*4);
     float z=R("F0").transform.InverseTransformPoint(player.transform.position).z;if(z< -18.85f||z> -18.2f)throw new Exception("Rear folio collision disagrees with visible cover: "+z);
     log.AppendLine($"PASS: real CharacterController stopped at new folio face, x={x}, z={z}.");
    }
    yield break;
   }
   // Batch play does not render each editor frame. Explicitly evaluate the camera's volume stack.
   Capture("00-profile-warmup");
   var stack=VolumeManager.instance.CreateStack();VolumeManager.instance.Update(stack,Camera.main.transform,Camera.main.GetUniversalAdditionalCameraData().volumeLayerMask);
   var grade=stack.GetComponent<ColorAdjustments>();
   if(Mathf.Abs(grade.contrast.value-5)>.01f||Mathf.Abs(grade.saturation.value+5)>.01f)throw new Exception("User settings override the shared art grade");
   if(Mathf.Abs(grade.postExposure.value-.35f)>.01f)throw new Exception("Default exposure has more than one effective owner");
   var bright=PaperSettingsRuntime.Values.Copy();bright.brightness=75;PaperSettingsRuntime.Instance.Apply(bright,false);yield return .3f;
   VolumeManager.instance.Update(stack,Camera.main.transform,Camera.main.GetUniversalAdditionalCameraData().volumeLayerMask);
   if(Mathf.Abs(grade.postExposure.value-.85f)>.01f||Mathf.Abs(grade.contrast.value-5)>.01f)throw new Exception("Brightness changes art contrast or wrong exposure");
   PaperSettingsRuntime.Instance.Apply(new PaperSettings(),false);yield return .3f;
   VolumeManager.instance.DestroyStack(stack);
   log.AppendLine("PASS: shared contrast 5 / saturation -5; user brightness alone owns exposure .35 at 50, .85 at 75.");
   foreach(var id in new[]{"F0","1A","1B"})
   {
    var room=R(id);Vector3 at=id=="F0"?new(0,3.1f,-10.5f):id=="1A"?new(-4,.2f,14):new(0,.2f,-11);
    foreach(float yaw in new[]{0,90,180,270}){Place(room,at,yaw,id=="1B"?-6:5);yield return 2f;Capture(id+"-"+yaw);Coverage(id+"-"+yaw);}
    Place(room,at,0,id=="1B"?-6:5);yield return 1f;
    var data=Camera.main.GetUniversalAdditionalCameraData();bool post=data.renderPostProcessing;data.renderPostProcessing=false;Capture(id+"-post-off");data.renderPostProcessing=post;
    Capture(id+"-16x10",1600,1000);Capture(id+"-21x9",2100,900);
    foreach(int q in new[]{0,2}){var s=PaperSettingsRuntime.Values.Copy();s.quality=q;PaperSettingsRuntime.Instance.Apply(s,false);yield return .4f;Capture(id+"-quality-"+q);}
    PaperSettingsRuntime.Instance.Apply(new PaperSettings(),false);
   }
   // Both record orders must work; this run chooses fox first (the existing verifier chooses rabbit first).
   var p=R("1A").GetComponentInChildren<StorybookLightPuzzle>();Place(R("1A"),new(-4,.2f,14),0,4);yield return 1f;
   p.DragPiece(2,ShadowDuet.FoxBodySolution);p.DragPiece(3,ShadowDuet.FoxTailSolution);p.TurnPiece(3);p.SelectLight(0);yield return 3f;
   if(!p.Duet.FoxRecorded||p.Duet.RabbitRecorded)throw new Exception("Fox-first recording failed");yield return 2f;Capture("1A-fox-parked");
   p.SelectLight(1);p.DragPiece(0,new(-1.8f,1,12.5f/1.5f));p.DragPiece(1,new(-.9f,1,12.5f/3));if(!p.Flipped)p.TurnPiece(1);yield return 3f;
   if(!p.Complete)throw new Exception("Rabbit cannot complete after fox parks");yield return 2f;Capture("1A-both-recorded");
   if(Mathf.Abs(p.body.localPosition.x)<5||Mathf.Abs(p.Duet.foxBody.localPosition.x)<5)throw new Exception("Recorded silhouettes did not clear the stage");
   log.AppendLine("PASS: fox-first completion, stable remembered projections, completed silhouettes move into wings.");
   Place(R("F0"),new(0,3.1f,-10.5f),0,5);yield return 1f;
   camera.SetAim(32,5);camera.LockObservation(1);yield return 1f;if(Mathf.Abs(Mathf.DeltaAngle(camera.YawDegrees,90))>.1f)throw new Exception("E cardinal snap");camera.ConsumeMovementYaw();if(camera.IsObservationLocked)throw new Exception("Movement failed to unlock camera");
   camera.SetAim(32,5);camera.LockObservation(-1);yield return 1f;if(Mathf.Abs(Mathf.DeltaAngle(camera.YawDegrees,0))>.1f)throw new Exception("Q cardinal snap");camera.ConsumeMovementYaw();
   log.AppendLine("PASS: Q/E use map cardinal axes; movement unlocks observation.");
   var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;R("F0").SetVisible(true);
   var motion=R("F0").GetComponentsInChildren<PaperRoomMotion>().First(m=>m.tips.Length>1);float phase=motion.Phase;
   R("F0").SetVisible(false);yield return .3f;if(motion.Phase!=phase)throw new Exception("Hidden room motion advances");R("F0").SetVisible(true);yield return .3f;
   phase=motion.Phase;Time.timeScale=0;yield return .3f;if(motion.Phase!=phase)throw new Exception("Paused motion advances");Time.timeScale=1;
   var settings=PaperSettingsRuntime.Values.Copy();settings.reduceStoryMotion=true;PaperSettingsRuntime.Instance.Apply(settings,false);yield return 1f;var still=motion.tips[0].localRotation;yield return .5f;if(Quaternion.Angle(still,motion.tips[0].localRotation)>.001f)throw new Exception("Reduced motion is not still");
   PaperSettingsRuntime.Instance.Apply(new PaperSettings(),false);manager.enabled=true;
   log.AppendLine("PASS: room motion suspends while hidden/paused and settles to still under accessibility setting.");
   // One minute of the actual camera, not a synthetic sine-curve test.
   Directory.CreateDirectory(Output+"/Motion");Place(R("F0"),new(-10,3.1f,-10),0,5);yield return 1f;
   float began=Time.unscaledTime;for(int i=0;i<=60;i++){Capture("Motion/frame-"+i.ToString("000"),960,540);yield return Mathf.Max(.02f,began+i+1-Time.unscaledTime);}
   log.AppendLine("Captured a 60-second environment sequence. Editor timings are not release performance evidence.");
   log.AppendLine($"Editor machine: {SystemInfo.processorType}; {SystemInfo.graphicsDeviceName}; {SystemInfo.systemMemorySize} MB RAM; {SystemInfo.graphicsDeviceType}; Unity {Application.unityVersion}.");
  }
  static void CheckRenderedEnvelope(string name)
  {
   int airCount=Shader.GetGlobalInt("_PaperAirCount"),beamCount=Shader.GetGlobalInt("_PaperBeamCount");
   var c=Camera.main;var data=c.GetUniversalAdditionalCameraData();bool post=data.renderPostProcessing,fog=RenderSettings.fog;var clear=c.clearFlags;var colour=c.backgroundColor;var old=c.targetTexture;var active=RenderTexture.active;float aspect=c.aspect;
   var rt=RenderTexture.GetTemporary(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
   try
   {
    Shader.SetGlobalInt("_PaperAirCount",0);Shader.SetGlobalInt("_PaperBeamCount",0);
    data.renderPostProcessing=false;RenderSettings.fog=false;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.magenta;c.targetTexture=rt;c.aspect=16f/9;c.Render();RenderTexture.active=rt;
    tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+"-envelope.png",tex.EncodeToPNG());
    int uncovered=tex.GetPixels32().Count(p=>p.r>180&&p.b>180&&p.g<85);log.AppendLine($"{name} rendered envelope: {uncovered} exposed magenta pixels at 960x540 (post and fog disabled).");
    if(uncovered>0)throw new Exception(name+" has exposed background pixels: "+uncovered);
   }
   finally{Shader.SetGlobalInt("_PaperAirCount",airCount);Shader.SetGlobalInt("_PaperBeamCount",beamCount);data.renderPostProcessing=post;RenderSettings.fog=fog;c.clearFlags=clear;c.backgroundColor=colour;c.targetTexture=old;c.aspect=aspect;RenderTexture.active=active;Object.DestroyImmediate(tex);RenderTexture.ReleaseTemporary(rt);}
  }
  static void Coverage(string name)
  {
   var missing=new StringBuilder();int hits=0,total=0,backfaces=0;var c=Camera.main;float aspect=c.aspect;c.aspect=16f/9;
   bool Eligible(RaycastHit h){var r=h.collider.GetComponentInParent<Room>();return (!r||r.IsVisible)&&!h.collider.transform.IsChildOf(player.transform);}
   for(int y=0;y<17;y++)for(int x=0;x<30;x++)
   {
    var ray=c.ViewportPointToRay(new Vector3((x+.5f)/30,(y+.5f)/17,0));
    bool covered=Physics.RaycastAll(ray,120,~0,QueryTriggerInteraction.Ignore).Any(Eligible);
    total++;if(covered)hits++;else
    {
     bool was=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;var other=Physics.RaycastAll(ray,120,~0,QueryTriggerInteraction.Ignore).Where(Eligible).OrderBy(h=>h.distance).FirstOrDefault();Physics.queriesHitBackfaces=was;
     if(other.collider){backfaces++;missing.AppendLine($"Backface hit: {other.collider.name}, viewport=({(x+.5f)/30:F3},{(y+.5f)/17:F3})");}
     else missing.AppendLine($"UNRESOLVED viewport=({(x+.5f)/30:F3},{(y+.5f)/17:F3}), ray={ray}");
    }
   }
   c.aspect=aspect;log.AppendLine($"{name} collision ray coverage {hits}/{total}, additional backfaces={backfaces}, unresolved={total-hits-backfaces}; geometry sampling, not proof of all visible seams.");
   File.WriteAllText(Output+"/"+name+"-coverage.txt",missing.Length>0?missing.ToString():"No misses in 30 x 17 viewport sample.");
  }
 }
}
