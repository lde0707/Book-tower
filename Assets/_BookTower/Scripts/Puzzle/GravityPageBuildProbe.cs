using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using BookTower.Core;
using BookTower.UI;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;

namespace BookTower.Puzzles
{
    // Explicit QA entry only; no probe object or shortcuts exist in ordinary play.
    public sealed partial class GravityPageBuildProbe : MonoBehaviour
    {
        public static Action<bool> Finished;
        public string output;
        readonly StringBuilder log=new();
        int failures;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {
            if(Environment.GetCommandLineArgs().Contains("-gravityQA")||Environment.GetCommandLineArgs().Contains("BookTower.EditorTools.GravityPageReview.Run"))
                PaperSettingsRuntime.TestStoragePath=Path.Combine(Application.temporaryCachePath,"gravity-qa-settings.json");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-gravityQA");
            bool editorReview=args.Contains("BookTower.EditorTools.GravityPageReview.Run");
            if(i<0&&!editorReview)return;
            Application.runInBackground=true;
            var probe=new GameObject("Gravity QA only").AddComponent<GravityPageBuildProbe>();
            probe.output=editorReview?Path.GetFullPath("Docs/ArtReview/GravityImplemented/Editor"):i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"GravityQA");
            Debug.Log("[Gravity QA] runtime installed at "+probe.output);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;
            VerifyVisibilityContract();
            VerifySettingsContract();
            IsolateInput();
            yield return null;yield return null;
            yield return VerifyWallSliding();
            if (Environment.GetCommandLineArgs().Contains("-gravityRailOnly"))
            {
                log.AppendLine($"RESULT: {(failures == 0 ? "PASS" : "FAIL")}; failures={failures}"); Flush();
                Application.logMessageReceived -= OnLog; RestoreInput();
                if (Finished != null) Finished(failures == 0);
                else Application.Quit(failures == 0 ? 0 : 1);
                yield break;
            }
            var ui=PaperUI.Instance;var story=FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();
            if(story){if(story.companion)story.companion.transform.SetParent(null,true);story.enabled=false;}
            if(ui){ui.EndStory();ui.Resume();}GameFlow.EnsureExists().Resume();
            var player=FindFirstObjectByType<PlayerController>();player.enabled=false;
            var camera=FindFirstObjectByType<PaperCamera>();
            var rooms=FindObjectsByType<Room>(FindObjectsSortMode.None);
            var puzzles=FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None).OrderBy(p=>Array.IndexOf(new[]{"F0","1A","1B","1C","1D","1E","2A","2B","2C","2D","2E","2F","3A","3B","3C","HD"},p.GetComponentInParent<Room>().RoomId)).ToArray();
            Check(puzzles.Length==16,"16 replacement puzzles present");
            Check(!FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).Any(p=>p is not GravityPagePuzzle&&p.enabled),"no active legacy puzzle");
            PaperSettingsRuntime.Instance.Apply(new PaperSettings{autoCamera=false},false);
            bool performanceOnly = Environment.GetCommandLineArgs().Contains("-gravityPerfOnly");
            foreach(var puzzle in puzzles)
            {
                var room=puzzle.GetComponentInParent<Room>();string id=room.RoomId;
                if (performanceOnly && id is not ("F0" or "1A" or "1B")) continue;
                var control=puzzle.GetComponentsInChildren<GravityPageControl>().First(c=>c.command==0);
                var at=control.transform.position+new Vector3(0,-1.4f,-3f);
                at.y=(id=="F0"?3:room.FloorY)+.2f;
                player.Controller.enabled=false;player.transform.position=at;player.Controller.enabled=true;
                Physics.SyncTransforms();camera.SetAim(0,-14);camera.ReturnToFollow();
                GameFlow.Instance.Resume();Time.timeScale=1;
                yield return new WaitForSecondsRealtime(.8f);
                room.SetVisible(true);
                int cargoMask = 1 << LayerMask.NameToLayer(GravityPageRailPhysics.CargoLayer);
                Check(puzzle.cargo.All(b => b.GetComponent<Collider>().gameObject.layer == LayerMask.NameToLayer(GravityPageRailPhysics.CargoLayer)), id + " cargo uses dedicated rail collision layer");
                var railMeshes = puzzle.GetComponentsInChildren<MeshCollider>().Where(c => c.name == "고정 선반").ToArray();
                Check(railMeshes.Length > 0 && railMeshes.All(c => c.enabled && (c.excludeLayers.value & cargoMask) != 0 &&
                    c.GetComponent<BoxCollider>().enabled && c.GetComponent<BoxCollider>().excludeLayers.value == ~cargoMask),
                    id + " flat rails affect only cargo; fitted shelf meshes remain for player");
                if(id is "F0" or "1A" or "1B" or "1C" or "HD")Capture(id+"-start");
                Check(!control.TryUse(control.transform.position+Vector3.one*100),id+" rejects out-of-range interaction");
                var initial=puzzle.cargo.Select(b=>b.position).ToArray();
                Check(puzzle.SetDirection(2),id+" accepts direction");
                yield return new WaitForSecondsRealtime(.12f);
                Check(!puzzle.SetDirection(1),id+" disallows timing-dependent mid-flight change");
                GameFlow.Instance.Pause();var paused=puzzle.cargo.Select(b=>b.position).ToArray();
                yield return new WaitForSecondsRealtime(.12f);
                Check(puzzle.cargo.Select((b,i)=>Vector3.Distance(b.position,paused[i])<.001f).All(v=>v),id+" pause freezes cargo");
                GameFlow.Instance.Resume();
                Check(puzzle.Undo(),id+" undo during movement");
                yield return new WaitForFixedUpdate();yield return null;
                Check(puzzle.cargo.Select((b,i)=>Vector3.Distance(b.position,initial[i])<.001f).All(v=>v),id+" undo restores positions");
                Check(puzzle.cargo.Select((b,i)=>Vector3.Distance(b.transform.position,initial[i])<.001f).All(v=>v),id+" undo survives interpolation and physics tick");
                Time.timeScale=4;
                foreach(int direction in puzzle.verificationRoute)
                {
                    Check(puzzle.SetDirection(direction),id+" route direction "+direction);
                    float deadline=Time.realtimeSinceStartup+16;
                    while(puzzle.Moving&&Time.realtimeSinceStartup<deadline)yield return null;
                    if(puzzle.Moving){Check(false,id+" motion failed to settle");break;}
                    log.AppendLine(id+" step "+direction+": "+string.Join("; ",puzzle.cargo.Select(b=>b.transform.localPosition.ToString("F2"))));
                    foreach(var body in puzzle.cargo)
                    {
                        var box=body.GetComponent<BoxCollider>();
                        foreach(var hit in Physics.OverlapBox(body.position,box.size*.5f+Vector3.one*.02f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                            if(!hit.transform.IsChildOf(puzzle.transform))log.AppendLine("FOREIGN CONTACT "+id+" "+body.name+" => "+PathOf(hit.transform)+" "+hit.bounds);
                    }
                    Flush();yield return null;
                }
                yield return new WaitForSecondsRealtime(.15f);
                Check(puzzle.State==PuzzleBase.PuzzleState.Solved,id+" actual physics solves all destinations: "+string.Join(",",puzzle.cargo.Select((b,i)=>Vector3.Distance(b.position,puzzle.destinations[i].position).ToString("F3"))));
                Time.timeScale=1;yield return new WaitForSecondsRealtime(1f);
                if(id is "F0" or "1A" or "1B" or "1C" or "HD")Capture(id+"-solved");
                if(id=="F0")
                {
                    yield return new WaitForSecondsRealtime(4f);
                    var door=room.GetComponentInChildren<BindingDoor>();Check(door&&door.IsOpen,"F0 preserved binding door opens from gravity solve");
                    camera.SetAim(32,5);camera.LockObservation(1);yield return new WaitForSecondsRealtime(2f);
                    Check(Mathf.Abs(Mathf.DeltaAngle(camera.YawDegrees,90))<.1f,"E keeps cardinal camera snap, actual="+camera.YawDegrees);camera.ConsumeMovementYaw();
                    Check(!camera.IsObservationLocked,"movement releases camera observation");
                }
                if(id=="1B")
                {
                    var settings=PaperSettingsRuntime.Values.Copy();settings.quality=0;settings.reduceStoryMotion=true;
                    PaperSettingsRuntime.Instance.Apply(settings,false);yield return new WaitForSecondsRealtime(.5f);Capture("1B-low-reduced-motion");
                    PaperSettingsRuntime.Instance.Apply(new PaperSettings{autoCamera=false},false);
                }
                if(!Application.isEditor&&(id is "F0" or "1A" or "1B"))yield return Measure(room,player,camera);
            }
            Time.timeScale=1;
            if (!performanceOnly)
            {
                yield return VerifyStructureCollision(player,camera);
                yield return VerifyPassages(player,camera);
            }
            else log.AppendLine("PERFORMANCE ONLY: full collision and passage regression skipped.");
            log.AppendLine($"Machine: {SystemInfo.processorType}; {SystemInfo.graphicsDeviceName}; Unity {Application.unityVersion}; editor={Application.isEditor}");
            log.AppendLine($"RESULT: {(failures==0?"PASS":"FAIL")}; failures={failures}. Visual quality and first-time enjoyment require separate review.");Flush();
            Application.logMessageReceived-=OnLog;
            RestoreInput();
            if(Finished!=null)Finished(failures==0);
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(failures==0?0:1);
#else
                Application.Quit(failures==0?0:1);
#endif
            }
        }
        void OnLog(string message,string stack,LogType type){if(type is LogType.Exception or LogType.Error){failures++;log.AppendLine("RUNTIME ERROR: "+message+"\n"+stack);Flush();}}
        void Check(bool condition,string text){if(!condition)failures++;log.AppendLine((condition?"PASS ":"FAIL ")+text);Flush();}
        void Flush()=>File.WriteAllText(Path.Combine(output,"checks.txt"),log.ToString());
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        void Capture(string name)
        {
            var cam=Camera.main;var rt=RenderTexture.GetTemporary(1600,900,24);var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;Texture2D tex=null;
            try{cam.targetTexture=rt;cam.aspect=16f/9;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());}
            finally{cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;if(tex)Destroy(tex);RenderTexture.ReleaseTemporary(rt);}
        }
    }
}
