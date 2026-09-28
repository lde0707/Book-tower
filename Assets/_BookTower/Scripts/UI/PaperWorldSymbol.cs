using UnityEngine;
namespace BookTower.UI
{
 public sealed class PaperWorldSymbol:MonoBehaviour
 {
  public Font font;public string symbol="◇";TextMesh label;BookTower.Rooms.Room room;
  void Start(){room=GetComponentInParent<BookTower.Rooms.Room>();var t=new GameObject("색 구분 문양");t.transform.SetParent(transform,false);t.transform.localPosition=Vector3.up*.9f;label=t.AddComponent<TextMesh>();label.font=font;label.text=symbol;label.anchor=TextAnchor.MiddleCenter;label.characterSize=.13f;label.fontSize=48;label.color=new Color(1,.94f,.75f);label.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
  void LateUpdate(){if(!label)return;var cam=Camera.main;bool on=PaperSettingsRuntime.Values.colorAssist&&BookTower.Core.GameFlow.Instance&&BookTower.Core.GameFlow.Instance.AcceptsPlayerInput&&(!room||room.IsVisible)&&cam&&Vector3.Distance(cam.transform.position,transform.position)<14;label.gameObject.SetActive(on);if(on)label.transform.rotation=cam.transform.rotation;}
 }
}
