using UnityEngine;
using BookTower.Interaction;
namespace BookTower.Puzzles
{
    [RequireComponent(typeof(Interactable))]
    public sealed class ReadingRoomControl:MonoBehaviour
    {
        public enum Action { Charge,Pickup,LeftRing,RightRing,RecordDock,TakeCrank,Crank,Latch,WindowCharge,ExitDock,Recover,Toy }
        public ReadingRoomPuzzle puzzle; public Action action;
        public string Prompt
        {
            get
            {
                if(!puzzle)return "살펴보기";
                if(puzzle.State==ReadingRoomPuzzle.LampState.Carried&&(action==Action.LeftRing||action==Action.RightRing))return "받침에 등불을 내려놓은 뒤 열기";
                switch(action)
                {
                    case Action.LeftRing: return puzzle.LeftLocked?"왼쪽 걸쇠: 잠김 · 열어 빛 내보내기":"왼쪽 걸쇠: 열림 · 잠가 빛 보존하기";
                    case Action.RightRing: return puzzle.RightLocked?"오른쪽 걸쇠: 잠김 · 열어 빛 내보내기":"오른쪽 걸쇠: 열림 · 잠가 빛 보존하기";
                    case Action.Pickup: return puzzle.State==ReadingRoomPuzzle.LampState.Carried?"기억등을 들고 있음":"양쪽 걸쇠를 잠근 뒤 등 들기";
                    case Action.Charge: return "채광 받침 · 빛 담기";
                    case Action.RecordDock: return puzzle.State==ReadingRoomPuzzle.LampState.RecordDocked&&puzzle.Charged?"등 앞의 두 걸쇠 열기":"책 문양 받침에 기억등 놓기";
                    case Action.ExitDock: return puzzle.State==ReadingRoomPuzzle.LampState.ExitDocked&&puzzle.Charged?"등 앞의 두 걸쇠 열기":"문 문양 받침에 기억등 놓기";
                    case Action.WindowCharge: return "창 아래 받침 · 창빛 담기";
                    case Action.Crank: return puzzle.HasCrank?(puzzle.WindowRaised?"창 내리기":"손잡이 돌려 창 올리기"):"도르래 축 · 손잡이가 비어 있음";
                    case Action.Latch: return puzzle.WindowLatched?"창 걸쇠: 고정됨":"올린 창 걸어 고정하기";
                    default: return null;
                }
            }
        }
        void Update(){var prompt=Prompt;if(prompt!=null)GetComponent<Interactable>().SetPresentation(name,prompt);}
        public bool TryUse(Vector3 actor)
        {
            if(!isActiveAndEnabled||!puzzle||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range)return false;
            var room=GetComponentInParent<BookTower.Rooms.Room>();if(room&&!room.IsVisible)return false;
            if(BookTower.Core.GameFlow.Instance&&!BookTower.Core.GameFlow.Instance.AcceptsPlayerInput)return false;
            return puzzle.Use(action);
        }
    }
}
