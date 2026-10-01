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
 public static class ReleaseReadingStructureCheck
 {
  public static void Run()
  {
   string result;int code=0;
   try
   {
    EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1B");var root=room.transform.Find("ReadingRoom1B");Physics.SyncTransforms();int floors=0,roofs=0,walls=0,ends=0;
    for(int x=-17;x<=17;x+=2)for(int z=-21;z<=21;z+=2)
    {var p=room.transform.TransformPoint(new Vector3(x,1,z));if(!Physics.RaycastAll(p,Vector3.down,3).Any(h=>h.collider.transform.IsChildOf(root)))throw new Exception("1B floor gap: "+p);floors++;
     if(!Physics.RaycastAll(p,Vector3.up,23).Any(h=>h.collider.name.Contains("vault")||h.collider.name.Contains("ceiling backing")))throw new Exception("1B ceiling gap: "+p);roofs++;}
    foreach(float y in new[]{1f,4,9,14})for(int z=-20;z<=20;z+=2)foreach(int side in new[]{-1,1})
    {var p=room.transform.TransformPoint(new Vector3(0,y,z));if(!Physics.RaycastAll(p,Vector3.right*side,19).Any(h=>h.collider.name=="sealed side wall"))throw new Exception("1B side wall gap: "+p);walls++;}
    foreach(float y in new[]{8f,12,16,18.5f})for(int x=-16;x<=16;x+=2)foreach(int side in new[]{-1,1})
    {var p=room.transform.TransformPoint(new Vector3(x,y,0));if(!Physics.RaycastAll(p,Vector3.forward*side,23).Any(h=>h.collider.name.StartsWith("door wall")||h.collider.name=="sealed lintel"))throw new Exception("1B end wall gap: "+p);ends++;}
    int renderers=0;foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy)){if(r.sharedMaterials.Any(m=>!m||m.shader.name=="Hidden/InternalErrorShader"))throw new Exception("Missing material: "+r.name);renderers++;}
    result=$"PASS: saved 1B shell: floors={floors}, roofs={roofs}, side walls={walls}, upper end walls={ends}; active renderer material bindings={renderers}.\nIntentional door openings excluded. Sampled collision coverage is not proof of all visible seams. Separate passage-camera and CharacterController checks are required.\n";
   }
   catch(Exception e){result="FAIL: "+e;code=1;}
   Directory.CreateDirectory(ReleaseLightingReview.Output);File.WriteAllText(ReleaseLightingReview.Output+"/1B-structure.txt",result);EditorApplication.Exit(code);
  }
 }
}
