using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class ReadingEntranceRepair
 {
  const string Out="Docs/ArtReview/ReadingEntrance";
  public static void Audit()
  {
   try{Directory.CreateDirectory(Out);EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    string report="";var area=new Bounds(new Vector3(154,11,-125),new Vector3(28,10,24));
    foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.bounds.Intersects(area))report+=$"{c.name} / {c.transform.parent?.name} center={c.bounds.center} size={c.bounds.size}\n";
    File.WriteAllText(Out+"/before-colliders.txt",report);Capture("before");EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  static void Capture(string prefix)
  {
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))foreach(var r in room.GetComponentsInChildren<Renderer>())r.enabled=room.RoomId=="1A"||room.RoomId=="1B";
   var cam=new GameObject("Entry review").AddComponent<Camera>();cam.fieldOfView=68;cam.nearClipPlane=.05f;cam.farClipPlane=100;cam.backgroundColor=new Color(1,0,1);cam.clearFlags=CameraClearFlags.SolidColor;
   var shots=new[]{(new Vector3(148,9.8f,-129),new Vector3(160,10.3f,-120),"approach"),(new Vector3(160,9.8f,-118),new Vector3(148,10.3f,-128),"return"),(new Vector3(157.5f,9.8f,-123),new Vector3(160,11,-117),"left-jamb"),(new Vector3(161.8f,9.8f,-122),new Vector3(153,12,-127),"ceiling")};
   foreach(var v in shots){cam.transform.position=v.Item1;cam.transform.LookAt(v.Item2);InteriorF0Builder.Capture(cam,Out+"/"+prefix+"-"+v.Item3+".png");}Object.DestroyImmediate(cam.gameObject);
  }
 }
}
