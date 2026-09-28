using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace BookTower.UI
{
 public sealed class PaperMenuHighlight:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
 {
  public Image strip;public GameObject ribbon;
  public void Set(bool on){if(strip)strip.color=on?new Color(.86f,.74f,.58f,.88f):new Color(1,1,1,.72f);if(ribbon)ribbon.SetActive(on);}
  public void OnPointerEnter(PointerEventData e){EventSystem.current?.SetSelectedGameObject(gameObject);Set(true);}
  public void OnPointerExit(PointerEventData e){if(EventSystem.current&&EventSystem.current.currentSelectedGameObject==gameObject)EventSystem.current.SetSelectedGameObject(null);Set(false);}
  public void OnSelect(BaseEventData e)=>Set(true);
  public void OnDeselect(BaseEventData e)=>Set(false);
 }
}
