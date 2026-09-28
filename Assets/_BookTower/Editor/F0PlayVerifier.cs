using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.CameraSystem;
using BookTower.PlayerSystem;
using BookTower.Rooms;
using BookTower.Info;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static class F0PlayVerifier
    {
        const string Flag="F0ReferenceVerify",Output="Docs/ArtReview/InteriorRoomsV3";
        static IEnumerator routine;static double started;static float next;static int frame;
        static readonly StringBuilder report=new();static PlayerController player;static PaperCamera camera;static Room room;
        static Mouse testMouse;static Keyboard keyboard;static InputSettings originalSettings;
        [InitializeOnLoadMethod]static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        public static void CancelOwnReview()
        {
            if(!SessionState.GetBool(Flag,false))return;
            EditorApplication.update-=Tick;routine=null;
            if(testMouse!=null)InputSystem.RemoveDevice(testMouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalSettings!=null)InputSystem.settings=originalSettings;
            SessionState.SetBool(Flag,false);EditorApplication.ExitPlaymode();
        }
        public static void RunBindingBatch(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");BindingPlayBuilder.Apply();Run();}
        public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {Application.targetFrameRate=60;report.Clear();var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story)story.gameObject.SetActive(false);started=EditorApplication.timeSinceStartup;next=0;frame=-1;routine=Verify();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Flag,false);
                File.WriteAllText("Logs/f0-editor.result",SessionState.GetString(Flag+".result","FAIL unknown"));
                if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+".code",1));
            }
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>200)throw new Exception("Play verification timeout");
                if(Time.frameCount==frame||Time.time<next)return;frame=Time.frameCount;
                if(!routine.MoveNext()){Finish(null);return;}next=Time.time+(routine.Current is float delay?delay:0);
            }
            catch(Exception error){Finish(error);}
        }
        static void Finish(Exception error)
        {
            EditorApplication.update-=Tick;
            if(testMouse!=null)InputSystem.RemoveDevice(testMouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(originalSettings!=null)InputSystem.settings=originalSettings;
            string result=error==null?"PASS: F0 runtime and camera checks":"FAIL: "+error;
            report.AppendLine(result);Directory.CreateDirectory(Output);File.WriteAllText(Output+"/F0-play-verification.txt",report.ToString());Debug.Log(report.ToString());
            SessionState.SetInt(Flag+".code",error==null?0:1);SessionState.SetString(Flag+".result",result);EditorApplication.ExitPlaymode();
        }
        static void Require(bool value,string msg){if(!value)throw new Exception(msg);report.AppendLine("PASS: "+msg);}
        static Vector3 World(float x,float z)=>room.transform.TransformPoint(new Vector3(x,.08f,z));
        static void Teleport(Vector3 p)
        {player.Controller.enabled=false;player.transform.position=p;player.Controller.enabled=true;player.VerticalVelocity=0;player.HorizontalVelocity=Vector3.zero;Physics.SyncTransforms();}
        static IEnumerator Walk(Vector3 goal)
        {
            float limit=Time.time+15;
            while(Vector2.Distance(new(player.transform.position.x,player.transform.position.z),new(goal.x,goal.z))>.22f)
            {
                if(Time.time>limit)throw new Exception("Walking blocked at "+player.transform.position+" toward "+goal+" colliders: "+string.Join(",",Physics.OverlapSphere(player.transform.position+Vector3.up*.6f,.9f).Select(c=>c.name)));
                Vector3 d=goal-player.transform.position;d.y=0;player.Controller.Move(Vector3.ClampMagnitude(d,5.5f*Time.deltaTime)+Vector3.down*3*Time.deltaTime);
                RequireHeight();yield return 0f;
            }
        }
        static void RequireHeight(){if(player.transform.position.y<room.transform.position.y-.2f)throw new Exception("Player fell below room floor");}
        static IEnumerator Verify()
        {
            yield return 1.2f;
            player=Object.FindFirstObjectByType<PlayerController>();camera=Object.FindFirstObjectByType<PaperCamera>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            Require(player!=null&&camera!=null,"Player and camera exist");RequireHeight();Require(player.Controller.isGrounded,$"Player grounded after startup (position {player.transform.position}, state {player.StateName}, velocity {player.VerticalVelocity}, dt {Time.deltaTime})");
            if(room.GetComponentInChildren<FoldBridgePuzzle>()!=null){foreach(var wait in Enumerate(CheckFoldBridge()))yield return wait;yield break;}
            foreach(Transform t in room.transform)if(t.name!="F0_Gameplay"&&t.name!=InteriorF0Builder.RootName&&t.name!="F0_살아있는제본실")Require(!t.gameObject.activeSelf,"Legacy root inactive: "+t.name);
            camera.SetAim(0,-2);yield return .8f;InteriorF0Builder.Capture(Camera.main,Output+"/F0-play-start.png");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(35,-1),0),"Q from 35 degrees -> world 0");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(35,1),90),"E from 35 degrees -> world 90");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(0,-1),270),"Q wraps 0 -> 270");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(270,1),0),"E wraps 270 -> 0");
            camera.SetAim(35,10);camera.LockObservation(1);camera.LockObservation(1);yield return 1f;
            Require(Mathf.Abs(Mathf.DeltaAngle(camera.YawDegrees,180))<.1f,"Rapid E E reaches exact world 180");
            float yaw=camera.ConsumeMovementYaw();Require(!camera.IsObservationLocked&&Mathf.Abs(Mathf.DeltaAngle(yaw,180))<.01f,"Movement releases observation without movement-axis jump");
            camera.SetAim(0,10);camera.ApplyLookDelta(new Vector2(250,0));Require(Mathf.Abs(camera.AimYaw-30)<.01f,"Mouse delta rotates aim independently of spirit facing");
            originalSettings=InputSystem.settings;var testSettings=Object.Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;testSettings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=testSettings;Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            Teleport(World(0,-10));camera.SetAim(0,10);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return .65f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .35f;
            Vector3 moved=room.transform.InverseTransformPoint(player.transform.position);Require(moved.x>1.5f&&Mathf.Abs(moved.z+10)<.25f,$"D strafes right without camera/avatar feedback spiral ({moved}, aim {camera.AimYaw}, cursor {Cursor.lockState}, input {player.Input.Move}, phase {BookTower.Core.GameFlow.Instance.Phase})");
            if(room.GetComponentInChildren<BindingPlayground>()!=null)foreach(var wait in Enumerate(CheckBindingPlay()))yield return wait;
            camera.SetAim(0,10);Teleport(World(-9.5f,-2f));yield return .3f;
            var a=room.GetComponentsInChildren<CollectiblePiece>().First(p=>p.PieceId=="P1_a");
            // Verify the central ray can reach the pickup without the stand or player blocking it.
            var saved=Camera.main.transform.rotation;Camera.main.transform.LookAt(a.transform.position+Vector3.up*.15f);
            Require(InteractionRay.Cast(PaperCamera.AimRay(Camera.main),out var hit,100)&&hit.collider.GetComponentInParent<CollectiblePiece>()==a,"Center aim ray hits left collectible");Camera.main.transform.rotation=saved;
            Require(a.TryPickup(player.transform.position),"Left fragment pickup succeeds");
            foreach(var wait in Enumerate(Walk(World(-5,0))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(5,-4))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(9.8f,-6))))yield return wait;
            var b=room.GetComponentsInChildren<CollectiblePiece>().First(p=>p.PieceId=="P1_b");Require(b.TryPickup(player.transform.position),"Right fragment pickup succeeds");
            foreach(var wait in Enumerate(Walk(World(4,-5))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(4,11))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(0,11))))yield return wait;
            var spot=room.GetComponentInChildren<AssemblyPoint>();Require(spot.TryAssemble(player.transform.position),"Assembly request accepted");yield return 2.8f;
            var door=room.GetComponentInChildren<BindingDoor>();Require(door.IsOpen,"P1 opens modeled double-page door");
            Require(Mathf.Abs(Mathf.DeltaAngle(door.left.localEulerAngles.y,170))<.1f&&Mathf.Abs(Mathf.DeltaAngle(door.right.localEulerAngles.y,-170))<.1f,"Door folds into F0, clear of rear corridor");
            camera.SetAim(0,-3);yield return .8f;Directory.CreateDirectory("Docs/ArtReview/Envelope");InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/Envelope/F0-open.png");
            var binding=room.GetComponentInChildren<BindingPlayground>();if(binding)Require(binding.CelebrationPlayed,"Door solution triggers lasting binding celebration");
            foreach(var wait in Enumerate(Walk(World(3,18.3f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(20,18.3f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(20,11.2f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(26,11.2f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(30,12))))yield return wait;
            RequireHeight();Require(true,"Walked both fragments, assembly, open door and exit corridor without falling");
        }
        static IEnumerator CheckBindingPlay()
        {
            var touches=room.GetComponentsInChildren<BindingTouch>();Require(touches.Length==8,"Eight F0 touch points exist");
            var knot=touches.First(t=>t.kind==BindingTouch.TouchKind.PullKnot&&t.index==0);
            Require(!knot.TryUse(knot.transform.position+Vector3.one*20),"Distant interaction rejected");
            room.SetVisible(false);Require(!knot.TryUse(knot.transform.position),"Hidden-room interaction rejected");room.SetVisible(true);
            Teleport(knot.transform.position+new Vector3(0,-1,-2));camera.enabled=false;Cursor.lockState=CursorLockMode.Locked;
            Camera.main.transform.position=knot.transform.position+new Vector3(0,.2f,-2);Camera.main.transform.LookAt(knot.transform.position);yield return .35f;
            Require(TargetInteractor.Current==knot.GetComponent<Interactable>(),"Center aim targets pull handle");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .10f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .4f;
            Require(knot.IsLatched&&knot.UseCount==1,"F pulls handle exactly once");
            testMouse=InputSystem.AddDevice<Mouse>();testMouse.MakeCurrent();InputSystem.QueueStateEvent(testMouse,new MouseState{buttons=1});yield return .1f;InputSystem.QueueStateEvent(testMouse,new MouseState());yield return .4f;
            Require(!knot.IsLatched&&knot.UseCount==2,"Left click releases same handle exactly once");camera.enabled=true;
            foreach(var t in touches)
            {
                Require(t.TryUse(t.transform.position),"Touch responds: "+t.kind+" "+t.index);
            }
            yield return 2.5f;
            Require(touches.Where(t=>t.kind==BindingTouch.TouchKind.PullKnot).All(t=>t.IsLatched),"Three reversible latches settle");
            var dust=touches.First(t=>t.kind==BindingTouch.TouchKind.BrushDust);Require(dust.IsRevealed&&dust.dust.localScale.magnitude<.01f&&dust.stamp.localScale.x>.9f,"Dust clears and stamp persists");
            Require(dust.TryUse(dust.transform.position),"Revisit dust note");yield return .1f;Require(dust.dust.localScale.magnitude<.01f&&dust.stamp.localScale.x>.9f,"Revisiting note preserves cleared dust and revealed stamp");
            var play=room.GetComponentInChildren<BindingPlayground>();Require(play.signalLights.All(l=>l.intensity>8),"Pull pulse arrives at door seam lights");
            Require(play.puzzle.State!=PuzzleBase.PuzzleState.Solved,"Optional play does not bypass fragment puzzle");
            // Leave one released to verify collecting fragments can still open the door.
            Require(knot.TryUse(knot.transform.position)&&!knot.IsLatched,"Knot can be released without softlock");
            Directory.CreateDirectory("Docs/ArtReview/BindingPlay");
            Teleport(World(1,-10));camera.SetAim(0,-3);yield return .7f;InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/BindingPlay/F0-live.png");
        }
        static IEnumerator CheckFoldBridge()
        {
            Directory.CreateDirectory("Docs/ArtReview/FoldBridge");
            var puzzle=room.GetComponentInChildren<FoldBridgePuzzle>();
            var controls=room.GetComponentsInChildren<FoldBridgeControl>();
            var near=controls.First(c=>c.name=="입구 제본 손잡이");
            var far=controls.First(c=>c.name=="반대편 제본 손잡이");
            var shutter=controls.First(c=>c.opticalControl==1);
            var first=controls.First(c=>c.opticalControl==2);
            var last=controls.First(c=>c.opticalControl==3);
            var seal=controls.First(c=>c.seal);
            Vector3 W(float x,float z)=>room.transform.TransformPoint(new Vector3(x,3.1f,z));
            Require(player.Controller.isGrounded&&player.transform.position.y>2.9f,"Start safely on entrance book bank");
            Require(!puzzle.LightConnected&&!puzzle.ShutterOpen,"Paper shutter initially blocks the light");
            Require(!seal.TryUse(player.transform.position),"Remote seal cannot be used from entrance");
            Require(!last.TryUse(player.transform.position),"Far reflector cannot be operated from entrance");
            Require(!shutter.TryUse(player.transform.position+Vector3.one*50),"Distant optical interaction rejected");
            room.SetVisible(false);Require(!shutter.TryUse(shutter.transform.position),"Hidden-room optical interaction rejected");room.SetVisible(true);
            Require(room.GetComponentsInChildren<CollectiblePiece>().Length==0,"Retired fragment route remains disabled");
            camera.SetAim(0,-12);yield return .6f;InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/FoldBridge/01-raised.png");
            if(room.GetComponentsInChildren<Transform>().Any(t=>t.name=="기준이미지_가죽제본기둥"))
            {
                Directory.CreateDirectory("Docs/ArtReview/BookSpinePillar");
                InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/BookSpinePillar/player-overview.png");
                var originalPosition=player.transform.position;
                Teleport(W(-5.5f,-15.5f));camera.SetAim(-48,-26);yield return .8f;
                InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/BookSpinePillar/player-pillar.png");
                if(room.transform.Find(InteriorF0Builder.RootName+"/F0_개별공예소품"))
                {
                    Directory.CreateDirectory("Docs/ArtReview/F0CraftedProps");
                    Teleport(W(-6,-11));camera.SetAim(-45,9);yield return .8f;
                    InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/F0CraftedProps/player-workbench.png");
                }
                Teleport(originalPosition);camera.SetAim(0,-12);yield return .3f;
            }
            Teleport(room.transform.TransformPoint(new Vector3(0,1.4f,3)));yield return .5f;
            Require(puzzle.Recoveries==1&&room.transform.InverseTransformPoint(player.transform.position).z<0,"Pit fall recovers at entrance bank");
            originalSettings=InputSystem.settings;var settings=Object.Instantiate(originalSettings);
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
            Teleport(W(-5.5f,-7.6f));camera.enabled=false;
            Camera.main.transform.position=shutter.transform.position+new Vector3(0,.15f,-2);Camera.main.transform.LookAt(shutter.transform.position);yield return .4f;
            InteractionRay.Cast(PaperCamera.AimRay(Camera.main),out var shutterHit,100);
            Require(TargetInteractor.Current==shutter.GetComponent<Interactable>(),"Aim ray targets physical shutter handle; hit="+(shutterHit.collider?shutterHit.collider.name:"none")+" current="+(TargetInteractor.Current?TargetInteractor.Current.name:"none")+" player="+player.transform.position+" handle="+shutter.transform.position+" inRange="+shutter.GetComponent<Interactable>().InRange);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return 2.2f;
            Require(puzzle.ShutterOpen&&puzzle.DirectReceiverLit&&!puzzle.LightConnected,"F folds shutter: direct beam wakes lower flower, not exit; stop="+puzzle.BeamStoppedAt);
            Teleport(W(3.2f,-5));yield return .4f;Require(near.TryUse(player.transform.position),"Bridge lowers from entrance handle");yield return 2.1f;
            Require(puzzle.BridgeDown&&!puzzle.DirectReceiverLit&&!puzzle.LightConnected,"Walkable page physically blocks the direct beam; stop="+puzzle.BeamStoppedAt);
            Require(!seal.TryUse(seal.transform.position),"Unlit exit rejects confirmation");
            Teleport(W(-3.2f,-4.5f));yield return .4f;Require(first.TryUse(player.transform.position),"First reflection can be explored");yield return 2.2f;
            Require(puzzle.lightSurfaces.Any(s=>s.name=="옆길의 종이꽃"&&s.Lit)&&!puzzle.LightConnected,"Wrong branch opens optional paper flower; stop="+puzzle.BeamStoppedAt);
            Require(first.TryUse(player.transform.position),"Entrance mirror can rotate to bypass");yield return 2.2f;
            Require(puzzle.lightSurfaces.Any(s=>s.name=="건너편 장식 종이꽃"&&s.Lit)&&!puzzle.LightConnected,"Bypass reaches far bank but initial far reflection goes to decoration; stop="+puzzle.BeamStoppedAt);
            camera.enabled=true;camera.SetAim(0,-12);yield return .6f;InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/FoldBridge/02-bridge.png");
            foreach(var wait in Enumerate(F0PaperIdentityVerifier.CaptureDirections(player,camera,room)))yield return wait;
            foreach(var wait in Enumerate(Walk(W(0,-3))))yield return wait;
            foreach(var wait in Enumerate(Walk(W(0,3))))yield return wait;
            Require(player.transform.position.y>2.8f,"Player walks across solid page bridge");
            Require(!puzzle.TryOperate(false,player.transform.position,player.transform.position,3),"Occupied bridge cannot fold under player");
            foreach(var wait in Enumerate(Walk(W(0,9))))yield return wait;yield return .4f;
            Teleport(W(3.2f,9));Require(far.TryUse(player.transform.position),"Far bridge handle still works");yield return 2.1f;
            Require(!puzzle.LightConnected,"Old lower-cross-raise solution no longer opens exit");
            Require(far.TryUse(player.transform.position),"Bridge can stay down while solving light path");yield return 2.1f;
            foreach(var wait in Enumerate(Walk(W(6,8))))yield return wait;
            camera.enabled=false;Camera.main.transform.position=last.transform.position+new Vector3(0,.15f,-2);Camera.main.transform.LookAt(last.transform.position);yield return .4f;
            Require(TargetInteractor.Current==last.GetComponent<Interactable>(),"Aim ray reaches far reflector handle");
            testMouse=InputSystem.AddDevice<Mouse>();testMouse.MakeCurrent();
            InputSystem.QueueStateEvent(testMouse,new MouseState{buttons=1});yield return .1f;InputSystem.QueueStateEvent(testMouse,new MouseState());yield return 2.2f;
            Require(puzzle.FarDirection==1&&!puzzle.LightConnected,"Mouse advances far mirror once; reverse path is not a solution");
            Require(last.TryUse(player.transform.position),"Far reflector rotates toward leaf receiver");yield return 2.2f;
            Require(puzzle.LightConnected&&puzzle.BridgeDown,"Physical reflected path lights exit while bridge remains walkable; stop="+puzzle.BeamStoppedAt);
            Require(puzzle.State!=PuzzleBase.PuzzleState.Solved,"Lit receiver waits for nearby seal confirmation");
            // Closing the source must invalidate even the correct mirror arrangement.
            Teleport(W(-5.5f,-7));Require(shutter.TryUse(player.transform.position),"Source can be closed again");yield return 2.2f;
            Require(!puzzle.LightConnected&&!seal.TryUse(seal.transform.position),"Closed paper shutter invalidates correct mirror arrangement");
            Require(shutter.TryUse(player.transform.position),"Source can be reopened without reset");yield return 2.2f;
            Require(puzzle.LightConnected,"Reopening source restores actual optical path");
            Teleport(W(0,9));yield return .5f;
            Teleport(room.transform.TransformPoint(new Vector3(0,1.4f,3)));yield return .5f;
            Require(puzzle.Recoveries==2&&room.transform.InverseTransformPoint(player.transform.position).z>7.7f,"Far-bank fall recovers beyond bridge");
            Require(seal.TryUse(player.transform.position),"Lit seal accepts nearby confirmation");yield return 2.5f;
            var door=room.GetComponentInChildren<BindingDoor>();
            Require(puzzle.State==PuzzleBase.PuzzleState.Solved&&door.IsOpen,"Reflected light and confirmation unlock P1 door");
            Require(Mathf.Abs(Mathf.DeltaAngle(door.left.localEulerAngles.y,170))<.1f,"Open door folds away from exit passage");
            camera.enabled=true;camera.SetAim(0,-3);yield return .6f;InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/FoldBridge/03-solved.png");
            camera.enabled=false;Camera.main.transform.position=room.transform.TransformPoint(new Vector3(-11,10,-12));Camera.main.transform.LookAt(room.transform.TransformPoint(new Vector3(0,5,4)));
            InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/FoldBridge/04-light-route.png");camera.enabled=true;
            Teleport(W(6,8));Require(last.TryUse(player.transform.position),"Mirrors remain reversible after solve");yield return 2.2f;
            Require(!puzzle.LightConnected&&door.IsOpen,"Changing light after solve cannot relock exit");
            Teleport(W(0,9));
            foreach(var at in new[]{W(0,11),World(0,15),World(3,18.3f),World(20,18.3f),World(20,11.2f),World(30,12)})foreach(var wait in Enumerate(Walk(at)))yield return wait;
            Require(true,"Walked bridge, page ramp, open door and F0-to-1A passage");
        }
        static System.Collections.Generic.IEnumerable<object> Enumerate(IEnumerator steps){while(steps.MoveNext())yield return steps.Current;}
    }
}
