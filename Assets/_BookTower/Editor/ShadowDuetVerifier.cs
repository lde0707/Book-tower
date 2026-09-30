using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Interaction;
using BookTower.Puzzles;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Rooms;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ShadowDuetVerifier
 {
  const string Flag="ShadowDuetQA",Out="Docs/ArtReview/ShadowDuet";static IEnumerator routine;static double next,started;static string report="";
  static Keyboard keys;static Mouse mouse;static InputSettings original;
  static ShadowDuetVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/shadow-duet-settings.json");}
  public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){routine=Test();started=EditorApplication.timeSinceStartup;next=started+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-started>160)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.05f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;if(keys!=null)InputSystem.RemoveDevice(keys);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(original)InputSystem.settings=original;Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play-check.txt",report+(e==null?"PASS":"FAIL: "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string m){if(!ok)throw new Exception(m);report+="PASS: "+m+"\n";}
  static IEnumerator Test()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   var ui=PaperUI.Instance;
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var recordField=typeof(PaperUI).GetField("records",flags);
   var old=JsonUtility.FromJson("{\"ids\":[\"F0.Book.Work\",\"M01\",\"M01\",\"piece.raw\"],\"titles\":[],\"texts\":[]}",recordField.FieldType);
   recordField.SetValue(ui,old);typeof(PaperUI).GetMethod("NormalizeRecords",flags).Invoke(ui,null);
   var json=JsonUtility.ToJson(recordField.GetValue(ui));Check(!json.Contains("F0.Book.Work")&&!json.Contains("piece.raw")&&json.Contains("돌아올 자리"),"Old journal drops puzzle notes and rebuilds essential summary");
   ui.RecordStory("M02","spoiler","solution");ui.RecordStory("1B.Record","hint","solution");
   json=JsonUtility.ToJson(recordField.GetValue(ui));Check(!json.Contains("solution")&&!json.Contains("1B.Record")&&json.Contains("모아"),"Only canonical story milestones are stored");
   var note=Object.FindFirstObjectByType<ReadableNote>();ui.Read(note);yield return .1f;
   Check(JsonUtility.ToJson(recordField.GetValue(ui))==json,"Reading a puzzle book does not add journal entries");ui.Back();yield return .1f;
   var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;
   var p=Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsSortMode.None).First(x=>x.realShadows);var room=p.GetComponentInParent<Room>();room.SetVisible(true);
   var player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;var drag=player.GetComponent<PaperShadowDrag>();var camera=Camera.main;var follow=camera.GetComponent<PaperCamera>();follow.enabled=false;
   var duet=p.Duet;Check(duet&&duet.lamps.Length==3,"Actual scene loads four cutouts and three projectors");
   original=InputSystem.settings;var options=Object.Instantiate(original);options.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;options.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;options.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=options;keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys.MakeCurrent();mouse.MakeCurrent();yield return .3f;
   var pieces=new[]{p.body,p.ears,duet.foxBody,duet.foxTail};
   for(int i=0;i<4;i++){
    var part=pieces[i];var handle=part.GetComponent<PaperShadowHandle>();var collider=part.GetComponent<BoxCollider>();
    Check(collider.center.y-collider.size.y*.5f<=-.85f,"Grab bounds include stand of "+part.name);
    foreach(float h in new[]{-.70f,collider.center.y,collider.center.y+collider.size.y*.45f}){
     var target=part.TransformPoint(new Vector3(collider.center.x,h,collider.center.z));camera.transform.position=target-p.stage.forward*2;camera.transform.LookAt(target);player.Controller.enabled=false;player.transform.position=part.position-p.stage.forward*7;player.Controller.enabled=true;Physics.SyncTransforms();yield return .1f;
     Check(PaperShadowDrag.Aimed==handle,"Aim selects stand / shaft / cutout of "+part.name+" at "+h);
    }
    InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;Check(drag.Held==handle,"Left mouse grabs "+part.name);
    var before=p.PiecePosition(i);InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,delta=new Vector2(35,35)});yield return .1f;
    Check(p.PiecePosition(i).x>before.x+.02f&&p.PiecePosition(i).z>before.z+.02f,"Mouse moves both axes together: "+part.name);
    before=p.PiecePosition(i);InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,delta=new Vector2(-35,-35)});yield return .1f;
    Check(p.PiecePosition(i).x<before.x-.02f&&p.PiecePosition(i).z<before.z-.02f,"Reverse mouse moves both axes: "+part.name);
    foreach(var step in new[]{Vector3.right*.2f,Vector3.left*.2f,Vector3.forward*.2f,Vector3.back*.2f}){
     before=p.PiecePosition(i);player.Controller.enabled=false;player.transform.position+=p.stage.TransformVector(step);player.Controller.enabled=true;yield return .1f;
     Check(Vector3.Distance(p.PiecePosition(i)-before,step)<.01f,"Player movement carries held piece: "+step);
    }
    InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));yield return .1f;Check(player.GetComponent<PlayerInputReader>().Move.x>0,"WASD remains available while holding");InputSystem.QueueStateEvent(keys,new KeyboardState());
    InputSystem.QueueStateEvent(mouse,new MouseState());yield return .1f;Check(!PaperShadowDrag.Holding,"Mouse release fixes position");
   }
   Check(PaperShadowDrag.DistanceGain(1)<PaperShadowDrag.DistanceGain(6)*.4f&&PaperShadowDrag.DistanceGain(15)<PaperShadowDrag.DistanceGain(6)*.4f,"Near and far movement are slower than middle distance");
   var pauseTarget=p.body.GetComponent<BoxCollider>().bounds.center;camera.transform.position=pauseTarget-p.stage.forward*2;camera.transform.LookAt(pauseTarget);
   player.Controller.enabled=false;player.transform.position=pauseTarget-p.stage.forward*7;player.Controller.enabled=true;yield return .15f;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;Check(PaperShadowDrag.Holding,"Can re-grab before pause");var pauseAt=p.BodyPosition;PaperUI.Instance.Pause();yield return .1f;
   Check(!PaperShadowDrag.Holding&&Vector3.Distance(pauseAt,p.BodyPosition)<.001f,"ESC releases drag without moving paper");InputSystem.QueueStateEvent(mouse,new MouseState());PaperUI.Instance.Resume();yield return .2f;
   player.Controller.enabled=false;player.transform.position=pauseTarget-p.stage.forward*20;player.Controller.enabled=true;yield return .1f;InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;Check(!PaperShadowDrag.Holding,"Out-of-range grab rejected");InputSystem.QueueStateEvent(mouse,new MouseState());yield return .1f;
   // Physical selection uses the existing F dispatcher, not a test-only setter.
   var switcher=duet.lamps[0].GetComponentInParent<StorybookLightControl>();var targetSwitch=switcher.GetComponent<Collider>().bounds.center;
   camera.transform.position=targetSwitch-p.stage.forward*2;camera.transform.LookAt(targetSwitch);player.Controller.enabled=false;player.transform.position=targetSwitch-p.stage.forward*2;player.Controller.enabled=true;Physics.SyncTransforms();yield return .2f;
   Check(TargetInteractor.Current==switcher.GetComponent<Interactable>(),"Actual aim reaches left projector control");InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F));yield return .15f;InputSystem.QueueStateEvent(keys,new KeyboardState());Check(duet.LightIndex==0,"F selects left projection light");
   p.SelectLight(1);p.DragPiece(0,new Vector3(-1.8f,1,12.5f/1.5f));p.DragPiece(1,new Vector3(-.9f,1,12.5f/3));if(!p.Flipped)p.TurnPiece(1);yield return 2f;
   Check(duet.RabbitRecorded&&!p.Complete,"Rabbit alone records memory but does not open exit");
   p.DragPiece(2,ShadowDuet.FoxBodySolution);p.DragPiece(3,ShadowDuet.FoxTailSolution);if(!duet.TailFlipped)p.TurnPiece(3);yield return 2f;
   Check(!duet.FoxAligned&&!duet.FoxRecorded,"Fox rejects matching positions under the wrong central light");
   p.DragPiece(2,ShadowDuet.FoxBodySolution+Vector3.right*(4*(1-1/1.8f)));p.DragPiece(3,ShadowDuet.FoxTailSolution+Vector3.right*(4*(1-1/2.5f)));yield return 1.5f;
   Check(!duet.FoxAligned,"Correcting lateral position under wrong light still cannot match folded fox silhouette");
   p.DragPiece(2,ShadowDuet.FoxBodySolution);p.DragPiece(3,ShadowDuet.FoxTailSolution);yield return 1f;
   p.SelectLight(2);yield return .5f;Check(!duet.FoxAligned,"Right light changes real folded silhouette and does not solve fox");
   Capture(p,camera,"wrong-light.png");p.SelectLight(0);yield return .3f;Check(duet.FoxAligned,"Actual left-lamp projection matches fox contour");Capture(p,camera,"fox-aligned.png");yield return 1.5f;
   Check(duet.AllRecorded&&p.Complete,"Both recorded animals finish room puzzle");
   var legacy=p.GetComponent<FoldCombinationPuzzle>();legacy.Activate();yield return .2f;Check(legacy.State==PuzzleBase.PuzzleState.Solved,"Existing P2 gate and story receive completed duet");
   room.SetVisible(false);yield return .1f;Check(duet.lamps.All(l=>!l.enabled)&&duet.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Hidden room disables new lights and art");room.SetVisible(true);yield return .1f;
   Check(duet.lamps.Count(l=>l.enabled)==1,"Only selected light restored when entering room");
  }
  static void Capture(StorybookLightPuzzle p,Camera camera,string name){Directory.CreateDirectory(Out);camera.transform.position=p.stage.TransformPoint(new Vector3(0,5,-8));camera.transform.LookAt(p.stage.TransformPoint(new Vector3(0,2.8f,9)));InteriorF0Builder.Capture(camera,Out+"/"+name);}
 }
}
