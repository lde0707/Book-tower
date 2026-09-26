using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BookTower.Fold;
using BookTower.Rooms;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class ShadowBridgeBuilder
    {
        public static Vector3[] Footprint(FoldPanel panel, float angle, Light sun, float floorY)
        {
            var leaf = panel.transform.Find("Panel");
            float w = leaf.localScale.x, length = leaf.localScale.z;
            var corners = new[] {new Vector3(-w/2,0,0), new Vector3(w/2,0,0),
                new Vector3(w/2,0,length), new Vector3(-w/2,0,length)};
            return corners.Select(v => {
                Vector3 p = panel.transform.parent.TransformPoint(panel.transform.localPosition + Quaternion.AngleAxis(angle, Vector3.left)*v);
                return p + sun.transform.forward * ((floorY-p.y)/sun.transform.forward.y);
            }).ToArray();
        }

        public static void Apply()
        {
            var room = Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId=="1B");
            var panel = room.GetComponentsInChildren<FoldPanel>().Single();
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            var polygon = Footprint(panel,panel.AngleAt(1),sun,room.FloorY+.1f)
                .Select(p=>room.transform.InverseTransformPoint(p)).ToList();
            polygon = Clip(Clip(polygon,-3.5f,true),3.5f,false);
            if(polygon.Count<3) throw new InvalidOperationException("P3 shadow does not meet the gap");
            var bridge = room.GetComponentInChildren<SolveBridge>();
            foreach(var child in bridge.GetComponentsInChildren<BookTower.Presentation.PencilOutline>())
                Object.DestroyImmediate(child);
            var outline=bridge.transform.Find("외곽선"); if(outline!=null) Object.DestroyImmediate(outline.gameObject);
            bridge.transform.localScale=Vector3.one;
            bridge.transform.localPosition=new Vector3(0,-.6f,0);
            int n=polygon.Count;
            var vertices=polygon.Concat(polygon.Select(p=>p-Vector3.up*.08f)).ToArray();
            var tris=new List<int>();
            for(int i=1;i<n-1;i++) { tris.AddRange(new[]{0,i+1,i,n,n+i,n+i+1}); }
            for(int i=0;i<n;i++) { int j=(i+1)%n; tris.AddRange(new[]{i,j,n+i,j,n+j,n+i}); }
            const string path="Assets/_BookTower/Generated/Direction/P3ShadowBridge.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh=mesh==null; if(fresh) mesh=new Mesh{name="P3ShadowBridge"}; else mesh.Clear();
            mesh.vertices=vertices; mesh.triangles=tris.ToArray(); mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if(fresh) AssetDatabase.CreateAsset(mesh,path); else EditorUtility.SetDirty(mesh);
            bridge.GetComponent<MeshFilter>().sharedMesh=mesh;
            foreach(var collider in bridge.GetComponents<Collider>()) Object.DestroyImmediate(collider);
            bridge.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
            var so=new SerializedObject(bridge); so.FindProperty("openY").floatValue=0; so.ApplyModifiedPropertiesWithoutUndo();
            var it=new SerializedObject(panel.GetComponent<BookTower.Interaction.Interactable>());
            it.FindProperty("lift").floatValue=0; it.ApplyModifiedPropertiesWithoutUndo();
            // The closed page folds away from the void. The logical state remains zero.
            // Awake captures identity as base rotation, so do not serialize the preview rotation here.
        }

        static List<Vector3> Clip(List<Vector3> input,float z,bool greater)
        {
            var output=new List<Vector3>();
            for(int i=0;i<input.Count;i++)
            {
                Vector3 a=input[i],b=input[(i+1)%input.Count];
                bool insideA=greater?a.z>=z:a.z<=z, insideB=greater?b.z>=z:b.z<=z;
                if(insideA)output.Add(a);
                if(insideA!=insideB)output.Add(Vector3.Lerp(a,b,(z-a.z)/(b.z-a.z)));
            }
            return output;
        }
    }
}
