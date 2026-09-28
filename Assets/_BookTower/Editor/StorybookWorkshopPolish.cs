using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class StorybookRoomBuilder
    {
        static Mesh OrganicLeaf(float length,float width)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int j=0;j<=20;j++)for(int i=0;i<3;i++)
            {float f=j/20f;float wave=Mathf.Max(0,Mathf.Sin(f*Mathf.PI));float w=Mathf.Pow(wave,.8f)*width*.5f;v.Add(V((i-1)*w+.05f*length*wave,f*length,-.10f*length*wave+(i==1?-.025f:0)));uv.Add(new Vector2(i*.5f,f));if(j<20&&i<2){int k=j*3+i;t.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+4});}}
            return Mesh(v,uv,t);
        }
        static Mesh RoundMedallion(float rx,float ry,float depth)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int j=0;j<=12;j++)for(int i=0;i<=48;i++)
            {float a=i*Mathf.PI*2/48,b=j*Mathf.PI/12;float r=Mathf.Sin(b)*(1+.035f*Mathf.Sin(a*5));v.Add(V(Mathf.Cos(a)*rx*r,Mathf.Sin(a)*ry*r,Mathf.Cos(b)*depth));uv.Add(new Vector2(i/48f,j/12f));if(j<12&&i<48){int k=j*49+i;t.AddRange(new[]{k,k+49,k+1,k+1,k+49,k+50});}}
            return Mesh(v,uv,t);
        }
        static void PolishWorkshop()
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())
            {
                // Existing paper undulation is 8.5 cm peak-to-peak. Give it real separation.
                if(t.name=="구겨진 천장")t.localPosition+=Vector3.down*.13f;
                if(t.name=="포개진 장지 바닥")t.localPosition+=Vector3.up*.065f;
                if(t.name=="초크 발자국")t.gameObject.SetActive(false);
            }
            var motion=root.gameObject.AddComponent<StorybookPaperMotion>();motion.papers=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="접힌 오각 종이별"&&t.localPosition.y>7).ToArray();
            // Rugs are layered leaves of a picturebook, with printed edging and sewn corners.
            foreach(var at in new[]{V(-18,.15f,-21),V(-23,.15f,0),V(24,.15f,1),V(-5,.15f,19)})
            {
                var rug=Group(root,"그림책 종이 러그",at);modules.Add(rug);
                float width=at.z>10?14:8,height=at.z>10?14:6;
                for(int i=0;i<3;i++)Paper(rug,"포갠 러그",V(0,i*.075f,-height/2),width-i*.18f,height-i*.12f,i==0?edge:cream,V(90,0,0));
                foreach(int s in new[]{-1,1})
                {
                    Stroke(rug,"크레파스 이중 테두리",new[]{V(s*(width/2-.35f),.23f,-height/2+.35f),V(s*(width/2-.42f),.23f,0),V(s*(width/2-.35f),.23f,height/2-.35f)},at.x<0?coral:teal,.035f);
                    for(float z=-height/2+.8f;z<height/2;z+=.8f){var leaf=Obj(rug,"러그 잎 문양",OrganicLeaf(.4f,.17f),at.x<0?coral:teal,V(s*(width/2-.7f),.24f,z));leaf.localRotation=Quaternion.Euler(90,0,s*35);}
                }
            }
            // Lower wall relief and botanical borders break large blank partition faces.
            foreach(float z in new[]{-7.65f,9.35f,27.55f})
            {
                float gap=z<0?-8:z<20?8:24.5f;
                for(int x=-29;x<=29;x+=6)
                {
                    if(Mathf.Abs(x-gap)<6)continue;
                    if(z>20&&x>-13&&x<3)continue; // clear projection screen
                    var g=Group(root,"종이 벽감과 식물 장식",V(x,0,z));modules.Add(g);
                    Box(g,"겹장 낮은 벽받침",V(0,.7f,0),V(5.2f,1.4f,.3f),x%2==0?teal:coral);
                    for(int k=0;k<6;k++)Stroke(g,"벽받침 제본선",new[]{V(-2.5f,.25f+k*.18f,-.18f),V(0,.25f+k*.18f,-.23f),V(2.5f,.25f+k*.18f,-.18f)},cream,.018f);
                    Leaves(g,V(-1.7f,2,-.15f),1.8f);Leaves(g,V(1.6f,1.6f,-.12f),1.2f);
                    for(int i=0;i<4;i++){var s=Star(g,V(-1.5f+i*.9f,9.4f+.4f*Mathf.Sin(i),-.2f),.23f,i%2==0?coral:teal);}
                    Stroke(g,"벽면 흐르는 잎줄기",Enumerable.Range(0,24).Select(i=>V(-2.6f+i*.22f,8.4f+.16f*Mathf.Sin(i*.6f),-.16f)).ToArray(),teal,.025f);
                }
            }
            // A side canopy gives the theatre a handmade stage silhouette and visible depth.
            foreach(int side in new[]{-1,1})
            {
                var g=Group(root,"접힌 극장 옆날개",V(-5+side*8,0,25.5f));modules.Add(g);
                for(int i=0;i<4;i++)
                {
                    var p=Paper(g,"부채처럼 포갠 무대 종이",V(side*i*.48f,0,i*.18f),1.5f,8-i*.3f,i%2==0?butter:cream,V(0,side*(18+i*4),0));
                    Leaves(p,V(0,.8f,-.09f),1.3f);
                }
                Hanging(g,V(0,7.5f,-.8f),11.6f,.7f);
            }
            // Each hanging star keeps its own warm pool. Soft shadows ground the furniture.
            var shadowLights=root.GetComponentsInChildren<Light>().Where(l=>l.intensity>=40).Take(3);
            foreach(var l in shadowLights){l.shadows=LightShadows.Soft;l.shadowBias=.025f;l.shadowNormalBias=.15f;l.intensity=55;l.color=new Color(1,.86f,.67f);}
            // Small hand-cut discs and stitches dress the open-book tables without blocking the clues.
            foreach(var desk in modules.Where(m=>m.name.StartsWith("제본 서랍")).ToArray())
            {
                for(int j=0;j<3;j++){var spool=Obj(desk,"둥근 크레파스 단면",RoundMedallion(.14f,.14f,.65f),j==0?coral:j==1?teal:ochre,V(1.9f,3.2f,j*.33f));spool.localRotation=Quaternion.Euler(0,70+j*7,0);}
                var scrap=Paper(desk,"잘라 쓰고 남은 종이",V(-2,3.11f,-.2f),.65f,1.1f,cream,V(90,0,18));
            }
        }
        static void CoverOutgoingPassage()
        {
            var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="1A"&&l.ToRoomId=="1B");var so=new SerializedObject(link);
            Vector3 a=so.FindProperty("fromPoint").vector3Value,b=so.FindProperty("toPoint").vector3Value;
            var old=link.transform.parent.Find("1A_다음열람실_닫힌통로");if(old)Object.DestroyImmediate(old.gameObject);
            var g=Group(link.transform.parent,"1A_다음열람실_닫힌통로",(a+b)*.5f);g.rotation=Quaternion.LookRotation(b-a,Vector3.up);
            var rb=g.gameObject.AddComponent<RoomBound>();var sr=new SerializedObject(rb);var ids=sr.FindProperty("roomIds");ids.arraySize=2;ids.GetArrayElementAtIndex(0).stringValue="1A";ids.GetArrayElementAtIndex(1).stringValue="1B";sr.ApplyModifiedPropertiesWithoutUndo();
            float length=Vector3.Distance(a,b),width=link.transform.lossyScale.x;
            foreach(int s in new[]{-1,1})Box(g,"책장 통로 연속 벽",V(s*(width/2+.2f),4,0),V(.5f,8.3f,length+4),cream,true);
            Box(g,"책장 통로 연속 천장",V(0,8,0),V(width+1,.5f,length+4),cream,true);
            for(float z=-length/2;z<length/2;z+=5)
            {
                Arch(g,V(0,0,z),width/2-.5f,4,0);Hanging(g,V(0,5.8f,z+1),7.6f,.65f);
                foreach(int s in new[]{-1,1})Leaves(g,V(s*(width/2-.1f),.6f,z+2),1.2f,s<0?90:-90);
                var leaf=Obj(g,"열람실을 가리키는 잎",OrganicLeaf(.8f,.28f),teal,V(0,.16f,z));leaf.localRotation=Quaternion.Euler(90,0,0);
            }
        }
        static void CaptureWorkshopParts()
        {
            var cam=new GameObject("Workshop part review").AddComponent<Camera>();cam.fieldOfView=42;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.19f,.19f);cam.farClipPlane=60;
            var key=new GameObject("Part softbox").AddComponent<Light>();key.type=LightType.Point;key.intensity=100;key.range=25;key.color=new Color(1,.9f,.75f);
            var fill=new GameObject("Part fill").AddComponent<Light>();fill.type=LightType.Point;fill.intensity=60;fill.range=25;fill.color=new Color(.75f,.88f,1);
            string[] prefixes={"제본 서랍","청록 그림책","꽃잎 기억등","층층 종이책"};
            try{for(int i=0;i<prefixes.Length;i++)
            {
                var source=modules.First(m=>m.name.StartsWith(prefixes[i]));var copy=Object.Instantiate(source.gameObject);copy.transform.position=V(-10000,0,0);copy.transform.rotation=Quaternion.identity;
                try{var renders=copy.GetComponentsInChildren<Renderer>();var bounds=renders[0].bounds;foreach(var r in renders){r.enabled=true;bounds.Encapsulate(r.bounds);}var center=bounds.center;float size=bounds.size.magnitude;
                    cam.transform.position=center+V(size*.55f,size*.35f,-size*1.35f);cam.transform.LookAt(center);key.transform.position=center+V(-3,6,-4);fill.transform.position=center+V(4,2,-2);
                    InteriorF0Builder.Capture(cam,WorkshopOut+"/part-"+(i+1)+".png");
                }finally{Object.DestroyImmediate(copy);}
            }}finally{Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);Object.DestroyImmediate(fill.gameObject);}
        }
    }
}

