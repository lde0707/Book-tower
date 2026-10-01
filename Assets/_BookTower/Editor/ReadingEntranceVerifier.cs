using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEditor;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.Core;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad]public static class ReadingEntranceVerifier
 {
  const string Flag="ReadingEntranceQA",Out="Docs/ArtReview/ReadingEntrance";
  static IEnumerator run;static double next,start;static string report;
  static ReadingEntranceVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/entry-test-settings.json");}
  public static void Run(){Directory.CreateDirectory(Out);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  public static void RunFresh(){Directory.CreateDirectory(Out);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");foreach(var name in new[]{ReadingEntrancePassage.RootName,StorybookReadingPassage.RootName}){var prior=GameObject.Find(name);if(prior)Object.DestroyImmediate(prior);}SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){report="";run=CheckAll();start=EditorApplication.timeSinceStartup;next=start+1;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-start>90)throw new Exception("Timeout");if(!run.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(run.Current is float f?f:.1f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;File.WriteAllText(Out+"/play-check.txt",report+(e==null?"PASS: entrance QA":"FAIL: "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static IEnumerator CheckAll()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();GameFlow.EnsureExists().Resume();
   var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))room.SetVisible(room.RoomId=="1A"||room.RoomId=="1B");
   foreach(var bound in Object.FindObjectsByType<RoomBound>(FindObjectsSortMode.None))bound.SetVisible(bound.Belongs("1A")||bound.Belongs("1B"));
   ReadingEntrancePassage.Ensure();yield return .5f;
   var root=GameObject.Find(ReadingEntrancePassage.RootName);if(!root||Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name==ReadingEntrancePassage.RootName)!=1)throw new Exception("Missing or duplicate passage");
   var oldArt=GameObject.Find(StorybookReadingPassage.RootName);if(oldArt&&oldArt.transform.Find("열람실 앞 굽은 회랑").gameObject.activeSelf)throw new Exception("Old crossing walls reappeared due to bootstrap ordering");
   var player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;
   var center=new[]{new Vector3(136.75f,7,-138),new Vector3(140.6f,8.02f,-133.6f),new Vector3(142.5f,8.15f,-132),new Vector3(150,8.15f,-129),new Vector3(158,8.15f,-126),new Vector3(160,8.15f,-124),new Vector3(160,8.15f,-120.8f),new Vector3(160,8.15f,-117.5f)};
   var side=new[]{new Vector3(.752f,0,-.659f),new Vector3(.752f,0,-.659f),new Vector3(.45f,0,-.90f),new Vector3(.351f,0,-.936f),new Vector3(.546f,0,-.838f),new Vector3(.924f,0,-.383f),Vector3.right,Vector3.right};
   foreach(float lane in new[]{-1.5f,0,1.5f})foreach(bool reverse in new[]{false,true})
   {
    var path=center.Select((p,i)=>p+side[i]*lane+Vector3.up*.25f).ToArray();if(reverse)Array.Reverse(path);
    player.Controller.enabled=false;player.transform.position=path[0];player.Controller.enabled=true;Physics.SyncTransforms();
    for(int s=1;s<path.Length;s++)for(int i=1;i<=180;i++){var target=Vector3.Lerp(path[s-1],path[s],i/180f);player.Controller.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.55f)throw new Exception($"Blocked lane {lane} reverse {reverse} segment {s} at {target}, actual {player.transform.position}");}
    report+=$"PASS: actual CharacterController lane={lane}, reverse={reverse}\n";
   }
   var cam=new GameObject("360-degree seam camera").AddComponent<Camera>();cam.fieldOfView=82;cam.nearClipPlane=.06f;cam.farClipPlane=150;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.magenta;cam.allowHDR=false;
   var points=new[]{new Vector3(142.5f,9.8f,-132),new Vector3(156.5f,9.8f,-126.5f),new Vector3(160,9.8f,-122)};
   var rt=new RenderTexture(480,300,24);rt.Create();var image=new Texture2D(480,300,TextureFormat.RGB24,false);var prior=RenderTexture.active;
   int airCount=Shader.GetGlobalInt("_PaperAirCount"),beamCount=Shader.GetGlobalInt("_PaperBeamCount");
   Shader.SetGlobalInt("_PaperAirCount",0);Shader.SetGlobalInt("_PaperBeamCount",0);
   try{for(int i=0;i<points.Length;i++)foreach(int yaw in new[]{0,90,180,270})
    {cam.transform.position=points[i];cam.transform.rotation=Quaternion.Euler(-12,yaw,0);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,300),0,0);image.Apply();int holes=image.GetPixels32().Count(c=>c.r>180&&c.b>180&&c.g<85);File.WriteAllBytes(Out+$"/seam-{i}-{yaw}.png",image.EncodeToPNG());report+=$"VIEW: station={i}, yaw={yaw}, exposed-background pixels={holes}\n";if(holes>0)throw new Exception($"Exterior visible at station {i} yaw {yaw}: {holes} pixels");}
   }finally{Shader.SetGlobalInt("_PaperAirCount",airCount);Shader.SetGlobalInt("_PaperBeamCount",beamCount);cam.targetTexture=null;RenderTexture.active=prior;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);}
   report+="PASS: 12 camera views show no magenta exterior background\n";
   foreach(string id in new[]{"1A","1B"}){var bound=root.GetComponent<RoomBound>();bound.SetVisible(bound.Belongs(id));if(root.GetComponentsInChildren<Renderer>().Any(r=>!r.enabled))throw new Exception("Passage culled in "+id);report+="PASS: visible from "+id+"\n";}
  }
 }
}
