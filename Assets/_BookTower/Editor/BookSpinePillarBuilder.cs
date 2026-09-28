using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace BookTower.EditorTools
{
 public static partial class InteriorF0Builder
 {
  static Material PillarLeather()
  {
   const string folder="Assets/_BookTower/Textures/BookSpinePillar/";
   foreach(string suffix in new[]{"Base","Normal","Metallic"})
   {
    var imp=(TextureImporter)AssetImporter.GetAtPath(folder+"Leather_"+suffix+".png");
    imp.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
    imp.sRGBTexture=suffix=="Base";imp.wrapMode=TextureWrapMode.Repeat;imp.anisoLevel=8;imp.maxTextureSize=1024;imp.SaveAndReimport();
   }
   string path=DetailFolder+"/SculptedLeather.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Leather_Base.png"));
   mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Leather_Normal.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.9f);
   mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Leather_Metallic.png"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",.42f);EditorUtility.SetDirty(mat);return mat;
  }
  static Mesh TurnedProfile(List<Vector2> profile,float totalHeight)
  {
   const int count=96;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
   for(int j=0;j<profile.Count;j++)for(int i=0;i<=count;i++)
   {
    float a=i*Mathf.PI*2/count;float rad=profile[j].x*(1+.0025f*Mathf.Sin(a*9+profile[j].y*3));
    v.Add(V(Mathf.Cos(a)*rad,profile[j].y,Mathf.Sin(a)*rad));uv.Add(new Vector2(i/(float)count,profile[j].y/totalHeight));
    if(j<profile.Count-1&&i<count){int t=j*(count+1)+i;tri.AddRange(new[]{t,t+count+1,t+1,t+1,t+count+1,t+count+2});}
   }
   var m=new Mesh{name="Turned leather binding profile"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateTangents();return m;
  }
  static void SculptedBindingColumn(Vector3 position,float height,float radius)
  {
   var p=Group(root,"기준이미지_가죽제본기둥",position);var leather=PillarLeather();
   var profile=new List<Vector2>{new(0,0),new(radius*1.38f,0),new(radius*1.40f,.08f),new(radius*1.40f,.30f),new(radius*1.35f,.37f),new(radius*1.12f,.4f)};
   // Continuous turned surface: molded convex rings, not stacked open cylinders.
   float[] rings={.55f,.86f,2.95f,5.03f,8.02f,10.16f,11.70f,12.17f};
   for(float y=.41f;y<height;y+=.035f)
   {
    float r=radius;
    foreach(float ring in rings){float d=Mathf.Abs(y-ring);if(d<.16f)r+=.12f*Mathf.Sqrt(1-d*d/(.16f*.16f));}
    profile.Add(new Vector2(r,y));
   }
   profile.Add(new Vector2(radius,height));profile.Add(new Vector2(0,height));
   MeshObject(p,"가죽곡면_금박압인",TurnedProfile(profile,height),leather,Vector3.zero);
   var col=p.gameObject.AddComponent<CapsuleCollider>();col.radius=radius*1.4f;col.height=height;col.center=V(0,height/2,0);
   float belt=5.15f,beltHeight=1.0f;
   var beltProfile=new List<Vector2>{new(radius+.015f,belt),new(radius+.07f,belt),new(radius+.105f,belt+.06f),new(radius+.10f,belt+beltHeight-.06f),new(radius+.07f,belt+beltHeight),new(radius+.015f,belt+beltHeight)};
   MeshObject(p,"노화황동_제본띠",TurnedProfile(beltProfile,height),brass,Vector3.zero);
   for(int face=0;face<12;face++)
   {
    float a=face*Mathf.PI*2/12;
    for(int edge=0;edge<2;edge++)
    {
     Sphere(p,"황동고정리벳",V(Mathf.Cos(a)*(radius+.115f),belt+.12f+edge*.76f,Mathf.Sin(a)*(radius+.115f)),V(.072f,.072f,.072f),brass);
    }
    if(face%2==0)FlowerInlay(p,a,belt+.5f,radius+.112f);
   }
  }
  static void FlowerInlay(Transform parent,float angle,float y,float radius)
  {
   var vs=new List<Vector3>();var ts=new List<int>();
   Vector3 On(float x,float h)=>V(Mathf.Cos(angle+x/radius)*radius,y+h,Mathf.Sin(angle+x/radius)*radius);
   for(int petal=0;petal<4;petal++)
   {
    int offset=vs.Count;float a=petal*Mathf.PI/2;
    vs.Add(On(Mathf.Cos(a)*.15f,Mathf.Sin(a)*.15f));
    for(int j=0;j<=24;j++)
    {
     float t=j*Mathf.PI*2/24;
     float longAxis=.16f+.17f*Mathf.Cos(t),shortAxis=.105f*Mathf.Sin(t);
     vs.Add(On(Mathf.Cos(a)*longAxis-Mathf.Sin(a)*shortAxis,Mathf.Sin(a)*longAxis+Mathf.Cos(a)*shortAxis));
     if(j<24){ts.AddRange(new[]{offset,offset+j+1,offset+j+2,offset,offset+j+2,offset+j+1});}
    }
   }
   var m=new Mesh{name="Curved floral dark brass inlay"};m.SetVertices(vs);m.SetTriangles(ts,0);m.RecalculateNormals();MeshObject(parent,"황동띠_꽃상감",m,wood,Vector3.zero);
  }
 }
}
