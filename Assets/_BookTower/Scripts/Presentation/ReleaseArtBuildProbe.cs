using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.Presentation
{
 // Opt-in standalone measurement. No component, settings or file writes in normal play.
 public sealed class ReleaseArtBuildProbe:MonoBehaviour
 {
  static string output;static bool Active=>Environment.GetCommandLineArgs().Contains("-releaseArtProbe");
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Storage()
  {
   if(!Active)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-releaseArtProbe");
   output=Path.GetFullPath(index+1<args.Length?args[index+1]:"ReleaseArtProbe");Directory.CreateDirectory(output);
   PaperSettingsRuntime.TestStoragePath=Path.Combine(output,"isolated-settings.json");
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Active)new GameObject("Opt-in release art measurement").AddComponent<ReleaseArtBuildProbe>();}
  PlayerController player;PaperCamera camera;Room[] rooms;readonly StringBuilder log=new();bool failed;
  void Start(){Application.runInBackground=true;Application.logMessageReceived+=Error;StartCoroutine(Guarded(Run()));}
  void OnDestroy(){Application.logMessageReceived-=Error;}
  void Error(string message,string stack,LogType kind){if(kind==LogType.Exception||kind==LogType.Error){failed=true;log.AppendLine("RUNTIME ERROR: "+message+"\n"+stack);}}
  IEnumerator Guarded(IEnumerator routine)
  {
   while(true){object next;try{if(!routine.MoveNext())break;next=routine.Current;}catch(Exception e){failed=true;log.AppendLine(e.ToString());break;}yield return next;}
   File.WriteAllText(Path.Combine(output,"standalone-report.txt"),log+"\n"+(failed?"FAIL":"PASS: standalone sequence completed; performance budgets evaluated in table."));Application.Quit(failed?1:0);
  }
  static IEnumerable<object> Each(IEnumerator e){while(e.MoveNext())yield return e.Current;}
  void Shot(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
  void Place(Room room,Vector3 local,float yaw=0,float pitch=5){player.Controller.enabled=false;player.transform.position=room.transform.TransformPoint(local);player.Controller.enabled=true;Physics.SyncTransforms();camera.SetAim(yaw,pitch);}
  IEnumerator Run()
  {
   yield return new WaitForSecondsRealtime(3);
   if(PaperAtmosphereDirector.Instance)
   {
    var calibration=Object.FindFirstObjectByType<PaperIrradianceCalibration>();
    if(!calibration||calibration.CalibratedCount<500)throw new Exception("Baked irradiance was not calibrated in the standalone player");
    log.AppendLine($"Atmosphere: {calibration.CalibratedCount} baked probes calibrated; {PaperAtmosphereDirector.Instance.ActiveVolumes} visible room volumes; shader supported={Shader.Find("BookTower/InteriorAtmosphere")?.isSupported}.");
   }
   log.AppendLine($"Unity {Application.unityVersion}, development={Debug.isDebugBuild}, platform={Application.platform}\nCPU: {SystemInfo.processorType}\nGPU: {SystemInfo.graphicsDeviceName}, VRAM {SystemInfo.graphicsMemorySize} MB, API {SystemInfo.graphicsDeviceType}\nRAM: {SystemInfo.systemMemorySize} MB\n");
   var settings=new PaperSettings{vsync=false,fps=-1,windowMode=2,width=1920,height=1080,muteBackground=false};PaperSettingsRuntime.Instance.Apply(settings,false);Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
   var ui=PaperUI.Instance;player=Object.FindFirstObjectByType<PlayerController>();camera=Object.FindFirstObjectByType<PaperCamera>();rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
   yield return new WaitForSecondsRealtime(2);Shot("00-opening");
   for(int i=0;i<18&&ui.StoryActive;i++){ui.AdvanceStoryText();yield return new WaitForSecondsRealtime(1.2f);if(i%3==0)Shot("01-story-"+i);}
   var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story){if(story.companion)story.companion.transform.SetParent(null,true);story.gameObject.SetActive(false);}
   ui.EndStory();ui.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();player.enabled=false;
   Room R(string id)=>rooms.First(r=>r.RoomId==id);
   Place(R("F0"),new(0,3.1f,-10.5f));yield return new WaitForSecondsRealtime(1);
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,1000),new Vector2Int(2100,900)})
   {
    Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.8f);
    ui.Pause();yield return new WaitForSecondsRealtime(.3f);Shot($"UI-{size.x}x{size.y}-pause");yield return new WaitForSecondsRealtime(.3f);
    ui.OpenSettings();ui.SelectTab(0);yield return new WaitForSecondsRealtime(.3f);Shot($"UI-{size.x}x{size.y}-settings");yield return new WaitForSecondsRealtime(.3f);ui.Discard();ui.Resume();
   }
   Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(1);
   log.AppendLine($"Measured resolution {Screen.width}x{Screen.height}. Each segment forces uncapped/VSync=0 and warms up 5s; q1=60s, q0/2=15s. See measurement-config.txt for actual segment settings. No screenshots during sampling.\nRoom,q,frames,mean_ms,p50_ms,p95_ms,p99_ms,max_ms,CPU_mean_ms,GPU_mean_ms,draw_calls_mean,triangles_mean,used_memory_MiB\n");
   File.WriteAllText(Path.Combine(output,"measurement-config.txt"),"Actual settings at segment start; GC counts are changes over each segment, not attribution of a spike.\n");
   File.WriteAllText(Path.Combine(output,"slow-frames.txt"),"Frames exceeding 33.33 ms. Captured in memory during sampling, written after each segment.\n");
   foreach(string id in new[]{"F0","1A","1B"})
   {
    var room=R(id);var at=id=="F0"?new Vector3(0,3.1f,-10.5f):id=="1A"?new Vector3(-4,.2f,12):new Vector3(0,.2f,-11);
    Place(room,at);foreach(var step in Each(Measure(room,at,1,60)))yield return step;
    Shot(id+"-standalone");yield return new WaitForSecondsRealtime(.3f);
    Directory.CreateDirectory(Path.Combine(output,id+"-clip"));
    for(int frame=0;frame<72;frame++){camera.SetAim(frame/71f*90,5);Shot(id+"-clip/frame-"+frame.ToString("000"));yield return new WaitForSecondsRealtime(1f/12);}
   }
   // Short quality samples supplement, rather than replace, the one-minute baseline.
   foreach(int quality in new[]{0,2})foreach(string id in new[]{"F0","1A","1B"})
   {var room=R(id);var at=id=="F0"?new Vector3(0,3.1f,-10.5f):id=="1A"?new Vector3(-4,.2f,12):new Vector3(0,.2f,-11);Place(room,at);foreach(var step in Each(Measure(room,at,quality,15)))yield return step;}
   log.AppendLine("\nQuality 0/2 samples are 15s plus warmup; baseline q1 is 60s. No full-game release-performance claim. FrameTiming zero means unsupported, not zero cost.");
  }
  IEnumerator Measure(Room room,Vector3 origin,int quality,float seconds)
  {
   var s=PaperSettingsRuntime.Values.Copy();s.quality=quality;s.vsync=false;s.fps=-1;PaperSettingsRuntime.Instance.Apply(s,false);
   yield return new WaitForSecondsRealtime(5);
   File.AppendAllText(Path.Combine(output,"measurement-config.txt"),$"{room.RoomId} q={quality}, {Screen.width}x{Screen.height}, vSync={QualitySettings.vSyncCount}, cap={Application.targetFrameRate}, measuredSeconds={seconds}\n");
   int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);var slow=new List<string>();
   var frames=new List<double>(16000);var cpus=new List<double>();var gpus=new List<double>();long draws=0,tris=0,samples=0,memory=0;
   var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);var mem=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);var timings=new FrameTiming[1];
   try
   {
    float start=Time.unscaledTime;
    while(Time.unscaledTime-start<seconds)
    {
     float elapsed=Time.unscaledTime-start;float t=elapsed/seconds;
     // A small safe local walk and complete cardinal survey use the production follow camera.
     var desired=room.transform.TransformPoint(origin+new Vector3(Mathf.Sin(t*Mathf.PI*2)*1.0f,0,Mathf.Sin(t*Mathf.PI*4)*.55f));
     var delta=desired-player.transform.position;delta.y=-.4f;player.Controller.Move(delta);camera.SetAim(t*360,5+Mathf.Sin(t*Mathf.PI*2)*12);
     FrameTimingManager.CaptureFrameTimings();yield return null;
     frames.Add(Time.unscaledDeltaTime*1000);
     if(Time.unscaledDeltaTime>1f/30)slow.Add(FormattableString.Invariant($"{room.RoomId},q={quality},elapsed={elapsed:F3}s,frame={frames.Count},duration={Time.unscaledDeltaTime*1000:F2}ms"));
     if(FrameTimingManager.GetLatestTimings(1,timings)>0){if(timings[0].cpuFrameTime>0)cpus.Add(timings[0].cpuFrameTime);if(timings[0].gpuFrameTime>0)gpus.Add(timings[0].gpuFrameTime);}
     if(draw.Valid){draws+=draw.LastValue;samples++;}if(triangles.Valid)tris+=triangles.LastValue;if(mem.Valid)memory=Math.Max(memory,mem.LastValue);
    }
    frames.Sort();double P(float q)=>frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt((frames.Count-1)*q))];string Mean(List<double> v)=>v.Count>0?v.Average().ToString("F2",System.Globalization.CultureInfo.InvariantCulture):"unavailable";
    string line=FormattableString.Invariant($"{room.RoomId},{quality},{frames.Count},{frames.Average():F2},{P(.5f):F2},{P(.95f):F2},{P(.99f):F2},{frames.Last():F2},{Mean(cpus)},{Mean(gpus)},{(samples>0?draws/samples:-1)},{(samples>0?tris/samples:-1)},{memory/1048576}");
    log.AppendLine(line);File.WriteAllText(Path.Combine(output,"standalone-progress.txt"),log.ToString());
    File.AppendAllText(Path.Combine(output,"measurement-config.txt"),$"{room.RoomId} q={quality}, GC generations collected={GC.CollectionCount(0)-gc0}/{GC.CollectionCount(1)-gc1}/{GC.CollectionCount(2)-gc2}\n");
    if(slow.Count>0)File.AppendAllLines(Path.Combine(output,"slow-frames.txt"),slow);
   }
   finally{draw.Dispose();triangles.Dispose();mem.Dispose();}
  }
 }
}
