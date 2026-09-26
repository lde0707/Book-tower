using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Interaction;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class DirectionPlayVerifier
    {
        const string Flag="DirectionPlayVerifier.Pending", Output="Docs/ArtReview/Direction";
        static IEnumerator routine;
        static float next;
        static int lastFrame;
        static double started;
        static readonly StringBuilder report=new();
        static PlayerController player;
        static Keyboard keyboard;
        static InputSettings originalInputSettings;

        [InitializeOnLoadMethod]
        static void Init() => EditorApplication.playModeStateChanged += Changed;

        // Batch invocation must omit -quit; the verifier exits after returning to Edit Mode.
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            SessionState.SetBool(Flag,true);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                report.Clear(); started=EditorApplication.timeSinceStartup; next=0; lastFrame=-1;
                Application.targetFrameRate=60; routine=Verify(); EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Flag,false);
                EditorApplication.Exit(SessionState.GetInt(Flag+".Exit",1));
            }
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>150)throw new Exception("Play verification timeout");
                if(Time.frameCount==lastFrame||Time.time<next)return;
                lastFrame=Time.frameCount;
                if(!routine.MoveNext()){Finish(null);return;}
                next=Time.time+(routine.Current is float wait?wait:0);
            }
            catch(Exception error){Finish(error);}
        }
        static void Finish(Exception error)
        {
            EditorApplication.update-=Tick;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalInputSettings!=null)InputSystem.settings=originalInputSettings;
            report.AppendLine(error==null?"PASS: automated Play Mode checks completed.":"FAIL: "+error);
            File.WriteAllText(Output+"/play-verification.txt",report.ToString());
            Debug.Log(report.ToString());
            SessionState.SetInt(Flag+".Exit",error==null?0:1);
            EditorApplication.ExitPlaymode();
        }
        static void Require(bool ok,string message)
        {if(!ok)throw new Exception(message);report.AppendLine("PASS: "+message);}
        static Room Room(string id)=>Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId==id);
        static PuzzleBase Puzzle(string id)=>Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).Single(p=>p.PuzzleId==id);
        static void Teleport(Vector3 point)
        {
            player.Controller.enabled=false;player.transform.position=point;player.Controller.enabled=true;
            Physics.SyncTransforms();
        }
        static IEnumerator Walk(Vector3 target)
        {
            float end=Time.time+12;
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z))>.15f)
            {
                if(Time.time>end)throw new Exception("Walk blocked at "+player.transform.position+" toward "+target+"; nearby: "+string.Join(", ",Physics.OverlapSphere(player.transform.position+Vector3.up*.5f,1.2f).Select(c=>c.name)));
                Vector3 delta=target-player.transform.position;delta.y=0;
                player.Controller.Move(Vector3.ClampMagnitude(delta,4.5f*Time.deltaTime)+Vector3.down*4*Time.deltaTime);
                if(player.transform.position.y<target.y-.35f)throw new Exception("Fell during crossing at "+player.transform.position);
                yield return 0f;
            }
        }
        static IEnumerator Verify()
        {
            yield return 1f;
            player=Object.FindFirstObjectByType<PlayerController>();
            var startRoom=Room("F0");
            Require(startRoom.Contains(player.transform.position),"Play starts with player inside F0 at "+player.transform.position);
            Require(player.transform.position.y>=startRoom.transform.position.y-.2f,"Player remains on the F0 floor after one second");
            Require(Physics.Raycast(player.transform.position+Vector3.up*.2f,Vector3.down,2f),"F0 start has a floor collider below the player");
            Require(startRoom.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"F0 map renderers are visible at Play start");
            player.enabled=false;
            originalInputSettings=InputSystem.settings;
            InputSystem.settings=Object.Instantiate(originalInputSettings);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>();
            var hud=Object.FindFirstObjectByType<BookTower.Presentation.PrototypeHud>();
            var hudData=new SerializedObject(hud);hudData.FindProperty("visible").boolValue=false;hudData.ApplyModifiedPropertiesWithoutUndo();
            var first=Room("1A"); var p2=first.GetComponentsInChildren<FoldPanel>().Single(p=>p.name=="P2_접이면");
            Teleport(first.transform.TransformPoint(new Vector3(-8,.12f,-5.5f)));yield return .6f;
            player.transform.LookAt(new Vector3(p2.transform.position.x,player.transform.position.y,p2.transform.position.z));yield return .3f;
            Require(TargetInteractor.Current==p2.GetComponent<Interactable>(),"1A F-key selects the intended fold");
            Capture("P2-runtime-closed");
            for(int i=0;i<2;i++)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .25f;
            }
            yield return 2f;
            Require(Puzzle("P2").State==PuzzleBase.PuzzleState.Solved && p2.StateIndex==2,"1A solves through two normal F inputs and opens entry to 1B (state="+p2.StateIndex+", phase="+BookTower.Core.GameFlow.Instance.Phase+")");
            Capture("P2-runtime-solved");
            var room=Room("1B");var panel=room.GetComponentsInChildren<FoldPanel>().Single();
            var puzzle=Puzzle("P3");var bridge=room.GetComponentInChildren<SolveBridge>();
            var near=room.transform.TransformPoint(new Vector3(-4,.12f,4.2f));
            var entry=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).Single(r=>r.ToRoomId=="1B");
            Vector3 entryPoint=new SerializedObject(entry).FindProperty("toPoint").vector3Value;
            Teleport(entryPoint+Vector3.up*.05f);yield return .5f;
            foreach(var local in new[]{new Vector3(-11.2f,.12f,8.8f),new Vector3(-4,.12f,8.8f),new Vector3(-4,.12f,4.2f)})
            {var walk=Walk(room.transform.TransformPoint(local));while(walk.MoveNext())yield return walk.Current;}
            Require(!bridge.GetComponent<Collider>().enabled,"P3 bridge collision absent before solving");
            Require(panel.StateIndex==0 && Mathf.Abs(Quaternion.Angle(panel.transform.localRotation,Quaternion.AngleAxis(180,Vector3.left)))<1,"P3 closed page points away from gap");
            Require(Physics.GetIgnoreCollision(panel.GetComponentInChildren<Collider>(),player.Controller),"P3 page cannot be used as a physical shortcut");
            Require(Vector3.Distance(player.transform.position,panel.transform.position)<panel.GetComponent<Interactable>().Range,"P3 is in interaction range from the safe north lip");
            Capture("P3-runtime-closed");
            panel.SetState(FoldPanel.FoldState.Upright);yield return 1.2f;
            Require(puzzle.State!=PuzzleBase.PuzzleState.Solved,"P3 rejects state 2");
            panel.SetState(FoldPanel.FoldState.Over);yield return 1.2f;
            Require(puzzle.State!=PuzzleBase.PuzzleState.Solved,"P3 rejects state 3");
            panel.SetState(FoldPanel.FoldState.Closed);yield return 1.2f;
            player.transform.LookAt(new Vector3(panel.transform.position.x,player.transform.position.y,panel.transform.position.z));
            yield return .3f;
            Require(TargetInteractor.Current==panel.GetComponent<Interactable>(),"F-key selects P3 from entry side");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return 2.8f;
            Require(panel.StateIndex==1 && puzzle.State==PuzzleBase.PuzzleState.Solved,"F input reaches state 1 and normal P3 solve logic fires");
            Require(bridge.GetComponent<Collider>().enabled && bridge.IsOpen,"Projected bridge finishes rising and enables collision");
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            Vector3[] footprint=ShadowBridgeBuilder.Footprint(panel,panel.AngleAt(1),sun,room.FloorY+.1f);
            var leaf=panel.transform.Find("Panel");
            foreach(float x in new[]{-.4f,.4f})foreach(float z in new[]{-.4f,.4f})
            {
                Vector3 start=leaf.TransformPoint(new Vector3(x,.51f,z))-sun.transform.forward*.08f;
                var blocked=Physics.RaycastAll(start,-sun.transform.forward,30).Where(h=>h.collider.GetComponentInParent<Room>()==room)
                    .Where(h=>h.collider.GetComponentInParent<FoldPanel>()!=panel)
                    .Where(h=>h.collider.GetComponent<Renderer>() is Renderer r && r.enabled && r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off).ToArray();
                Require(blocked.Length==0,"Sun reaches P3 panel sample "+x+","+z+ (blocked.Length>0?" blocked by "+blocked[0].collider.name:""));
            }
            foreach(Vector3 vertex in bridge.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                Vector3 world=bridge.transform.TransformPoint(vertex); world.y=room.FloorY+.1f;
                if(!Inside(footprint,world))throw new Exception("Bridge extends outside real shadow footprint: "+world);
            }
            report.AppendLine("PASS: all bridge collision vertices lie within the directional shadow footprint.");
            Capture("P3-runtime-solved");
            var crossing=Walk(room.transform.TransformPoint(new Vector3(-4,.12f,-5)));
            while(crossing.MoveNext())yield return crossing.Current;
            Require(player.transform.position.z<room.transform.position.z-4.8f,"CharacterController crossed the shadow bridge without falling");

            var p15=(SurfaceAnamorphosis)Puzzle("P15");var beacon=p15.beacon;
            Require(!beacon.TryIgnite(beacon.transform.position),"Beacon rejects missing P13/P14 prerequisites");
            report.AppendLine("SETUP: P13/P14 solved directly to isolate P15; their full playthrough is not covered.");
            Puzzle("P13").Solve();Puzzle("P14").Solve();
            Require(!beacon.TryIgnite(beacon.transform.position+Vector3.right*20),"Beacon rejects out-of-range activation");
            var r3=Room("3A"); Teleport(r3.transform.TransformPoint(new Vector3(-6,.12f,10)));
            p15.panel.SetState(FoldPanel.FoldState.Upright);yield return 1.5f;
            Require(p15.Blend==0 && p15.State==PuzzleBase.PuzzleState.Locked,"P15 stays hidden/locked before lighting beacon");
            p15.panel.SetState(FoldPanel.FoldState.Closed);
            Teleport(beacon.transform.position+Vector3.back*2);yield return .5f;
            Require(beacon.TryIgnite(player.transform.position),"3C beacon lights after prerequisites and nearby interaction");
            Teleport(r3.transform.TransformPoint(new Vector3(-6,.12f,10)));yield return .6f;
            var cam=Object.FindFirstObjectByType<PaperCamera>();cam.FrameRoom(r3);cam.SetYawStep(0);
            yield return 1.2f;Capture("P15-runtime-scattered");
            cam.SetYawStep(1);yield return 1.2f;
            p15.panel.SetState(FoldPanel.FoldState.Upright);yield return 2.2f;
            Require(p15.Blend>.99f && p15.State!=PuzzleBase.PuzzleState.Solved,"P15 rejects wrong camera direction despite aligned fold");
            Capture("P15-runtime-wrong-yaw");
            p15.panel.SetState(FoldPanel.FoldState.Low);cam.SetYawStep(0);yield return 2f;
            Require(p15.State!=PuzzleBase.PuzzleState.Solved,"P15 rejects wrong fold at correct camera direction");
            p15.panel.SetState(FoldPanel.FoldState.Upright);yield return 2.2f;
            Require(p15.State==PuzzleBase.PuzzleState.Solved,"Production P15 solves through settled fold, beacon and actual camera alignment");
            float deviation=0;
            foreach(var part in p15.fragments)
                foreach(var v in part.GetComponent<MeshFilter>().sharedMesh.vertices)
                {
                    Vector3 p=r3.transform.InverseTransformPoint(part.TransformPoint(v));
                    deviation=Mathf.Max(deviation,Mathf.Min(Mathf.Abs(p.y-.115f),Mathf.Abs(p.y-10.01f),Mathf.Abs(p.z-22.1f)));
                }
            Require(deviation<.005f,"Aligned P15 vertices sit on floor, ceiling or north-wall surfaces");
            Capture("P15-runtime-aligned");
            report.AppendLine("LIMIT: automated input/physics and geometry verification, not a novice usability test; physical light backtracking and endings remain unfinished.");
        }
        static bool Inside(Vector3[] poly,Vector3 p)
        {
            float sign=0;
            for(int i=0;i<poly.Length;i++)
            {
                Vector3 a=poly[i],b=poly[(i+1)%poly.Length];float c=(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
                if(Mathf.Abs(c)<.005f)continue;if(sign==0)sign=Mathf.Sign(c);else if(Mathf.Sign(c)!=sign)return false;
            }
            return true;
        }
        static void Capture(string name)
        {
            var camera=Camera.main;var rt=new RenderTexture(1280,800,24);var previous=RenderTexture.active;
            camera.targetTexture=rt;camera.aspect=1.6f;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
            File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
