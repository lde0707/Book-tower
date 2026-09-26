using UnityEngine;
using BookTower.Fold;
using BookTower.Rooms;
using BookTower.CameraSystem;

namespace BookTower.Puzzles
{
    // Surface patches in the production room. Completion requires progression,
    // room presence, actual camera alignment, and the settled fold geometry.
    public class SurfaceAnamorphosis : PuzzleBase
    {
        public FoldPanel panel;
        public MemoryBeacon beacon;
        public Room room;
        public Transform[] fragments;
        public Vector3[] unfolded, aligned;
        public float Blend { get; private set; }
        private PaperCamera cameraRig;

        protected override void Update()
        {
            if(cameraRig==null) cameraRig=FindFirstObjectByType<PaperCamera>();
            float target=beacon!=null && beacon.IsLit && panel.StateIndex==2 ? 1 : 0;
            Blend=Mathf.MoveTowards(Blend,target,Time.deltaTime*.65f);
            for(int i=0;i<fragments.Length;i++)
            {
                fragments[i].localPosition=Vector3.Lerp(unfolded[i],aligned[i],Mathf.SmoothStep(0,1,Blend));
                fragments[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(Blend*Mathf.PI)*18);
            }
            if(beacon==null || !beacon.IsLit || Player==null || !room.Contains(Player.position)) return;
            base.Update();
        }
        private bool IsFullyInView()
        {
            var camera=Camera.main;
            if(camera==null)return false;
            foreach(var part in fragments)
                foreach(var vertex in part.GetComponent<MeshFilter>().sharedMesh.vertices)
                {
                    var p=camera.WorldToViewportPoint(part.TransformPoint(vertex));
                    if(p.z<=0 || p.x<.03f || p.x>.97f || p.y<.03f || p.y>.97f)return false;
                }
            return true;
        }
        protected override bool CheckSolved()
        {
            return beacon!=null && beacon.IsLit && panel.StateIndex==2 && panel.IsSettled && Blend>.999f
                && cameraRig!=null && cameraRig.Mode==PaperCamera.CameraMode.Room
                && Quaternion.Angle(cameraRig.transform.rotation,Quaternion.Euler(30,0,0))<1
                && room.Contains(Player.position)
                && Vector3.Distance(Player.position, panel.transform.position)<7f
                && IsFullyInView();
        }
    }
}
