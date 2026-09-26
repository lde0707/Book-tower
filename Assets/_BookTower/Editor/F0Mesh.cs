using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BookTower.EditorTools
{
    // Actual UV-mapped meshes, in metres: texture grain is consistent across large and small props.
    public static class F0Mesh
    {
        public static Mesh RoundedBox(Vector3 size,float bevel=.06f)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            Vector3 half=size*.5f;float r=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);
            Vector3 inner=half-Vector3.one*r;
            Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var normal in normals)
            {
                Vector3 u=normal.y!=0?Vector3.right:Vector3.Cross(Vector3.up,normal);
                Vector3 w=Vector3.Cross(normal,u);
                float hu=Vector3.Dot(Abs(u),half),hw=Vector3.Dot(Abs(w),half),hn=Vector3.Dot(Abs(normal),half);
                float[] xs={-hu,-hu+r,hu-r,hu};float[] ys={-hw,-hw+r,hw-r,hw};int start=v.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 raw=normal*hn+u*xs[x]+w*ys[y];
                    Vector3 nearest=new(Mathf.Clamp(raw.x,-inner.x,inner.x),Mathf.Clamp(raw.y,-inner.y,inner.y),Mathf.Clamp(raw.z,-inner.z,inner.z));
                    v.Add(nearest+(raw-nearest).normalized*r);uv.Add(new Vector2(xs[x],ys[y])*.65f);
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++)
                {int a=start+y*4+x;t.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
            return Mesh(v,uv,t,"Beveled book geometry");
        }
        public static Mesh Tube(IList<Vector3> path,float radius,int sides=8)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float distance=0;
            for(int i=0;i<path.Count;i++)
            {
                if(i>0)distance+=Vector3.Distance(path[i],path[i-1]);
                Vector3 tangent=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)]).normalized;
                Vector3 a=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;
                Vector3 b=Vector3.Cross(tangent,a);
                for(int j=0;j<=sides;j++)
                {float angle=j*Mathf.PI*2/sides;v.Add(path[i]+radius*(a*Mathf.Cos(angle)+b*Mathf.Sin(angle)));uv.Add(new Vector2(j/(float)sides,distance*1.5f));}
                if(i==0)continue;
                for(int j=0;j<sides;j++){int q=(i-1)*(sides+1)+j;t.AddRange(new[]{q,q+sides+1,q+1,q+1,q+sides+1,q+sides+2});}
            }
            return Mesh(v,uv,t,"Twisted cotton rope");
        }
        public static Mesh Paper(float width,float height,int seed)
        {
            int nx=Mathf.Max(5,Mathf.CeilToInt(width*5)),ny=Mathf.Max(5,Mathf.CeilToInt(height*5));
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
            {
                float px=width*(x/(float)nx-.5f),py=height*y/ny;
                float n=Mathf.PerlinNoise(px*2+seed,py*2+seed);
                float edge=(x==0||x==nx||y==0||y==ny)?(n-.5f)*.08f:0;
                v.Add(new Vector3(px+edge,py+edge,(n-.5f)*.085f));uv.Add(new Vector2(px,py)*.48f);
                if(x<nx&&y<ny){int q=y*(nx+1)+x;t.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}
            }
            return Mesh(v,uv,t,"Handmade deckle paper");
        }
        public static Mesh Leaf(float length,float width)
        {
            var v=new List<Vector3>{Vector3.zero,new(-width*.5f,length*.45f,-.02f),new(0,length,-.01f),new(width*.5f,length*.45f,-.02f),new(0,length*.45f,-.07f)};
            var uv=new List<Vector2>{new(.5f,0),new(0,.45f),new(.5f,1),new(1,.45f),new(.5f,.45f)};
            return Mesh(v,uv,new List<int>{0,4,1,1,4,2,2,4,3,3,4,0},"Pressed leaf motif");
        }
        static Vector3 Abs(Vector3 v)=>new(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        static Mesh Mesh(List<Vector3> v,List<Vector2> uv,List<int> triangles,string name)
        {var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
    }
}
