using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class InteriorF0Builder
 {
  const string DetailFolder="Assets/_BookTower/Generated/F0ReferenceDetail";
  const string DetailRoot="레퍼런스_입체제본장식";
  // Additive art pass: never regenerate puzzle objects or replace the room shell.
  [MenuItem("Book Tower/F0 레퍼런스 벽면 디테일 적용")]
  public static void ApplyReferenceDetails()
  {
   if(EditorApplication.isPlaying)throw new Exception("Exit play mode before applying art");
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame first");
   Directory.CreateDirectory("Logs/F0DetailBackups");
   EditorSceneManager.SaveScene(scene,"Logs/F0DetailBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
   var architecture=room.transform.Find(RootName);
   var old=architecture.Find(DetailRoot);if(old)Object.DestroyImmediate(old.gameObject);
   root=Group(architecture,DetailRoot,Vector3.zero);meshes.Clear();EnsureFolder(DetailFolder);
   Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+n+".mat");
   paper=Load("WarmPaper");ivory=Load("IvoryPaper");edge=Load("LayeredPages");oxblood=Load("OxbloodBook");umber=Load("UmberBook");ochre=Load("OchreBook");rope=Load("CottonRope");brass=Load("AgedBrass");ink=Load("TerracottaInk");wood=Load("DarkBinding");glow=Load("LanternPaper");
   foreach(int side in new[]{-1,1})
   {
    // Dense recessed shelving sits between existing windows, not in front of them.
    foreach(float z in new[]{-17f,0f,14f})
    {
     var bay=Group(root,"벽면_입체서가",V(side*15.7f,3,z));bay.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
     ShelfBay(bay,3.1f,11.9f);
    }
    // Raised wainscot follows the actual gameplay bank height.
    foreach(float z in new[]{-10f,-5.2f,5.4f,10f})
    {
     var panel=Group(root,"겹장지_벽감",V(side*15.55f,3.1f,z));panel.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
     Panel(panel,3.3f,2.4f);
     Box(panel,"두꺼운 창턱",V(0,2.55f,-.27f),V(3.7f,.22f,.7f),wood);
     for(int i=0;i<3;i++)FlatBook(panel,V(-1.1f+i*.85f,2.69f,-.25f),.72f,.12f,.48f,i%2==0?ochre:oxblood,i*3);
    }
    var cornice=Group(root,"벽천장_적층처마",V(side*15.55f,14.9f,-2));
    for(int j=0;j<4;j++)Box(cornice,"겹친처마",V(-side*j*.12f,j*.19f,0),V(.5f+j*.12f,.15f,35.5f),j==2?brass:wood);
    foreach(float z in new[]{-11.8f,11.1f})SculptedBindingColumn(V(side*10.65f,3,z),12.4f,1.2f);
    var mural=Group(root,"문옆_제본액자",V(side*11,4.4f,15.35f));Panel(mural,7.2f,8.4f);
    var symbol=Group(mural,"양각_잎문양",V(0,2,-.24f));Sprig(symbol,Vector3.zero,3.8f,ink);
    for(int k=0;k<3;k++)FlatBook(root,V(side*12.8f,3.04f+k*.3f,-14.8f),3.8f,.25f,1.65f,k%2==0?oxblood:umber,side*(k*3-2));
   }
   RaisedPaperFloor();
   // A raised header reduces the empty strip around the giant door without touching its swing.
   foreach(float y in new[]{14.15f,14.5f,14.8f})Box(root,"문상부_적층인방",V(0,y,15.02f),V(13.4f,.17f,.7f),y==14.5f?brass:wood);
   for(int i=-5;i<=5;i++)Sprig(root,V(i*1.15f,14.95f,14.62f),.66f,brass);
   // Decorative pendants occupy lateral ceiling bays, away from the optical puzzle at x=0..10,z=3..9.
   Lantern(V(-9.1f,11.2f,-4),.85f);Lantern(V(11.9f,11.8f,-4.5f),.9f);
   CombineDecorations(root);
   int index=0;
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
   {
    var mesh=filter.sharedMesh;if(!mesh||AssetDatabase.Contains(mesh))continue;
    string path=DetailFolder+"/Detail_"+(index++).ToString("000")+".asset";
    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved){saved.Clear();saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.uv=mesh.uv;saved.triangles=mesh.triangles;saved.bounds=mesh.bounds;saved.UploadMeshData(false);filter.sharedMesh=saved;Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
    else AssetDatabase.CreateAsset(mesh,path);
   }
   PrefabUtility.SaveAsPrefabAsset(root.gameObject,DetailFolder+"/F0_ReferenceDetail.prefab");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   CaptureDetailViews();
  }
  static void Panel(Transform p,float w,float h)
  {
   Box(p,"깊은가죽받침",V(0,h/2,0),V(w+.2f,h+.2f,.22f),umber);
   for(int layer=0;layer<3;layer++)MeshObject(p,"겹쳐붙인장지",F0Mesh.Paper(w-layer*.10f,h-layer*.10f,315+layer),layer==2?ivory:paper,V(layer*.025f,.04f+layer*.04f,-.15f-layer*.055f));
   foreach(float x in new[]{-w/2,w/2})Box(p,"책표지세로양각",V(x,h/2,-.3f),V(.10f,h,.10f),brass);
   foreach(float y in new[]{0f,h})Box(p,"책표지가로양각",V(0,y,-.3f),V(w,.10f,.10f),brass);
   foreach(float x in new[]{-w*.44f,w*.44f})foreach(float y in new[]{h*.06f,h*.88f})Sprig(p,V(x,y,-.36f),Mathf.Min(.55f,h*.14f),ink);
   for(int i=0;i<7;i++)Tube(p,"가장자리실밥",new[]{V(-w*.47f,h*(i+.5f)/7,-.27f),V(-w*.5f-.08f,h*(i+.5f)/7+.06f,-.36f),V(-w*.52f,h*(i+.5f)/7+.12f,-.22f)},.023f,rope);
  }
  static void ShelfBay(Transform p,float w,float h)
  {
   Box(p,"서가뒷판",V(0,h/2,.07f),V(w,h,.15f),wood);
   foreach(float x in new[]{-w/2,w/2})Box(p,"서가두꺼운옆판",V(x,h/2,-.4f),V(.2f,h,.95f),oxblood);
   for(int row=0;row<5;row++)
   {
    float y=row*2.3f;
    Box(p,"입체책선반",V(0,y,-.48f),V(w+.25f,.22f,1.05f),wood);
    Box(p,"선반황동테두리",V(0,y-.035f,-1.02f),V(w+.25f,.07f,.05f),brass);
    for(int b=0;b<5;b++)
    {
     float ht=1.35f+((b+row*2)%4)*.16f;
     var book=Group(p,"벽서가장서",V(-w*.38f+b*w*.185f,y+.12f,-.50f));
     Book(book,"가죽제본장서",Vector3.zero,.42f,ht,.72f,(b+row)%3==0?oxblood:(b%2==0?ochre:umber),(b%3-1)*3);
    }
   }
  }
  static Mesh RoundMesh(float radius,float height)
  {
   var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();
   const int segments=48;
   for(int j=0;j<=8;j++)for(int i=0;i<=segments;i++)
   {
    float a=i*Mathf.PI*2/segments,y=j*height/8;
    float r=radius*(1+.015f*Mathf.Sin(a*7+y*1.5f));
    vs.Add(V(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));uv.Add(new Vector2(i/(float)segments*radius*3,y*.48f));
    if(i<segments&&j<8){int n=j*(segments+1)+i;ts.AddRange(new[]{n,n+segments+1,n+1,n+1,n+segments+1,n+segments+2});}
   }
   var m=new Mesh();m.SetVertices(vs);m.SetUVs(0,uv);m.SetTriangles(ts,0);m.RecalculateNormals();m.RecalculateTangents();return m;
  }
  static void RoundBindingColumn(Vector3 at,float h,float r)
  {
   var p=Group(root,"거대한둥근책등기둥",at);
   MeshObject(p,"볼록한가죽책등",RoundMesh(r,h),oxblood,Vector3.zero);
   var c=p.gameObject.AddComponent<CapsuleCollider>();c.radius=r;c.height=h;c.center=V(0,h/2,0);
   foreach(float y in new[]{.1f,.35f,3.1f,8.9f,h-.45f,h-.15f})
   {
    MeshObject(p,"책등양각둥근띠",RoundMesh(r+.11f,.16f),umber,V(0,y,0));
    MeshObject(p,"둥근금박실선",RoundMesh(r+.13f,.035f),brass,V(0,y+.08f,0));
   }
   MeshObject(p,"황동제본고리",RoundMesh(r+.07f,.85f),brass,V(0,6,0));
   for(int face=0;face<4;face++)
   {
    var motif=Group(p,"기둥금박문양",Vector3.zero);motif.localRotation=Quaternion.Euler(0,face*90,0);
    Sprig(motif,V(0,9.8f,-r-.03f),1.8f,brass);Sprig(motif,V(0,1.0f,-r-.03f),1.4f,brass);
    Sprig(motif,V(0,6.14f,-r-.1f),.55f,wood);
    foreach(int side in new[]{-1,1})Sphere(motif,"제본리벳",V(side*.42f,6.42f,-r-.105f),V(.10f,.10f,.065f),wood);
   }
  }
  static void WorkDesk()
  {
   var p=Group(root,"왼쪽_제본작업책상",V(-10,3.05f,-7));
   Box(p,"책상상판",V(0,1.15f,0),V(4.7f,.25f,2.5f),wood,true);
   foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(p,"조각다리",V(x*2,.55f,z*.95f),V(.22f,1.1f,.22f),umber,true);
   for(int x=-1;x<=1;x++){Box(p,"서랍",V(x*1.48f,.82f,-1.15f),V(1.35f,.42f,.25f),oxblood);Sphere(p,"황동서랍손잡이",V(x*1.48f,.82f,-1.31f),V(.13f,.13f,.10f),brass);}
   var book=Group(p,"펼쳐진압인작업책",V(0,1.3f,0));book.localRotation=Quaternion.Euler(23,0,0);
   foreach(int side in new[]{-1,1})
   {
    var page=Group(book,"겹겹의작업페이지",V(side*.9f,0,0));page.localRotation=Quaternion.Euler(0,0,side*-8);
    Box(page,"가죽표지",V(0,0,0),V(1.82f,.12f,1.7f),oxblood);
    for(int i=0;i<4;i++)Box(page,"장지책장",V(0,.09f+i*.025f,0),V(1.73f-i*.02f,.018f,1.58f),ivory);
    var diagram=Group(page,"입체압인도면",V(0,.20f,-.60f));diagram.localRotation=Quaternion.Euler(90,0,0);Sprig(diagram,Vector3.zero,1.1f,ink);
   }
   for(int i=0;i<3;i++)FlatBook(p,V(-1.55f,1.28f+i*.15f,.35f),.9f,.12f,.75f,ochre,i*4);
  }
  static void WheeledBookcase()
  {
   var p=Group(root,"오른쪽_바퀴달린제본선반",V(12.1f,3.08f,8.6f));
   foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
   {
    Sphere(p,"책장바퀴",V(x*1.42f,.18f,z*.67f),V(.30f,.34f,.17f),brass);
    Box(p,"선반기둥",V(x*1.45f,1.4f,z*.7f),V(.14f,2.5f,.14f),wood);
   }
   foreach(float y in new[]{.4f,1.5f,2.6f})
   {
    Box(p,"선반판",V(0,y,0),V(3.3f,.16f,1.7f),wood,true);
    Box(p,"선반황동장식",V(0,y,-.9f),V(3.3f,.19f,.08f),brass);
   }
   for(int i=0;i<4;i++)FlatBook(p,V(-.55f,.52f+i*.16f,0),1.5f,.13f,1.2f,i%2==0?oxblood:ochre,i*2);
   var plaque=Group(root,"오른쪽_잎표지액자",V(14.95f,8,9));plaque.localRotation=Quaternion.Euler(0,90,0);Panel(plaque,3.2f,3.8f);Sprig(plaque,V(0,.5f,-.34f),2.7f,brass);
  }
  static void RaisedPaperFloor()
  {
   for(int x=0;x<7;x++)for(int z=0;z<4;z++)
   {
    var sheet=MeshObject(root,"높아진대지_장지표면",F0Mesh.Paper(4.48f,4.46f,802+x*7+z),ivory,V(-13.5f+x*4.5f,3.09f,-19.4f+z*4.5f));sheet.localRotation=Quaternion.Euler(90,0,0);
   }
   foreach(float x in new[]{-2.6f,2.6f})Box(root,"대지위인쇄선",V(x,3.145f,-10.5f),V(.06f,.012f,17.5f),ink);
   for(int strand=-1;strand<=1;strand++)Rope(root,"대지위제본실",V(strand*.09f,3.16f,-19.2f),V(strand*.09f,3.16f,-1.65f),.025f);
  }
  static void CaptureDetailViews()
  {
   string output="Docs/ArtReview/F0ReferenceDetail";Directory.CreateDirectory(output);
   var cam=new GameObject("DetailReviewCamera").AddComponent<Camera>();cam.nearClipPlane=.08f;cam.farClipPlane=90;cam.fieldOfView=67;
   try
   {
    cam.transform.position=room.transform.TransformPoint(V(-1.5f,5.2f,-17.5f));cam.transform.LookAt(room.transform.TransformPoint(V(0,8,10)));Capture(cam,output+"/front.png");
    cam.transform.position=room.transform.TransformPoint(V(-12,7,-8));cam.transform.LookAt(room.transform.TransformPoint(V(6,7,7)));Capture(cam,output+"/side.png");
    cam.transform.position=room.transform.TransformPoint(V(4,6,-10));cam.transform.LookAt(room.transform.TransformPoint(V(15.4f,9,0)));Capture(cam,output+"/wall.png");
   }
   finally{Object.DestroyImmediate(cam.gameObject);}
  }
  public static void RunReferenceDetailBatch()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ApplyReferenceDetails();F0PlayVerifier.Run();
  }
 }
}
