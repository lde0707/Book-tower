using System.Collections.Generic;
using UnityEngine;
namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        static void Ropes()
        {
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-6f,10.4f})
            {
                Vector3 at=V(side*7.7f,0,z);
                Box(root,"밧줄기둥받침",at+V(0,.2f,0),V(1.7f,.4f,1.7f),umber,true,.16f);Box(root,"밧줄기둥소켓",at+V(0,.55f,0),V(1.25f,.5f,1.25f),brass,true,.15f);
                for(int strand=-1;strand<=1;strand++)Rope(root,"천장제본밧줄",at+V(strand*.19f,.68f,0),at+V(strand*.19f,16,0),.078f);
                Knot(root,at+V(0,2.05f,0),.36f,false);Knot(root,at+V(0,15.4f,0),.34f,false);
            }
            Box(root,"우측대형밧줄받침",V(12.2f,.35f,-9),V(2.3f,.7f,2),umber,true,.16f);
            for(int s=-1;s<=1;s++)Rope(root,"우측대형밧줄",V(12.2f+s*.25f,.7f,-9),V(12.2f+s*.25f,16,-9),.115f);
            Knot(root,V(12.2f,2.6f,-9),.64f,false);
            for(int i=-1;i<=1;i++)Rope(root,"바닥길잡이실",V(i*.12f,.10f,-19.7f),V(i*.12f,.10f,15.1f),.046f);
            foreach(float z in new[]{-7f,.6f,9f})Knot(root,V(0,.17f,z),.36f,true);
            Rope(root,"출구뒤실동선",V(3,.1f,18.3f),V(20,.1f,18.3f),.055f);
            Rope(root,"출구꺾임실동선",V(20,.1f,18.3f),V(20,.1f,11.2f),.055f);
            Rope(root,"출구연결실동선",V(20,.1f,11.2f),V(28,.1f,11.2f),.055f);
            Rope(root,"왼쪽단서밧줄",V(-10,.13f,3),V(0,.13f,.6f),.065f);Rope(root,"오른쪽단서밧줄",V(0,.13f,-7),V(10,.13f,-4),.065f);
            foreach(float x in new[]{-2.6f,2.6f})Box(root,"바닥적갈색인쇄선",V(x,.12f,-1),V(.09f,.014f,33),ink);
            foreach(float z in new[]{-16f,13.5f})Box(root,"바닥적갈색가로선",V(0,.12f,z),V(5.3f,.014f,.09f),ink);
        }
        static void WindowsAndLights()
        {
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-5.2f,5.4f})
            {
                var w=Group(root,"격자창문",V(side*15.9f,5.6f,z));w.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
                Box(w,"빛나는창호지",V(0,3,.03f),V(4.4f,6.1f,.1f),glow);
                foreach(float x in new[]{-2.35f,2.35f})Box(w,"창틀",V(x,3,-.16f),V(.22f,6.65f,.45f),wood);
                foreach(float y in new[]{-.2f,6.2f})Box(w,"창틀",V(0,y,-.16f),V(4.9f,.24f,.45f),wood);
                for(int i=-2;i<=2;i++)Box(w,"창세로살",V(i*.73f,3,-.24f),V(.085f,6.1f,.13f),brass);
                for(int i=1;i<8;i++)Box(w,"창가로살",V(0,i*.75f,-.24f),V(4.4f,.085f,.13f),brass);
                var key=Group(root,"창문투광",V(side*15.45f,10.3f,z)).gameObject.AddComponent<Light>();
                key.type=LightType.Spot;key.color=new Color(1f,.76f,.44f);key.intensity=side<0?105:36;key.range=34;key.spotAngle=74;key.innerSpotAngle=52;
                key.shadows=side<0?LightShadows.Soft:LightShadows.None;key.shadowBias=.015f;key.shadowNormalBias=.035f;
                key.transform.LookAt(room.transform.TransformPoint(V(side*-2,.1f,z+6)));key.cookie=WindowCookie();
            }
            var bounce=Group(root,"문쪽반사광",V(0,7,6)).gameObject.AddComponent<Light>();bounce.type=LightType.Point;bounce.color=new Color(1f,.83f,.61f);bounce.intensity=65;bounce.range=27;bounce.shadows=LightShadows.None;
            Lantern(V(10.2f,10.3f,1.5f),1.05f);Lantern(V(-10,11.6f,8.5f),.85f);Lantern(V(9,12.2f,-13),.75f);
            var fill=Group(root,"실내반사광",V(0,6,-9)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(1,.89f,.72f);fill.intensity=60;fill.range=22;fill.shadows=LightShadows.None;
        }
        static void Lantern(Vector3 at,float scale)
        {
            var p=Group(root,"매달린종이등",at);p.localScale=Vector3.one*scale;Rope(root,"등걸이",at+V(0,1.8f*scale,0),V(at.x,16,at.z),.038f);
            Box(p,"등종이",V(0,0,0),V(1.05f,1.65f,1.05f),glow);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(p,"등프레임",V(x*.57f,0,z*.57f),V(.09f,1.9f,.09f),wood);
            foreach(float y in new[]{-.94f,.94f})
            {
                Box(p,"등테두리",V(0,y,-.57f),V(1.23f,.09f,.09f),brass);Box(p,"등테두리",V(0,y,.57f),V(1.23f,.09f,.09f),brass);
                Box(p,"등테두리",V(-.57f,y,0),V(.09f,.09f,1.2f),brass);Box(p,"등테두리",V(.57f,y,0),V(.09f,.09f,1.2f),brass);
            }
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Tube(p,"등지붕",new[]{V(x*.58f,.97f,z*.58f),V(0,1.65f,0)},.055f,brass);
            for(int side=0;side<4;side++){var motif=Group(p,"등꽃무늬",Vector3.zero);motif.localRotation=Quaternion.Euler(0,90*side,0);Sprig(motif,V(0,-.6f,-.54f),1.13f,ink);}
            var light=p.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.55f,.23f);light.intensity=9;light.range=12;light.shadows=LightShadows.None;
        }
        static void SmallDetails()
        {
            for(int i=0;i<24;i++)
            {
                float x=Mathf.Sin(i*7.3f)*13,z=Mathf.Cos(i*2.6f)*15;if(Mathf.Abs(x)<1.2f)continue;
                var flake=MeshObject(root,"떨어진종이조각",F0Mesh.Paper(.12f+(i%4)*.08f,.22f,40+i),paper,V(x,.13f,z));flake.localRotation=Quaternion.Euler(83,i*37,0);
            }
            var banner=MeshObject(root,"황동걸쇠의찢긴쪽지",F0Mesh.Paper(1.5f,2.3f,871),ivory,V(-11.3f,6.5f,-14.9f));banner.localRotation=Quaternion.Euler(-12,-24,-12);
            Sprig(root,V(-10.5f,6.6f,-15.25f),1.8f,ochre);
        }
        static void Sprig(Transform p,Vector3 at,float h,Material material)
        {
            Tube(p,"금박잎줄기",new[]{at,at+V(h*.04f,h*.5f,0),at+V(0,h,0)},h*.009f,material);
            for(int i=0;i<4;i++)foreach(int side in new[]{-1,1})
            {var leaf=MeshObject(p,"압인잎",F0Mesh.Leaf(h*.26f,h*.105f),material,at+V(h*.025f,h*(.12f+i*.2f),-.018f));leaf.localRotation=Quaternion.Euler(0,0,side*43);}
        }
        static void Rope(Transform p,string name,Vector3 a,Vector3 b,float r)
        {
            Vector3 tangent=(b-a).normalized,side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized,up=Vector3.Cross(tangent,side);
            int steps=Mathf.Max(12,Mathf.CeilToInt(Vector3.Distance(a,b)*24));
            for(int strand=0;strand<3;strand++)
            {
                var points=new Vector3[steps+1];for(int i=0;i<=steps;i++){float t=i/(float)steps,angle=t*Vector3.Distance(a,b)*21+strand*Mathf.PI*2/3;points[i]=Vector3.Lerp(a,b,t)+(side*Mathf.Cos(angle)+up*Mathf.Sin(angle))*r*.47f;}
                Tube(p,name,points,r*.56f,rope);
            }
        }
        static void Knot(Transform p,Vector3 at,float r,bool floor)
        {
            var g=Group(p,"꼬인제본매듭",at);if(floor)g.localRotation=Quaternion.Euler(90,0,0);
            for(int s=0;s<3;s++)
            {
                var path=new Vector3[65];for(int i=0;i<path.Length;i++){float a=i*Mathf.PI*2/64;path[i]=V(Mathf.Sin(a)*r*(1.1f+s*.1f),Mathf.Sin(a*2)*r*.45f+s*.085f,Mathf.Cos(a)*r*.50f);}
                Tube(g,"매듭고리",path,r*.17f,rope);
            }
            for(int side=-1;side<=1;side+=2)Rope(g,"매듭끝",V(side*r*.3f,0,0),V(side*r*1.35f,-r*.65f,-.04f),r*.14f);
        }
    }
}
