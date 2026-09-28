using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class ReadingRoomBuilder
 {
  public static void Capture()
  {
   var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToDictionary(r=>r,r=>r.enabled);
   var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).ToDictionary(l=>l,l=>(l.enabled,l.intensity));
   foreach(var r in renderers.Keys){var owner=r.GetComponentInParent<Room>();if(owner)r.enabled=owner==room;else if(r.GetComponentInParent<RoomBound>())r.enabled=r.transform.root.name=="ReadingRoom1B_Connections";}
   foreach(var l in lights.Keys){var owner=l.GetComponentInParent<Room>();if(owner)l.enabled=owner==room;else l.enabled=false;}
   var oldMode=RenderSettings.ambientMode;var oldProbe=RenderSettings.ambientProbe;var oldAmbient=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.28f,.34f);RenderSettings.fog=false;var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(pass>=2?new Color(.42f,.40f,.45f):new Color(.20f,.20f,.25f));RenderSettings.ambientProbe=sh;
   var go=new GameObject("Reading room GPU review");var cam=go.AddComponent<Camera>();cam.fieldOfView=60;cam.nearClipPlane=.07f;cam.farClipPlane=95;cam.allowHDR=true;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
   string path=Out+"/Iterations/Pass"+pass;
   try
   {
    var views=new[]{(V(0,2.7f,-19),V(0,7,12),"01-front"),(V(7,3,-14),V(-11,5,-4),"02-left"),(V(-6,3,-10),V(12,6,7),"03-right"),(V(0,3,18),V(-3,5,-17),"04-back"),(V(-5,1.8f,-18),V(-8,1.2f,-16),"05-lantern"),(V(6,2.6f,13),V(1,6,21),"06-window")};
    foreach(var v in views){cam.transform.position=room.transform.TransformPoint(v.Item1);cam.transform.LookAt(room.transform.TransformPoint(v.Item2));InteriorF0Builder.Capture(cam,path+"/"+v.Item3+".png");}
   }
   finally{Object.DestroyImmediate(go);RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientProbe=oldProbe;RenderSettings.fog=fog;foreach(var v in renderers)if(v.Key)v.Key.enabled=v.Value;foreach(var v in lights)if(v.Key){v.Key.enabled=v.Value.Item1;v.Key.intensity=v.Value.Item2;}}
  }
  public static void Structure()
  {
   Physics.SyncTransforms();var report=new StringBuilder();int floor=0,roof=0,wall=0;
   for(int x=-17;x<=17;x+=2)for(int z=-21;z<=21;z+=2)
   {
    var p=room.transform.TransformPoint(V(x,1,z));
    if(!Physics.RaycastAll(p,Vector3.down,3).Any(h=>h.collider.transform.IsChildOf(root)))throw new Exception("Floor gap "+p);floor++;
    if(!Physics.RaycastAll(p,Vector3.up,23).Any(h=>h.collider.name.Contains("vault")||h.collider.name.Contains("ceiling backing")))throw new Exception("Roof gap "+p);roof++;
   }
   foreach(float y in new[]{1f,4,9,14})for(int z=-20;z<=20;z+=2)foreach(int s in new[]{-1,1})
   {var p=room.transform.TransformPoint(V(0,y,z));if(!Physics.RaycastAll(p,Vector3.right*s,19).Any(h=>h.collider.name=="sealed side wall"))throw new Exception("Wall gap "+p);wall++;}
   foreach(var f in root.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.sqrMagnitude))throw new Exception("Invalid mesh "+f.name);
   int ends=0;foreach(float y in new[]{8f,12,16,18.5f})for(int x=-16;x<=16;x+=2)foreach(int side in new[]{-1,1}){var p=room.transform.TransformPoint(V(x,y,0));if(!Physics.RaycastAll(p,Vector3.forward*side,23).Any(h=>h.collider.name.StartsWith("door wall")||h.collider.name=="sealed lintel"))throw new Exception("End wall gap "+p);ends++;}
   report.AppendLine("PASS: upper end-wall closure samples="+ends);
   if(!puzzle.legacyPuzzle||!puzzle.doorBlocker||!puzzle.recordSocket||!puzzle.exitSocket)throw new Exception("Missing puzzle binding");
   report.AppendLine("PASS: floor="+floor+" roof="+roof+" side wall="+wall+" sampled rays. Door openings intentionally excluded.");
   report.AppendLine("PASS: finite mesh vertices and P3/control/door bindings.");
   report.AppendLine("Sampling is not an exhaustive proof of all cracks; visual views and play traversal are separate checks.");
   File.WriteAllText(Out+"/Iterations/Pass"+pass+"/structure.txt",report.ToString());
  }
 }
}
