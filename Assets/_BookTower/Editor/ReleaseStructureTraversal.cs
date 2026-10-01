using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Rooms;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class ReleaseStructureTraversal
 {
  const string Flag="ReleaseStructureTraversal";
  static IEnumerator routine;static double started;static float next;static int frame=-1;
  static PlayerController player;static PaperCamera camera;static Keyboard keys;static InputSettings original;
  static readonly StringBuilder report=new();static int images;static float nextImage;
  static string Output=>ReleaseStructureReview.Output+"/Traversal";
  static ReleaseStructureTraversal(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/structure-traversal-settings.json");}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  public static void Build()
  {
   Directory.CreateDirectory("Builds/ReleaseStructure");
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/InGame.unity"},locationPathName="Builds/ReleaseStructure/BookTower.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
   Directory.CreateDirectory(Output);File.WriteAllText(Output+"/build-player.txt",$"{result.summary.result}\nErrors={result.summary.totalErrors}, warnings={result.summary.totalWarnings}, duration={result.summary.totalTime}, size={result.summary.totalSize}\n");
   EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
  }
  static void Changed(PlayModeStateChange state)
  {
   if(!SessionState.GetBool(Flag,false))return;
   if(state==PlayModeStateChange.EnteredPlayMode){Directory.CreateDirectory(Output);started=EditorApplication.timeSinceStartup;next=0;frame=-1;report.Clear();routine=Test();EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}
  }
  static void Tick(){try{if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Timeout");if(frame==Time.frameCount||Time.time<next)return;frame=Time.frameCount;if(!routine.MoveNext()){Finish(null);return;}next=Time.time+(routine.Current is float delay?delay:0);}catch(Exception e){Finish(e);}}
  static void Finish(Exception error){EditorApplication.update-=Tick;if(keys!=null)InputSystem.RemoveDevice(keys);if(original)InputSystem.settings=original;File.WriteAllText(Output+"/checks.txt",report+(error==null?"PASS: traversal checks completed\n":"FAIL: "+error));SessionState.SetInt(Flag+"exit",error==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string label){if(!ok)throw new Exception(label);report.AppendLine("PASS: "+label);}
  static void Place(Vector3 p){player.Controller.enabled=false;player.transform.position=p;player.Controller.enabled=true;player.HorizontalVelocity=Vector3.zero;player.VerticalVelocity=0;Physics.SyncTransforms();}
  static bool Ray(Vector3 p,Vector3 direction,float distance)=>Physics.RaycastAll(p,direction,distance,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.collider.transform.IsChildOf(player.transform));
  static IEnumerator Test()
  {
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   yield return .5f;PaperUI.Instance.EndStory();PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   player=Object.FindFirstObjectByType<PlayerController>();camera=Object.FindFirstObjectByType<PaperCamera>();
   var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId=="F0");
   original=InputSystem.settings;var settings=Object.Instantiate(original);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   player.enabled=true;Place(room.transform.TransformPoint(new Vector3(0,3.1f,-17)));camera.SetAim(0,10);yield return .4f;
   Vector3 from=player.transform.position;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W));yield return 1f;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return .25f;float walk=Vector3.Distance(from,player.transform.position);
   Check(walk>2.9f&&walk<5,"Actual W input walks across paper joints: "+walk.ToString("F2")+"u");
   from=player.transform.position;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.LeftShift));yield return 1f;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return .25f;float run=Vector3.Distance(from,player.transform.position);
   Check(run>walk+.7f&&player.transform.position.y>2.9f,"Shift runs without seam snag: "+run.ToString("F2")+"u");
   float y=player.transform.position.y,highest=y;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Space));yield return .08f;InputSystem.QueueStateEvent(keys,new KeyboardState());float until=Time.time+1.4f;while(Time.time<until){highest=Mathf.Max(highest,player.transform.position.y);yield return 0f;}
   Check(highest-y>.8f&&highest-y<1.6f&&player.Controller.isGrounded,"Space jumps and lands on start bank, rise="+(highest-y).ToString("F2"));
   player.enabled=false;
   int roof=0;foreach(float x in new[]{-14f,-7f,0f,7f,14f})foreach(float z in new[]{-18f,-10f,-2f,8f,14f}){Check(Ray(room.transform.TransformPoint(new Vector3(x,5,z)),Vector3.up,14),"F0 roof collision sample "+x+","+z);roof++;}
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");var data=new SerializedObject(link);Vector3 a=data.FindProperty("fromPoint").vector3Value,b=data.FindProperty("toPoint").vector3Value;
   Vector3 forward=Vector3.ProjectOnPlane(b-a,Vector3.up).normalized,right=Vector3.Cross(Vector3.up,forward);int shell=0;
   for(int i=1;i<20;i++)foreach(float offset in new[]{-1.9f,0f,1.9f}){Vector3 at=Vector3.Lerp(a,b,i/20f)+right*offset+Vector3.up*1.5f;if(!Ray(at,Vector3.up,12)||!Ray(at,Vector3.down,3)||!Ray(at,right,8)||!Ray(at,-right,8))throw new Exception("Passage shell missing at "+at);shell++;}
   Check(shell==57,"57 passage centre/edge positions have floor, roof and both walls (collision sampling; screenshots reviewed separately)");
   var structure=room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ReleaseStructure").ToArray();Check(structure.Length==10,"Ten owned structure roots reloaded");
   foreach(var root in structure)foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){Check(filter.sharedMesh&&AssetDatabase.Contains(filter.sharedMesh),"Stored mesh: "+filter.sharedMesh?.name);if(filter.GetComponent<Renderer>().sharedMaterials.Any(m=>!m))throw new Exception("Missing material");}
   Place(a+Vector3.up*.2f);camera.SetAim(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,-15);yield return .5f;images=0;nextImage=Time.time;
   foreach(var step in Steps(Walk(b,player.WalkSpeed,true)))yield return step;
   Check(Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-b,Vector3.up),Vector3.zero)<.3f,"Walked full F0-to-1A incline with live RoomManager");
   yield return .4f;Check(Object.FindFirstObjectByType<RoomManager>().Current.RoomId=="1A","Live room transition reaches 1A");
   camera.SetAim(Mathf.Atan2(-forward.x,-forward.z)*Mathf.Rad2Deg,-5);
   foreach(var step in Steps(Walk(a,player.RunSpeed,false)))yield return step;
   Check(true,"Ran full incline back to F0 without falling; actual CharacterController.Move per game frame");
  }
  static IEnumerator Walk(Vector3 target,float speed,bool capture)
  {
   float timeout=Time.time+35;while(Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up).magnitude>.25f)
   {if(Time.time>timeout)throw new Exception("Traversal blocked at "+player.transform.position);Vector3 delta=target-player.transform.position;delta.y=0;player.Controller.Move(Vector3.ClampMagnitude(delta,speed*Time.deltaTime)+Vector3.down*3*Time.deltaTime);if(!Ray(player.transform.position+Vector3.up*.5f,Vector3.down,1.5f))throw new Exception("No supporting floor at "+player.transform.position);
    if(capture&&Time.time>=nextImage){Capture(Output+"/frame-"+(images++).ToString("D4")+".png");nextImage=Time.time+.2f;}yield return 0f;}
  }
  static System.Collections.Generic.IEnumerable<object> Steps(IEnumerator source){while(source.MoveNext())yield return source.Current;}
  static void Capture(string path){var c=Camera.main;var rt=RenderTexture.GetTemporary(960,540,24);var old=c.targetTexture;var active=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(960,540,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,960,540),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.Destroy(t);}finally{RenderTexture.active=active;c.targetTexture=old;RenderTexture.ReleaseTemporary(rt);}}
 }
}
