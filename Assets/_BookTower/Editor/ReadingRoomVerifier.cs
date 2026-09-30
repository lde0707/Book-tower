using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Core;
using Object=UnityEngine.Object;
using Action=BookTower.Puzzles.ReadingRoomControl.Action;
namespace BookTower.EditorTools {
 [InitializeOnLoad] public static class ReadingRoomVerifier {
  const string Flag="ReadingRoomVerify",Out="Docs/ArtReview/ReadingRoom1B";
  static IEnumerator routine;static double started;static int frame;static float next;
  static PlayerController player;static Room room;static ReadingRoomPuzzle puzzle;static PaperCamera follow;
  static Keyboard keyboard;static Mouse mouse;static InputSettings original;static readonly StringBuilder report=new();
  static ReadingRoomVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Isolate(){if(SessionState.GetBool(Flag,false))BookTower.UI.PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/reading-test-settings.json");}
  public static void RunBatch(){Directory.CreateDirectory(Out);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;
   if(s==PlayModeStateChange.EnteredPlayMode){Application.targetFrameRate=60;var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();if(story)story.gameObject.SetActive(false);routine=Verify();started=EditorApplication.timeSinceStartup;frame=-1;next=0;report.Clear();EditorApplication.update+=Tick;}
   if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+"Code",1));}
  }
  static void Tick(){try{if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Timeout");if(Time.frameCount==frame||Time.time<next)return;frame=Time.frameCount;if(!routine.MoveNext()){Finish(null);return;}next=Time.time+(routine.Current is float f?f:0);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(original)InputSystem.settings=original;report.AppendLine(e==null?"PASS: 1B play verification":"FAIL: "+e);File.WriteAllText(Out+"/play-check.txt",report.ToString());Debug.Log(report.ToString());SessionState.SetInt(Flag+"Code",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);report.AppendLine("PASS: "+name);}
  static Vector3 W(float x,float z)=>room.transform.TransformPoint(new Vector3(x,.2f,z));
  static void Teleport(Vector3 at){player.Controller.enabled=false;player.transform.position=at;player.Controller.enabled=true;player.VerticalVelocity=0;Physics.SyncTransforms();}
  static IEnumerator Walk(Vector3 at){float end=Time.time+18;while(Vector2.Distance(new(player.transform.position.x,player.transform.position.z),new(at.x,at.z))>.4f){if(Time.time>end)throw new Exception("Blocked "+player.transform.position+" toward "+at);var d=at-player.transform.position;d.y=0;player.Controller.Move(Vector3.ClampMagnitude(d,7*Time.deltaTime)+Vector3.down*3*Time.deltaTime);if(player.transform.position.y<6)throw new Exception("Fell below 1B foundation");yield return 0f;}}
  static ReadingRoomControl C(Action a)=>puzzle.GetComponentsInChildren<ReadingRoomControl>(true).First(c=>c.action==a);
  static void Use(Action a,bool expected=true){var c=C(a);Check(c.TryUse(c.transform.position)==expected,"Control "+a+" expected="+expected);}
  static void LockCarry(){Use(Action.LeftRing);Use(Action.RightRing);Use(Action.Pickup);}
  static void Open(){Use(Action.LeftRing);Use(Action.RightRing);}
  static IEnumerator Verify(){
   yield return 1.5f;GameFlow.EnsureExists().Resume();player=Object.FindFirstObjectByType<PlayerController>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1B");puzzle=room.GetComponentInChildren<ReadingRoomPuzzle>();follow=Object.FindFirstObjectByType<PaperCamera>();
   original=InputSystem.settings;var settings=Object.Instantiate(original);settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
   Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).First(p=>p.PuzzleId=="P2").Solve();yield return 1f;
   Teleport(new Vector3(142,8.25f,-132));foreach(var at in new[]{W(-10,-22),W(-10,-19),W(-5,-19),W(-5,-8),W(-11,-8)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}
   Check(room.IsVisible,"Covered 1A approach and desk arch traversable with real CharacterController");
   Teleport(W(-8,-18));yield return .4f;Check(!puzzle.Complete&&!puzzle.Charged,"Initial state unsolved and empty");
   Check(puzzle.lantern.Find(ReadingLanternPresentation.ModelName)!=null,"Updated lantern is actually bound in the gameplay scene");
   Check(Quaternion.Angle(puzzle.leftRing.localRotation,Quaternion.Euler(65,0,0))<1,"Open latch lifts outwards by 65 degrees");
   Check(C(Action.LeftRing).Prompt.Contains("열림")&&C(Action.LeftRing).Prompt.Contains("잠가"),"Prompt states current open state and next locking action");
   Check(!C(Action.Charge).TryUse(W(15,15)),"Out-of-range interaction rejected");Use(Action.Pickup,false);Use(Action.TakeCrank,false);Use(Action.Latch,false);
   follow.enabled=false;var target=C(Action.Charge).transform.position;Camera.main.transform.position=target+new Vector3(0,.4f,-2);Camera.main.transform.LookAt(target);yield return .3f;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .15f;
   Check(puzzle.Charged,"Actual F charges lantern through aim ray");
   target=C(Action.LeftRing).transform.position;Camera.main.transform.position=target+new Vector3(-1,.1f,-2);Camera.main.transform.LookAt(target);yield return .3f;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .15f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return .15f;
   Check(puzzle.LeftLocked,"Actual mouse click toggles one ring");Use(Action.Pickup,false);Use(Action.RightRing);Use(Action.Pickup);yield return .5f;
   Check(Quaternion.Angle(puzzle.leftRing.localRotation,Quaternion.identity)<1,"Locked tongue visibly seats against receiver");
   Check(puzzle.State==ReadingRoomPuzzle.LampState.Carried,"Both rings allow carrying");Use(Action.LeftRing,false);Use(Action.ExitDock);
   Check(C(Action.LeftRing).Prompt.Contains("잠김")&&C(Action.LeftRing).Prompt.Contains("열어"),"Docked prompt switches from locking to opening");
   Check(puzzle.Charged&&!puzzle.Complete,"Placing a locked lantern does not automatically release light");Open();
   Check(puzzle.FailedExitAttempts==1&&!puzzle.Charged&&!puzzle.Complete,"Exit-first experiment consumes light without false success");
   Use(Action.Charge);LockCarry();yield return 1f;Check(puzzle.Charged,"Charge does not decay while carried");
   Teleport(W(-13.25f,-6.7f));target=C(Action.RecordDock).transform.position;Camera.main.transform.position=target+new Vector3(.65f,1.1f,1.7f);Camera.main.transform.LookAt(target);yield return .3f;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return 2f;
   Check(puzzle.State==ReadingRoomPuzzle.LampState.RecordDocked&&puzzle.Charged&&!puzzle.RecordOpen,"Actual F places locked lantern without prematurely opening service panel");
   Use(Action.LeftRing);Check(!puzzle.RecordOpen&&puzzle.Charged,"One unlocked latch still preserves light on the stand");
   target=C(Action.RightRing).transform.position;Camera.main.transform.position=target+new Vector3(.5f,.2f,1.7f);Camera.main.transform.LookAt(target);yield return .3f;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .15f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return 2f;
   Check(puzzle.RecordOpen,"Actual mouse opens the remaining docked latch and releases light");
   Check(puzzle.RecordOpen&&!puzzle.serviceBlocker.enabled,"Light reveals service panel and physical opening");
   Check(Vector3.Distance(puzzle.lantern.position,puzzle.recordSocket.position)<.01f,"Placed lantern exactly matches the illustrated receiving tray");
   Teleport(W(-13.25f,-6.7f));target=C(Action.TakeCrank).transform.position;Camera.main.transform.position=target+new Vector3(0,.35f,2.4f);Camera.main.transform.LookAt(target);yield return .3f;
   report.AppendLine("Crank approach distance="+Vector3.Distance(player.transform.position,target)+" aim="+(BookTower.Interaction.TargetInteractor.Current?BookTower.Interaction.TargetInteractor.Current.name:"none"));
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .15f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .15f;
   Check(puzzle.HasCrank,"Actual F retrieves crank through the opened physical cabinet");Use(Action.Crank);Use(Action.WindowCharge,false);Use(Action.Latch);Use(Action.WindowCharge);
   Check(Vector3.Distance(puzzle.lantern.position,puzzle.windowChargeSocket.position)<.01f,"Window recharge stays at its own visible plinth");LockCarry();Use(Action.ExitDock);Open();yield return 3f;
   Check(puzzle.Complete&&puzzle.legacyPuzzle.State==PuzzleBase.PuzzleState.Solved&&!puzzle.doorBlocker.enabled,"Final release solves original P3 and opens door");
   Teleport(W(0,-8));foreach(var at in new[]{W(0,-3),W(0,7),W(0,18),W(0,24),W(0,31),new Vector3(170,12.2f,-30)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}Check(player.transform.position.y>11.6f,"Central bridge and entire covered exit ramp to 1C traversable");
   Teleport(W(0,-13));follow.enabled=true;follow.SetAim(0,-5);yield return 1f;InteriorF0Builder.Capture(Camera.main,Out+"/play-third-person.png");
   report.AppendLine("Input tested physically: charge(F), left ring(mouse), record docking(F), docked right latch(mouse), crank retrieval(F). Remaining branches invoked through range/visibility-checked controls; not a human usability test.");
  }
 }
}
