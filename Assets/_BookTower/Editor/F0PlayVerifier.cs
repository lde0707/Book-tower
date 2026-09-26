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
        static Keyboard keyboard;static InputSettings originalSettings;
        [InitializeOnLoadMethod]static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        public static void Run(){SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {report.Clear();started=EditorApplication.timeSinceStartup;next=0;frame=-1;routine=Verify();EditorApplication.update+=Tick;}
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
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(originalSettings!=null)InputSystem.settings=originalSettings;
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
                if(Time.time>limit)throw new Exception("Walking blocked at "+player.transform.position+" toward "+goal);
                Vector3 d=goal-player.transform.position;d.y=0;player.Controller.Move(Vector3.ClampMagnitude(d,5.5f*Time.deltaTime)+Vector3.down*3*Time.deltaTime);
                RequireHeight();yield return 0f;
            }
        }
        static void RequireHeight(){if(player.transform.position.y<room.transform.position.y-.2f)throw new Exception("Player fell below room floor");}
        static IEnumerator Verify()
        {
            yield return 1.2f;
            player=Object.FindFirstObjectByType<PlayerController>();camera=Object.FindFirstObjectByType<PaperCamera>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            Require(player!=null&&camera!=null,"Player and camera exist");RequireHeight();Require(player.Controller.isGrounded,"Player grounded after startup");
            foreach(Transform t in room.transform)if(t.name!="F0_Gameplay"&&t.name!=InteriorF0Builder.RootName)Require(!t.gameObject.activeSelf,"Legacy root inactive: "+t.name);
            camera.SetAim(0,10);yield return .8f;InteriorF0Builder.Capture(Camera.main,Output+"/F0-play-start.png");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(35,-1),0),"Q from 35 degrees -> world 0");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(35,1),90),"E from 35 degrees -> world 90");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(0,-1),270),"Q wraps 0 -> 270");
            Require(Mathf.Approximately(PaperCamera.CardinalYaw(270,1),0),"E wraps 270 -> 0");
            camera.SetAim(35,10);camera.LockObservation(1);camera.LockObservation(1);yield return 1f;
            Require(Mathf.Abs(Mathf.DeltaAngle(camera.YawDegrees,180))<.1f,"Rapid E E reaches exact world 180");
            float yaw=camera.ConsumeMovementYaw();Require(!camera.IsObservationLocked&&Mathf.Abs(Mathf.DeltaAngle(yaw,180))<.01f,"Movement releases observation without movement-axis jump");
            camera.SetAim(0,10);camera.ApplyLookDelta(new Vector2(250,0));Require(Mathf.Abs(camera.AimYaw-30)<.01f,"Mouse delta rotates aim independently of spirit facing");
            originalSettings=InputSystem.settings;var testSettings=Object.Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=testSettings;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            Teleport(World(0,-10));camera.SetAim(0,10);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return .65f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .35f;
            Vector3 moved=room.transform.InverseTransformPoint(player.transform.position);Require(moved.x>1.5f&&Mathf.Abs(moved.z+10)<.25f,"D strafes right without camera/avatar feedback spiral");
            camera.SetAim(0,10);Teleport(World(-9.5f,.7f));yield return .3f;
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
            foreach(var wait in Enumerate(Walk(World(3,18.3f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(23,18.3f))))yield return wait;
            foreach(var wait in Enumerate(Walk(World(26,11.2f))))yield return wait;
            RequireHeight();Require(true,"Walked both fragments, assembly, open door and exit corridor without falling");
        }
        static System.Collections.Generic.IEnumerable<object> Enumerate(IEnumerator steps){while(steps.MoveNext())yield return steps.Current;}
    }
}
