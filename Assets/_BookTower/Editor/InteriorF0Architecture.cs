using System.Collections.Generic;
using UnityEngine;
using BookTower.Rooms;
namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        static void Shell()
        {
            Box(root,"연속_충돌바닥",V(0,-.28f,0),V(56,.5f,44),paper,true);
            for(int x=0;x<8;x++)for(int z=0;z<10;z++)
            {
                var tile=MeshObject(root,"장지바닥",F0Mesh.Paper(3.98f,3.98f,71+x*19+z),((x+z)%3==0)?ivory:paper,V(-14+x*4,.055f,-20+z*4));
                tile.localRotation=Quaternion.Euler(90,0,0);
            }
            Box(root,"뒷복도바닥",V(0,-.02f,19),V(55,.07f,6),paper);
            Box(root,"출구복도바닥",V(22,-.02f,11),V(12,.07f,16),paper);
            Box(root,"서벽",V(-16.3f,8,0),V(.55f,16.4f,40),umber,true);
            Box(root,"동벽",V(16.3f,8,-2),V(.55f,16.4f,36),umber,true);
            Box(root,"남벽",V(0,8,-20.3f),V(33,16.4f,.6f),umber,true);
            Box(root,"북쪽복도벽",V(0,8,22),V(56,16.4f,.55f),umber,true);
            Box(root,"동쪽출구_남",V(28,4,-6),V(.55f,8,29.6f),umber,true);
            Box(root,"동쪽출구_북",V(28,4,18),V(.55f,8,9.2f),umber,true);
            Box(root,"동쪽출구_상",V(28,7,11.2f),V(.55f,2,5.6f),wood,true);
            Box(root,"문벽_좌",V(-11.2f,8,15.8f),V(10.1f,16,.65f),umber,true);
            Box(root,"문벽_우",V(11.2f,8,15.8f),V(10.1f,16,.65f),umber,true);
            Box(root,"문벽_상",V(0,14.9f,15.8f),V(12.5f,2.25f,.65f),wood,true);
            Box(root,"제본천장",V(0,16.45f,0),V(33,.6f,41),paper,true);
            for(int z=-17;z<=15;z+=8)
            {Box(root,"천장보",V(0,16,z),V(33,.6f,.75f),wood);Box(root,"천장금테",V(0,15.65f,z),V(32,.10f,.1f),brass);}
            foreach(int side in new[]{-1,1})
            {
                Box(root,"벽하부책장",V(side*15.9f,1.4f,-2),V(.25f,2.8f,36),wood);
                for(int z=-18;z<=14;z+=4)
                {Box(root,"벽기둥세로띠",V(side*15.72f,7.5f,z),V(.15f,15,.18f),brass);Box(root,"벽하부패널",V(side*15.65f,1.4f,z+1.8f),V(.13f,2.1f,3.2f),oxblood);}
            }
        }
        static void Door()
        {
            leftDoor=Group(root,"제본문_왼쪽",V(-5.8f,0,15.2f));rightDoor=Group(root,"제본문_오른쪽",V(5.8f,0,15.2f));
            foreach(int side in new[]{-1,1})
            {
                Transform parent=side<0?leftDoor:rightDoor;float cx=-side*2.87f;
                Box(parent,"제본문_충돌",V(cx,6.8f,.16f),V(5.74f,13.6f,.35f),ivory,true);
                MeshObject(parent,"제본문_주름장지",F0Mesh.Paper(5.68f,13.5f,33+side),ivory,V(cx,.05f,-.07f));
                foreach(float x in new[]{cx-2.66f,cx+2.66f})Box(parent,"인쇄테두리",V(x,6.8f,-.14f),V(.055f,13.16f,.025f),ink);
                foreach(float y in new[]{.27f,13.33f})Box(parent,"인쇄테두리",V(cx,y,-.14f),V(5.33f,.055f,.025f),ink);
                Sprig(parent,V(cx-side*.2f,4,-.22f),4.2f,ink);
                foreach(float y in new[]{.55f,13.05f})foreach(float x in new[]{cx-2.38f,cx+2.38f})Sprig(parent,V(x,y,-.2f),.42f,ink);
            }
            Box(root,"문황동프레임좌",V(-6.05f,6.85f,15.05f),V(.27f,13.9f,.45f),brass,true);
            Box(root,"문황동프레임우",V(6.05f,6.85f,15.05f),V(.27f,13.9f,.45f),brass,true);
            Box(root,"문황동프레임상",V(0,13.85f,15.05f),V(12.4f,.3f,.45f),brass);
            var seam=Group(leftDoor,"빛나는제본실",V(5.77f,0,-.22f));Rope(seam,"문중앙밧줄",V(0,.25f,0),V(0,13.65f,0),.085f);
            Box(seam,"문틈빛",V(.035f,6.8f,.2f),V(.075f,13.4f,.02f),glow);
            foreach(float y in new[]{2.6f,6.25f,10.3f})Knot(seam,V(0,y,-.12f),.46f,false);
            var rig=root.gameObject.AddComponent<BindingDoor>();rig.left=leftDoor;rig.right=rightDoor;
        }
        static void Library()
        {
            foreach(int side in new[]{-1,1})
            {
                int k=0;foreach(float z in new[]{-13f,-3f,10.8f})
                {
                    Book(root,$"거대제본책_{side}_{k}",V(side*13.4f,0,z),3.6f,15.7f,3.3f,k%2==0?oxblood:umber,side*4);
                    Book(root,"끼워진책",V(side*15.6f,0,z+.4f),1f,15.3f,3.2f,ochre,side*3);k++;
                }
            }
            for(int i=0;i<6;i++)
            {
                FlatBook(root,V(-11+i*.12f,.08f+i*.46f,4),6.3f-i*.28f,.40f,3.1f,i%2==0?oxblood:umber,i%2==0?2:-4);
                FlatBook(root,V(12-i*.15f,.08f+i*.4f,11.5f),5.8f-i*.18f,.35f,3.4f,i%2==0?umber:ochre,i%2==0?-3:5);
            }
            for(int i=0;i<4;i++)Book(root,"뒷편장서",V(-12.5f+i*1.4f,0,13),1.2f,6.5f+i*.4f,2.8f,i%2==0?umber:oxblood,0);
            Table(root,V(12,0,2.8f));
            for(int i=0;i<18;i++)Box(root,"책상위낱장",V(12+Mathf.Sin(i)*.07f,3.65f+i*.055f,2.8f),V(4.3f,.045f,2.35f),i%3==0?paper:ivory);
            PaperArch(V(-10.7f,0,-10));
        }
        static void Book(Transform p,string name,Vector3 at,float w,float h,float d,Material cover,float yaw)
        {
            Transform b=Group(p,name,at);b.localRotation=Quaternion.Euler(0,yaw,0);
            Box(b,"책장덩어리",V(0,h*.5f,.06f),V(w-.22f,h-.28f,d-.28f),edge,true,.1f);
            Box(b,"가죽책등",V(0,h*.5f,-d*.5f),V(w,h,.5f),cover,true,.22f);
            foreach(int s in new[]{-1,1})Box(b,"책표지",V(s*w*.5f,h*.5f,.1f),V(.18f,h,d+.35f),cover,true,.055f);
            foreach(float y in new[]{.16f,.39f,h*.25f,h*.75f,h-.35f,h-.13f})
            {Box(b,"책등양각띠",V(0,y,-d*.5f-.28f),V(w+.1f,.13f,.13f),cover,false,.06f);Box(b,"금박테두리",V(0,y+.065f,-d*.5f-.36f),V(w-.15f,.025f,.028f),brass);}
            for(int i=0;i<20;i++)Box(b,"종이단면",V(w*.5f-.09f,.3f+i*(h-.6f)/20,.07f),V(.025f,.02f,d-.37f),paper);
            Sprig(b,V(0,h*.79f,-d*.5f-.3f),h*.13f,brass);
            if(h>10)
            {
                Box(b,"황동걸쇠",V(0,h*.49f,-d*.5f-.33f),V(w+.18f,1.6f,.14f),brass,false,.10f);
                Box(b,"걸쇠맞춤선",V(.3f,h*.49f,-d*.5f-.43f),V(.06f,1.5f,.03f),umber);Sprig(b,V(-.38f,h*.46f,-d*.5f-.44f),.9f,umber);
                foreach(float x in new[]{-w*.42f,w*.42f})foreach(float y in new[]{h*.49f-.55f,h*.49f+.55f})Sphere(b,"걸쇠리벳",V(x,y,-d*.5f-.45f),V(.09f,.09f,.06f),brass);
            }
        }
        static void FlatBook(Transform p,Vector3 at,float w,float h,float d,Material cover,float yaw)
        {
            var b=Group(p,"가로로쌓인거대책",at);b.localRotation=Quaternion.Euler(0,yaw,0);
            Box(b,"책장",V(0,h*.5f,0),V(w-.16f,h,d-.18f),edge,true,.04f);
            foreach(float y in new[]{.025f,h+.04f})Box(b,"표지",V(0,y,0),V(w+.16f,.07f,d+.13f),cover);
            for(int i=0;i<7;i++)Box(b,"겹겹의책장",V(Mathf.Sin(i)*.025f,.06f+i*(h-.1f)/7,-d*.5f-.02f),V(w-.2f,.012f,.045f),paper);
            Box(b,"책잠금띠",V(w*.29f,h*.5f,-d*.5f-.045f),V(.52f,h+.12f,.08f),brass);
        }
        static void Table(Transform p,Vector3 at)
        {
            var g=Group(p,"책상",at);Box(g,"상판",V(0,3.5f,0),V(5.3f,.25f,3.2f),wood,true);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {Box(g,"다리",V(x*2.15f,1.65f,z*1.1f),V(.35f,3.3f,.35f),wood,true);Box(g,"황동다리밴드",V(x*2.15f,2.9f,z*1.1f),V(.42f,.3f,.42f),brass);}
            Box(g,"가로지지대",V(0,.7f,.9f),V(4.5f,.22f,.24f),wood);
        }
        static void PaperArch(Vector3 at)
        {
            var p=Group(root,"접힌종이아치",at);p.localRotation=Quaternion.Euler(0,9,0);
            var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int i=0;i<=40;i++)
            {
                float a=i*Mathf.PI/40;foreach(float r in new[]{1.16f,1.60f}){verts.Add(V(Mathf.Cos(a)*r,1.48f+Mathf.Sin(a)*r,-.12f+Mathf.Sin(i*3.4f)*.035f));uv.Add(new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
                if(i>0){int q=(i-1)*2;tris.AddRange(new[]{q,q+1,q+2,q+1,q+3,q+2});}
            }
            var m=new Mesh();m.SetVertices(verts);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateTangents();MeshObject(p,"찢긴종이아치면",m,ivory,Vector3.zero);
            foreach(int s in new[]{-1,1})Box(p,"아치기둥",V(s*1.4f,.74f,.05f),V(.43f,1.48f,.3f),paper,true);
            for(int i=0;i<19;i++){float a=i*Mathf.PI/18;Vector3 pos=V(Mathf.Cos(a)*1.48f,1.48f+Mathf.Sin(a)*1.48f,-.21f);Tube(p,"아치실밥",new[]{pos+V(-.09f,0,0),pos+V(0,.02f,-.1f),pos+V(.09f,0,0)},.043f,rope);}
            Box(p,"종이집지붕",V(0,2.85f,1.2f),V(3.2f,.15f,2.6f),paper);Box(p,"종이집뒷벽",V(0,1.4f,2.3f),V(3.2f,2.8f,.2f),umber,true);
        }
    }
}
