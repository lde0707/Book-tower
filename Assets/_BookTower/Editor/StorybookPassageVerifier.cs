using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEditor;
using BookTower.Core;
using BookTower.Rooms;
using BookTower.UI;
using BookTower.Puzzles;
using BookTower.PlayerSystem;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class StorybookPassageVerifier
 {
  const string Flag="StorybookPassageQA";static IEnumerator routine;static double next,start;static string report="";
  static StorybookPassageVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/passage-art-settings.json");}
  public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){routine=Test();start=EditorApplication.timeSinceStartup;next=start+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-start>70)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.1f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;File.WriteAllText("Logs/storybook-passage-play-check.txt",report+(e==null?"PASS":"FAIL "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool condition,string message){if(!condition)throw new Exception(message);report+="PASS: "+message+"\n";}
  static IEnumerator Test()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();GameFlow.EnsureExists().Resume();
   var art=GameObject.Find(StorybookReadingPassage.RootName);Check(art,"Authored passage prefab instantiated in actual InGame");
   Check(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name==StorybookReadingPassage.RootName)==1,"No duplicate passage instances");
   var bound=art.GetComponent<RoomBound>();Check(bound.Belongs("1A")&&bound.Belongs("1B"),"Art visible from both adjacent rooms");
   var player=Object.FindFirstObjectByType<PlayerController>();var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;player.enabled=false;
   Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).First(p=>p.PuzzleId=="P2").Solve();yield return 2f;
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="1A"&&l.ToRoomId=="1B");var so=new SerializedObject(link);var a=so.FindProperty("fromPoint").vector3Value;var b=so.FindProperty("toPoint").vector3Value;
   var apron=link.transform.parent.Find(link.name+"_접합부");Check(apron.Cast<Transform>().Where(t=>t.name.StartsWith("접힌측벽_")&&t.name.EndsWith("_2")).All(t=>!t.gameObject.activeSelf),"Obsolete arrival rails no longer cut across dogleg");
   foreach(bool backwards in new[]{false,true})
   {
    var path=new[]{a,b,new Vector3(160,8.1f,-122),new Vector3(160,8.1f,-119)};if(backwards)Array.Reverse(path);
    player.Controller.enabled=false;player.transform.position=path[0]+Vector3.up*.25f;player.Controller.enabled=true;Physics.SyncTransforms();
    for(int j=1;j<path.Length;j++)for(int i=1;i<=150;i++){var target=Vector3.Lerp(path[j-1],path[j],i/150f)+Vector3.up*.25f;player.Controller.Move(target-player.transform.position);if(Vector3.Distance(target,player.transform.position)>.8f)throw new Exception("Passage blocked at segment "+j+" step "+i+" target="+target+" actual="+player.transform.position);}
    Check(true,backwards?"Actual player capsule returns through decorated dogleg and ramp":"Actual player capsule passes ramp, dogleg and reading-room entrance");
   }
   foreach(string id in new[]{"1A","1B"}){bound.SetVisible(bound.Belongs(id));Check(art.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"All passage art stays visible in "+id);}
   Check(art.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh&&f.sharedMesh.vertexCount>0),"All merged art meshes present");
  }
 }
}
