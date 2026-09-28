using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class ReadingRoomBuilder
 {
  public const string Dir="Assets/_BookTower/Generated/ReadingRoom1B", Out="Docs/ArtReview/ReadingRoom1B";
  static Material paper,edge,navy,wine,gold,ink,glow,blue;static Material[] covers;
  static int meshId,seed;static readonly Dictionary<string,Mesh> cache=new();
  static readonly List<Transform> modules=new();
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  static Transform Group(Transform p,string n,Vector3 pos){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=pos;return t;}
  static Mesh SaveMesh(Mesh m)
  {foreach(var v in m.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.sqrMagnitude))throw new Exception("Non-finite generated mesh: "+m.name+" / "+meshId);string folder=Dir+"/Meshes/Pass"+pass;Directory.CreateDirectory(folder);string path=folder+"/M"+(meshId++).ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;old.normals=m.normals;old.tangents=m.tangents;old.uv=m.uv;old.triangles=m.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,path);return m;}
  static Transform MeshAt(Transform p,string n,Mesh m,Material mat,Vector3 at)
  {var t=Group(p,n,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=m;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.TwoSided;return t;}
  static Transform Box(Transform p,string n,Vector3 at,Vector3 size,Material m,bool solid=false)
  {string key="box"+size.ToString("F3");if(!cache.TryGetValue(key,out var mesh)){mesh=SaveMesh(F0Mesh.RoundedBox(size,.10f));cache[key]=mesh;}var t=MeshAt(p,n,mesh,m,at);if(solid)t.gameObject.AddComponent<BoxCollider>().size=size;return t;}
  static Transform Tube(Transform p,string n,IList<Vector3> path,float radius,Material m)
  =>MeshAt(p,n,SaveMesh(F0Mesh.Tube(path,radius,8)),m,Vector3.zero);
  static Transform Ring(Transform p,string n,Vector3 at,float radius,float width,Material m)
  {var pts=new List<Vector3>();for(int i=0;i<=40;i++){float a=i*Mathf.PI/20;pts.Add(V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}var t=Tube(p,n,pts,width,m);t.localPosition=at;return t;}
  static Transform Sheet(Transform p,string n,Vector3 at,float w,float h,Material mat,Vector3 rot)
  {var t=MeshAt(p,n,SaveMesh(F0Mesh.Paper(w,h,seed++)),mat,at);t.localEulerAngles=rot;return t;}
  static Material Mat(string n,Color c,float metallic=0,float emission=0)
  {
   string path=Dir+"/Materials/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",metallic>0?.27f:.06f);m.SetFloat("_Metallic",metallic);m.SetFloat("_Cull",0);
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png"));
   m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiberNormal.png"));m.SetFloat("_BumpScale",metallic>0?.12f:.42f);m.EnableKeyword("_NORMALMAP");
   if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*emission);}else m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;
  }
  static void Materials()
  {
   paper=Mat("Ivory_Mulberry",new Color(.78f,.69f,.52f));edge=Mat("Page_Edges",new Color(.53f,.41f,.27f));navy=Mat("Indigo_Bookboard",new Color(.115f,.14f,.20f));wine=Mat("Burgundy_Paper",new Color(.27f,.065f,.09f));gold=Mat("Antique_Gold",new Color(.55f,.32f,.105f),.58f);ink=Mat("Raven_Ink",new Color(.045f,.035f,.045f));glow=Mat("Lantern_Paper",new Color(1,.68f,.24f),0,1.4f);blue=Mat("Moon_Window",new Color(.25f,.48f,.82f),0,1.5f);
   covers=new[]{navy,wine,Mat("Ochre_Binding",new Color(.35f,.23f,.12f)),Mat("Mauve_Binding",new Color(.22f,.13f,.22f))};
  }
  static void Leaf(Transform p,Vector3 at,float size,float angle,Material m)
  {string key="leaf"+size;if(!cache.TryGetValue(key,out var mesh)){
    if(pass>=4){var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();for(int i=0;i<=12;i++){float t=i/12f,y=t*size,w=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.85f)*size*.17f,bend=t*t*size*.09f;v.Add(V(bend-w,y,-.008f));v.Add(V(bend,y,-Mathf.Sin(t*Mathf.PI)*size*.035f));v.Add(V(bend+w,y,-.008f));uv.Add(new Vector2(0,t));uv.Add(new Vector2(.5f,t));uv.Add(new Vector2(1,t));if(i<12){int a=i*3;tri.AddRange(new[]{a,a+3,a+1,a+1,a+3,a+4,a+1,a+4,a+2,a+2,a+4,a+5});}}mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh);}else mesh=SaveMesh(F0Mesh.Leaf(size,size*.45f));cache[key]=mesh;}var leaf=MeshAt(p,"pressed leaf",mesh,m,at);leaf.localEulerAngles=V(0,0,angle);}
  static void Sprig(Transform p,Vector3 at,float size,Material mat)
  {var g=Group(p,"embossed botanical branch",at);Tube(g,"stem",new[]{V(0,0,0),V(.04f,size*.5f,0),V(0,size,0)},.018f,mat);for(int i=0;i<4;i++){Leaf(g,V(.015f,size*(.12f+i*.2f),-.015f),size*.25f,-55,mat);Leaf(g,V(-.015f,size*(.2f+i*.2f),-.015f),size*.25f,55,mat);}}
  static void Bird(Transform p,Vector3 at,float size,Material mat)
  {
   var g=Group(p,"raven paper inlay",at);g.localScale=Vector3.one*size;
   Vector2[] pts={new(-.55f,-.1f),new(-.95f,-.7f),new(-.28f,-.4f),new(.2f,-.35f),new(.45f,-.05f),new(.42f,.43f),new(.65f,.52f),new(.38f,.58f),new(.27f,.8f),new(.05f,.82f),new(-.07f,.64f),new(-.05f,.3f),new(-.35f,.2f)};
   var v=new List<Vector3>{V(0,0,0)};v.AddRange(pts.Select(x=>V(x.x,x.y,0)));var tri=new List<int>();for(int i=0;i<pts.Length;i++)tri.AddRange(new[]{0,i+1,(i+1)%pts.Length+1});
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.SetUVs(0,v.Select(x=>new Vector2(x.x,x.y)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();MeshAt(g,"cut silhouette",SaveMesh(mesh),mat,Vector3.zero);
   foreach(float x in new[]{-.12f,.17f})Tube(g,"raven feet",new[]{V(x,-.28f,0),V(x,-.65f,0),V(x+.16f,-.7f,0)},.025f,mat);
  }
  static void Book(Transform p,Vector3 at,float width,float height,float depth,int variant,float lean=0)
  {
   var g=Group(p,"rounded stitched book",at);g.localEulerAngles=V(0,0,lean);var mat=covers[variant%covers.Length];
   Box(g,"page block",V(0,height*.5f,0),V(width*.86f,height-.12f,depth*.9f),paper);
   foreach(float x in new[]{-width*.5f,width*.5f})Box(g,"pressed cover",V(x,height*.5f,0),V(.065f,height,depth),mat);
   string key="spine"+width.ToString("F2")+height.ToString("F2");if(!cache.TryGetValue(key,out var mesh)){mesh=SaveMesh(F0Mesh.Spine(width,height,.14f));cache[key]=mesh;}
   MeshAt(g,"convex cloth spine",mesh,mat,V(0,0,-depth*.5f));
   foreach(float y in new[]{.16f,height*.23f,height*.76f,height-.16f})Box(g,"gilt raised band",V(0,y,-depth*.5f-.105f),V(width*.98f,.033f,.06f),gold);
   if(variant%3==0)Sprig(g,V(0,height*.35f,-depth*.5f-.16f),height*.28f,gold);
  }
  static void Merge(Transform g)
  {
   var filters=g.GetComponentsInChildren<MeshFilter>();
   foreach(var set in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
   {var combined=new Mesh{indexFormat=IndexFormat.UInt32,name=g.name+" "+set.Key.name};combined.CombineMeshes(set.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=g.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);MeshAt(g,"merged "+set.Key.name,SaveMesh(combined),set.Key,Vector3.zero);}
   foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
  }
  static Transform Shelf(Transform p,Vector3 at,float yaw)
  {
   var g=Group(p,"01_TallBookcase",at);g.localEulerAngles=V(0,yaw,0);
   Box(g,"back board",V(0,6,.8f),V(5,12,.3f),navy,true);
   foreach(float x in new[]{-2.5f,2.5f}){Box(g,"carved stile",V(x,6,0),V(.32f,12.3f,1.85f),navy,true);Box(g,"gold edge",V(x,6,-.95f),V(.035f,11.6f,.035f),gold);}
   for(int row=0;row<6;row++)
   {float y=.25f+row*1.95f;Box(g,"bowed shelf",V(0,y,0),V(5.3f,.20f,1.9f),navy);float x=-2.15f;for(int j=0;j<9;j++){float w=.32f+(j%3)*.06f,h=1.35f+((row+j)%4)*.10f;Book(g,V(x,y+.12f,-.12f),w,h,1.3f,(row+j)%4,j%5==0?-6:0);x+=w+.085f;}}
   for(int j=0;j<3;j++)Box(g,"layered cornice",V(0,12.1f+j*.16f,0),V(5.6f+j*.18f,.15f,2+j*.1f),j==1?gold:navy);
   Merge(g);return g;
  }
  static Transform Desk(Transform p,Vector3 at,float yaw)
  {
   var g=Group(p,"02_ArchedWritingDesk",at);g.localEulerAngles=V(0,yaw,0);
   foreach(float x in new[]{-3.8f,3.8f})
   {
    Box(g,"drawer pedestal",V(x,2.6f,0),V(2.8f,5.2f,3.8f),navy,true);
    for(int i=0;i<5;i++){Box(g,"inset drawer",V(x,.55f+i*.99f,-1.95f),V(2.52f,.85f,.14f),wine);Ring(g,"drawer pull",V(x,.55f+i*.99f,-2.08f),.13f,.035f,gold);foreach(float side in new[]{-1.13f,1.13f})Sprig(g,V(x+side,.23f+i*.99f,-2.05f),.52f,gold);}
    Box(g,"plinth",V(x,.13f,0),V(3.05f,.25f,4),gold);
   }
   Box(g,"thick bookboard top",V(0,5.45f,0),V(11,.46f,4.4f),navy,true);
   for(int i=0;i<3;i++)Box(g,"layered paper edge",V(0,5.18f+i*.06f,-2.23f),V(10.9f,.025f,.04f),edge);
   var arc=new List<Vector3>();for(int i=0;i<=30;i++){float a=Mathf.PI*i/30;arc.Add(V(Mathf.Cos(a)*2.35f,2.4f+Mathf.Sin(a)*2.6f,-1.91f));}Tube(g,"walkable arched opening",arc,.18f,gold);
   foreach(float x in new[]{-2.3f,2.3f})Box(g,"arch pier",V(x,1.2f,-1.9f),V(.25f,2.4f,.32f),gold);
   for(int i=0;i<7;i++){var b=Group(g,"stack on desk",V(-3.8f+i*1.13f,5.8f,.1f));b.localEulerAngles=V(0,0,90);Book(b,Vector3.zero,.32f,1.5f,1.15f,i%4);}
   Merge(g);return g;
  }
  static Transform Lantern(Transform p,Vector3 at,bool interactive=false)
  {
   var g=Group(p,interactive?"03_MemoryLantern":"paper lantern",at);
   Box(g,"brass base",V(0,.13f,0),V(1.1f,.25f,1.1f),gold);
   for(int i=0;i<6;i++)
   {float a=i*60*Mathf.Deg2Rad;var rib=Group(g,"arched brass rib",Vector3.zero);var path=new[]{V(Mathf.Cos(a)*.48f,.25f,Mathf.Sin(a)*.48f),V(Mathf.Cos(a)*.48f,1.3f,Mathf.Sin(a)*.48f),V(0,1.85f,0)};Tube(rib,"rib",path,.038f,gold);
    var pane=Sheet(g,"translucent raven paper",V(Mathf.Sin(a)*.42f,.25f,Mathf.Cos(a)*.42f),.46f,1.05f,glow,V(0,i*60+180,0));Bird(pane,V(0,.48f,-.04f),.29f,ink);}
   Ring(g,"top handle",V(0,1.96f,0),.17f,.03f,gold);
   Ring(g,"left locking ring",V(-.64f,.85f,0),.22f,.04f,gold);Ring(g,"right locking ring",V(.64f,.85f,0),.22f,.04f,gold);
   Box(g,"charge ribbon",V(0,.32f,-.55f),V(.8f,.12f,.045f),glow);
   return g;
  }
  static Transform Chandelier(Transform p,Vector3 at)
  {
   var g=Group(p,"05_PaperChandelier",at);var ring=Ring(g,"chandelier crown",Vector3.zero,1.5f,.09f,gold);ring.localEulerAngles=V(90,0,0);
   for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var c=V(Mathf.Cos(a)*1.5f,0,Mathf.Sin(a)*1.5f);Tube(g,"suspension chain",new[]{c,V(0,3,0)},.03f,gold);Box(g,"rolled paper candle",c+V(0,.4f,0),V(.18f,.8f,.18f),paper);var flame=MeshAt(g,"folded warm flame",SaveMesh(F0Mesh.Leaf(.42f,.16f)),glow,c+V(0,.8f,0));var strip=Sheet(g,"hanging torn manuscript",c+V(0,-1.5f,0),.4f,1.2f,paper,V(0,-i*60,0));Sprig(strip,V(0,.15f,-.06f),.7f,ink);}
   Merge(g);Point(g,V(0,.7f,0),pass>=2?35:8,22,new Color(1,.65f,.29f));return g;
  }
  static Transform Banner(Transform p,Vector3 at,float yaw)
  {
   var g=Group(p,"06_RavenBanner",at);g.localEulerAngles=V(0,yaw,0);
   Sheet(g,"deckled ivory border",V(0,0,.03f),2.55f,7,paper,Vector3.zero);if(pass>=2)Print(g,"raven woodblock paper",V(0,.18f,-.08f),2.22f,6.7f,bannerPrint,Vector3.zero,true);else Sheet(g,"burgundy printed paper",V(0,.18f,-.04f),2.22f,6.7f,wine,Vector3.zero);
   foreach(float x in new[]{-1.03f,1.03f})Tube(g,"stitched gold border",new[]{V(x,.35f,-.13f),V(x,6.7f,-.13f)},.028f,gold);
   if(pass<2){Bird(g,V(0,4.75f,-.18f),1.25f,paper);Sprig(g,V(0,1.2f,-.17f),2,gold);}Tube(g,"hanging rod",new[]{V(-1.5f,7.1f,0),V(1.5f,7.1f,0)},.07f,gold);Merge(g);return g;
  }
  static Light Point(Transform p,Vector3 at,float intensity,float range,Color color)
  {var g=Group(p,"warm paper light",at);var l=g.gameObject.AddComponent<Light>();l.type=LightType.Point;l.intensity=intensity;l.range=range;l.color=color;l.shadows=LightShadows.None;return l;}
  static void Rope(Transform p,Vector3 a,Vector3 b,float radius=.06f)
  {var direction=(b-a).normalized;var u=Vector3.Cross(direction,Vector3.right).normalized;if(u.sqrMagnitude<.1f)u=Vector3.up;var v=Vector3.Cross(direction,u);int count=Mathf.CeilToInt(Vector3.Distance(a,b)*10);for(int s=0;s<3;s++){var pts=new List<Vector3>();for(int i=0;i<=count;i++){float t=i/(float)count,ang=t*Vector3.Distance(a,b)*8+s*Mathf.PI*2/3;pts.Add(Vector3.Lerp(a,b,t)+radius*.5f*(u*Mathf.Cos(ang)+v*Mathf.Sin(ang)));}Tube(p,"twisted cotton strand",pts,radius*.45f,paper);}}
 }
}
