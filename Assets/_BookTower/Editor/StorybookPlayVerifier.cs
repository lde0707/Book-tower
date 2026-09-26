using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class StorybookPlayVerifier
 {
  static IEnumerator routine;static int frame;static float next;static double started;static PlayerController player;static Room room;static string report;
  static StorybookPlayVerifier(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
  public static void BuildAndVerify(){StorybookRoomBuilder.RunBatch();SessionState.SetBool("StorybookVerify",true);EditorApplication.EnterPlaymode();}
  public static void RunBatch(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool("StorybookVerify",true);EditorApplication.EnterPlaymode();}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/storybook-play.request"))return;File.Delete("Logs/storybook-play.request");SessionState.SetBool("StorybookVerify",true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool("StorybookVerify",false))return;if(s==PlayModeStateChange.EnteredPlayMode){Application.targetFrameRate=60;report="";frame=-1;next=0;started=EditorApplication.timeSinceStartup;routine=Verify();EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("StorybookVerify",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt("StorybookCode",1));}}
  static void Tick(){try{if(EditorApplication.timeSinceStartup-started>180)throw new Exception("timeout");if(Time.frameCount==frame||Time.time<next)return;frame=Time.frameCount;if(!routine.MoveNext()){Finish(null);return;}next=Time.time+(routine.Current is float f?f:0);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;report+=(e==null?"PASS: room, passage and P2 playback":"FAIL: "+e);File.WriteAllText("Docs/ArtReview/Storybook1A/play-check.txt",report);Debug.Log(report);SessionState.SetInt("StorybookCode",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);report+="PASS: "+name+"\n";}
  static Vector3 W(float x,float z)=>room.transform.TransformPoint(new Vector3(x,.15f,z));
  static IEnumerator Walk(Vector3 target){float end=Time.time+24;while(Vector2.Distance(new(player.transform.position.x,player.transform.position.z),new(target.x,target.z))>.35f){if(Time.time>end)throw new Exception("Blocked "+player.transform.position+" toward "+target);Vector3 d=target-player.transform.position;d.y=0;player.Controller.Move(Vector3.ClampMagnitude(d,7*Time.deltaTime)+Vector3.down*3*Time.deltaTime);if(player.transform.position.y<-1)throw new Exception("fell");yield return 0f;}}
  static void Teleport(Vector3 p){player.Controller.enabled=false;player.transform.position=p;player.Controller.enabled=true;player.VerticalVelocity=0;Physics.SyncTransforms();}
  static IEnumerator Verify()
  {
   yield return 1.5f;player=Object.FindFirstObjectByType<PlayerController>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1A");
   var p1=Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).First(p=>p.PuzzleId=="P1");p1.Solve();yield return 1.5f;
   Teleport(new Vector3(30,.25f,-208));foreach(var at in new[]{new Vector3(48,2.3f,-200.8f),new Vector3(68,4.25f,-192.8f),W(-25,-10)}){var walk=Walk(at);while(walk.MoveNext())yield return walk.Current;}Check(player.transform.position.y>3.7f,"F0 covered passage reaches 1A without falling");
   foreach(var at in new[]{W(-18,-20),W(-8,-11),W(-8,-2),W(1,3),W(8,6),W(8,14),W(15,14)}){var walk=Walk(at);while(walk.MoveNext())yield return walk.Current;}Check(true,"Both page arches traversed");
   var theatre=room.GetComponentInChildren<StorybookShadowTheatre>();var puzzle=theatre.puzzle;puzzle.Activate();
   theatre.practice.CycleForward();theatre.practice.CycleForward();yield return .8f;Check(puzzle.State!=PuzzleBase.PuzzleState.Solved,"One fold cannot complete P2");
   theatre.left.CycleForward();yield return .6f;Check(puzzle.State!=PuzzleBase.PuzzleState.Solved,"Two folds still require the second shadow half");
   theatre.right.CycleForward();theatre.right.CycleForward();theatre.right.CycleForward();yield return 2f;Check(puzzle.State==PuzzleBase.PuzzleState.Solved,"Three fold combination solves P2");Check(Vector2.Distance(theatre.leftShadow.localPosition,theatre.rightShadow.localPosition)<.05f,"Rabbit shadow halves align");
   var cam=Object.FindFirstObjectByType<PaperCamera>();Teleport(W(7,13));cam.SetAim(0,-4);yield return 1;InteriorF0Builder.Capture(Camera.main,"Docs/ArtReview/Storybook1A/1A-play.png");
   foreach(var at in new[]{W(17,20),W(24.5f,23),W(26.5f,30)}){var walk=Walk(at);while(walk.MoveNext())yield return walk.Current;}Check(player.transform.position.y>3.7f,"Solved exit reaches existing 1B bridge");
  }
 }
}
