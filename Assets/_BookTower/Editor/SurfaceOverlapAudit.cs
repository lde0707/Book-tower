using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class SurfaceOverlapAudit
    {
        class Face {public Transform owner;public Vector3 normal;public float plane;public Vector3[] vertices;public Bounds bounds;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            Write("Docs/ArtReview/Overlap/before.txt");
        }
        public static void After()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            Write("Docs/ArtReview/Overlap/after.txt");
        }
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        public static void Write(string path)
        {
            var buckets=new Dictionary<(int,int,int),List<Face>>();
            var hits=new Dictionary<string,float>();
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;
                if(mesh==null || renderer==null || !renderer.enabled || filter.name=="외곽선" || !mesh.isReadable)continue;
                var v=mesh.vertices.Select(filter.transform.TransformPoint).ToArray();var tris=mesh.triangles;
                for(int i=0;i<tris.Length;i+=3)
                {
                    var p=new[]{v[tris[i]],v[tris[i+1]],v[tris[i+2]]};var normal=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized;
                    int axis=Mathf.Abs(normal.x)>.9999f?0:Mathf.Abs(normal.y)>.9999f?1:Mathf.Abs(normal.z)>.9999f?2:-1;
                    if(axis<0)continue;
                    float plane=p[0][axis];int bin=Mathf.RoundToInt(plane/.005f),sign=normal[axis]>0?1:-1;
                    var bounds=new Bounds(p[0],Vector3.zero);bounds.Encapsulate(p[1]);bounds.Encapsulate(p[2]);
                    var face=new Face{owner=filter.transform,normal=normal,plane=plane,vertices=p,bounds=bounds};
                    for(int k=bin-1;k<=bin+1;k++)
                    {
                        if(!buckets.TryGetValue((axis,k,sign),out var candidates))continue;
                        foreach(var other in candidates)
                        {
                            if(other.owner==face.owner || Mathf.Abs(other.plane-plane)>.004f)continue;
                            int u=(axis+1)%3,w=(axis+2)%3;
                            if(Mathf.Min(bounds.max[u],other.bounds.max[u])-Mathf.Max(bounds.min[u],other.bounds.min[u])<.015f || Mathf.Min(bounds.max[w],other.bounds.max[w])-Mathf.Max(bounds.min[w],other.bounds.min[w])<.015f)continue;
                            float area=Intersection(p,other.vertices,u,w);if(area<.001f)continue;
                            var names=new[]{PathOf(face.owner),PathOf(other.owner)};Array.Sort(names,StringComparer.Ordinal);
                            string key=names[0]+" <> "+names[1];hits[key]=hits.GetValueOrDefault(key)+area;
                        }
                    }
                    if(!buckets.TryGetValue((axis,bin,sign),out var list))buckets[(axis,bin,sign)]=list=new();list.Add(face);
                }
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            File.WriteAllLines(path,new[]{"Near-coplanar axis-aligned visible-mesh candidates (<4mm). Excludes outline shells, moving poses and non-axis-aligned faces. Buried internal faces may appear; this is not a universal no-overlap proof.","Pairs: "+hits.Count}.Concat(hits.OrderByDescending(p=>p.Value).Select(p=>$"{p.Value:F4} m2 | {p.Key}")));
            Debug.Log("[SurfaceOverlap] "+hits.Count+" candidates: "+path);
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static float Intersection(Vector3[] a,Vector3[] b,int u,int w)
        {
            var polygon=a.Select(p=>new Vector2(p[u],p[w])).ToList();var clip=b.Select(p=>new Vector2(p[u],p[w])).ToArray();
            float sign=Mathf.Sign(Cross(clip[1]-clip[0],clip[2]-clip[0]));
            for(int i=0;i<3 && polygon.Count>0;i++)
            {
                var edge=clip[(i+1)%3]-clip[i];var result=new List<Vector2>();
                for(int j=0;j<polygon.Count;j++)
                {
                    var p=polygon[j];var q=polygon[(j+1)%polygon.Count];float dp=sign*Cross(edge,p-clip[i]),dq=sign*Cross(edge,q-clip[i]);
                    if(dp>=0)result.Add(p);if((dp>=0)!=(dq>=0))result.Add(Vector2.Lerp(p,q,dp/(dp-dq)));
                }polygon=result;
            }
            float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);return Mathf.Abs(area)*.5f;
        }
    }
}
