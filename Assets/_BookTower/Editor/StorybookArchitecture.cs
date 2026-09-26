using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
namespace BookTower.EditorTools
{
    public static partial class StorybookRoomBuilder
    {
        static void Shell()
        {
            Box(root,"연속 충돌 바닥",V(0,-.20f,0),V(64,.4f,56),butter,true);
            for(int x=-28;x<=28;x+=8)for(int z=-28;z<28;z+=7)
                Paper(root,"포개진 장지 바닥",V(x,.026f+(x+28)/8*.001f,z),8,7,butter,V(90,0,0));
            Box(root,"천장 바탕",V(0,12.2f,0),V(64,.6f,56),butter,true);
            for(int x=-28;x<=28;x+=8)for(int z=-28;z<28;z+=7)Paper(root,"구겨진 천장",V(x,11.87f,z),8,7,cream,V(90,0,0));
            // Doorway openings match the existing F0 and 1B ramp endpoints.
            WallSide(-32,-28,-17.5f);WallSide(-32,-8,28);WallSide(32,-28,28);
            Box(root,"남쪽 종이 벽",V(0,6,-28),V(64,12,.4f),butter,true);
            Box(root,"북쪽 좌벽",V(-5.8f,6,28),V(52.4f,12,.4f),butter,true);
            Box(root,"북쪽 우벽",V(30.5f,6,28),V(3,12,.4f),butter,true);
            Box(root,"출구 윗벽",V(24.7f,10.5f,28),V(8.5f,3,.4f),butter,true);
            Partition(-7,-8);Partition(10,8);
            Arch(root,V(-8,0,-7.42f),4.7f,4.0f,0);Arch(root,V(8,0,9.58f),4.7f,4,0);
            Arch(root,V(24.5f,0,27.65f),4.15f,4,0);
            Arch(root,V(-31.6f,0,-12.8f),4.6f,3.4f,90);
            for(int z=-22;z<26;z+=8)
            {
                Paper(root,"겹친 서쪽 벽지",V(-31.73f,0,z),7.8f,11.8f,cream,V(0,90,0));
                Paper(root,"겹친 동쪽 벽지",V(31.73f,0,z),7.8f,11.8f,cream,V(0,-90,0));
            }
            // Remove the west wallpaper that would span the real entry hole.
            foreach(Transform c in root)if(c.name=="겹친 서쪽 벽지"&&Mathf.Abs(c.localPosition.z+12.8f)<6)c.gameObject.SetActive(false);
        }
        static void WallSide(float x,float a,float b)=>Box(root,"외벽 충돌",V(x,6,(a+b)/2),V(.4f,12,b-a),butter,true);
        static void Partition(float z,float center)
        {
            float left=center-4.7f,right=center+4.7f;
            Box(root,"구역 분리 접힌 벽",V((-32+left)/2,6,z),V(left+32,12,.55f),butter,true);
            Box(root,"구역 분리 접힌 벽",V((right+32)/2,6,z),V(32-right,12,.55f),butter,true);
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int i=0;i<=48;i++){float x=-4.7f+9.4f*i/48;float y=4+Mathf.Sqrt(Mathf.Max(0,4.7f*4.7f-x*x));v.Add(V(x,y,0));v.Add(V(x,12,0));uv.Add(new Vector2(x,y)*.5f);uv.Add(new Vector2(x,12)*.5f);if(i>0){int q=(i-1)*2;tri.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3});}}
            Obj(root,"아치 위 찢긴 벽",Mesh(v,uv,tri),butter,V(center,0,z),true);
            Paper(root,"왼쪽 벽지",V((-32+left)/2,0,z-.33f),left+32,11.9f,cream,Vector3.zero);
            Paper(root,"오른쪽 벽지",V((right+32)/2,0,z-.33f),32-right,11.9f,cream,Vector3.zero);
        }
        static Mesh Mesh(List<Vector3> v,List<Vector2> uv,List<int> t)
        {var m=new Mesh{indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        static void Arch(Transform parent,Vector3 at,float r,float spring,float yaw)
        {
            var g=Group(parent,"겹겹이 찢어진 페이지 아치",at);g.localRotation=Quaternion.Euler(0,yaw,0);
            for(int layer=0;layer<4;layer++)
            {
                float rad=r+layer*.19f;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
                for(int i=0;i<=64;i++)
                {
                    float a=Mathf.PI*i/64;float rough=.035f*Mathf.Sin(i*2.3f+layer)+.022f*Mathf.Sin(i*4.7f);
                    foreach(float rr in new[]{rad+rough,rad+.23f+rough}){v.Add(V(Mathf.Cos(a)*rr,spring+Mathf.Sin(a)*rr,-layer*.11f));uv.Add(new Vector2(a*rad,rr)*.6f);}
                    if(i>0){int q=(i-1)*2;t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3});}
                }
                Obj(g,"뜯긴 아치 테두리",Mesh(v,uv,t),layer%2==0?cream:butter,Vector3.zero);
                foreach(float side in new[]{-1f,1f})Paper(g,"아치 겹침 기둥",V(side*(rad+.12f),0,-layer*.11f),.24f,spring+.03f,layer%2==0?cream:butter,Vector3.zero);
            }
        }
        static Transform Star(Transform parent,Vector3 at,float radius,Material mat)
        {
            var v=new List<Vector3>{V(0,0,-radius*.29f),V(0,0,radius*.29f)};var uv=new List<Vector2>{new(.5f,.5f),new(.5f,.5f)};var t=new List<int>();
            for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;float r=i%2==0?radius:radius*.43f;v.Add(V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));uv.Add(new Vector2(.5f+Mathf.Cos(a)*r,.5f+Mathf.Sin(a)*r));}
            for(int i=0;i<10;i++){int a=i+2,b=(i+1)%10+2;t.AddRange(new[]{0,b,a,1,a,b});}
            return Obj(parent,"접힌 오각 종이별",Mesh(v,uv,t),mat,at);
        }
        static void Hanging(Transform p,Vector3 at,float top,float radius)
        {Star(p,at,radius,glow);Stroke(p,"별을 매단 제본실",new[]{at+Vector3.up*radius,V(at.x,top,at.z)},ochre,.025f);LightAt(p,at-Vector3.up*.3f,5,15);}
        static Material Illustration(string name)
        {
            string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/Storybook/"+name+".png");m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",Color.white);m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.25f);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",0);m.renderQueue=2450;EditorUtility.SetDirty(m);return m;
        }
        static Transform Art(Transform p,string animal,Vector3 at,float size,float yaw=0)
        {
            var m=Mesh(new List<Vector3>{V(-size/2,0,0),V(-size/2,size,0),V(size/2,size,0),V(size/2,0,0)},new List<Vector2>{new(0,0),new(0,1),new(1,1),new(1,0)},new List<int>{0,1,2,0,2,3});
            var t=Obj(p,"크레파스 "+animal,m,Illustration(animal),at);t.localRotation=Quaternion.Euler(0,yaw,0);return t;
        }
        static void Leaves(Transform p,Vector3 at,float scale,float yaw=0)
        {
            var g=Group(p,"청록 크레파스 잎",at);g.localRotation=Quaternion.Euler(0,yaw,0);Stroke(g,"줄기",new[]{V(0,0,0),V(.18f*scale,scale,0),V(0,2*scale,0)},teal,.04f*scale);
            for(int i=0;i<5;i++){var leaf=Obj(g,"입체 압화",F0Mesh.Leaf(.68f*scale,.30f*scale),teal,V(.1f*scale,(.25f+i*.32f)*scale,-.035f));leaf.localRotation=Quaternion.Euler(0,0,i%2==0?-48:48);}
        }
        static void Books(Vector3 at,int count,float yaw)
        {
            var g=Group(root,"겹친 그림책 더미",at);g.localRotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<count;i++)
            {
                var book=Group(g,"둥근 팝업북",V(Mathf.Sin(i)*.15f,i*.8f,0));book.localRotation=Quaternion.Euler(0,Mathf.Sin(i*7)*5,0);
                Box(book,"크레파스 표지",V(0,.06f,0),V(4.4f,.14f,3.1f),i%2==0?coral:teal);
                Box(book,"두꺼운 종이단면",V(0,.42f,0),V(4.1f,.58f,2.85f),cream);
                Box(book,"윗 표지",V(0,.76f,0),V(4.4f,.14f,3.1f),i%2==0?coral:teal);
                for(int line=0;line<6;line++)Stroke(book,"층층 책장",new[]{V(-2.0f,.18f+line*.075f,-1.43f),V(0,.18f+line*.075f,-1.48f),V(2,.18f+line*.075f,-1.43f)},butter,.016f);
            }
            var collision=Group(g,"책더미 충돌",V(0,count*.4f,0)).gameObject.AddComponent<BoxCollider>();collision.size=V(4.2f,count*.8f,3);
        }
        static void Dress()
        {
            Art(root,"Rabbit",V(-22,1,-7.39f),10);Art(root,"Bird",V(17,1,-7.39f),9);
            Art(root,"Fox",V(-19,1,9.61f),10);Art(root,"Rabbit",V(23,1,9.61f),8);
            Art(root,"Fox",V(4,1,27.66f),11);Art(root,"Bird",V(-31.65f,1,4),9,90);
            for(int z=-22;z<=20;z+=14)foreach(float side in new[]{-1f,1f}){Leaves(root,V(side*31.6f,.2f,z),2.4f,side<0?90:-90);Hanging(root,V(side*12,8.8f,z),12,1.05f);}
            Hanging(root,V(-8,9.8f,-6),12,.85f);Hanging(root,V(8,9.8f,11),12,.85f);
            foreach(var at in new[]{V(-26,0,-23),V(24,0,-23),V(-27,0,3),V(27,0,3),V(-24,0,21)}){Books(at,3,at.x);Leaves(root,at+V(0,2.4f,0),1.1f);}
            foreach(float z in new[]{-18f,1f,19f}){LightAt(root,V(0,7,z),45,32);LightAt(root,V(-23,5,z),20,23);LightAt(root,V(23,5,z),20,23);}
            // Interrupted crayon trails guide toward arches without a HUD.
            var points=new[]{V(-29,.06f,-12.8f),V(-16,.06f,-16),V(-8,.06f,-11),V(-8,.06f,-2),V(0,.06f,2),V(8,.06f,6),V(8,.06f,16),V(16,.06f,22),V(24.5f,.06f,26)};
            for(int i=0;i<points.Length-1;i++)for(int j=0;j<7;j++)Stroke(root,"초크 발자국",new[]{Vector3.Lerp(points[i],points[i+1],j/7f),Vector3.Lerp(points[i],points[i+1],(j+.48f)/7f)},i<4?coral:teal,.055f);
            for(int i=0;i<18;i++){float x=-28+(i*13%56),z=-24+(i*17%48);var star=Star(root,V(x,11.94f,z),.5f,i%2==0?coral:teal);star.localRotation=Quaternion.Euler(90,0,i*31);}
        }
    }
}
