using UnityEngine;
using BookTower.UI;
using BookTower.Rooms;

namespace BookTower.Puzzles
{
    // Projected geometry uses the same distance ratio as the visible paper puppets.
    // No angle combination from the former FoldPanels participates in this puzzle.
    public sealed partial class StorybookLightPuzzle : MonoBehaviour
    {
        public Transform body, ears, bodyShadow, earShadow, bodyPickup, earPickup, drawer;
        public Transform rabbitPopup, rewardStar, toyStar;
        public Light lantern;
        public Transform stage;
        public bool realShadows;
        public const float ScreenDistance=12.5f;
        Vector3 bodyAt=new(-2,1,7),earAt=new(2,1,5.8f);
        public Vector3 BodyPosition=>bodyAt;
        public Vector3 EarPosition=>earAt;
        public bool BodyFound { get; private set; }
        public bool EarsFound { get; private set; }
        public bool BodyInstalled { get; private set; }
        public bool EarsInstalled { get; private set; }
        public bool LampOn { get; private set; }
        public bool Flipped { get; private set; }
        public bool Complete { get; private set; }
        public int BodyStep { get; private set; }
        public int EarStep { get; private set; } = 4;
        public float BodyDistance => realShadows?bodyAt.z:4 + BodyStep;
        public float EarDistance => realShadows?earAt.z:3 + EarStep;
        public float BodyRatio => (realShadows?ScreenDistance:12) / BodyDistance;
        public float EarRatio => (realShadows?ScreenDistance:12) / EarDistance;
        public bool Aligned => Duet ? Duet.RabbitAligned : BodyInstalled && EarsInstalled && LampOn && Flipped &&
            Mathf.Abs(BodyRatio - 1.5f) < .08f && Mathf.Abs(EarRatio - 3f) < .12f &&
            (!realShadows||(Mathf.Abs(bodyAt.x*BodyRatio)<.18f&&Mathf.Abs(earAt.x*EarRatio)<.18f));
        float hold, unfold, toy, shownBody = 4, shownEar = 7;
        string lastGoal;
        RoomManager rooms;Room owner;
        void Awake(){rooms=FindFirstObjectByType<RoomManager>();owner=GetComponentInParent<Room>();if(realShadows){BodyFound=EarsFound=BodyInstalled=EarsInstalled=true;LampOn=true;shownBody=bodyAt.z;shownEar=earAt.z;SetupDuet();}}
        public void DragPaper(bool isEar,Vector3 localPosition)
        {
            if(!realShadows||Complete)return;
            localPosition=new Vector3(Mathf.Clamp(localPosition.x,Duet?-5.5f:-3.3f,Duet?5.5f:3.3f),1,Mathf.Clamp(localPosition.z,3,10));
            if(isEar)earAt=localPosition;else bodyAt=localPosition;
        }
        public void TurnPaper(bool isEar){if(realShadows&&isEar&&!Complete)Flipped=!Flipped;}
        public string Audit => $"active={isActiveAndEnabled} found={BodyFound}/{EarsFound} installed={BodyInstalled}/{EarsInstalled} lamp={LampOn} flip={Flipped} d={BodyDistance}/{EarDistance} shown={shownBody}/{shownEar} aligned={Aligned} hold={hold} complete={Complete}";

        public void Use(StorybookLightControl.Action action, bool reverse = false)
        {
            if (Complete) { if(action==StorybookLightControl.Action.Toy)toy+=1; return; }
            switch(action)
            {
                case StorybookLightControl.Action.FindBody: BodyFound = true; PaperUI.Notify("몸통 오림을 찾았다. 극장의 넓은 종이 꽂이에 끼울 수 있다."); break;
                case StorybookLightControl.Action.FindEars: EarsFound = true; PaperUI.Notify("서랍 속에서 작은 귀 오림을 찾았다."); break;
                case StorybookLightControl.Action.BodyRail:
                    if(realShadows)break;
                    if (!BodyFound) { PaperUI.Notify("몸통이 비어 있다. 청록 책장에 남은 오림 자국을 살펴보자."); break; }
                    if (!BodyInstalled) BodyInstalled = true;
                    else BodyStep = (BodyStep + (reverse ? 6 : 1)) % 7;
                    break;
                case StorybookLightControl.Action.EarRail:
                    if(realShadows)break;
                    if (!EarsFound) { PaperUI.Notify("귀가 비어 있다. 코럴 작업대의 반쯤 열린 서랍을 살펴보자."); break; }
                    if (!EarsInstalled) EarsInstalled = true;
                    else EarStep = (EarStep + (reverse ? 4 : 1)) % 5;
                    break;
                case StorybookLightControl.Action.Flip: Flipped = !Flipped; break;
                case StorybookLightControl.Action.Lamp: LampOn = !LampOn; break;
                case StorybookLightControl.Action.LightLeft: SelectLight(0); break;
                case StorybookLightControl.Action.LightCentre: SelectLight(1); break;
                case StorybookLightControl.Action.LightRight: SelectLight(2); break;
                case StorybookLightControl.Action.Toy: toy += 1; break;
            }
        }

        void Update()
        {
            float dt=Time.deltaTime;
            shownBody=Mathf.MoveTowards(shownBody,BodyDistance,dt*3);
            shownEar=Mathf.MoveTowards(shownEar,EarDistance,dt*3);
            if(body){body.gameObject.SetActive(BodyInstalled);body.localPosition=realShadows?Vector3.MoveTowards(body.localPosition,Duet?Duet.VisualPosition(0,bodyAt):bodyAt,dt*(Duet&&Duet.RabbitRecorded?3:9)):new Vector3(0,1,shownBody);}
            if(ears){ears.gameObject.SetActive(EarsInstalled);ears.localPosition=realShadows?Vector3.MoveTowards(ears.localPosition,Duet?Duet.VisualPosition(1,earAt):earAt,dt*(Duet&&Duet.RabbitRecorded?3:9)):new Vector3(0,1,shownEar);ears.localScale=new Vector3(Flipped?1:-1,1,1);}
            if(bodyPickup)bodyPickup.gameObject.SetActive(!BodyFound);
            if(earPickup)earPickup.gameObject.SetActive(!EarsFound);
            if(drawer)drawer.localPosition=Vector3.Lerp(drawer.localPosition,new Vector3(0,1.2f,EarsFound?-1.6f:-.35f),1-Mathf.Exp(-dt*5));
            if(!realShadows){Shadow(bodyShadow,BodyInstalled,12/shownBody,false);Shadow(earShadow,EarsInstalled,12/shownEar,!Flipped);}
            if(lantern)lantern.intensity=Mathf.Lerp(lantern.intensity,LampOn?(realShadows?ShadowDuet.ProjectionIntensity:16):0,1-Mathf.Exp(-dt*5));
            bool settled=Mathf.Abs(shownBody-BodyDistance)<.01f&&Mathf.Abs(shownEar-EarDistance)<.01f;
            if(realShadows)settled=Vector3.Distance(body.localPosition,bodyAt)<.025f&&Vector3.Distance(ears.localPosition,earAt)<.025f&&!BookTower.Interaction.PaperShadowDrag.Holding;
            hold=Aligned&&settled?hold+dt:0;
            if(Duet){Duet.Tick(dt);if(Duet.AllRecorded)Complete=true;}else if(hold>=1.25f)Complete=true;
            unfold=Mathf.MoveTowards(unfold,Complete?1:0,dt*.4f);
            if(rabbitPopup)rabbitPopup.localRotation=Quaternion.Euler(Mathf.Lerp(86,0,Mathf.SmoothStep(0,1,unfold)),0,0);
            if(rewardStar)rewardStar.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time)*5+unfold*30);
            if(toyStar)toyStar.localRotation=Quaternion.Slerp(toyStar.localRotation,Quaternion.Euler(0,toy*72,Mathf.Sin(Time.time*1.7f)*6),1-Mathf.Exp(-dt*4));
            if(rooms&&rooms.Current==owner&&PaperUI.Instance&&!PaperUI.Instance.StoryActive)
            {
                string goal=Complete?"모아의 이름을 되찾았다 · 별등을 따라 다음 열람실로":
                    !BodyFound||!EarsFound?"토끼의 이름 되찾기 · 작업대와 책장에서 몸통·귀 오림 찾기":
                    !BodyInstalled||!EarsInstalled?"토끼의 이름 되찾기 · 두 오림을 극장의 종이 꽂이에 끼우기":
                    Duet?Duet.Goal:realShadows?"토끼의 이름 되찾기 · 클릭 유지 + 마우스 / 이동 키 · 우클릭 뒤집기":
                    "토끼의 이름 되찾기 · 등과의 거리·귀 방향으로 벽의 윤곽 맞추기";
                if(goal!=lastGoal){PaperUI.Instance.Objective("1A.shadow-workshop",goal);lastGoal=goal;}
            }
        }
        void Shadow(Transform t,bool installed,float ratio,bool flipped)
        {
            if(!t)return;
            t.gameObject.SetActive(installed&&LampOn);
            t.localScale=new Vector3((flipped?-1:1)*ratio,ratio,1);
        }
    }
}
