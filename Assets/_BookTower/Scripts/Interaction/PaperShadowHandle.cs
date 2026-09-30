using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Interaction
{
    [RequireComponent(typeof(Interactable))]
    public sealed class PaperShadowHandle:MonoBehaviour
    {
        public StorybookLightPuzzle puzzle;
        public bool isEar;
        public int extraPart;
        public int PieceIndex=>extraPart>=2?extraPart:(isEar?1:0);
        public bool CanGrab=>puzzle&&!puzzle.Complete&&(!puzzle.Duet||!puzzle.Duet.Recorded(PieceIndex));
        public void FitWholeObject()
        {
            Bounds b=default;bool first=true;
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh)continue;var bounds=filter.sharedMesh.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {var p=transform.InverseTransformPoint(filter.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
            }
            if(first)return;var collider=GetComponent<BoxCollider>();if(!collider)collider=gameObject.AddComponent<BoxCollider>();
            collider.center=b.center;collider.size=new Vector3(Mathf.Max(b.size.x,.24f),b.size.y+.08f,Mathf.Max(b.size.z,.24f));
        }
    }
}
