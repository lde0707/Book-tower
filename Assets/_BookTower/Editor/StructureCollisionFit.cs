using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class StructureCollisionFit
 {
  public const string Output="Docs/ArtReview/CollisionFit";
  [Serializable] public class Record {public string room,path,mesh;public Vector3 center,size;public int vertices;public string[] colliders;public bool gravity;}
  [Serializable] public class Inventory {public Record[] meshes;}
  public static string PathOf(Transform t){var parts=new List<string>();while(t){parts.Add(t.name);t=t.parent;}parts.Reverse();return string.Join("/",parts);}
  public static void Audit()
  {
   try{
    EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory("Logs/CollisionFit");
    if(!File.Exists("Logs/CollisionFit/InGame-before.unity"))File.Copy("Assets/Scenes/InGame.unity","Logs/CollisionFit/InGame-before.unity");
    Physics.SyncTransforms();
    var data=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&f.GetComponent<Renderer>()).Select(f=>{
     var r=f.GetComponent<Renderer>();var room=f.GetComponentInParent<Room>();
     return new Record{room=room?room.RoomId:"external",path=PathOf(f.transform),mesh=AssetDatabase.GetAssetPath(f.sharedMesh),center=r.bounds.center,size=r.bounds.size,vertices=f.sharedMesh.vertexCount,gravity=f.GetComponentInParent<BookTower.Puzzles.GravityPagePuzzle>(),colliders=f.GetComponentsInParent<Collider>().Select(c=>c.GetType().Name+":"+c.enabled+":"+c.isTrigger+":"+PathOf(c.transform)+":"+c.bounds).ToArray()};
    }).ToArray();
    File.WriteAllText("Logs/CollisionFit/inventory.json",JsonUtility.ToJson(new Inventory{meshes=data},true));
    EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
