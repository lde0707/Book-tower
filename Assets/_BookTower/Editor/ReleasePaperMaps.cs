using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 // Physical surface data authored independently from colour/printing. No colour-to-height conversion.
 public static class ReleasePaperMaps
 {
  public const string Folder="Assets/_BookTower/Generated/ReleaseMaterials/Textures";
  const int N=1024;
  public static void Generate()
  {
   Directory.CreateDirectory(Folder);
   foreach(string family in new[]{"Paper","Cover","Edge","Cotton","Brass","Foil"})Generate(family);
   PadAtlas("Spirit","Assets/_BookTower/Art/PaperHoodSpirit/Spirit_");
   PadAtlas("Moa","Assets/_BookTower/Art/MoaSculpt/Moa_");
   AssetDatabase.Refresh();
   foreach(string file in Directory.GetFiles(Folder,"*.png"))
   {var imp=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.Contains("Normal"),colour=file.Contains("Color");imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;imp.convertToNormalmap=false;imp.sRGBTexture=colour;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Repeat;imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=8;imp.maxTextureSize=file.Contains("Padded")?2048:1024;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.isReadable=false;imp.SaveAndReimport();}
  }
  static float Noise(float x,float y,int frequency,int seed)
  {
   float u=x*frequency,v=y*frequency;
   return Mathf.Lerp(Mathf.Lerp(Mathf.PerlinNoise(u+seed,v+seed),Mathf.PerlinNoise(u-frequency+seed,v+seed),x),Mathf.Lerp(Mathf.PerlinNoise(u+seed,v-frequency+seed),Mathf.PerlinNoise(u-frequency+seed,v-frequency+seed),x),y)-.5f;
  }
  static void Generate(string family)
  {
   int seed=family=="Paper"?31:family=="Cover"?53:family=="Edge"?79:family=="Cotton"?101:family=="Brass"?137:163;
   var height=new float[N*N];var stain=new float[N*N];var fibres=new float[N*N];var rng=new System.Random(seed);
   // Sparse, long pulp fibres. Rasterise periodically so the tile edges are continuous.
   for(int i=0;i<8500;i++)
   {float x=(float)rng.NextDouble()*N,y=(float)rng.NextDouble()*N,a=(float)rng.NextDouble()*Mathf.PI*2,length=4+(float)rng.NextDouble()*30;
    float amplitude=.08f+(float)rng.NextDouble()*.18f;for(int s=0;s<(int)length;s++){float t=s/length;int px=Mathf.RoundToInt(x+Mathf.Cos(a)*s),py=Mathf.RoundToInt(y+Mathf.Sin(a)*s+Mathf.Sin(t*Mathf.PI)*1.5f);fibres[((py+N*2)%N)*N+(px+N*2)%N]+=amplitude*Mathf.Sin(t*Mathf.PI);}}
   for(int y=0;y<N;y++)for(int x=0;x<N;x++)
   {int i=y*N+x;float u=x/(float)N,v=y/(float)N,cloud=Noise(u,v,4,seed),fine=Noise(u,v,110,seed+1),mid=Noise(u,v,24,seed+2);
    stain[i]=cloud*.04f+mid*.022f+fine*.015f;
    float h=fine*.04f+mid*.018f+fibres[i]*.14f;
    if(family=="Cover")h=h*.48f+Noise(u,v,55,seed)*.028f;
    if(family=="Edge")h=.08f*Mathf.Sin((v*85+Mathf.Sin(u*Mathf.PI*2)*.35f)*Mathf.PI*2)+fine*.02f;
    if(family=="Cotton")h=.065f*Mathf.Sin((u*48+v*3)*Mathf.PI*2)+fine*.025f;
    if(family=="Brass")h=Noise(u,v,75,seed)*.009f+.002f*Mathf.Sin(u*250*Mathf.PI*2);
    if(family=="Foil")h=mid*.28f+cloud*.18f+fine*.003f;
    height[i]=h;
   }
   var colours=new Color32[N*N];var normals=new Color32[N*N];var masks=new Color32[N*N];
   for(int y=0;y<N;y++)for(int x=0;x<N;x++)
   {int i=y*N+x;float dx=(height[y*N+(x+1)%N]-height[y*N+(x+N-1)%N])*1.5f,dy=(height[((y+1)%N)*N+x]-height[((y+N-1)%N)*N+x])*1.5f;Vector3 n=new Vector3(-dx,-dy,1).normalized;
    normals[i]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
    float c=.88f+stain[i]-(family=="Paper"?fibres[i]*.012f:0);colours[i]=new Color(c,c,c,1);
    float metal=0,smooth=.12f+stain[i];
    if(family=="Cover")smooth=.22f+stain[i]*2;
    if(family=="Edge"||family=="Cotton")smooth=.09f+stain[i];
    if(family=="Brass"){bool oxide=stain[i]<-.008f;metal=oxide?0:1;smooth=oxide?.20f:.42f+stain[i];colours[i]=new Color(c*(oxide?.65f:1),c*(oxide?.70f:1),c*(oxide?.68f:1),1);}
    if(family=="Foil"){metal=stain[i]<-.025f?0:1;smooth=.61f+stain[i]*2;}
    masks[i]=new Color(metal,1,0,Mathf.Clamp01(smooth));
   }
   Write(family+"_Color",N,colours);Write(family+"_Normal",N,normals);Write(family+"_Mask",N,masks);
  }
  static void Write(string name,int size,Color32[] pixels){var t=new Texture2D(size,size,TextureFormat.RGBA32,false,true);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());Object.DestroyImmediate(t);}
  static Color32[] Load(string path,out int size){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(path));size=t.width;var p=t.GetPixels32();Object.DestroyImmediate(t);return p;}
  // UV-bake gutter repair: extend existing island texels into unused black space. Painted texels stay byte-identical.
  static void PadAtlas(string name,string source)
  {
   var basePixels=Load(source+"BaseColor.png",out int size);int count=basePixels.Length;var owner=new int[count];var queue=new int[count];int head=0,tail=0;
   for(int i=0;i<count;i++){var c=basePixels[i];owner[i]=c.r+c.g+c.b>12?i:-1;if(owner[i]>=0)queue[tail++]=i;}
   int valid=tail;
   while(head<tail){int p=queue[head++],x=p%size,y=p/size;void Add(int q){if(owner[q]>=0)return;owner[q]=owner[p];queue[tail++]=q;}if(x>0)Add(p-1);if(x<size-1)Add(p+1);if(y>0)Add(p-size);if(y<size-1)Add(p+size);}
   foreach(string map in new[]{"BaseColor","Normal","MetallicSmoothness"}){var pixels=map=="BaseColor"?basePixels:Load(source+map+".png",out _);var padded=new Color32[count];for(int i=0;i<count;i++)padded[i]=pixels[owner[i]];Write(name+"_Padded_"+map,size,padded);}
   Directory.CreateDirectory(ReleaseMaterialAudit.Output);File.WriteAllText(ReleaseMaterialAudit.Output+"/"+name+"-atlas-padding.txt",$"Bake gutter extension: {count-valid} unpainted texels padded; {valid} painted texels preserved byte-for-byte. Mesh, UVs, rig unchanged.\n");
  }
 }
}
