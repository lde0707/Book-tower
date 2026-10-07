using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object = UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class GravityMigrationAudit
 {
  public static void Run()
  {
   try {
    Directory.CreateDirectory("Logs/GravityMigration");
    EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    var sb=new StringBuilder();
    foreach(var room in Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
     sb.AppendLine($"ROOM {room.RoomId} world={room.transform.position} floor={room.FloorY} bounds={room.WorldBounds}");
     foreach(Transform c in room.transform) {
      sb.AppendLine($" ROOT {c.name} active={c.gameObject.activeSelf} local={c.localPosition} renderers={c.GetComponentsInChildren<Renderer>(true).Length}");
      if(c.gameObject.activeSelf)foreach(Transform d in c)sb.AppendLine($"  CHILD {d.name} local={d.localPosition} scale={d.localScale} active={d.gameObject.activeSelf}");
     }
     foreach(var p in room.GetComponentsInChildren<PuzzleBase>(true))sb.AppendLine($" PUZZLE {p.PuzzleId} {p.GetType().Name} {p.name} active={p.isActiveAndEnabled} local={room.transform.InverseTransformPoint(p.transform.position)}");
     foreach(var n in room.GetComponentsInChildren<BookTower.UI.ReadableNote>(true))sb.AppendLine($" NOTE {n.id} {n.title} {room.transform.InverseTransformPoint(n.transform.position)} active={n.isActiveAndEnabled}");
    }
    foreach(var m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
     if(!m||!m.gameObject.activeInHierarchy)continue;
     if(m is RampGate||m is BindingDoor||m is SolveBridge)sb.AppendLine($"GATE {m.GetType().Name} {m.name} world={m.transform.position} parent={m.transform.parent?.name}");
    }
    File.WriteAllText("Logs/GravityMigration/before-inventory.txt",sb.ToString());
    if(!File.Exists("Logs/GravityMigration/InGame-before.unity"))File.Copy("Assets/Scenes/InGame.unity","Logs/GravityMigration/InGame-before.unity");
    EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
