using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Puzzles;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class SurfaceAnamorphosisBuilder
    {
        const string Dir="Assets/_BookTower/Generated/Direction";
        public static void Apply()
        {
            var rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            var room=rooms.Single(r=>r.RoomId=="3A"); var last=rooms.Single(r=>r.RoomId=="3C");
            var old=room.transform.Find("P15_왜상"); if(old!=null)Object.DestroyImmediate(old.gameObject);
            foreach(var t in room.GetComponentsInChildren<Transform>())
                if(t.name.StartsWith("왜상조각_"))t.gameObject.SetActive(false);
            var go=new GameObject("P15_왜상"); go.transform.SetParent(room.transform,false);
            var puzzle=go.AddComponent<SurfaceAnamorphosis>();
            puzzle.room=room; puzzle.panel=room.GetComponentsInChildren<FoldPanel>().Single(p=>p.name=="P15_공간접이면");
            var beaconObject=last.transform.Find("빛_점화대").gameObject;
            var beacon=beaconObject.GetComponent<MemoryBeacon>() ?? beaconObject.AddComponent<MemoryBeacon>();
            beacon.prerequisites=Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).Where(p=>p.PuzzleId=="P13"||p.PuzzleId=="P14").ToArray();
            if(beacon.prerequisites.Length!=2)throw new InvalidOperationException("Missing P13/P14 prerequisites");
            var lightObject=beaconObject.transform.Find("기억의빛");
            if(lightObject==null)
            {
                lightObject=new GameObject("기억의빛").transform; lightObject.SetParent(beaconObject.transform,false);
                lightObject.localPosition=Vector3.up*1.3f;
                beacon.glow=lightObject.gameObject.AddComponent<Light>();
            }
            else beacon.glow=lightObject.GetComponent<Light>();
            beacon.glow.type=LightType.Point; beacon.glow.range=9; beacon.glow.intensity=0;
            beacon.glow.color=new Color(1,.78f,.5f); beacon.glow.shadows=UnityEngine.LightShadows.None;
            var interact=beaconObject.GetComponent<BookTower.Interaction.Interactable>() ?? beaconObject.AddComponent<BookTower.Interaction.Interactable>();
            var interaction=new SerializedObject(interact); interaction.FindProperty("range").floatValue=3.5f;
            interaction.FindProperty("label").stringValue="기억의 빛"; interaction.FindProperty("verb").stringValue="불을 켠다";
            interaction.FindProperty("lift").floatValue=0; interaction.ApplyModifiedPropertiesWithoutUndo();
            puzzle.beacon=beacon;
            var data=new SerializedObject(puzzle); data.FindProperty("puzzleId").stringValue="P15";
            data.FindProperty("targetSeconds").floatValue=240; data.FindProperty("activateRadius").floatValue=100;
            data.ApplyModifiedPropertiesWithoutUndo();
            var ink=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/CueGraphite.mat");
            var red=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/Ribbon.mat");
            Quaternion view=Quaternion.Euler(30,0,0); Vector3 ray=view*Vector3.forward;
            Vector3 focus=new Vector3(0,3,16);
            var parts=new List<Transform>(); var aligned=new List<Vector3>(); var unfolded=new List<Vector3>();
            for(int i=0;i<12;i++)
            {
                // Rays intersect the room's floor, north wall and ceiling, never a floating billboard.
                int surface=i==9||i==11?1:i>=6?2:0;
                var vertices=new List<Vector3>(); var triangles=new List<int>();
                float y0=-3.7f+i*.61f, y1=Mathf.Min(3.6f,y0+.61f);
                for(int j=0;j<=12;j++)
                {
                    float y=Mathf.Lerp(y0,y1,j/12f), width,shift;
                    if(y>.15f) {width=Mathf.Sqrt(Mathf.Max(0,3.4f-(y-1.75f)*(y-1.75f)));shift=0;}
                    else {float t=Mathf.Clamp01((y+3.7f)/3.85f);width=.04f+1.28f*Mathf.Sin(t*Mathf.PI*.60f);shift=.85f*(1-t)*(1-t);}
                    foreach(float x in new[]{shift-width,shift+width})
                    {
                        Vector3 p=focus+view*new Vector3(x*.8f,y*.8f,0);
                        float distance=surface==1?(22.1f-p.z)/ray.z:((surface==2?10.01f:.115f)-p.y)/ray.y;
                        Vector3 v=p+ray*distance;
                        if(v.y<.10f||v.y>10.03f||Mathf.Abs(v.z)>22.2f)throw new InvalidOperationException("P15 surface outside room: "+v);
                        vertices.Add(v);
                    }
                    if(j>0){int k=j*2;triangles.AddRange(new[]{k-2,k-1,k,k,k-1,k+1});}
                }
                Vector3 pivot=vertices.Aggregate(Vector3.zero,(s,v)=>s+v)/vertices.Count;
                string path=Dir+$"/P15Surface{i}.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool fresh=mesh==null;
                if(fresh)mesh=new Mesh{name=$"P15Surface{i}"};else mesh.Clear();
                mesh.SetVertices(vertices.Select(v=>v-pivot).ToList());mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                if(fresh)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
                var part=new GameObject($"{(surface==0?"바닥":surface==1?"북벽":"천장")}_종이기억_{i:00}").transform;
                part.SetParent(go.transform,false); part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=part.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=i==6?red:ink;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Vector3 offset=new Vector3((i%2==0?1:-1)*(6+i%3*3),0,surface==1?0:-(8+i%3*7));
                aligned.Add(pivot);unfolded.Add(pivot+offset);part.localPosition=pivot+offset;parts.Add(part);
            }
            puzzle.fragments=parts.ToArray();puzzle.aligned=aligned.ToArray();puzzle.unfolded=unfolded.ToArray();
        }
    }
}
