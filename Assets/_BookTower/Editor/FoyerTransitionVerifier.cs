using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Core;
using BookTower.Rooms;
using BookTower.Story;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class FoyerTransitionVerifier
 {
  const string Flag="FoyerTransitionQA";static IEnumerator routine;static double next,started;static string report="";static Keyboard keys;static InputSettings original;
  static FoyerTransitionVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/foyer-transition-settings.json");}
  public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){routine=Test();started=EditorApplication.timeSinceStartup;next=started+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.05f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;if(keys!=null)InputSystem.RemoveDevice(keys);if(original)InputSystem.settings=original;File.WriteAllText("Logs/foyer-transition-play-check.txt",report+(e==null?"PASS":"FAIL: "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);report+="PASS: "+message+"\n";}
  static bool captured;
  static void VerifyFrame(OpeningStoryDirector story){var camera=story.storyCamera.GetComponent<Camera>();var b=story.DoorRevealBounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1)throw new Exception("Door sweep outside frame "+p);}}
  static void Capture(OpeningStoryDirector story){Directory.CreateDirectory("Docs/ArtReview/F0Feedback");var camera=story.storyCamera.GetComponent<Camera>();var rt=RenderTexture.GetTemporary(1280,720,24);var old=camera.targetTexture;camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1280,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes("Docs/ArtReview/F0Feedback/door-reveal.png",t.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=old;RenderTexture.ReleaseTemporary(rt);Object.Destroy(t);}
  static IEnumerator Test()
  {
   var story=Object.FindFirstObjectByType<OpeningStoryDirector>();var ui=PaperUI.Instance;
   Check(story&&ui,"Actual scene story and UI present");
   while(!story.Completed(OpeningStoryData.Promise)){ui.AdvanceStoryText();yield return .12f;}
   yield return .2f;
   var doors=Object.FindObjectsByType<BindingDoor>(FindObjectsSortMode.None).Where(d=>d.puzzle==story.EntrancePuzzle).ToArray();Check(doors.Length>0&&!doors.Any(d=>d.IsOpen),"F0 bound door initially closed");
   original=InputSystem.settings;var options=Object.Instantiate(original);options.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;options.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;options.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=options;keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   var at=story.player.transform.position;story.EntrancePuzzle.Solve();InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.Space));
   Check(story.WaitingForFoyerDoor&&!GameFlow.Instance.AcceptsPlayerInput&&!story.player.enabled,"Puzzle solved synchronously locks locomotion");
   while(!doors.All(d=>d.IsOpen)){Check(story.ActiveSequence!=OpeningStoryData.Return,"No story before actual door completion");yield return .1f;if(doors.Any(d=>d.OpeningProgress>0)&&!doors.All(d=>d.IsOpen)){VerifyFrame(story);if(doors.Any(d=>d.OpeningProgress>.5f)&&!captured){Capture(story);captured=true;}}Check(Vector3.Distance(at,story.player.transform.position)<.005f,"Walking/jumping cannot move player during door animation");}
   yield return .05f;Check(story.ActiveSequence==OpeningStoryData.Return&&!story.WaitingForFoyerDoor,"Fully opened door immediately starts Return story");
   Check(doors.All(d=>d.OpeningSoundPlayed),"Door opening foley triggered");Check(captured,"Door swept bounds fit camera throughout opening");
   Check(!GameFlow.Instance.AcceptsPlayerInput&&story.player.enabled,"Story continues input lock without disabling player permanently");
   InputSystem.QueueStateEvent(keys,new KeyboardState());while(!story.Completed(OpeningStoryData.Return)){ui.AdvanceStoryText();yield return .1f;}
   Check(GameFlow.Instance.AcceptsPlayerInput,"Finishing story restores gameplay");
   var ceiling=GameObject.Find(FoyerPassageCeiling.RootName);Check(ceiling,"Ceiling prefab present at runtime");
   var bound=ceiling.GetComponent<RoomBound>();Check(bound.Belongs("F0")&&bound.Belongs("1A"),"Ceiling belongs to both rooms across transition");
   var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;
   foreach(string id in new[]{"F0","1A"}){bound.SetVisible(bound.Belongs(id));Check(ceiling.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"All ceiling renderers remain visible in "+id);}
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");var data=new SerializedObject(link);Vector3 a=data.FindProperty("fromPoint").vector3Value,b=data.FindProperty("toPoint").vector3Value;
   story.enabled=false;var player=story.player;player.enabled=false;player.Controller.enabled=false;player.transform.position=a+Vector3.up*.25f;player.Controller.enabled=true;Physics.SyncTransforms();
   int samples=200;for(int i=1;i<=samples;i++){var target=Vector3.Lerp(a,b,(float)i/samples)+Vector3.up*.25f;player.Controller.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.75f)throw new Exception("Covered passage blocks capsule at "+i+" actual="+player.transform.position+" target="+target);}
   Check(Vector3.Distance(player.transform.position,b+Vector3.up*.25f)<.75f,"Actual CharacterController traverses entire covered F0 to 1A ramp");
  }
 }
}
