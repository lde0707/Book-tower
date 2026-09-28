using UnityEngine;
using BookTower.Interaction;
namespace BookTower.Puzzles
{
    [RequireComponent(typeof(Interactable))]
    public sealed class StorybookLightControl : MonoBehaviour
    {
        public enum Action { FindBody, FindEars, BodyRail, EarRail, Flip, Lamp, Toy }
        public StorybookLightPuzzle puzzle;
        public Action action;
        void Update(){if(puzzle&&((action==Action.FindBody&&puzzle.BodyFound)||(action==Action.FindEars&&puzzle.EarsFound)))gameObject.SetActive(false);}
        public bool TryUse(Vector3 actor, bool reverse=false)
        {
            if(!isActiveAndEnabled||!puzzle||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range)return false;
            var room=GetComponentInParent<BookTower.Rooms.Room>();
            if(room&&!room.IsVisible)return false;
            puzzle.Use(action,reverse);return true;
        }
    }
}
