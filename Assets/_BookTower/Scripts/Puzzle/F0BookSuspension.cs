using UnityEngine;
namespace BookTower.Puzzles
{
    // The drying book slides and tilts; its suspension must stay attached to the cover.
    public sealed class F0BookSuspension : MonoBehaviour
    {
        public Transform book, roomSpace;
        public LineRenderer left,right;
        void LateUpdate()
        {
            if(!book||!roomSpace||!left||!right)return;
            UpdateThread(left,-.75f);UpdateThread(right,.75f);
        }
        void UpdateThread(LineRenderer line,float side)
        {
            var end=book.TransformPoint(new Vector3(side,1.06f,.14f));
            var top=roomSpace.InverseTransformPoint(end);top.y=10.6f;top.z=3;
            line.SetPosition(0,roomSpace.TransformPoint(top));line.SetPosition(1,end);
        }
    }
}
