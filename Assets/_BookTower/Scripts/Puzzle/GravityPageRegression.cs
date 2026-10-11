using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
namespace BookTower.Puzzles
{
    public sealed partial class GravityPageBuildProbe
    {
        Keyboard qaKeys;Mouse qaMouse;
        void IsolateInput(){qaKeys=InputSystem.AddDevice<Keyboard>();qaMouse=InputSystem.AddDevice<Mouse>();qaKeys.MakeCurrent();qaMouse.MakeCurrent();}
        void RestoreInput(){if(qaKeys!=null)InputSystem.RemoveDevice(qaKeys);if(qaMouse!=null)InputSystem.RemoveDevice(qaMouse);}
        IEnumerator Measure(Room room,PlayerController player,PaperCamera camera)
        {
            var originalSettings = PaperSettingsRuntime.Values.Copy();
            var renderer = Camera.main.GetUniversalAdditionalCameraData().scriptableRenderer as UniversalRenderer;
            var originalDepthMode = renderer.depthPrimingMode;
            try
            {
                yield return MeasureSample(room, camera, "normal");
                if (Environment.GetCommandLineArgs().Contains("-gravityMSAAReference"))
                    yield return MeasureSample(room, camera, "msaa4");
                if (Environment.GetCommandLineArgs().Contains("-gravityQualityAblation"))
                {
                    yield return MeasureSample(room, camera, "msaa4");
                    yield return MeasureSample(room, camera, "msaa2");
                    yield return MeasureSample(room, camera, "smaa-depth");
                }
                if (!Environment.GetCommandLineArgs().Contains("-gravityRenderAblation")) yield break;
                var atmosphere = BookTower.Presentation.PaperAtmosphereDirector.Instance;
                if (atmosphere)
                {
                    bool original = atmosphere.effectsEnabled;
                    atmosphere.effectsEnabled = false;
                    try { yield return MeasureSample(room, camera, "no-air"); }
                    finally { atmosphere.effectsEnabled = original; }
                }
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                var shadows = lights.Select(l => l.shadows).ToArray();
                try
                {
                    foreach (var light in lights) light.shadows = LightShadows.None;
                    yield return MeasureSample(room, camera, "no-shadows");
                }
                finally { for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].shadows = shadows[i]; }
            }
            finally
            {
                PaperSettingsRuntime.Instance.Apply(originalSettings, false);
                renderer.depthPrimingMode = originalDepthMode;
            }
        }
        IEnumerator MeasureSample(Room room, PaperCamera camera, string mode)
        {
            var settings=PaperSettingsRuntime.Values.Copy();settings.vsync=false;settings.fps=-1;settings.quality=1;settings.autoCamera=false;
            PaperSettingsRuntime.Instance.Apply(settings,false);camera.SetAim(0,-14);
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var renderer = Camera.main.GetUniversalAdditionalCameraData().scriptableRenderer as UniversalRenderer;
            if (mode == "msaa4") pipeline.msaaSampleCount = 4;
            if (mode == "msaa2") pipeline.msaaSampleCount = 2;
            if (mode == "smaa-depth")
            {
                pipeline.msaaSampleCount = 1;
                renderer.depthPrimingMode = DepthPrimingMode.Forced;
            }
            yield return new WaitForSecondsRealtime(3);
            var frames=new List<float>(4096);var cpu=new List<double>(4096);var gpu=new List<double>(4096);
            using var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            using var memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
            using var allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            long calls=0,maxMemory=0,allocated=0;var timing=new FrameTiming[1];
            float start=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-start<15)
            {
                FrameTimingManager.CaptureFrameTimings();yield return null;
                frames.Add(Time.unscaledDeltaTime*1000);
                if(draw.Valid)calls+=draw.LastValue;if(memory.Valid)maxMemory=Math.Max(maxMemory,memory.LastValue);
                if(allocations.Valid)allocated+=allocations.LastValue;
                if(FrameTimingManager.GetLatestTimings(1,timing)>0){if(timing[0].cpuFrameTime>0)cpu.Add(timing[0].cpuFrameTime);if(timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);}
            }
            frames.Sort();
            log.AppendLine($"PERF {room.RoomId} [{mode}]: {Screen.width}x{Screen.height}, q=1, uncapped, {frames.Count} frames/15s; mean={frames.Average():F2} ms, p95={frames[(int)((frames.Count-1)*.95f)]:F2} ms, drawcalls={calls/Math.Max(1,frames.Count)}, peak memory={maxMemory/1048576} MiB; CPU={(cpu.Count>0?cpu.Average().ToString("F2"):"unavailable")}, GPU={(gpu.Count>0?gpu.Average().ToString("F2"):"unavailable")}");Flush();
            yield return CaptureRenderedFrame(room.RoomId+"-perf-"+mode);
            log.AppendLine($"GC {room.RoomId} [{mode}]: {(allocations.Valid?(allocated/Math.Max(1,frames.Count)).ToString():"unavailable")} bytes/frame");Flush();
            if(mode=="normal")
            {
                var meshes=room.GetComponentsInChildren<MeshRenderer>();
                Check(!room.GetComponentsInChildren<GravityPagePuzzle>().SelectMany(p=>p.GetComponentsInChildren<MeshRenderer>()).Any(r=>r.isPartOfStaticBatch), room.RoomId+" puzzle rendering remains dynamic");
                log.AppendLine($"BATCH {room.RoomId}: {meshes.Count(r=>r.isPartOfStaticBatch)} / {meshes.Length}");Flush();
            }
        }
        IEnumerator CaptureRenderedFrame(string name)
        {
            // Capture the actual backbuffer, including its MSAA/SMAA configuration.
            // Rendering into a separate single-sample texture would invalidate AA comparisons.
            yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, name+".png"), frame.EncodeToPNG()); }
            finally { Destroy(frame); }
        }
        IEnumerator VerifyPassages(PlayerController player,PaperCamera camera)
        {
            var link=FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");
            foreach(bool reverse in new[]{false,true})
            {
                var a=reverse?link.ToPoint:link.FromPoint;var b=reverse?link.FromPoint:link.ToPoint;
                Place(player,a+Vector3.up*.25f);yield return null;bool pass=true;
                for(int i=1;i<=220;i++)
                {
                    var target=Vector3.Lerp(a,b,i/220f)+Vector3.up*.25f;player.Controller.Move(target-player.transform.position);
                    if(Vector3.Distance(player.transform.position,target)>.75f){pass=false;break;}
                    if(i%20==0)yield return null;
                }
                Check(pass,"F0–1A real capsule passage, reverse="+reverse);
            }
            var center=new[]{new Vector3(136.75f,7,-138),new Vector3(140.6f,8.02f,-133.6f),new Vector3(142.5f,8.15f,-132),new Vector3(150,8.15f,-129),new Vector3(158,8.15f,-126),new Vector3(160,8.15f,-124),new Vector3(160,8.15f,-120.8f),new Vector3(160,8.15f,-117.5f)};
            var side=new[]{new Vector3(.752f,0,-.659f),new Vector3(.752f,0,-.659f),new Vector3(.45f,0,-.90f),new Vector3(.351f,0,-.936f),new Vector3(.546f,0,-.838f),new Vector3(.924f,0,-.383f),Vector3.right,Vector3.right};
            foreach(float lane in new[]{-1.5f,0,1.5f})foreach(bool reverse in new[]{false,true})
            {
                var path=center.Select((p,i)=>p+side[i]*lane+Vector3.up*.25f).ToArray();if(reverse)Array.Reverse(path);
                Place(player,path[0]);yield return null;bool pass=true;
                for(int s=1;s<path.Length;s++)
                {
                    for(int i=1;i<=120;i++){var target=Vector3.Lerp(path[s-1],path[s],i/120f);player.Controller.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.55f){pass=false;break;}}
                    yield return null;if(!pass)break;
                }
                Check(pass,$"1A–1B real capsule passage lane={lane}, reverse={reverse}");
            }
            foreach(var id in new[]{"F0","1A","1B"})
            {
                var room=FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId==id);
                var puzzle=room.GetComponentInChildren<GravityPagePuzzle>();var chamber=puzzle.transform.Find("기록 운반실");
                var chamberRenderer=chamber.GetComponentsInChildren<Renderer>().First(r=>r.name=="두꺼운 제본 뒷벽");
                float x=id=="F0"?room.transform.position.x+6.9f:chamberRenderer.bounds.max.x+1.3f;float floor=id=="F0"?3:room.FloorY;
                float z=id=="F0"?puzzle.transform.TransformPoint(puzzle.completedPageHinge).z:chamber.position.z;
                var a=new Vector3(x,floor+.2f,z-3);var b=new Vector3(x,floor+.2f,z+4);
                Place(player,a);yield return null;bool pass=true;
                for(int i=1;i<=150;i++){var target=Vector3.Lerp(a,b,i/150f);player.Controller.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.65f){pass=false;break;}}
                Check(pass,id+" new return aisle capsule clearance, actual="+player.transform.position);
                camera.SetAim(180,0);yield return new WaitForSecondsRealtime(.5f);Capture(id+"-return-aisle");
            }
        }
        static void Place(PlayerController player,Vector3 at){player.Controller.enabled=false;player.transform.position=at;player.Controller.enabled=true;Physics.SyncTransforms();}
    }
}
