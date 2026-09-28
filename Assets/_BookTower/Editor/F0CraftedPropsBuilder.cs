using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class InteriorF0Builder
 {
  const string PropsFolder="Assets/_BookTower/Generated/F0CraftedProps";
  static Transform propsRoot;
  [MenuItem("Book Tower/F0 개별 공예 소품 제작")]
  public static void ApplyCraftedProps()
  {
   // Rebuild the earlier additive detail pass without its simplified desk/cart.
   ApplyReferenceDetails();EnsureFolder(PropsFolder);
   var arch=room.transform.Find(RootName);var old=arch.Find("F0_개별공예소품");if(old)Object.DestroyImmediate(old.gameObject);
   propsRoot=Group(arch,"F0_개별공예소품",Vector3.zero);
   MakeProp("01_BindingWorkbench",CraftDesk,V(-10,3.05f,-7),Vector3.zero,1);
   MakeProp("02_OpenClueBook",CraftOpenBook,V(-10,4.64f,-6.9f),V(0,-6,0),1);
   MakeProp("03_InkwellAndQuill",CraftInkwell,V(-8.05f,4.64f,-7),Vector3.zero,1);
   MakeProp("04_PaperLantern",CraftLantern,V(-12.12f,4.64f,-7.15f),Vector3.zero,.55f);
   MakeProp("05_BoundBookStack",CraftStack,V(-12.7f,3.12f,-9.6f),V(0,7,0),1);
   MakeProp("06_WheeledArchive",CraftCart,V(12.5f,3.08f,8.6f),V(0,-6,0),1);
   EditorSceneManager.MarkSceneDirty(room.gameObject.scene);EditorSceneManager.SaveScene(room.gameObject.scene);AssetDatabase.SaveAssets();
   F0AtmosphereBuilder.Apply();CaptureCraftedPropGallery();
  }
  static void MakeProp(string name,Action<Transform> build,Vector3 at,Vector3 angles,float scale)
  {
   var p=Group(null,name,Vector3.zero);build(p);
   foreach(var leaf in p.GetComponentsInChildren<Transform>().Where(t=>t.name=="압인잎"))leaf.localScale=new Vector3(1,1,.15f);
   CombineDecorations(p);
   string folder=PropsFolder+"/"+name;EnsureFolder(folder);int index=0;
   foreach(var f in p.GetComponentsInChildren<MeshFilter>())
   {
    var m=f.sharedMesh;if(!m||AssetDatabase.Contains(m))continue;
    string path=folder+"/Mesh_"+(index++).ToString("000")+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved){saved.Clear();saved.indexFormat=m.indexFormat;saved.vertices=m.vertices;saved.normals=m.normals;saved.tangents=m.tangents;saved.uv=m.uv;saved.triangles=m.triangles;saved.bounds=m.bounds;saved.UploadMeshData(false);f.sharedMesh=saved;Object.DestroyImmediate(m);EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(m,path);
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(p.gameObject,folder+"/"+name+".prefab");Object.DestroyImmediate(p.gameObject);
   var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,propsRoot);placed.transform.localPosition=at;placed.transform.localEulerAngles=angles;placed.transform.localScale=Vector3.one*scale;
  }
  static void CraftDesk(Transform p)
  {
   Box(p,"두꺼운제본상판",V(0,1.42f,0),V(5.8f,.25f,3.2f),oxblood,true,.09f);
   Box(p,"겹친표지아랫단",V(0,1.24f,0),V(5.96f,.12f,3.3f),wood,false,.055f);
   for(int i=0;i<6;i++)Box(p,"표지사이종이단면",V(0,1.28f+i*.021f,-1.6f),V(5.6f,.013f,.05f),edge);
   foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
   {
    var leg=Box(p,"닳은제본다리",V(x*2.5f,.62f,z*1.25f),V(.30f,1.24f,.32f),wood,true,.08f);leg.localEulerAngles=V(z*3,0,x*-3);
    Box(p,"황동다리캡",V(x*2.52f,.12f,z*1.26f),V(.34f,.20f,.36f),brass);
    Box(p,"상판모서리보강판",V(x*2.57f,1.565f,z*1.29f),V(.54f,.035f,.5f),brass);
    foreach(float d in new[]{-.15f,.15f})Sphere(p,"보강판리벳",V(x*2.57f+d,1.59f,z*1.29f),V(.055f,.032f,.055f),wood);
   }
   Box(p,"하부가로지지대",V(0,.40f,.9f),V(5.1f,.2f,.23f),wood,true);
   for(int i=-1;i<=1;i++)
   {
    Box(p,"손때묻은서랍",V(i*1.8f,1.02f,-1.40f),V(1.70f,.38f,.28f),umber);
    Box(p,"서랍금속받침",V(i*1.8f,1.02f,-1.56f),V(.45f,.20f,.03f),brass);
    Tube(p,"굽은황동손잡이",new[]{V(i*1.8f-.17f,1.04f,-1.59f),V(i*1.8f-.14f,.95f,-1.71f),V(i*1.8f+.14f,.95f,-1.71f),V(i*1.8f+.17f,1.04f,-1.59f)},.028f,brass);
   }
   // Irregular cotton stitching follows the tabletop edge, outside the work surface.
   for(int i=0;i<32;i++)Tube(p,"상판제본실",new[]{V(-2.72f+i*.175f,1.48f,-1.615f),V(-2.69f+i*.175f,1.39f,-1.637f)},.012f,rope);
  }
  static Vector3 PagePoint(float u,float v,int side,float layer=0)
  {
   return V(side*(.035f+u*1.43f),.08f+layer+.15f*Mathf.Sin(u*Mathf.PI)+.085f*Mathf.Pow(u,5)+.009f*Mathf.Sin(v*19+u*9), (v-.5f)*2.0f);
  }
  static Mesh CurledPage(int side,float layer)
  {
   const int nx=24,ny=24;var vs=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){var point=PagePoint(x/(float)nx,y/(float)ny,side,layer);if(x==nx)point.x+=side*.008f*Mathf.Sin(y*2.6f);vs.Add(point);uv.Add(new Vector2(x/(float)nx,y/(float)ny));}
   int surface=vs.Count;for(int i=0;i<surface;i++){vs.Add(vs[i]-Vector3.up*.006f);uv.Add(uv[i]);}
   for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
   {
    int a=y*(nx+1)+x;int[] face=side>0?new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2}:new[]{a,a+1,a+nx+1,a+1,a+nx+2,a+nx+1};
    tris.AddRange(face);for(int k=0;k<6;k+=3)tris.AddRange(new[]{face[k]+surface,face[k+2]+surface,face[k+1]+surface});
   }
   for(int i=0;i<nx;i++)foreach(int start in new[]{i,ny*(nx+1)+i})tris.AddRange(new[]{start,start+1,start+surface,start+1,start+surface+1,start+surface});
   for(int i=0;i<ny;i++)foreach(int start in new[]{i*(nx+1),i*(nx+1)+nx})tris.AddRange(new[]{start,start+surface,start+nx+1,start+surface,start+surface+nx+1,start+nx+1});
   var m=new Mesh{name="Curled deckle book page"};m.SetVertices(vs);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateTangents();return m;
  }
  static void PageStroke(Transform p,int side,params Vector2[] line)
  {
   var points=new List<Vector3>();
   for(int segment=0;segment<line.Length-1;segment++)for(int i=0;i<24;i++){var q=Vector2.Lerp(line[segment],line[segment+1],i/24f);points.Add(PagePoint(q.x,q.y,side,.102f));}
   var end=line[line.Length-1];points.Add(PagePoint(end.x,end.y,side,.102f));Tube(p,"페이지압인선",points,.008f,ink);
  }
  static void CraftOpenBook(Transform p)
  {
   Box(p,"독서대받침",V(0,.035f,0),V(2.8f,.07f,1.70f),wood);
   foreach(int side in new[]{-1,1})Box(p,"책받침후면지지대",V(side*1.05f,.46f,.60f),V(.10f,.86f,.14f),brass);
   var opened=Group(p,"기울어진펼친책",V(0,.58f,0));opened.localEulerAngles=V(-28,0,0);p=opened;
   foreach(int side in new[]{-1,1})
   {
    Box(p,"가죽책표지",V(side*.76f,.025f,0),V(1.52f,.09f,2.18f),oxblood,false,.04f);
    for(int i=0;i<8;i++)MeshObject(p,"휘어진장지_"+i,CurledPage(side,i*.012f),i==7?ivory:paper,Vector3.zero);
    PageStroke(p,side,new(.10f,.09f),new(.91f,.09f),new(.91f,.91f),new(.10f,.91f));
    for(int k=0;k<4;k++)PageStroke(p,side,new(.15f,.18f+k*.024f),new(.40f+(k%2)*.18f,.18f+k*.024f));
   }
   Tube(p,"책중앙제본끈",new[]{V(0,.09f,-1.12f),V(0,.15f,0),V(0,.11f,1.1f)},.032f,rope);
   // Bridge and bent-light diagram: environmental illustration, no new interaction flag.
   PageStroke(p,-1,new(.20f,.50f),new(.78f,.50f));PageStroke(p,-1,new(.22f,.46f),new(.22f,.68f));PageStroke(p,-1,new(.76f,.46f),new(.76f,.68f));
   PageStroke(p,-1,new(.29f,.78f),new(.29f,.62f),new(.72f,.62f));PageStroke(p,-1,new(.61f,.67f),new(.72f,.62f),new(.61f,.57f));
   var leaf=Group(p,"페이지속잎그림",PagePoint(.5f,.43f,1,.105f));leaf.localRotation=Quaternion.Euler(90,0,0);Sprig(leaf,Vector3.zero,.65f,ink);
   var ribbon=MeshObject(p,"늘어진책갈피",F0Mesh.Paper(.15f,.75f,192),ink,V(.12f,.04f,-1.02f));ribbon.localEulerAngles=V(165,0,-8);
  }
  static void CraftInkwell(Transform p)
  {
   var profile=new List<Vector2>{new(0,0),new(.27f,0),new(.31f,.06f),new(.32f,.23f),new(.27f,.36f),new(.17f,.40f),new(.16f,.54f),new(.22f,.56f),new(.22f,.61f),new(.14f,.61f),new(.14f,.49f)};
   MeshObject(p,"둥근잉크병",TurnedProfile(profile,1),umber,Vector3.zero);
   MeshObject(p,"잉크병입구금속테",TurnedProfile(new List<Vector2>{new(.22f,.54f),new(.23f,.56f),new(.23f,.62f),new(.18f,.64f)},1),brass,Vector3.zero);
   Box(p,"종이라벨",V(0,.22f,-.315f),V(.30f,.23f,.018f),paper);Sprig(p,V(0,.13f,-.33f),.18f,ink);
   var pen=Group(p,"깃펜",V(.04f,.48f,0));pen.localEulerAngles=V(-14,0,-24);
   Tube(pen,"깃펜심",new[]{V(0,0,0),V(0,.6f,.02f),V(.035f,1.3f,.06f)},.013f,brass);
   MeshObject(pen,"접힌깃펜종이",F0Mesh.Leaf(.91f,.35f),ivory,V(0,.41f,0));
   for(int i=0;i<11;i++){float y=.5f+i*.06f,w=.15f*Mathf.Sin((y-.4f)/.91f*Mathf.PI);Tube(pen,"깃펜가느다란결",new[]{V(-w,y+.04f,-.025f),V(0,y,-.055f),V(w,y+.04f,-.025f)},.004f,edge);}
  }
  static void CraftLantern(Transform p)
  {
   const float radius=.55f;int n=6;
   for(int i=0;i<n;i++)
   {
    float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
    Vector3 v=V(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),w=V(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
    var pane=Group(p,"육각등의종이면",(v+w)*.5f+V(0,.28f,0));pane.localRotation=Quaternion.LookRotation(-(v+w).normalized);
    MeshObject(pane,"섬유종이",F0Mesh.Paper((v-w).magnitude,1.1f,29+i),glow,Vector3.zero);
    Sprig(pane,V(0,.12f,-.045f),.72f,ink);
    Tube(p,"육각등살",new[]{v+V(0,.16f,0),v+V(0,1.45f,0),V(0,1.77f,0)},.035f,brass);
    foreach(float y in new[]{.20f,1.40f})Tube(p,"등테두리",new[]{v+V(0,y,0),w+V(0,y,0)},.044f,wood);
   }
   MeshObject(p,"등받침",TurnedProfile(new List<Vector2>{new(0,0),new(.60f,0),new(.60f,.08f),new(.48f,.15f),new(0,.15f)},2),brass,Vector3.zero);
   var loop=new List<Vector3>();for(int i=0;i<=24;i++){float t=i*Mathf.PI*2/24;loop.Add(V(Mathf.Cos(t)*.13f,1.88f+Mathf.Sin(t)*.13f,0));}Tube(p,"등고리",loop,.022f,brass);
   var light=p.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=2.3f;light.color=new Color(1,.70f,.40f);light.shadows=LightShadows.None;
  }
  static void CraftStack(Transform p)
  {
   for(int i=0;i<4;i++)
   {
    var b=Group(p,"손제본책_"+i,V(Mathf.Sin(i)*.09f,i*.29f,0));b.localEulerAngles=V(0,i%2==0?-6:5,0);
    FlatBook(b,Vector3.zero,2.6f-i*.15f,.24f,1.8f-i*.08f,i%2==0?oxblood:ochre,0);
    for(int layer=0;layer<12;layer++)Box(b,"불규칙종이단면",V(Mathf.Sin(layer*3)*.015f,.04f+layer*.016f,-.88f+i*.04f),V(2.34f-i*.15f,.006f,.06f),paper);
    var mark=Group(b,"표지잎금박",V(.2f,.34f,-.4f));mark.localEulerAngles=V(90,0,0);Sprig(mark,Vector3.zero,.65f,brass);
   }
  }
  static void CraftCart(Transform p)
  {
   foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
   {
    var wheel=Group(p,"바퀴",V(x*1.43f,.28f,z*.68f));wheel.localEulerAngles=V(90,0,0);
    MeshObject(wheel,"둥근목재바퀴",TurnedProfile(new List<Vector2>{new(0,-.08f),new(.25f,-.08f),new(.28f,-.04f),new(.28f,.04f),new(.25f,.08f),new(0,.08f)},.3f),wood,Vector3.zero);
    Sphere(p,"바퀴황동축",V(x*1.43f,.28f,z*.79f),V(.12f,.12f,.07f),brass);
    Box(p,"선반세로기둥",V(x*1.5f,1.43f,z*.67f),V(.17f,2.35f,.18f),wood);
   }
   foreach(float y in new[]{.53f,1.62f,2.68f})
   {
    Box(p,"가죽선반판",V(0,y,0),V(3.55f,.14f,1.75f),oxblood,true,.055f);
    for(int x=-1;x<=1;x+=2){Box(p,"황동모서리",V(x*1.48f,y,-.9f),V(.48f,.24f,.08f),brass);Sphere(p,"선반리벳",V(x*1.48f,y,-.951f),V(.066f,.066f,.04f),wood);}
   }
   for(int i=0;i<4;i++)FlatBook(p,V(-.60f,.62f+i*.20f,0),1.7f,.17f,1.32f,i%2==0?umber:ochre,i%2==0?4:-3);
   for(int i=0;i<3;i++)FlatBook(p,V(.75f,1.70f+i*.22f,0),1.15f,.18f,1.34f,oxblood,-3+i*3);
   var plaque=Group(p,"토끼표지",V(0,2.8f,.55f));Panel(plaque,2.8f,1.9f);
   Sphere(plaque,"토끼몸통",V(0,.68f,-.33f),V(.70f,.67f,.035f),ink);Sphere(plaque,"토끼머리",V(.25f,1.10f,-.33f),V(.47f,.42f,.035f),ink);
   for(int i=0;i<2;i++)Sphere(plaque,"토끼귀",V(.16f+i*.24f,1.43f,-.33f),V(.14f,.52f,.035f),ink);
   Sphere(plaque,"토끼눈",V(.35f,1.14f,-.36f),V(.045f,.055f,.015f),ivory);
   Tube(p,"책수레손잡이",new[]{V(-1.48f,2.4f,.7f),V(-1.48f,3.02f,.7f),V(1.48f,3.02f,.7f),V(1.48f,2.4f,.7f)},.047f,brass);
  }
  static void CaptureCraftedPropGallery()
  {
   string output="Docs/ArtReview/F0CraftedProps";Directory.CreateDirectory(output);
   var report=new System.Text.StringBuilder("# F0 individual prop checks\n");
   var setup=new GameObject("TemporaryPropReview");setup.transform.position=V(0,1000,0);
   var cameraObject=new GameObject("PropCamera");cameraObject.transform.SetParent(setup.transform,false);var cam=cameraObject.AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.22f,.20f,.18f);cam.fieldOfView=40;cam.nearClipPlane=.05f;cam.farClipPlane=100;
   var lightObj=new GameObject("StudioKey");lightObj.transform.SetParent(setup.transform,false);lightObj.transform.localEulerAngles=V(45,-25,0);var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.color=new Color(1,.92f,.82f);light.cullingMask=1<<30;light.shadows=LightShadows.None;
   var fillObj=new GameObject("StudioFill");fillObj.transform.SetParent(setup.transform,false);fillObj.transform.localPosition=V(3,4,-4);var fill=fillObj.AddComponent<Light>();fill.type=LightType.Point;fill.intensity=45;fill.range=25;fill.color=new Color(.8f,.87f,1);fill.cullingMask=1<<30;
   try
   {
    foreach(string path in Directory.GetFiles(PropsFolder,"*.prefab",SearchOption.AllDirectories).OrderBy(s=>s))
    {
     var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\','/'));var model=Object.Instantiate(prefab,setup.transform);model.transform.localPosition=Vector3.zero;
     foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
     foreach(var l in model.GetComponentsInChildren<Light>())l.cullingMask=1<<30;
     var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Empty prop: "+path);
     var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);if(r.sharedMaterial==null)throw new Exception("Missing material "+path);}
     int tris=model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
     cam.transform.position=bounds.center+V(1,.60f,-1.65f).normalized*(bounds.extents.magnitude/Mathf.Sin(20*Mathf.Deg2Rad)*1.12f);cam.transform.LookAt(bounds.center);
     Capture(cam,output+"/"+prefab.name+".png");report.AppendLine($"PASS {prefab.name}: {tris} triangles, {renderers.Length} renderers, bounds {bounds.size}");Object.DestroyImmediate(model);
    }
   }
   finally{Object.DestroyImmediate(setup);}
   File.WriteAllText(output+"/asset-checks.txt",report.ToString());
  }
  public static void RunCraftedPropsBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ApplyCraftedProps();F0PlayVerifier.Run();}
 }
}
