using UnityEngine;
using BookTower.Fold;
using BookTower.Puzzles;
namespace BookTower.Rooms
{
    // Authored shadow theatre: silhouettes respond continuously to the folding panels.
    public class StorybookShadowTheatre : MonoBehaviour
    {
        public FoldPanel practice,left,right;
        public Transform leftShadow,rightShadow,reward;
        public Light rewardLight;
        public PuzzleBase puzzle;
        Vector3 leftHome,rightHome;
        void Awake(){leftHome=leftShadow.localPosition;rightHome=rightShadow.localPosition;}
        void Update()
        {
            float a=left.StateIndex-1,b=right.StateIndex-3;
            leftShadow.localPosition=Vector3.Lerp(leftShadow.localPosition,leftHome+new Vector3(a*1.1f,a*.28f,0),1-Mathf.Exp(-6*Time.deltaTime));
            rightShadow.localPosition=Vector3.Lerp(rightShadow.localPosition,rightHome+new Vector3(b*.85f,-b*.22f,0),1-Mathf.Exp(-6*Time.deltaTime));
            leftShadow.localRotation=Quaternion.Slerp(leftShadow.localRotation,Quaternion.Euler(0,0,a*14),1-Mathf.Exp(-6*Time.deltaTime));
            rightShadow.localRotation=Quaternion.Slerp(rightShadow.localRotation,Quaternion.Euler(0,0,b*-10),1-Mathf.Exp(-6*Time.deltaTime));
            bool solved=puzzle!=null&&puzzle.State==PuzzleBase.PuzzleState.Solved;
            reward.localScale=Vector3.one*(solved?1.35f:1f);
            reward.Rotate(0,solved?25*Time.deltaTime:0,0,Space.Self);
            rewardLight.intensity=Mathf.Lerp(rewardLight.intensity,solved?22:3,Time.deltaTime*3);
        }
    }
}
