using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Info;
using BookTower.EchoSystem;
using BookTower.Core;

namespace BookTower.EditorTools
{
    /// <summary>
    /// RoomRedesignV2 설계도의 로컬 구역과 길을 실제 3D 블록아웃으로 만든다.
    /// 기존 퍼즐 컴포넌트는 유지하고 직사각형 바닥만 비정형 종이 섬으로 교체한다.
    /// </summary>
    public static class BlueprintMapBuilder
    {
        const float PlayerHeight = 1.4f;
        const float PlayerDiameter = .6f;
        const float Walkway = 5.5f;
        const float PlatformRadius = 8f;
        const float PaperThickness = .22f;
        const float MovingClearance = .35f;

        sealed class Spec
        {
            public string id;
            public Vector3[] zones;
            public int[] route;
            public int[] skipRoute;
            public float landmarkHeight;
            public Spec(string id, Vector3[] zones, float landmarkHeight=12f, int[] route=null, int[] skipRoute=null)
            { this.id=id; this.zones=zones; this.landmarkHeight=landmarkHeight; this.route=route; this.skipRoute=skipRoute; }
        }

        static readonly Spec[] Specs =
        {
            S("F0", 12, P(0,0,-16),P(-15,1,0),P(15,1,0),P(0,0,3),P(0,3,16)),
            S("1A", 18, P(0,0,-21),P(-17,2,-5),P(15,5,4),P(0,4,17),P(0,4,23)),
            // 1B 바닥은 P3의 실제 간극/SolveBridge를 보존해야 하므로 교체하지 않는다.
            S("1B", 16, P(-19,0,-22),P(-16,1,-4),P(3,2,3),P(15,2,14),P(0,3,27)),
            S("1C", 20, P(0,0,-23),P(-18,2,-3),P(0,4,4),P(18,2,-3),P(0,5,24), route:new[]{0,1,2,3,2,4}),
            S("1D", 13, P(0,0,-22),P(-19,0,-7),P(18,1,2),P(-4,2,15),P(0,2,24), route:new[]{0,1,3,2,3,4}),
            S("1E", 26, P(0,0,-26),P(-22,5,0),P(22,8,0),P(0,0,0),P(0,6,23), route:new[]{0,3,1,3,2,3,4}),
            S("2A", 18, P(-24,0,-19),P(-15,2,-3),P(13,3,-4),P(2,4,11),P(20,5,21)),
            S("2B", 20, P(0,0,-24),P(0,2,-8),P(-22,4,13),P(23,3,3),P(0,4,25), route:new[]{0,1,2,1,3,4}),
            S("2C", 24, P(0,0,-32),P(0,3,0),P(-26,3,2),P(26,3,-2),P(0,5,29), route:new[]{0,1,2,1,3,1,4}),
            S("2D", 22, P(0,0,-26),P(0,3,-9),P(-20,5,3),P(20,7,9),P(0,9,25), route:new[]{0,1,2,1,3,4}),
            S("2E", 28, P(0,0,-24),P(23,3,-7),P(14,6,20),P(-14,9,20),P(-23,12,-7), route:new[]{0,1,2,3,4,0}),
            S("2F", 24, P(0,0,-25),P(-19,1,-9),P(13,2,0),P(-13,3,11),P(0,3,23), route:new[]{0,1,2,3,4}),
            S("3A", 32, P(0,0,-33),P(-27,2,0),P(27,3,0),P(0,2,7),P(0,4,34), route:new[]{0,3,1,3,2,3,4}),
            S("3B", 26, P(0,0,-29),P(-23,3,-3),P(23,3,-3),P(0,4,17),P(0,4,30), route:new[]{0,1,3,2,3,4}),
            S("3C", 38, P(0,0,-29),P(-18,5,-9),P(17,10,2),P(-9,15,19),P(0,20,10)),
            S("HD", 18, P(-16,0,-19),P(0,2,-6),P(16,3,0),P(4,5,19),P(-18,2,11), route:new[]{0,1,2,3,4,0}),
        };

        static Vector3 P(float x,float y,float z) => new(x, Mathf.Min(y*.25f,4.5f), z);
        static Spec S(string id,float h,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 e,int[] route=null,int[] skipRoute=null)
            => new(id,new[]{a,b,c,d,e},h,route,skipRoute);

        public static void Apply()
        {
            var paper = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/M_Paper_Mid.mat");
            var light = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/M_Paper_Light.mat");
            var shade = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/M_Paper_Shade.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/M_Ribbon.mat");
            if (paper==null || light==null || shade==null) { Debug.LogError("[BlueprintMap] paper materials missing"); return; }

            foreach(var spec in Specs)
            {
                var room = FindRoom(spec.id);
                if(room==null) { Debug.LogWarning($"[BlueprintMap] room {spec.id} missing"); continue; }
                Transform root=room.transform;
                var old=root.Find("V2_블록아웃");
                if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var group=new GameObject("V2_블록아웃").transform;
                group.SetParent(root,false);

                if(spec.id!="1B") DisableOldFloor(root);
                BuildZones(group,spec,paper,light,accent);
                BuildOuterPages(group,spec,shade,light);
                if(spec.id=="1C") OpenCenteredWall(root,group,"방1_뒷벽",8f);
                SupportExistingGameplay(group,root,spec,paper);
                AddScaleFigure(group,light,accent);
                AddClearanceNotes(group,spec);
            }
            Debug.Log($"[BlueprintMap] V2 rooms {Specs.Length} built — player {PlayerHeight:F1}h/{PlayerDiameter:F1}d, path {Walkway:F1}u, moving clearance {MovingClearance:F2}u");
        }

        static void OpenCenteredWall(Transform room,Transform output,string wallName,float opening)
        {
            Transform wall=null;
            foreach(var t in room.GetComponentsInChildren<Transform>(true))
                if(t.name==wallName){wall=t;break;}
            if(wall==null)return;
            var renderer=wall.GetComponent<Renderer>();
            Material mat=renderer!=null?renderer.sharedMaterial:null;
            Vector3 p=wall.localPosition, s=wall.localScale;
            float side=(s.x-opening)*.5f;
            if(side<=.1f)return;
            wall.gameObject.SetActive(false);
            MakeBox(output,wallName+"_문좌",new Vector3(p.x-(opening+side)*.5f,p.y,p.z),new Vector3(side,s.y,s.z),mat);
            MakeBox(output,wallName+"_문우",new Vector3(p.x+(opening+side)*.5f,p.y,p.z),new Vector3(side,s.y,s.z),mat);
        }

        static Room FindRoom(string id)
        {
            foreach(var r in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(r.RoomId==id) return r;
            return null;
        }

        static void DisableOldFloor(Transform room)
        {
            foreach(Transform child in room)
            {
                if(child.name=="바닥" || child.name=="바닥_앞" || child.name=="바닥_뒤")
                    child.gameObject.SetActive(false);
            }
        }

        static void BuildZones(Transform parent,Spec spec,Material paper,Material light,Material accent)
        {
            // 외부 경사로는 방 기준 높이에서 시작한다. 입구와 출구 구역도 같은
            // 높이로 맞춰 내부의 높은 퍼즐 발판이 잠금 페이지 위를 넘어가지 않게 한다.
            int[] route=spec.route??new[]{0,1,2,3,4};
            spec.zones[route[0]].y=0f;
            spec.zones[route[route.Length-1]].y=0f;
            for(int i=0;i<spec.zones.Length;i++)
            {
                float radius = i==0 || i==spec.zones.Length-1 ? PlatformRadius-1f : PlatformRadius;
                var p=MakeCylinder(parent,$"구역_Z{i+1}",spec.zones[i]+Vector3.down*.11f,radius,PaperThickness,paper,16);
                MakeCylinder(parent,$"구역테두리_Z{i+1}",spec.zones[i]+Vector3.down*.005f,radius-.28f,.025f,
                    i==spec.zones.Length-1?accent:light,16,false);
            }
            for(int i=0;i<route.Length-1;i++) MakePath(parent,$"주동선_{i+1}",spec.zones[route[i]],spec.zones[route[i+1]],Walkway,paper);

            // 방의 상징은 중앙에 겹치지 않고 북쪽 바깥으로 치운 거대한 접힌 페이지.
            Vector3 center=Vector3.zero;
            foreach(var z in spec.zones) center+=z;
            center/=spec.zones.Length;
            Vector3 landmark=new(center.x,spec.landmarkHeight*.5f,center.z+10f);
            var left=MakeBox(parent,"랜드마크_왼페이지",landmark+Vector3.left*2.6f,new Vector3(5,.18f,spec.landmarkHeight),light);
            left.transform.localRotation=Quaternion.Euler(0,0,68);
            var right=MakeBox(parent,"랜드마크_오른페이지",landmark+Vector3.right*2.6f,new Vector3(5,.18f,spec.landmarkHeight),light);
            right.transform.localRotation=Quaternion.Euler(0,0,-68);
            RemoveCollider(left); RemoveCollider(right);
        }

        static void BuildOuterPages(Transform parent,Spec spec,Material shade,Material light)
        {
            float max=0;
            foreach(var z in spec.zones) max=Mathf.Max(max,new Vector2(z.x,z.z).magnitude);
            float radius=max+10f;
            for(int i=0;i<10;i++)
            {
                float a=i*36f*Mathf.Deg2Rad;
                // 남/북 문 쪽에는 확실한 5u 이상의 출입 틈을 둔다.
                if(i==0 || i==5) continue;
                float h=8f+(i%3)*2.5f;
                Vector3 p=new(Mathf.Sin(a)*radius,h*.5f,Mathf.Cos(a)*radius);
                var page=MakeBox(parent,$"외곽접힌페이지_{i}",p,new Vector3(13f,h,.18f),i%2==0?light:shade);
                page.transform.localRotation=Quaternion.Euler(0,i*36f,0);
                RemoveCollider(page); // 낙하 경계는 플랫폼 끝. 장식벽이 보이지 않는 충돌을 만들지 않는다.
            }
        }

        static void SupportExistingGameplay(Transform parent,Transform room,Spec spec,Material paper)
        {
            var seen=new HashSet<Vector2>();
            Component[] gameplay=room.GetComponentsInChildren<Component>(true);
            foreach(var c in gameplay)
            {
                if(c==null || c.transform==room || c.transform.IsChildOf(parent)) continue;
                if(c is not FoldPanel && c is not CollectiblePiece && c is not EchoCrease) continue;
                Vector3 p=room.InverseTransformPoint(c.transform.position);
                Vector2 key=new(Mathf.Round(p.x*2)/2f,Mathf.Round(p.z*2)/2f);
                if(!seen.Add(key)) continue;
                p.y=Mathf.Max(0,p.y-.18f);
                MakeCylinder(parent,"상호작용_받침",p,3.2f,.20f,paper,12);
                int nearest=0; float bd=float.MaxValue;
                for(int i=0;i<spec.zones.Length;i++)
                {
                    float d=(new Vector2(p.x,p.z)-new Vector2(spec.zones[i].x,spec.zones[i].z)).sqrMagnitude;
                    if(d<bd){bd=d;nearest=i;}
                }
                Vector3 a=spec.zones[nearest]; a.y=p.y;
                if(Vector3.Distance(a,p)>4f) MakePath(parent,"상호작용_접근길",a,p,4f,paper);
            }
        }

        static void AddScaleFigure(Transform parent,Material paper,Material accent)
        {
            var root=new GameObject("플레이어_스케일기준_1_4u").transform;
            root.SetParent(parent,false); root.localPosition=new Vector3(-2,0,-2);
            var body=MakeCylinder(root,"몸",new Vector3(0,.45f,0),.28f,.9f,paper,12,false);
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name="머리"; head.transform.SetParent(root,false);
            head.transform.localPosition=new Vector3(0,1.08f,0);head.transform.localScale=Vector3.one*.62f;
            head.GetComponent<Renderer>().sharedMaterial=paper;RemoveCollider(head);
            var ribbon=MakeBox(root,"리본",new Vector3(0,.78f,0),new Vector3(.85f,.07f,.2f),accent);RemoveCollider(ribbon);
            root.gameObject.SetActive(false); // Scene 뷰에서 필요할 때만 켜는 축척 기준.
        }

        static void AddClearanceNotes(Transform parent,Spec spec)
        {
            var note=parent.gameObject.AddComponent<LevelNote>();
            var so=new SerializedObject(note);
            so.FindProperty("note").stringValue=
                $"V2 {spec.id}: player H{PlayerHeight:F1}/D{PlayerDiameter:F1}, walkway {Walkway:F1}, " +
                $"required jump <=2.0, moving clearance {MovingClearance:F2}, visual surface offset >=0.025";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject MakePath(Transform parent,string name,Vector3 from,Vector3 to,float width,Material mat)
        {
            Vector3 d=to-from; if(d.magnitude<.1f)return null;
            var go=MakeBox(parent,name,(from+to)*.5f,new Vector3(width,PaperThickness,d.magnitude+.35f),mat);
            go.transform.localRotation=Quaternion.LookRotation(d.normalized,Vector3.up);
            return go;
        }

        static GameObject MakeCylinder(Transform parent,string name,Vector3 pos,float radius,float height,Material mat,int sides,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            // Unity Cylinder의 기본 CapsuleCollider를 XZ로 크게 늘리면 바닥이 둥근
            // 언덕처럼 계산된다. 정적 종이 발판은 렌더 메시 그대로 평평하게 충돌한다.
            var primitive=go.GetComponent<Collider>();
            if(primitive!=null)UnityEngine.Object.DestroyImmediate(primitive);
            if(collider)
            {
                var mesh=go.GetComponent<MeshFilter>();
                var meshCollider=go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh=mesh.sharedMesh;
            }
            return go;
        }

        static GameObject MakeBox(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;return go;
        }
        static void RemoveCollider(GameObject go){var c=go.GetComponent<Collider>();if(c!=null)UnityEngine.Object.DestroyImmediate(c);}
    }
}
