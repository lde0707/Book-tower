using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    [InitializeOnLoad] public static partial class WorkshopVerifier
    {
        const string Flag="WorkshopVerify",Out=StorybookRoomBuilder.WorkshopOut;
        static IEnumerator routine;static float next;static int frame;static double start;
        static PlayerController player;static Room room;static StorybookLightPuzzle puzzle;
        static readonly StringBuilder report=new();static Keyboard keyboard;static Mouse mouse;static InputSettings original;
        static WorkshopVerifier(){EditorApplication.playModeStateChanged+=Changed;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateStorage(){if(SessionState.GetBool(Flag,false))BookTower.UI.PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/workshop-test-settings.json");}
        public static void Run(){SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void RunBatch(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Structure();Run();}
        public static void Structure()
        {
            Physics.SyncTransforms();var r=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(x=>x.RoomId=="1A");int count=0;
            for(int x=-30;x<32;x+=2)for(int z=-26;z<28;z+=2)
            {var p=r.transform.TransformPoint(new Vector3(x,.6f,z));if(!Physics.Raycast(p,Vector3.down,1.5f))throw new Exception("1A floor gap "+p);if(!Physics.Raycast(p,Vector3.up,13))throw new Exception("1A roof gap "+p);count+=2;}
            var t=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(x=>x.name=="이음새 없는 통로 외피");
            var roof=t.Find("연속 통로 천장").GetComponent<BoxCollider>();float length=roof.size.z-4;
            for(float z=-length*.5f;z<=length*.5f;z+=.5f)foreach(float y in new[]{1f,3f,5f})
            {var p=t.TransformPoint(new Vector3(0,y,z));foreach(var dir in new[]{t.up,-t.right,t.right}){if(!Physics.Raycast(p,dir,10))throw new Exception("Passage envelope gap "+p);count++;}}
            if(!r.GetComponentInChildren<StorybookLightPuzzle>())throw new Exception("New P2 missing");
            foreach(var m in r.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).Where(m=>m).Distinct())foreach(var v in m.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.sqrMagnitude))throw new Exception("Invalid mesh vertex "+m.name);
            if(r.GetComponentsInChildren<BookTower.UI.ReadableNote>().Count(n=>n.id=="1A.Book.Lamp")!=1)throw new Exception("Duplicate exit clue book");
            File.WriteAllText(Out+"/structure-check.txt","PASS: "+count+" sampled floor/roof/corridor rays. Decorative and continuous opaque shells coexist. Finite sampling is not an exhaustive proof.\n");
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){Application.targetFrameRate=60;report.Clear();var s=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(s)s.gameObject.SetActive(false);routine=Verify();start=EditorApplication.timeSinceStartup;next=0;frame=-1;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+"Code",1));}
        }
        static void Tick(){try{if(EditorApplication.timeSinceStartup-start>240)throw new Exception("Timeout");if(frame==Time.frameCount||Time.time<next)return;frame=Time.frameCount;if(!routine.MoveNext()){Finish(null);return;}next=Time.time+(routine.Current is float f?f:0);}catch(Exception e){Finish(e);}}
        static void Finish(Exception error){EditorApplication.update-=Tick;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(original)InputSystem.settings=original;report.AppendLine(error==null?"PASS: workshop play verification":"FAIL: "+error);File.WriteAllText(Out+"/play-check.txt",report.ToString());Debug.Log(report.ToString());SessionState.SetInt(Flag+"Code",error==null?0:1);EditorApplication.ExitPlaymode();}
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);report.AppendLine("PASS: "+text);}
        static Vector3 W(float x,float z)=>room.transform.TransformPoint(new Vector3(x,.2f,z));
        static void Teleport(Vector3 p){player.Controller.enabled=false;player.transform.position=p;player.Controller.enabled=true;player.VerticalVelocity=0;Physics.SyncTransforms();}
        static IEnumerator Walk(Vector3 target){float deadline=Time.time+25;while(Vector2.Distance(new(player.transform.position.x,player.transform.position.z),new(target.x,target.z))>.4f){if(Time.time>deadline)throw new Exception("Blocked "+player.transform.position+" toward "+target);var d=target-player.transform.position;d.y=0;player.Controller.Move(Vector3.ClampMagnitude(d,7*Time.deltaTime)+Vector3.down*3*Time.deltaTime);if(player.transform.position.y<-1)throw new Exception("Fell through floor");yield return 0f;}}
        static StorybookLightControl C(StorybookLightControl.Action a)=>room.GetComponentsInChildren<StorybookLightControl>().First(c=>c.action==a);
        static void Use(StorybookLightControl.Action a,bool reverse=false){var c=C(a);Check(c.TryUse(c.transform.position,reverse),"Interact "+a+(reverse?" backwards":""));}
        static IEnumerator Verify()
        {
            if(Object.FindFirstObjectByType<StorybookLightPuzzle>().realShadows){var real=VerifyReal();while(real.MoveNext())yield return real.Current;yield break;}
            yield return 1.5f;player=Object.FindFirstObjectByType<PlayerController>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(x=>x.RoomId=="1A");puzzle=room.GetComponentInChildren<StorybookLightPuzzle>();
            original=InputSystem.settings;var settings=Object.Instantiate(original);settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
            var p1=Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).First(p=>p.PuzzleId=="P1");p1.Solve();yield return 1.5f;
            Teleport(new Vector3(30,.25f,-208));
            foreach(var at in new[]{new Vector3(48,2.3f,-200.8f),new Vector3(68,4.25f,-192.8f),W(-25,-10),W(-18,-18),W(-8,-11),W(-8,-2),W(1,3),W(8,6),W(8,14)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}
            Check(player.transform.position.y>3.7f,"Covered passage and both arches traversed without falling");
            var basePuzzle=puzzle.GetComponent<FoldCombinationPuzzle>();basePuzzle.Activate();
            Use(StorybookLightControl.Action.BodyRail);Use(StorybookLightControl.Action.EarRail);yield return .2f;Check(!puzzle.BodyInstalled&&!puzzle.EarsInstalled,"Cannot install pieces before discovering them");
            Check(!C(StorybookLightControl.Action.Lamp).TryUse(W(28,-25)),"Remote interaction rejected");
            // Drive a real F press through retargeting, not only public puzzle methods.
            var lamp=C(StorybookLightControl.Action.Lamp);Teleport(lamp.transform.position+Vector3.back*2);yield return .3f;
            var camera=Object.FindFirstObjectByType<PaperCamera>();camera.enabled=false;Camera.main.transform.position=player.transform.position+Vector3.up*.7f;Camera.main.transform.LookAt(lamp.transform.position);yield return .25f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .2f;
            Check(puzzle.LampOn,"Actual F input opens the flower lantern");
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return .2f;
            Check(!puzzle.LampOn,"Actual mouse click closes the same lantern once");camera.enabled=true;
            Use(StorybookLightControl.Action.FindBody);Use(StorybookLightControl.Action.FindEars);Use(StorybookLightControl.Action.BodyRail);Use(StorybookLightControl.Action.EarRail);Use(StorybookLightControl.Action.Lamp);yield return 2f;
            Check(!puzzle.Complete,"Finding/installing everything is insufficient at wrong distances");
            for(int i=0;i<4;i++)Use(StorybookLightControl.Action.BodyRail);Use(StorybookLightControl.Action.EarRail);Use(StorybookLightControl.Action.EarRail);yield return 2f;
            Check(!puzzle.Complete&&Mathf.Abs(puzzle.BodyRatio-1.5f)<.001f&&Mathf.Abs(puzzle.EarRatio-3)<.001f,"Correct scale still requires asymmetric ear orientation");
            Use(StorybookLightControl.Action.BodyRail,true);yield return .5f;Check(puzzle.BodyStep==3,"Shift direction reverses rail movement");Use(StorybookLightControl.Action.BodyRail);Use(StorybookLightControl.Action.Flip);Use(StorybookLightControl.Action.Lamp);yield return 2f;Check(!puzzle.Complete,"Correct geometry without light cannot solve");
            Use(StorybookLightControl.Action.Lamp);yield return 3f;
            report.AppendLine("AUDIT: "+puzzle.Audit+" P2="+basePuzzle.State);
            Check(puzzle.Complete&&basePuzzle.State==PuzzleBase.PuzzleState.Solved,"Distance, orientation and sustained light solve the original P2 and its gate");
            Check(Vector3.Distance(puzzle.bodyShadow.localScale,new Vector3(1.5f,1.5f,1))<.01f&&Vector3.Distance(puzzle.earShadow.localScale,new Vector3(3,3,1))<.01f,"Visual projection agrees with solved geometry");
            Teleport(W(3,14));camera.SetAim(-20,-3);yield return 1f;InteriorF0Builder.Capture(Camera.main,Out+"/1A-play-solved.png");
            foreach(var at in new[]{W(12,18),W(18,22),W(24.5f,23)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}
            yield return .3f;Check(Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1B").IsVisible,"Next room is visible through the exit before reaching the ramp");
            {var e=Walk(W(26.5f,30));while(e.MoveNext())yield return e.Current;}
            Check(player.transform.position.y>3.7f,"Solved exit reaches the existing 1B connection");
        }
    }
}

