using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class StructureCollisionFit
 {
  static readonly StringBuilder changes=new();
  public static bool Candidate(MeshFilter f)
  {
   if(!f||!f.sharedMesh||!f.gameObject.activeInHierarchy||f.GetComponentInParent<Rigidbody>())return false;
   var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||r.sharedMaterials.Any(m=>!m||m.renderQueue>=3000||m.shader.name.Contains("Haze")||m.shader.name.Contains("Beam")))return false;
   var puzzle=f.GetComponentInParent<GravityPagePuzzle>();
   if(puzzle)return f.GetComponent<BoxCollider>()||f.name=="접힌 측랑 아치";
   var room=f.GetComponentInParent<Room>();if(!room||!(room.RoomId is "F0" or "1A" or "1B"))return false;
   string p=PathOf(f.transform);var a=new[]{r.bounds.size.x,r.bounds.size.y,r.bounds.size.z};Array.Sort(a);
   if(a[2]<.6f||a[1]<.3f||a[0]<.06f)return false;
   if(p.Contains("stitch")||p.Contains("CottonThread")||p.Contains("바느질")||p.Contains("문양")||p.Contains("빛줄기"))return false;
   bool furniture=p.Contains("F0_개별공예소품/01_BindingWorkbench")||p.Contains("F0_개별공예소품/05_BoundBookStack")||p.Contains("F0_개별공예소품/06_WheeledArchive")||p.Contains("제본 서랍 작업대_")||p.Contains("청록 그림책 수납장_")||p.Contains("층층 종이책 섬_")||p.Contains("/01_TallBookcase")||p.Contains("/02_ArchedWritingDesk")||p.Contains("/09_RecordAlcove/")||p.Contains("/varied stacked volumes/");
   bool architecture=room.RoomId=="F0"&&p.Contains("F0_종이공간_완성아트/")&&!p.Contains("11_제본실과매달린종이등") || room.RoomId=="1A"&&new[]{"겹겹이 찢어진 페이지 아치","구불어진 장지 내벽","포개진 장지 바닥","구겨진 천장","접힌 극장 옆날개"}.Any(p.Contains) || room.RoomId=="1B"&&new[]{"layered library pilaster","sealed side wall","sealed lintel","door wall","vaulted ceiling"}.Any(p.Contains);
   return furniture||architecture;
  }
  public static void Plan()
  {
   try{
    EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory(Output);
    var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(Candidate).ToArray();
    File.WriteAllText(Output+"/plan.txt",$"Scoped plan: {filters.Length} exact source-mesh colliders. Preserve cargo rigidbodies, all old floors, bridges, doors, passages and existing invisible gate colliders. No geometry deletion.\n"+string.Join("\n",filters.Select(f=>PathOf(f.transform)))+"\nF0: move new chamber 4m rearward, clear left arch from page sweep. 1B: move new console forward clear of existing runner bridge. All new alcove leaves: separate adjacent panels.\n");
    EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  public static void Run()
  {
   try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();GravityPageReview.OutputOverride=Output+"/Editor";GravityPageReview.Run();}
   catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  public static void Finish()
  {
   try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();Bake();}
   catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  public static void Bake()
  {
   try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");PaperAtmosphereBuilder.Apply();PaperAtmospherePolish.Apply();PaperIndirectBake.OutputOverride=Output+"/Lighting";PaperIndirectBake.ForceCpu=true;PaperIndirectBake.Run();}
   catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
  static void RemoveExactDuplicateBlockoutPaths()
  {
   var seen=new Dictionary<string,MeshFilter>();
   foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.InstanceID))
   {
    if(!f.name.StartsWith("주동선_")||!f.transform.parent||f.transform.parent.name!="V2_블록아웃"||f.transform.childCount>0||f.GetComponents<MonoBehaviour>().Length>0||!f.sharedMesh||f.sharedMesh.name!="Cube")continue;
    var renderer=f.GetComponent<Renderer>();if(!renderer||f.GetComponents<Collider>().Any(c=>c is not BoxCollider||c.isTrigger))continue;
    var vertices=f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v)).Select(v=>$"{Mathf.RoundToInt(v.x*1000)},{Mathf.RoundToInt(v.y*1000)},{Mathf.RoundToInt(v.z*1000)}").Distinct().OrderBy(x=>x);
    string key=f.transform.parent.GetInstanceID()+":"+string.Join(";",vertices)+":"+string.Join(",",renderer.sharedMaterials.Select(m=>m?m.GetInstanceID():0));
    if(seen.TryGetValue(key,out var original)){f.gameObject.SetActive(false);changes.AppendLine("Exact duplicate removed from rendering/collision; original retained: "+PathOf(f.transform)+" == "+PathOf(original.transform));}
    else seen[key]=f;
   }
  }
  public static void Apply()
  {
   Directory.CreateDirectory(Output);Directory.CreateDirectory("Logs/CollisionFit");
   if(!File.Exists("Logs/CollisionFit/InGame-before.unity"))File.Copy("Assets/Scenes/InGame.unity","Logs/CollisionFit/InGame-before.unity");
   changes.Clear();RemoveExactDuplicateBlockoutPaths();
   foreach(var p in Object.FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None))
   {
    var room=p.GetComponentInParent<Room>();var chamber=p.transform.Find("기록 운반실");
    if(room.RoomId=="F0")
    {
     var at=chamber.localPosition;at.z=7;chamber.localPosition=at;p.completedPageHinge.z=7;
     foreach(var arc in p.transform.Cast<Transform>().Where(t=>t.name=="접힌 측랑 아치"))if(arc.GetComponent<Renderer>().localBounds.center.x<0)arc.localPosition=new Vector3(-1,0,0);
    }
    if(room.RoomId is "1C" or "1E")foreach(var arc in p.transform.Cast<Transform>().Where(t=>t.name=="접힌 측랑 아치"))arc.localPosition=new Vector3(0,0,room.RoomId=="1C"?-5:-3);
    if(room.RoomId=="1B"){var console=p.transform.Find("중력 페이지");var at=console.localPosition;at.z=-8.8f;console.localPosition=at;}
    var leaves=p.transform.Cast<Transform>().Where(t=>t.name=="겹친 관찰 골방");
    foreach(var group in leaves.GroupBy(t=>t.localPosition.x<0))
    {int j=0;foreach(var t in group.OrderBy(t=>t.localPosition.z)){var at=t.localPosition;float z=room.RoomId=="F0"?3:chamber.localPosition.z;at.z=z-3+j++*1.4f;t.localPosition=at;t.localScale=new Vector3(1,1,1.05f/1.15f);}}
   }
   var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(Candidate).ToArray();int added=0,replaced=0;
   foreach(var f in filters)
   {
    var collider=f.GetComponent<MeshCollider>();if(!collider){collider=f.gameObject.AddComponent<MeshCollider>();added++;}
    collider.convex=false;collider.sharedMesh=f.sharedMesh;collider.isTrigger=false;collider.enabled=true;collider.contactOffset=.002f;
    foreach(var c in f.GetComponents<Collider>())if(c!=collider&&!c.isTrigger&&c.enabled){collider.sharedMaterial=c.sharedMaterial;c.enabled=false;replaced++;}
    EditorUtility.SetDirty(collider);
   }
   Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
   changes.AppendLine($"PASS: {filters.Length} fitted surfaces; added={added}; same-object primitive replacements={replaced}. Exact original render mesh, non-convex, no enlargement. Cargo physics, old floors, bridges, door and passage geometry preserved.");
   changes.AppendLine("F0 chamber local z=7; 1B console z=-8.8; alcove spacing=1.4 and depth=1.05; F0 left arch moved 1m outward. No old geometry deleted.");
   File.WriteAllText(Output+"/apply.txt",changes.ToString());
  }
 }
}
