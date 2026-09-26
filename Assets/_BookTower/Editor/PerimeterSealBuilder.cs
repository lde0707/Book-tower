using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.Rooms;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Subtract actual bridge corridors from each perimeter strip. This preserves oblique
    // triangular jambs which an axis-aligned rectangular doorway cut would discard.
    public static class PerimeterSealBuilder
    {
        public static void Apply()
        {
            var level=JsonUtility.FromJson<LevelDef>(File.ReadAllText("Assets/_BookTower/Level/level_layout.json"));
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/Direction/SurfacePaper.mat");
            foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                var area=level.areas.Single(a=>a.id==room.RoomId);
                var previous=room.transform.Find("출입구_종이마감");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var root=new GameObject("출입구_종이마감").transform;root.SetParent(room.transform,false);
                float x=area.size.x*.5f,z=area.size.z*.5f,t=.24f;
                var strips=new[]{Rect(-x-t,-z-t,x+t,-z+t),Rect(-x-t,z-t,x+t,z+t),Rect(-x-t,-z+t,-x+t,z-t),Rect(x-t,-z+t,x+t,z-t)};
                var walls=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<BoxCollider>()!=null
                    && !r.name.StartsWith("장식_") && r.GetComponent<RoomWall>()!=null
                    && r.bounds.min.y<=room.FloorY+.11f && r.bounds.max.y>=room.FloorY+2.4f).ToArray();
                var faces=new[]{WallFace.South,WallFace.North,WallFace.West,WallFace.East};
                for(int face=0;face<4;face++)
                {
                    var pieces=new List<List<Vector2>>{strips[face]};
                    foreach(var ramp in level.ramps.Where(r=>r.fromId==area.id||r.toId==area.id))
                    {
                        var start=new Vector2(ramp.from.x-area.pos.x,ramp.from.z-area.pos.z);
                        var end=new Vector2(ramp.to.x-area.pos.x,ramp.to.z-area.pos.z);
                        var u=(end-start).normalized;var n=new Vector2(-u.y,u.x);
                        float width=ramp.width*.5f+.04f;
                        var normals=new[]{u,-u,n,-n};
                        var distances=new[]{Vector2.Dot(u,start)-6f,Vector2.Dot(-u,end)-6f,Vector2.Dot(n,start)-width,Vector2.Dot(-n,start)-width};
                        var remainder=new List<List<Vector2>>();
                        foreach(var piece in pieces)
                        {
                            var inside=piece;
                            for(int plane=0;plane<4 && inside.Count>=3;plane++)
                            {
                                var outside=Clip(inside,-normals[plane],-distances[plane]);
                                if(outside.Count>=3)remainder.Add(outside);
                                inside=Clip(inside,normals[plane],distances[plane]);
                            }
                        }
                        pieces=remainder;
                    }
                    for(int part=0;part<pieces.Count;part++)
                    {
                        var p=pieces[part];float area2=0;for(int i=0;i<p.Count;i++)area2+=p[i].x*p[(i+1)%p.Count].y-p[(i+1)%p.Count].x*p[i].y;
                        if(Mathf.Abs(area2)<.001f)continue;
                        var mesh=Extrude(p);string path=$"Assets/_BookTower/Generated/Direction/Seal_{area.id}_{face}_{part}.asset";
                        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
                        var go=new GameObject($"문틈마감_{face}_{part}");go.transform.SetParent(root,false);
                        // Collision seals remain continuous. Render only the part not already covered by a wall.
                        var visible=new List<List<Vector2>>{p};
                        foreach(var existingWall in walls)
                        {
                            var min=room.transform.InverseTransformPoint(existingWall.bounds.min)-Vector3.one*.012f;
                            var max=room.transform.InverseTransformPoint(existingWall.bounds.max)+Vector3.one*.012f;
                            visible=SubtractRect(visible,min.x,min.z,max.x,max.z);
                        }
                        var combines=new List<CombineInstance>();
                        foreach(var polygon in visible)
                            if(polygon.Count>=3)combines.Add(new CombineInstance{mesh=Extrude(polygon),transform=Matrix4x4.identity});
                        var visual=new Mesh();if(combines.Count>0)visual.CombineMeshes(combines.ToArray());
                        string visualPath=path.Replace(".asset","_Visible.asset");var visualAsset=AssetDatabase.LoadAssetAtPath<Mesh>(visualPath);
                        if(visualAsset==null){AssetDatabase.CreateAsset(visual,visualPath);visualAsset=visual;}else{EditorUtility.CopySerialized(visual,visualAsset);Object.DestroyImmediate(visual);}
                        foreach(var combine in combines)Object.DestroyImmediate(combine.mesh);
                        go.AddComponent<MeshFilter>().sharedMesh=visualAsset;
                        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                        var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=saved;collider.convex=true;
                        var wall=go.AddComponent<RoomWall>();var so=new SerializedObject(wall);so.FindProperty("face").enumValueIndex=(int)faces[face];so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
        static List<Vector2> Rect(float x0,float z0,float x1,float z1)=>new(){new(x0,z0),new(x1,z0),new(x1,z1),new(x0,z1)};
        static List<List<Vector2>> SubtractRect(List<List<Vector2>> pieces,float x0,float z0,float x1,float z1)
        {
            var output=new List<List<Vector2>>();var normals=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};var distances=new[]{x0,-x1,z0,-z1};
            foreach(var piece in pieces)
            {
                var inside=piece;
                for(int i=0;i<4 && inside.Count>=3;i++)
                {var outside=Clip(inside,-normals[i],-distances[i]);if(outside.Count>=3)output.Add(outside);inside=Clip(inside,normals[i],distances[i]);}
            }
            return output;
        }
        static List<Vector2> Clip(List<Vector2> polygon,Vector2 normal,float distance)
        {
            var result=new List<Vector2>();
            for(int i=0;i<polygon.Count;i++)
            {
                var p=polygon[i];var q=polygon[(i+1)%polygon.Count];float a=Vector2.Dot(p,normal)-distance,b=Vector2.Dot(q,normal)-distance;
                if(a>=0)result.Add(p);
                if((a>=0)!=(b>=0))result.Add(Vector2.Lerp(p,q,a/(a-b)));
            }
            return result;
        }
        static Mesh Extrude(List<Vector2> polygon)
        {
            int n=polygon.Count;var vertices=new Vector3[n*2];var uv=new Vector2[n*2];var indices=new List<int>();
            for(int i=0;i<n;i++)
            {
                vertices[i]=new Vector3(polygon[i].x,-.12f,polygon[i].y);vertices[i+n]=vertices[i]+Vector3.up*2.52f;
                uv[i]=polygon[i]*.2f;uv[i+n]=uv[i]+Vector2.up;
                int j=(i+1)%n;indices.AddRange(new[]{i,i+n,j+n,i,j+n,j});
            }
            for(int i=1;i<n-1;i++)indices.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});
            var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
