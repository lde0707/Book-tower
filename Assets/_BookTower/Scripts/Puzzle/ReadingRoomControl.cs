using UnityEngine;
using BookTower.Interaction;
namespace BookTower.Puzzles
{
    [RequireComponent(typeof(Interactable))]
    public sealed class ReadingRoomControl:MonoBehaviour
    {
        public enum Action { Charge,Pickup,LeftRing,RightRing,RecordDock,TakeCrank,Crank,Latch,WindowCharge,ExitDock,Recover,Toy }
        public ReadingRoomPuzzle puzzle; public Action action;
        public bool TryUse(Vector3 actor)
        {
            if(!isActiveAndEnabled||!puzzle||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range)return false;
            var room=GetComponentInParent<BookTower.Rooms.Room>();if(room&&!room.IsVisible)return false;
            if(BookTower.Core.GameFlow.Instance&&!BookTower.Core.GameFlow.Instance.AcceptsPlayerInput)return false;
            return puzzle.Use(action);
        }
    }
}
