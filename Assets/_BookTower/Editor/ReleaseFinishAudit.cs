using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class ReleaseFinishAudit
 {
  public const string Output="Docs/ArtReview/ReleaseArt/08-Final";
  public static void Run()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory(Output);var b=new StringBuilder();
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target))
   {
    b.AppendLine($"ROOM {room.RoomId} world={room.transform.position} bounds={room.WorldBounds}");
    foreach(var t in room.GetComponentsInChildren<Transform>(true))
    {
     int depth=0;for(var p=t;p&&p!=room.transform;p=p.parent)depth++;
     if(depth<=3||t.name.Contains("작업책")||t.name.Contains("안내")||t.name.Contains("둥근")||t.name.Contains("모빌")||t.name.Contains("천장"))
     {var renderer=t.GetComponent<Renderer>();b.AppendLine($"{ReleaseLightingReview.PathOf(t)} local={t.localPosition:F2} room={room.transform.InverseTransformPoint(t.position):F2} scale={t.localScale:F2} bounds={(renderer?renderer.bounds.ToString():"none")}");}
    }
    foreach(var c in room.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c&&(c is FoldBridgePuzzle||c is StorybookLightPuzzle||c is ReadingRoomPuzzle||c.GetType().Name.Contains("Guide"))))b.AppendLine(c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
   }
   File.WriteAllText(Output+"/scene-inventory.txt",b.ToString());EditorApplication.Exit(0);
  }
 }
}
