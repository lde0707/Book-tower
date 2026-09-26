using UnityEngine;
using BookTower.Interaction;
namespace BookTower.UI
{
 [RequireComponent(typeof(Interactable))] public sealed class ReadableNote:MonoBehaviour
 {
  public string id="f0-binder",title="제본사의 기록";
  [TextArea] public string[] pages={"길을 남기려다, 빛을 가두었다.","접힌 자리에도 이야기는 남아 있다."};
  public bool TryRead(Vector3 actor){if(!isActiveAndEnabled||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range)return false;var room=GetComponentInParent<BookTower.Rooms.Room>();if(room&&!room.IsVisible)return false;PaperUI.Instance?.Read(this);return true;}
 }
}
