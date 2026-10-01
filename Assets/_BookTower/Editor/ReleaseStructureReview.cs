using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.Interaction;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ReleaseStructureReview
 {
  public const string Output="Docs/ArtReview/ReleaseArt/F0/01-Structure";
  const string Flag="ReleaseStructureReview";
  static IEnumerator routine;static double next,start;static string folder;
  static readonly StringBuilder log=new();static PlayerController player;static PaperCamera camera;static Room room;
  static ReleaseStructureReview(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Storage(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/release-structure-"+DateTime.Now.Ticks+".json");}
  public static void Before()=>Begin("Before");
  public static void After()=>Begin("After");
  static void Begin(string stage){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetString(Flag+"stage",stage);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange s)
  {
   if(!SessionState.GetBool(Flag,false))return;
   if(s==PlayModeStateChange.EnteredPlayMode){folder=Output+"/"+SessionState.GetString(Flag+"stage","Before");Directory.CreateDirectory(folder);log.Clear();start=EditorApplication.timeSinceStartup;next=start+1;routine=Review();EditorApplication.update+=Tick;}
   if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}
  }
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-start>160)throw new Exception("Review timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.05f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;File.WriteAllText(folder+"/review.txt",log+(e==null?"PASS: capture and measurement complete; visual approval is separate.\n":"FAIL: "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  static void Place(Vector3 world){player.Controller.enabled=false;player.transform.position=world;player.Controller.enabled=true;player.VerticalVelocity=0;player.HorizontalVelocity=Vector3.zero;Physics.SyncTransforms();}
  static IEnumerator Shot(string name,Vector3 point,float yaw,float pitch)
  {Place(point);camera.SetAim(yaw,pitch);yield return .75f;F0PaperIdentityVerifier.Capture(Camera.main,folder+"/"+name+".png");var c=Camera.main;log.AppendLine($"CAMERA {name}: player={player.transform.position:F4} position={c.transform.position:F4} rotation={c.transform.eulerAngles:F4} FOV={c.fieldOfView:F3} near={c.nearClipPlane} far={c.farClipPlane} 1920x1080, world render (overlay UI excluded)");}
  static IEnumerator Review()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   player=Object.FindFirstObjectByType<PlayerController>();camera=Object.FindFirstObjectByType<PaperCamera>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId=="F0");player.enabled=false;
   log.AppendLine("Unity "+Application.unityVersion+"; source scene="+EditorSceneManager.GetActiveScene().path);
   var cc=player.Controller;log.AppendLine($"PLAYER capsule height={cc.height} diameter={cc.radius*2} step={cc.stepOffset} skin={cc.skinWidth} center={cc.center}; walk={player.WalkSpeed} run={player.RunSpeed} jump={player.JumpHeight}");
   var skins=player.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length>0){var b=skins[0].bounds;foreach(var s in skins)b.Encapsulate(s.bounds);log.AppendLine("PLAYER rendered bounds "+b);}
   log.AppendLine($"ROOM origin={room.transform.position} rotation={room.transform.eulerAngles} bounds={room.WorldBounds} floor={room.FloorY}");
   var p=room.GetComponentInChildren<FoldBridgePuzzle>();
   foreach(var t in new[]{p.hinge,p.source,p.receiver,p.shutter,p.entranceMirror,p.farMirror})log.AppendLine($"PIVOT {PathOf(t)} localToRoom={room.transform.InverseTransformPoint(t.position):F4} rotation={t.rotation.eulerAngles:F4}");
   foreach(var surface in p.lightSurfaces)log.AppendLine("OPTICAL "+surface.name+" "+surface.transform.position.ToString("F4")+" "+surface.transform.eulerAngles.ToString("F4"));
   var door=room.GetComponentInChildren<BindingDoor>();log.AppendLine($"DOOR left={room.transform.InverseTransformPoint(door.left.position)} right={room.transform.InverseTransformPoint(door.right.position)}");
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");var data=new SerializedObject(link);var a=data.FindProperty("fromPoint").vector3Value;var bEnd=data.FindProperty("toPoint").vector3Value;
   log.AppendLine($"LINK a={a} b={bEnd} scale={link.transform.lossyScale} slope={Vector3.Angle(bEnd-a,Vector3.ProjectOnPlane(bEnd-a,Vector3.up))} length={Vector3.Distance(a,bEnd)}");
   var inventory=new StringBuilder();foreach(var t in room.GetComponentsInChildren<Transform>()){var r=t.GetComponent<Renderer>();var c=t.GetComponent<Collider>();if(r||c)inventory.AppendLine($"{PathOf(t)} | room={room.transform.InverseTransformPoint(t.position):F3} | {(r?"R="+r.bounds+" mat="+r.sharedMaterial?.name:"")} | {(c?"C="+c.bounds+" enabled="+c.enabled:"")}");}
   foreach(var bound in Object.FindObjectsByType<RoomBound>(FindObjectsSortMode.None).Where(x=>x.Belongs("F0")))foreach(var t in bound.GetComponentsInChildren<Transform>()){var r=t.GetComponent<Renderer>();if(r)inventory.AppendLine("BOUND "+PathOf(t)+" "+r.bounds);}
   File.WriteAllText(folder+"/inventory.txt",inventory.ToString());
   Vector3 W(float x,float y,float z)=>room.transform.TransformPoint(new Vector3(x,y,z));
   foreach(var step in Steps(Shot("01-start",W(0,3.1f,-10.5f),0,-2)))yield return step;
   foreach(var step in Steps(Shot("02-workbook",W(-6,3.1f,-11),315,9)))yield return step;
   foreach(var step in Steps(Shot("03-floor",W(0,3.1f,-12),0,36)))yield return step;
   foreach(var step in Steps(Shot("04-ceiling",W(0,3.1f,-10),0,-55)))yield return step;
   foreach(var step in Steps(Shot("05-left",W(0,3.1f,-6),270,-2)))yield return step;
   foreach(var step in Steps(Shot("06-right",W(0,3.1f,-6),90,-2)))yield return step;
   foreach(var step in Steps(Shot("07-rear",W(0,3.1f,-6),180,-2)))yield return step;
   var controls=room.GetComponentsInChildren<FoldBridgeControl>();var handle=controls.First(c=>c.name=="입구 제본 손잡이");handle.TryUse(handle.transform.position);yield return 2.2f;
   p.Solve();yield return 4.5f;
   foreach(var step in Steps(Shot("08-open-door",W(0,3.1f,8),0,-4)))yield return step;
   foreach(var step in Steps(Shot("09-back-corridor",W(3,.15f,18.3f),90,-6)))yield return step;
   foreach(var step in Steps(Shot("10-corner",W(20,.15f,18),180,-6)))yield return step;
   var forward=Vector3.ProjectOnPlane(bEnd-a,Vector3.up).normalized;float heading=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
   foreach(var step in Steps(Shot("11-link",(a+bEnd)*.5f+Vector3.up*.2f,heading,-12)))yield return step;
   foreach(var step in Steps(Shot("12-1A-entry",bEnd+forward*2+Vector3.up*.2f,heading,-5)))yield return step;
  }
  static System.Collections.Generic.IEnumerable<object> Steps(IEnumerator routine){while(routine.MoveNext())yield return routine.Current;}
 }
}
