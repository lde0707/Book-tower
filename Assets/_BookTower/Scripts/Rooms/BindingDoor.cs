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
        public float OpeningProgress=>opened;
        public bool OpeningSoundPlayed=>sounded;
        public bool IsOpen => opened>=1f;
        void Update()
        {
            if(puzzle==null || puzzle.State != PuzzleBase.PuzzleState.Solved)return;
            if((puzzle is FoldBridgePuzzle || puzzle is GravityPagePuzzle)){anticipation+=Time.deltaTime;if(anticipation<.95f)return;}
            if(!sounded){sounded=true;BookTower.Audio.SfxDirector.PlayMechanism("binding_door_open",(left.position+right.position)*.5f+Vector3.up*4,.9f);}
            opened=Mathf.MoveTowards(opened,1,Time.deltaTime*((puzzle is FoldBridgePuzzle || puzzle is GravityPagePuzzle)?1/3.2f:.55f));
            float eased=opened*opened*(3-2*opened);
            left.localRotation=Quaternion.Euler(0,170*eased,0);
            right.localRotation=Quaternion.Euler(0,-170*eased,0);
            foreach(var c in left.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var c in right.GetComponentsInChildren<Collider>())c.enabled=false;
        }
    }
}

