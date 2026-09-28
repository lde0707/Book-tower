using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BookTower.EditorTools
{
    public static partial class MoaBirdBuilder
    {
        // One continuous, asymmetric profile. Head and breast share topology and a
        // soft bone-weight transition instead of intersecting ellipsoids.
        static readonly Vector4[] BirdProfile={
            new(.13f,0,0,-.06f),new(.17f,.116f,.130f,-.055f),
            new(.25f,.218f,.236f,-.048f),new(.35f,.266f,.286f,-.035f),
            new(.44f,.271f,.285f,-.012f),new(.53f,.236f,.249f,.033f),
            new(.61f,.223f,.222f,.075f),new(.69f,.249f,.231f,.099f),
            new(.77f,.247f,.228f,.104f),new(.85f,.210f,.194f,.093f),
            new(.91f,.133f,.125f,.082f),new(.944f,0,0,.078f)};
        static Vector3 Profile(float y)
        {
            int i=0;while(i<BirdProfile.Length-2&&y>BirdProfile[i+1].x)i++;
            var a=BirdProfile[i];var b=BirdProfile[i+1];
            float t=Mathf.Clamp01((y-a.x)/(b.x-a.x));
            float Smooth(int column)
            {
                int prev=Mathf.Max(0,i-1),next=Mathf.Min(BirdProfile.Length-1,i+2);
                float m0=(b[column]-BirdProfile[prev][column])/(b.x-BirdProfile[prev].x);
                float m1=(BirdProfile[next][column]-a[column])/(BirdProfile[next].x-a.x);
                return (2*t*t*t-3*t*t+1)*a[column]+(t*t*t-2*t*t+t)*m0*(b.x-a.x)
                    +(-2*t*t*t+3*t*t)*b[column]+(t*t*t-t*t)*m1*(b.x-a.x);
            }
            float cap=1;
            if(i==0){float q=Mathf.Max(.00001f,t);cap+=(1/Mathf.Sqrt(q)-1)*(1-q)*(1-q);}
            if(i==BirdProfile.Length-2){float q=Mathf.Max(.00001f,1-t);cap+=(1/Mathf.Sqrt(q)-1)*(1-q)*(1-q);}
            return V(Mathf.Max(0,Smooth(1))*cap,Mathf.Max(0,Smooth(2))*cap,Smooth(3));
        }
        static Vector3 Skin(float y,float angle)
        {
            var p=Profile(y);float sa=Mathf.Sin(angle),ca=Mathf.Cos(angle);
            // Subtle broad undulation, not a perfect surface of revolution.
            float ripple=1+.008f*Mathf.Sin(angle*3+y*13)+.005f*Mathf.Sin(angle*7-y*21);
            return V(sa*p.x*ripple,y,p.z+ca*p.y*ripple);
        }
        static Vector3 SkinNormal(float y,float angle)
        {return Vector3.Cross(Skin(y,angle+.001f)-Skin(y,angle-.001f),Skin(y+.0005f,angle)-Skin(y-.0005f,angle)).normalized;}
        static float HeadWeight(float y)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.69f,y));
        static void SkinRenderer(Transform t,Transform body,Transform head)
        {
            var filter=t.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
            var renderer=t.GetComponent<MeshRenderer>();var mat=renderer.sharedMaterial;
            var weights=new BoneWeight[mesh.vertexCount];var verts=mesh.vertices;
            for(int i=0;i<weights.Length;i++){float w=HeadWeight(verts[i].y);weights[i]=w>=.5f?new BoneWeight{boneIndex0=1,boneIndex1=0,weight0=w,weight1=1-w}:new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-w,weight1=w};}
            mesh.boneWeights=weights;mesh.bindposes=new[]{body.worldToLocalMatrix*t.localToWorldMatrix,head.worldToLocalMatrix*t.localToWorldMatrix};EditorUtility.SetDirty(mesh);
            UnityEngine.Object.DestroyImmediate(renderer);UnityEngine.Object.DestroyImmediate(filter);
            var skin=t.gameObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=mat;skin.bones=new[]{body,head};skin.rootBone=body.parent;skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone2;
            skin.localBounds=new Bounds(V(0,.50f,0),V(1.25f,1.3f,1.3f));
        }
        static void BuildNaturalAnatomy(Transform rig,BookTower.Story.PaperCompanion c)
        {
            var body=Group(rig,"Body",V(0,.365f,-.055f));var head=Group(rig,"Head",V(0,.655f,.072f));c.head=head;
            const int rows=100,cols=96;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float b=-Mathf.PI*.5f+j*Mathf.PI/rows;float y=.537f+.407f*Mathf.Sin(b),a=Mathf.PI+i*Mathf.PI*2/cols;
                v.Add(Skin(y,a));uv.Add(new Vector2(i/(float)cols,j/(float)rows));
                if(j<rows&&i<cols){int q=j*(cols+1)+i;tr.AddRange(new[]{q,q+1,q+cols+1,q+1,q+cols+2,q+cols+1});}
            }
            var core=Put(rig,"ContinuousPaperBody",Make("Sculpted continuous head breast and belly",v,uv,tr),paper,Vector3.zero);SkinRenderer(core,body,head);

            // A few long, narrow torn-paper seams follow the body. No helmet rim or
            // horizontal collar around the head, and no rectangular facial overlay.
            for(int seam=0;seam<3;seam++)
            {
                int n=seam;
                var strip=Put(rig,"HandlaidPaperSeam"+seam,Sheet((t,s)=>
                {
                    float y=.24f+.60f*t,angle=(n==0?2.35f:n==1?-1.48f:-2.7f)+.22f*Mathf.Sin(t*4+n);
                    float width=.022f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.7f);angle+=s*width;
                    var p=Skin(y,angle);return p+SkinNormal(y,angle)*(.0018f+.0012f*(1-s*s));
                },.0007f,60,6),wrap,Vector3.zero);SkinRenderer(strip,body,head);
            }
            foreach(int side in new[]{-1,1})
            {
                // Ink is a conforming oval on the skin rather than a floating black ball.
                var eyeV=new List<Vector3>();var eyeUV=new List<Vector2>();var eyeTr=new List<int>();
                const int rings=14,segments=56;
                for(int r=0;r<=rings;r++)for(int i=0;i<=segments;i++)
                {
                    float phi=i*Mathf.PI*2/segments,rad=r/(float)rings;
                    float y=.736f+Mathf.Sin(phi)*.039f*rad,a=side*.77f+Mathf.Cos(phi)*.128f*rad;
                    eyeV.Add(Skin(y,a)+SkinNormal(y,a)*.007f-head.localPosition);eyeUV.Add(new Vector2(.5f+.5f*rad*Mathf.Cos(phi),.5f+.5f*rad*Mathf.Sin(phi)));
                    if(r<rings&&i<segments){int q=r*(segments+1)+i;eyeTr.AddRange(new[]{q,q+segments+1,q+1,q+1,q+segments+1,q+segments+2});}
                }
                Put(head,"Eye"+(side<0?"L":"R"),Make("Ink follows cheek curvature",eyeV,eyeUV,eyeTr),ink,Vector3.zero);
                Vector3 hinge=V(side*.222f,.55f,.035f);var wing=Group(rig,side<0?"WingL":"WingR",hinge);if(side<0)c.leftWing=wing;else c.rightWing=wing;
                for(int layer=2;layer>=0;layer--)
                {
                    int n=layer;
                    Vector3 At(float t,float s)
                    {
                        float width=(.031f*(1-t)+.094f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.8f))*(1-.22f*t);
                        if(side<0&&n<2&&s<-.30f)width*=1-.40f*Mathf.Exp(-Mathf.Pow((t-.81f)/.065f,2));
                        float y=.555f-(.218f+n*.032f)*t+s*width*.69f-n*.012f;
                        float z=.078f-(.300f+n*.030f)*t-s*width*.73f;
                        var p=Profile(y);float zz=(z-p.z)/p.y;
                        float x=p.x*Mathf.Sqrt(Mathf.Max(.025f,1-zz*zz));
                        x+=.007f+(2-n)*.009f+.012f*Mathf.Sin(Mathf.PI*t)*(1-s*s)-.035f*Mathf.Pow(1-t,5)+.025f*Mathf.Pow(t,6);
                        x+=.0016f*Mathf.Pow(Mathf.Abs(s),6)*Mathf.Sin(36*t+n*2);
                        return V(side*x,y,z)-hinge;
                    }
                    Put(wing,"Wing"+(side<0?"L":"R")+"Sheet"+layer,Sheet(At,.0027f,38,18),wrap,Vector3.zero);
                    if(side<0&&n==0)
                        for(int spot=0;spot<3;spot++)
                        {
                            int k=spot;Put(wing,"WingGold"+spot,Sheet((t,s)=>
                            {
                                float tt=.68f+k*.049f+(t-.5f)*.042f,ss=-.18f+k*.19f+s*.045f*Mathf.Sin(t*Mathf.PI);
                                return At(tt,ss)+V(-.0024f,0,0);
                            },.0005f,10,6),gold,Vector3.zero);
                        }
                }
                var foot=Group(rig,side<0?"FootL":"FootR",V(side*.103f,.155f,.02f));
                Put(foot,"Leg"+side,Tube(new[]{V(0,.026f,-.02f),V(side*.008f,-.035f,0),V(0,-.124f,.043f)},.0115f,10),ochre,Vector3.zero);
                for(int toe=-1;toe<=2;toe++)
                {
                    bool back=toe==2;float angle=back?Mathf.PI:toe*.55f;
                    Vector3 start=V(0,-.126f,.043f),end=start+V(Mathf.Sin(angle)*(back?.042f:.069f),-.005f,Mathf.Cos(angle)*(back?.042f:.081f));
                    Vector3 across=Vector3.Cross(Vector3.up,end-start).normalized;
                    Put(foot,"Toe"+side+"_"+toe,Sheet((t,s)=>Vector3.Lerp(start,end,t)+across*s*.012f*Mathf.Pow(Mathf.Max(.001f,1-t),.5f)+Vector3.up*(.006f*Mathf.Sin(t*Mathf.PI)*(1-Mathf.Abs(s))),.004f,16,6),ochre,Vector3.zero);
                }
            }
            Put(head,"UpperBeak",SoftBeak(true),ochre,V(0,.053f,.236f));
            Put(head,"LowerBeak",SoftBeak(false),ochre,V(0,.048f,.236f));
            var tail=Group(rig,"Tail",V(0,.34f,-.272f));
            foreach(int feather in new[]{-1,1,0})
            {
                int n=feather;
                Put(tail,"TailSheet"+n,Sheet((t,s)=>
                {
                    float w=(.030f*(1-t)+.065f*Mathf.Sin(Mathf.PI*t))*(1-.15f*t);
                    return V(n*.078f*t+s*w,.025f+.135f*t+.065f*t*t+.016f*Mathf.Sin(Mathf.PI*t)*(1-Mathf.Abs(s))+(n==0?.009f:0),-.255f*t+.017f*s*s);
                },.0028f,34,16),wrap,Vector3.zero);
            }
        }
        static Mesh SoftBeak(bool upper)
        {
            return Sheet((t,s)=>V(s*.047f*Mathf.Pow(Mathf.Max(0,1-t),.72f),
                (upper?1:-.48f)*(.023f*Mathf.Sin((t*.76f+.20f)*Mathf.PI)*(1-Mathf.Abs(s)))-.012f*t,t*.088f),.003f,24,16);
        }
    }
}
