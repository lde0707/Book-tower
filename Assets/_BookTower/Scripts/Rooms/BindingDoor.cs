using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Rooms
{
    public sealed class BindingDoor : MonoBehaviour
    {
        public Transform left,right;
        public PuzzleBase puzzle;
        float opened;
        float anticipation;bool sounded;
        Collider[] blockers;
        public float OpeningProgress=>opened;
        public bool OpeningSoundPlayed=>sounded;
        public bool IsOpen => opened>=1f;
        void Awake()
        {
            if (!left || !right) { enabled = false; return; }
            var all = new System.Collections.Generic.List<Collider>();
            left.GetComponentsInChildren(true, all);
            all.AddRange(right.GetComponentsInChildren<Collider>(true));
            blockers = all.ToArray();
        }
        void Update()
        {
            if(IsOpen || puzzle==null || puzzle.State != PuzzleBase.PuzzleState.Solved)return;
            if((puzzle is FoldBridgePuzzle || puzzle is GravityPagePuzzle)){anticipation+=Time.deltaTime;if(anticipation<.95f)return;}
            if(!sounded)
            {
                sounded=true;
                foreach(var blocker in blockers) if(blocker) blocker.enabled=false;
                BookTower.Audio.SfxDirector.PlayMechanism("binding_door_open",(left.position+right.position)*.5f+Vector3.up*4,.9f);
            }
            opened=Mathf.MoveTowards(opened,1,Time.deltaTime*((puzzle is FoldBridgePuzzle || puzzle is GravityPagePuzzle)?1/3.2f:.55f));
            float eased=opened*opened*(3-2*opened);
            left.localRotation=Quaternion.Euler(0,170*eased,0);
            right.localRotation=Quaternion.Euler(0,-170*eased,0);
        }
    }
}

