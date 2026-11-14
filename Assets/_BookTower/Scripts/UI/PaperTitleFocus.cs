using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BookTower.UI
{
    public sealed class PaperTitleFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image illuminated, leaf;
        public RectTransform plaque;
        public float Highlight { get; private set; }
        bool selected, pressed;
        float age;
        public void OnPointerEnter(PointerEventData e)
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
        }
        public void OnPointerExit(PointerEventData e) { pressed = false; }
        public void OnSelect(BaseEventData e) { selected = true; }
        public void OnDeselect(BaseEventData e) { selected = pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        void Update()
        {
            bool reduced = PaperSettingsRuntime.Values.reduceStoryMotion;
            age += Time.unscaledDeltaTime;
            Highlight = reduced ? (selected ? 1 : 0) : Mathf.MoveTowards(Highlight, selected ? 1 : 0, Time.unscaledDeltaTime * 7);
            float pulse = reduced || PaperSettingsRuntime.Values.reduceFlash ? 1 : .94f + .06f * Mathf.Sin(age * 1.7f);
            illuminated.color = new Color(1, 1, 1, Highlight * pulse);
            leaf.color = new Color(1, 1, 1, Highlight);
            plaque.localScale = Vector3.one * (reduced ? 1 : pressed ? .985f : 1 + .022f * Highlight);
            leaf.rectTransform.anchoredPosition = new Vector2(-125, reduced ? 0 : Mathf.Sin(age * 1.4f) * .7f * Highlight);
        }
    }
}
