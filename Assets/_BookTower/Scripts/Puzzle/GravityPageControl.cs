using UnityEngine;
using BookTower.Interaction;
namespace BookTower.Puzzles
{
    [RequireComponent(typeof(Interactable))]
    public sealed class GravityPageControl : MonoBehaviour
    {
        public GravityPagePuzzle puzzle;
        public int command;
        public bool TryUse(Vector3 actor)
        {
            if(!isActiveAndEnabled||!puzzle||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range)return false;
            var room=GetComponentInParent<BookTower.Rooms.Room>();
            if(room&&!room.IsVisible)return false;
            return command==4?puzzle.Undo():command==5?puzzle.ResetCargo():puzzle.SetDirection(command);
        }
    }
}
