using UnityEngine;
using UnityEngine.UI;
namespace BookTower.UI
{
    // Vector details retain crisp, slightly irregular paper edges at every UI scale.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PaperMenuOrnament : MaskableGraphic
    {
        public bool signature;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            void Line(Vector2 a,Vector2 b,float width,Color c)
            {
                var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=vh.currentVertCount;
                vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddVert(a-n,c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
            }
            if(signature)
            {
                Vector2 P(float x,float y)=>new(r.xMin+x*r.width,r.yMin+y*r.height);
                // The game's open page, shared light and two growing branches form a watermark.
                for(int side=-1;side<=1;side+=2)
                {
                    Vector2 prev=P(.5f,.18f);
                    for(int i=1;i<=40;i++){float t=i/40f;var p=P(.5f+side*(.12f*t+.25f*t*t),.18f+.65f*t);Line(prev,p,2,color);prev=p;}
                    for(int j=0;j<7;j++)
                    {
                        float t=.16f+j*.105f;var start=P(.5f+side*(.12f*t+.25f*t*t),.18f+.65f*t);
                        var end=start+new Vector2(side*r.width*(.075f+.03f*t),r.height*.075f);
                        var mid=(start+end)*.5f;var n=new Vector2(-(end-start).y,(end-start).x).normalized*r.width*.018f;
                        int k=vh.currentVertCount;vh.AddVert(start,color,Vector2.zero);vh.AddVert(mid+n,color,Vector2.zero);vh.AddVert(end,color,Vector2.zero);vh.AddVert(mid-n,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
                    }
                    Line(P(.5f,.32f),P(.5f+side*.18f,.38f),2,color);Line(P(.5f+side*.18f,.38f),P(.5f+side*.18f,.57f),2,color);Line(P(.5f+side*.18f,.57f),P(.5f,.51f),2,color);
                }
                Line(P(.5f,.32f),P(.5f,.51f),2,color);
                var c=P(.5f,.69f);float rad=r.width*.045f;Vector2 last=c+Vector2.up*rad;
                for(int i=1;i<=8;i++){float a=i*Mathf.PI/4;float rr=i%2==0?rad:rad*.4f;var p=c+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*rr;Line(last,p,2,color);last=p;}
                return;
            }
            for(int layer=0;layer<2;layer++)
            {
                float inset=10+layer*3;var box=new Rect(r.xMin+inset,r.yMin+inset,r.width-inset*2,r.height-inset*2);float radius=Mathf.Min(20,box.height*.45f);
                var centers=new[]{new Vector2(box.xMax-radius,box.yMax-radius),new Vector2(box.xMin+radius,box.yMax-radius),new Vector2(box.xMin+radius,box.yMin+radius),new Vector2(box.xMax-radius,box.yMin+radius)};
                Vector2 first=Vector2.zero,previous=Vector2.zero;bool started=false;
                for(int corner=0;corner<4;corner++)for(int i=0;i<=12;i++)
                {float angle=(corner*90+i*7.5f)*Mathf.Deg2Rad;float rr=radius+Mathf.Sin(i*2.7f+corner)*.7f;var point=centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rr;if(!started){first=point;started=true;}else Line(previous,point,layer==0?1.2f:.6f,color*(layer==0?1:.55f));previous=point;}
                Line(previous,first,1,color);

            }
            for(int i=0;i<r.width/17;i++){float x=r.xMin+35+i*17;if(x>r.xMax-35)break;Line(new Vector2(x,r.yMin+10),new Vector2(x+3,r.yMin+13),.65f,color*.55f);}
        }
    }
}
