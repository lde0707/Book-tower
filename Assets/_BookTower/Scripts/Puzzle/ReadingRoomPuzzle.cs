using UnityEngine;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.UI;
using BookTower.Core;
namespace BookTower.Puzzles
{
    // The lantern is a reusable resource, not a sequence of mandatory note-read flags.
    public sealed class ReadingRoomPuzzle : MonoBehaviour
    {
        public enum LampState { Empty, Charged, Carried, RecordDocked, ExitDocked }
        public PuzzleBase legacyPuzzle;
        public Transform lantern, chargingSocket, recordSocket, exitSocket, windowChargeSocket, servicePanel, crank, shutter, latch, door;
        public Transform leftRing, rightRing, chargeRibbon, pulley, exitFlower;
        public Collider doorBlocker, serviceBlocker;
        public Light lanternLight, windowLight, recordLight;
        public LampState State { get; private set; }
        public bool Charged { get; private set; }
        public bool LeftLocked { get; private set; }
        public bool RightLocked { get; private set; }
        public bool RecordOpen { get; private set; }
        public bool HasCrank { get; private set; }
        public bool WindowRaised { get; private set; }
        public bool WindowLatched { get; private set; }
        public bool Complete { get; private set; }
        public int FailedExitAttempts { get; private set; }
        Room room; PlayerController player; Vector3 serviceRest, shutterRest, doorRest;
        float doorOpen, reveal, windowOpen, failedPulse; bool entered;
        Renderer[] panes;MaterialPropertyBlock paneBlock;
        void Awake()
        {
            ReadingLanternPresentation.Apply(this);
            room=GetComponentInParent<Room>(); player=FindFirstObjectByType<PlayerController>();
            serviceRest=servicePanel.localPosition; shutterRest=shutter.localPosition; doorRest=door.localPosition;
            if(legacyPuzzle)legacyPuzzle.enabled=false;
            panes=System.Array.FindAll(lantern.GetComponentsInChildren<Renderer>(),r=>r.name=="translucent raven paper");paneBlock=new MaterialPropertyBlock();
        }
        public bool Use(ReadingRoomControl.Action action)
        {
            if(Complete && action!=ReadingRoomControl.Action.Recover) return false;
            if(State==LampState.Carried&&(action==ReadingRoomControl.Action.LeftRing||action==ReadingRoomControl.Action.RightRing))
            {Say("나","먼저 문양이 있는 받침에 등불을 내려놓자. 그다음 걸쇠를 열 수 있어.");return false;}
            switch(action)
            {
                case ReadingRoomControl.Action.Charge:
                    State=LampState.Charged; Charged=true; LeftLocked=RightLocked=false; Dock(chargingSocket);
                    Say("기억등", "빛이 종이 띠에 차올랐다. 양쪽 보존 고리가 열려 있다."); return true;
                case ReadingRoomControl.Action.LeftRing: LeftLocked=!LeftLocked; LockFeedback("왼쪽",LeftLocked); ReleaseDockedLight(); return true;
                case ReadingRoomControl.Action.RightRing: RightLocked=!RightLocked; LockFeedback("오른쪽",RightLocked); ReleaseDockedLight(); return true;
                case ReadingRoomControl.Action.Pickup:
                    if(!Charged){Say("나","아직 빛이 담겨 있지 않아.");return false;}
                    if(!LeftLocked||!RightLocked){Say("나","빛을 옮기려면 양쪽 고리를 잠가야겠어.");return false;}
                    State=LampState.Carried;Say("나","빛을 담아 옮길 수 있어. 등 밑의 홈과 같은 받침을 찾아보자.");return true;
                case ReadingRoomControl.Action.RecordDock:
                    if(!CanDock())return false;
                    State=LampState.RecordDocked;Dock(recordSocket);
                    Say("나","홈에 꼭 맞아. 이제 등 앞의 두 걸쇠를 열면 빛을 건넬 수 있어.");ReleaseDockedLight();return true;
                case ReadingRoomControl.Action.TakeCrank:
                    if(!RecordOpen||HasCrank)return false;HasCrank=true;crank.gameObject.SetActive(false);
                    Say("나","부러진 게 아니라 빼 두었던 손잡이였어.");return true;
                case ReadingRoomControl.Action.Crank:
                    if(!HasCrank){Say("나","축만 헛돌아. 손잡이가 빠져 있어.");return false;}
                    WindowRaised=!WindowRaised;if(!WindowRaised)WindowLatched=false;return true;
                case ReadingRoomControl.Action.Latch:
                    if(!WindowRaised){Say("나","창이 내려와 있어 걸 수 없어.");return false;}
                    WindowLatched=true;Say("모아","이제 방 안에도 빛이 들어와.");return true;
                case ReadingRoomControl.Action.WindowCharge:
                    if(!WindowLatched){Say("나","이 창으로는 아직 빛이 들어오지 않아.");return false;}
                    State=LampState.Charged;Charged=true;LeftLocked=RightLocked=false;Dock(windowChargeSocket);return true;
                case ReadingRoomControl.Action.ExitDock:
                    if(!CanDock())return false;
                    State=LampState.ExitDocked;Dock(exitSocket);
                    Say("나","문 앞 받침에 맞물렸어. 등 앞의 두 걸쇠로 빛을 열어 보자.");
                    return ReleaseDockedLight();
                case ReadingRoomControl.Action.Recover:
                    State=Charged?LampState.Charged:LampState.Empty;Dock(chargingSocket);return true;
                case ReadingRoomControl.Action.Toy: Say("모아","책벌레들도 따뜻한 빛을 좋아하나 봐.");return true;
            }
            return false;
        }
        bool CanDock()
        {
            if(Charged&&(State==LampState.RecordDocked||State==LampState.ExitDocked)){Say("나","등은 이미 받침 위에 있어. 등 앞의 두 걸쇠를 열면 돼.");return false;}
            if(State!=LampState.Carried||!Charged){Say("나","빛이 담긴 기억등을 가져와야 해.");return false;}
            return true;
        }
        bool ReleaseDockedLight()
        {
            if(!Charged||LeftLocked||RightLocked)return true;
            if(State==LampState.RecordDocked)
            {
                Charged=false;RecordOpen=true;if(serviceBlocker)serviceBlocker.enabled=false;
                Say("제본사의 기록","창문은 안에서 닫았다. 수선한 뒤 다시 열 수 있도록 손잡이는 점검대에 두었다.");
                PaperUI.Instance?.RecordStory("1B.Record","안쪽에서 닫힌 창","닫힌 창 아래에는 손잡이 축만 남아 있다. 점검대에 보관한 손잡이로 다시 열 수 있다.");
            }
            else if(State==LampState.ExitDocked)
            {
                Charged=false;
                if(!WindowLatched){FailedExitAttempts++;failedPulse=2.8f;Say("나","등에는 반응했지만, 위쪽 도르래가 헛돌았어.");return false;}
                Complete=true;if(legacyPuzzle)legacyPuzzle.Solve();if(doorBlocker)doorBlocker.enabled=false;
                PaperUI.Instance?.RecordStory("M03","닫힌 문","보호하려 닫았던 문. 이제 빛을 다시 들일 수 있다.");
                Say("모아","밖에서 잠근 게 아니었네. 누가 여기 있었던 걸까?");
            }
            return true;
        }
        void Dock(Transform target){lantern.position=target.position;lantern.rotation=target.rotation;}
        void LockFeedback(string side,bool locked){Say("기억등",side+(locked?" 걸쇠가 맞물렸다. 빛을 안에 보존한다.":" 걸쇠가 열렸다. 이쪽 틈으로 빛이 나온다."));}
        void Say(string who,string message){PaperUI.Instance?.Bark(who,message,5);}
        void Update()
        {
            bool visible=room&&room.IsVisible;
            if(visible&&player&&room.Contains(player.transform.position)&&!entered)
            {entered=true;Say("모아","이 방은 안쪽에서 닫은 것 같아. 누군가 돌아올 생각이었을까?");}
            if(State==LampState.Carried&&player)
            {
                var camera=Camera.main;var right=camera?camera.transform.right:player.transform.right;
                var forward=camera?Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized:player.transform.forward;
                lantern.position=Vector3.Lerp(lantern.position,player.transform.position+Vector3.up*1.05f+right*1.35f+forward*.15f,1-Mathf.Exp(-12*Time.deltaTime));
                lantern.rotation=Quaternion.Slerp(lantern.rotation,Quaternion.LookRotation(forward),1-Mathf.Exp(-9*Time.deltaTime));
            }
            reveal=Mathf.MoveTowards(reveal,RecordOpen?1:0,Time.deltaTime*.6f);
            servicePanel.localPosition=serviceRest+Vector3.up*(reveal*3.2f);
            windowOpen=Mathf.MoveTowards(windowOpen,WindowRaised?1:0,Time.deltaTime*.45f);
            shutter.localScale=new Vector3(1,Mathf.Lerp(1,.13f,windowOpen),1);
            shutter.localPosition=shutterRest+Vector3.up*(windowOpen*5.5f);
            failedPulse=Mathf.Max(0,failedPulse-Time.deltaTime);
            if(pulley)pulley.localRotation=Quaternion.Euler(0,0,windowOpen*360+(failedPulse>0?Mathf.Sin(Time.time*17)*failedPulse*9:0));
            if(exitFlower)exitFlower.localScale=Vector3.one*(Complete?1.35f:1+failedPulse*.10f*Mathf.Abs(Mathf.Sin(Time.time*7)));
            if(latch)latch.localRotation=Quaternion.Euler(0,0,WindowLatched?90:0);
            doorOpen=Mathf.MoveTowards(doorOpen,Complete?1:0,Time.deltaTime*.45f);
            door.localPosition=doorRest+Vector3.right*(doorOpen*5.5f);
            leftRing.localRotation=Quaternion.Slerp(leftRing.localRotation,Quaternion.Euler(LeftLocked?0:65,0,0),1-Mathf.Exp(-10*Time.deltaTime));rightRing.localRotation=Quaternion.Slerp(rightRing.localRotation,Quaternion.Euler(RightLocked?0:65,0,0),1-Mathf.Exp(-10*Time.deltaTime));
            chargeRibbon.localScale=new Vector3(1,Charged?1:.18f,1);
            paneBlock.SetColor("_EmissionColor",new Color(1,.68f,.24f)*(Charged?1.4f:State==LampState.RecordDocked?.8f:.08f));foreach(var pane in panes)pane.SetPropertyBlock(paneBlock);
            lanternLight.intensity=Charged?6:State==LampState.RecordDocked?3:.3f;lanternLight.enabled=visible;
            recordLight.enabled=visible;recordLight.intensity=reveal*4;
            windowLight.enabled=visible;windowLight.intensity=windowOpen*12;
        }
    }
}
