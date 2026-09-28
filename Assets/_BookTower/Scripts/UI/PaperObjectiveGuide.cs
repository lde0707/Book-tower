using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using BookTower.Interaction;

namespace BookTower.UI
{
    public sealed partial class PaperUI
    {
        Interactable guidedTarget;
        RectTransform guideCard;
        Text guideTitle,guideAction;
        public bool ObjectiveGuideVisible=>guideCard&&guideCard.gameObject.activeInHierarchy;
        public Interactable GuidedTarget=>guidedTarget;
        public void GuideTo(Interactable target){guidedTarget=target;if(!target&&guideCard)guideCard.gameObject.SetActive(false);}
        void UpdateObjectiveGuide()
        {
            var room=guidedTarget?guidedTarget.GetComponentInParent<BookTower.Rooms.Room>():null;
            bool visible=guidedTarget&&ScreenState==PaperScreen.Playing&&room&&room.IsVisible&&objectiveRooms&&objectiveRooms.Current==room;
            if(!visible){if(guideCard)guideCard.gameObject.SetActive(false);return;}
            var camera=Camera.main;if(!camera)return;
            if(!guideCard)
            {
                var panel=Image(hud,"Work book objective tag",0,0,350,94,Color.white,textPaper);
                PaperBox(panel);guideCard=panel.rectTransform;
                guideTitle=Label(guideCard,"제본사의 작업책",12,6,326,40,25,true,TextAnchor.MiddleCenter);
                guideAction=Label(guideCard,"",12,47,326,35,21,false,TextAnchor.MiddleCenter);
            }
            guideCard.gameObject.SetActive(true);
            Vector3 screen=camera.WorldToScreenPoint(guidedTarget.transform.position+Vector3.up*1.05f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud,screen,null,out var point);
            bool outside=screen.z<=0||point.x<185||point.x>1735||point.y> -245||point.y< -785;
            if(screen.z<=0)
            {
                var relative=camera.transform.InverseTransformPoint(guidedTarget.transform.position);
                point=new Vector2(relative.x<0?190:1730,-410);
            }
            point.x=Mathf.Clamp(point.x,190,1730);point.y=Mathf.Clamp(point.y,-785,-245);
            guideCard.anchoredPosition=new Vector2(point.x-175,point.y+100);
            guideTitle.text=outside?(point.x<960?"← 제본사의 작업책":"제본사의 작업책 →"):"제본사의 작업책  ↓";
            guideAction.text=guidedTarget.InRange?((Key)PaperSettingsRuntime.Values.keys[5])+" / 좌클릭 · 읽기":"황금빛이 머무는 펼쳐진 책";
        }
    }
}
