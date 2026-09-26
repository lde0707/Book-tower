using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Rooms
{
    public sealed class BindingDoor : MonoBehaviour
    {
        public Transform left,right;
        public PuzzleBase puzzle;
        float opened;
        public bool IsOpen => opened>.99f;
        void Update()
        {
            if(puzzle==null || puzzle.State != PuzzleBase.PuzzleState.Solved)return;
            opened=Mathf.MoveTowards(opened,1,Time.deltaTime*.55f);
            float eased=opened*opened*(3-2*opened);
            left.localRotation=Quaternion.Euler(0,-95*eased,0);
            right.localRotation=Quaternion.Euler(0,95*eased,0);
            foreach(var c in left.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var c in right.GetComponentsInChildren<Collider>())c.enabled=false;
        }
    }
}

