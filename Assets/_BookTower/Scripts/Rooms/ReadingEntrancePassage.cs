using UnityEngine;
using UnityEngine.SceneManagement;
namespace BookTower.Rooms
{
 public static class ReadingEntrancePassage
 {
  public const string RootName="1B_문틀에맞춘연속회랑";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
  static void Loaded(Scene scene,LoadSceneMode mode)
  {if(scene.name!="InGame")return;Ensure();}
  public static void Ensure()
  {if(!GameObject.Find(RootName)){var prefab=Resources.Load<GameObject>("ReadingEntrancePassage");if(!prefab)return;var instance=Object.Instantiate(prefab);instance.name=RootName;}
   ClearOldJunction();}
  public static void ClearOldJunction()
  {
   StorybookReadingPassage.ClearArrivalRails();
   // The former ramp enclosure extends two metres past the landing. Its end pierced the turn.
   var rampShell=GameObject.Find("1A_다음열람실_닫힌통로");if(rampShell)foreach(Transform wall in rampShell.transform)
    if(wall.name=="책장 통로 연속 벽")
    {var mesh=wall.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh){float length=mesh.sharedMesh.bounds.size.z;wall.localScale=new Vector3(1,1,(length-3.5f)/length);wall.localPosition=new Vector3(wall.localPosition.x,wall.localPosition.y,-1.75f);}}
   var old=GameObject.Find("ReadingRoom1B_Connections");if(old)foreach(Transform child in old.transform)
    if(child.name=="sealed approach passage"&&Vector3.Distance(child.position,new Vector3(151,8,-127))<.2f)child.gameObject.SetActive(false);
   var art=GameObject.Find(StorybookReadingPassage.RootName);if(art){var approach=art.transform.Find("열람실 앞 굽은 회랑");if(approach)approach.gameObject.SetActive(false);}
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))if(room.RoomId=="1B")
    foreach(var t in room.GetComponentsInChildren<Transform>(true))if(t.name=="layered entry arch"&&t.localPosition==Vector3.zero)
    {var mesh=t.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh.bounds.center.z< -20)t.gameObject.SetActive(false);}
  }
 }
}
