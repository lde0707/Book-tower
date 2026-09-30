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
 [InitializeOnLoad] public static class ShadowDragVerifier
 {
  const string Flag="ShadowDragQA";static IEnumerator routine;static double next,started;
  static Keyboard keys;static Mouse mouse;static InputSettings original;static string report;
  static ShadowDragVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/shadow-drag-settings.json");}
  public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){routine=Test();started=EditorApplication.timeSinceStartup;next=started+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.1f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;if(keys!=null)InputSystem.RemoveDevice(keys);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(original)InputSystem.settings=original;File.WriteAllText("Logs/shadow-drag-play-check.txt",report+(e==null?"PASS":"FAIL "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool condition,string message){if(!condition)throw new Exception(message);report+="PASS: "+message+"\n";}
  static IEnumerator Test()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   if(PaperUI.Instance){PaperUI.Instance.EndStory();PaperUI.Instance.Resume();}BookTower.Core.GameFlow.EnsureExists().Resume();
   var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;
   var puzzle=Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsSortMode.None).First(p=>p.realShadows);
   puzzle.GetComponentInParent<Room>().SetVisible(true);
   var player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;
   var drag=player.GetComponent<PaperShadowDrag>();Check(drag,"Drag component in actual scene");
   var camera=Camera.main;var follow=camera.GetComponent<PaperCamera>();follow.enabled=false;
   original=InputSystem.settings;var settings=Object.Instantiate(original);settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
   keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys.MakeCurrent();mouse.MakeCurrent();yield return .4f;
   foreach(var part in new[]{puzzle.body,puzzle.ears})
   {
    var handle=part.GetComponent<PaperShadowHandle>();var centre=part.GetComponent<Collider>().bounds.center;
    player.Controller.enabled=false;player.transform.position=centre-puzzle.stage.forward*8-puzzle.stage.up;player.Controller.enabled=true;
    camera.transform.position=centre-puzzle.stage.forward*8+puzzle.stage.up*2;camera.transform.LookAt(centre);Physics.SyncTransforms();yield return .15f;
    Check(PaperShadowDrag.Aimed==handle,"Aim recognises "+part.name+" beyond old 6m limit");
    InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F));yield return .1f;Check(!PaperShadowDrag.Holding,"F does not start dragging");InputSystem.QueueStateEvent(keys,new KeyboardState());
    InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .15f;Check(drag.Held==handle,"Left hold picks up "+part.name);
    float yaw=follow.AimYaw;follow.ApplyLookDelta(new Vector2(25,0));Check(Mathf.Abs(Mathf.DeltaAngle(yaw,follow.AimYaw))>1,"Look rotation remains enabled while holding");
    var before=handle.isEar?puzzle.EarPosition:puzzle.BodyPosition;
    camera.transform.LookAt(centre+puzzle.stage.right);InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));yield return .3f;
    Check(player.GetComponent<PlayerInputReader>().Move.x>0,"Movement input available during drag");
    Check(Vector3.Distance(before,handle.isEar?puzzle.EarPosition:puzzle.BodyPosition)>.1f,"Paper follows moving centre aim");InputSystem.QueueStateEvent(keys,new KeyboardState());
    if(handle.isEar){bool flipped=puzzle.Flipped;InputSystem.QueueStateEvent(mouse,new MouseState{buttons=3});yield return .1f;Check(puzzle.Flipped!=flipped,"Right click flips held ears");}
    InputSystem.QueueStateEvent(mouse,new MouseState());yield return .15f;Check(!PaperShadowDrag.Holding,"Mouse release drops paper");
   }
   var target=puzzle.body.GetComponent<Collider>().bounds.center;camera.transform.position=target-puzzle.stage.forward*8+puzzle.stage.up*2;camera.transform.LookAt(target);
   player.Controller.enabled=false;player.transform.position=target-puzzle.stage.forward*14;player.Controller.enabled=true;yield return .15f;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;Check(!PaperShadowDrag.Holding,"Beyond 12m rejected");InputSystem.QueueStateEvent(mouse,new MouseState());yield return .1f;
   player.Controller.enabled=false;player.transform.position=target-puzzle.stage.forward*8;player.Controller.enabled=true;yield return .1f;
   InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .1f;Check(PaperShadowDrag.Holding,"Can grab again");PaperUI.Instance.Pause();yield return .1f;Check(!PaperShadowDrag.Holding,"ESC pause releases held paper");
  }
 }
}
