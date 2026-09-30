using UnityEngine;
using UnityEngine.SceneManagement;
namespace BookTower.Rooms
{
 public static class StorybookReadingPassage
 {
  public const string RootName="1A_1B_이야기가깊어지는회랑";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
  static void Loaded(Scene scene,LoadSceneMode mode)
  {if(scene.name!="InGame")return;foreach(var root in scene.GetRootGameObjects())if(root.name==RootName){ClearArrivalRails();if(GameObject.Find(ReadingEntrancePassage.RootName))ReadingEntrancePassage.ClearOldJunction();return;}var asset=Resources.Load<GameObject>("StorybookReadingPassage");if(asset){var instance=Object.Instantiate(asset);instance.name=RootName;SceneManager.MoveGameObjectToScene(instance,scene);ClearArrivalRails();if(GameObject.Find(ReadingEntrancePassage.RootName))ReadingEntrancePassage.ClearOldJunction();}}
  public static void ClearArrivalRails()
  {
   // The enclosed dogleg replaces only the obsolete six-metre arrival rail extensions.
   // Keep the bridge deck, apron floor and sloped guard walls intact.
   Material paper=null;var art=GameObject.Find(RootName);
   if(art)foreach(var renderer in art.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(material&&material.name=="Ivory paper")paper=material;
   foreach(var link in Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None))
   {if(link.FromRoomId!="1A"||link.ToRoomId!="1B")continue;
    var deck=link.GetComponent<Renderer>();if(paper&&deck)deck.sharedMaterial=paper;
    var join=link.transform.parent.Find(link.name+"_접합부");if(!join)continue;
    foreach(Transform child in join)
    {if(child.name.StartsWith("접힌측벽_")&&child.name.EndsWith("_2"))child.gameObject.SetActive(false);
     if(paper&&(child.name=="시작_겹친페이지"||child.name=="도착_겹친페이지")){var surface=child.GetComponent<Renderer>();if(surface)surface.sharedMaterial=paper;}}
   }
  }
 }
}
