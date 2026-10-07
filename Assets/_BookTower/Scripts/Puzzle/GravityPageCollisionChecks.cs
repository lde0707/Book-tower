using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
namespace BookTower.Puzzles
{
 public sealed partial class GravityPageBuildProbe
 {
  IEnumerator VerifyStructureCollision(PlayerController player,PaperCamera camera)
  {
   foreach(var puzzle in FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None))
   {
    var room=puzzle.GetComponentInParent<Room>();var surfaces=puzzle.GetComponentsInChildren<MeshCollider>();
    Check(surfaces.Length>0&&surfaces.All(c=>!c.convex&&c.GetComponent<MeshFilter>()&&c.sharedMesh==c.GetComponent<MeshFilter>().sharedMesh),room.RoomId+" structural colliders use exact render mesh without expanded hull");
    var console=puzzle.transform.Find("중력 페이지");var core=console.Find("접힌 책 기둥").GetComponent<MeshCollider>();
    if(core)
    {
     float floor=console.position.y;var a=new Vector3(core.bounds.center.x,floor+.04f,core.bounds.min.z-1f);var b=a+Vector3.forward*2.5f;
     Place(player,a);yield return null;room.SetVisible(true);
     for(int i=0;i<100;i++)player.Controller.Move(Vector3.forward*.025f);
     Check(player.transform.position.z<core.bounds.min.z+.1f,room.RoomId+" actual player capsule blocked by console, z="+player.transform.position.z.ToString("F3"));
    }
    // The opening under an arch must stay open; an enclosing box would fail this.
    var arch=puzzle.GetComponentsInChildren<MeshCollider>().FirstOrDefault(c=>c.name=="접힌 측랑 아치"&&c.bounds.center.x>puzzle.transform.position.x);
    if(arch)
    {
     var a=arch.bounds.center+new Vector3(0,-arch.bounds.extents.y-.9f,-.8f);a.y=room.RoomId=="F0"?3.1f:room.FloorY+.1f;
     // Place on the real raised floor, not inside the old blockout ramp.
     var ground=Physics.RaycastAll(new Vector3(a.x,a.y+2.8f,a.z),Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.5f&&h.collider.GetComponentInParent<Room>()==room).OrderByDescending(h=>h.point.y).FirstOrDefault();
     if(ground.collider)a.y=ground.point.y+.05f;
     Place(player,a);yield return null;
     for(int i=0;i<64;i++)player.Controller.Move(new Vector3(0,-.02f,.025f));
     Check(player.transform.position.z>a.z+1.45f,room.RoomId+" arch opening remains traversable, start="+a+", actual="+player.transform.position);
    }
    if(room.RoomId is "F0" or "1A" or "1B")
    {
     var furniture=room.GetComponentsInChildren<MeshCollider>().Where(c=>!c.GetComponentInParent<GravityPagePuzzle>()&&c.bounds.size.y>.9f&&c.bounds.size.y<15f).FirstOrDefault(c=>c.transform.parent&&(c.transform.parent.name.Contains("06_WheeledArchive")&&c.name=="장식_UmberBook"||c.transform.parent.name.Contains("수납장_")||c.transform.parent.name.Contains("TallBookcase")));
     if(furniture)
     {
      var centre=furniture.bounds.center;var origin=centre+Vector3.back*(furniture.bounds.extents.z+2);
      if(furniture.Raycast(new Ray(origin,Vector3.forward),out var hit,8))
      {
       var startAt=hit.point+hit.normal*(player.Controller.radius+.7f)-Vector3.up*player.Controller.center.y;
       Place(player,startAt);yield return null;
       for(int i=0;i<80;i++)player.Controller.Move(-hit.normal*.02f);
       Check(Vector3.Dot(player.transform.position+Vector3.up*player.Controller.center.y-hit.point,hit.normal)>-.02f,room.RoomId+" actual capsule blocked by fitted furniture surface, target="+furniture.name+", normal="+hit.normal+", actor="+player.transform.position);
      }
     }
     var wall=puzzle.GetComponentsInChildren<MeshCollider>().First(c=>c.name=="겹친 관찰 골방");
     var a=wall.bounds.center+wall.transform.right*.8f;a.y=console.position.y+.06f;
     Place(player,a);yield return null;
     Vector3 start=player.transform.position;for(int i=0;i<70;i++)player.Controller.Move(-wall.transform.right*.02f);
     Check(Vector3.Dot(player.transform.position-wall.bounds.center,wall.transform.right)>0,room.RoomId+" actual player capsule blocked by alcove panel");
     camera.SetAim(0,-14);var at=console.position+new Vector3(-1.3f,.15f,-3.3f);Place(player,at);yield return new WaitForSecondsRealtime(3);Capture(room.RoomId+"-collision-layout");
    }
   }
   var reading=FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1B");
   var gp=reading.GetComponentInChildren<GravityPagePuzzle>();var panel=gp.transform.Find("중력 페이지/펼친 조작 페이지").GetComponent<Renderer>().bounds;
   var bridge=reading.transform.Find("ReadingRoom1B/07_RunnerBridge");
   Check(bridge&&!bridge.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.Intersects(panel)),"1B console no longer intersects old runner bridge");
   Flush();
  }
 }
}
