using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 public static class ReleaseStructureBuilder
 {
  public const string Folder="Assets/_BookTower/Generated/ReleaseStructure";
  const string Root="ReleaseStructure";
  static Material paper,edge,cover,ink,rope,gold;
  static readonly List<Transform> groups=new();
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  static Transform Group(Transform parent,string name,Vector3 at=default){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
  static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0PaperIdentity/"+name+".mat");
  static Transform Mesh(Transform parent,string name,UnityEngine.Mesh mesh,Material material,Vector3 at=default)
  {var t=Group(parent,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
  static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,float bevel=.04f,bool collision=false)
  {var t=Mesh(p,name,F0Mesh.RoundedBox(size,bevel),mat,at);if(collision)t.gameObject.AddComponent<BoxCollider>().size=size;return t;}
  static void Tube(Transform p,string name,IList<Vector3> points,float radius,Material mat)=>Mesh(p,name,F0Mesh.Tube(points,radius,8),mat);
  static Transform ReplaceVisuals(Transform zone)
  {
   if(!zone)throw new Exception("Expected art zone missing");
   foreach(var r in zone.GetComponentsInChildren<MeshRenderer>(true)){var f=r.GetComponent<MeshFilter>();Object.DestroyImmediate(r);if(f)Object.DestroyImmediate(f);}
   var old=zone.Find(Root);if(old)Object.DestroyImmediate(old.gameObject);
   var group=Group(zone,Root);groups.Add(group);return group;
  }
  static Transform ReplaceOwn(Transform parent,string name)
  {var old=parent.Find(name);if(old)Object.DestroyImmediate(old.gameObject);var t=Group(parent,name);groups.Add(t);return t;}
  [MenuItem("Book Tower/완성 아트 01 구조 적용")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new Exception("Exit play first");
   var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame first");
   Directory.CreateDirectory("Logs/ReleaseStructureBackups");EditorSceneManager.SaveScene(scene,"Logs/ReleaseStructureBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();groups.Clear();
   paper=Mat("IvoryMulberry");edge=Mat("LayeredCottonEdges");cover=Mat("UmberDyedPaper");ink=Mat("TerracottaPrint");rope=Mat("CottonThread");gold=Mat("DullBindingBrass");
   if(!paper||!edge||!cover||!ink||!rope||!gold)throw new Exception("Existing material library incomplete");
   var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId=="F0");var shell=room.transform.Find(InteriorF0Builder.RootName);var art=shell.Find(InteriorF0Builder.IdentityRootName);
   var puzzle=room.GetComponentInChildren<FoldBridgePuzzle>();var saved=Snapshot(puzzle);
   Floor(ReplaceVisuals(art.Find("01_시작_장지대지")),-20,18.8f,false);
   Floor(ReplaceVisuals(art.Find("02_건너편_장지대지")),7.2f,4.4f,true);
   Ceiling(ReplaceVisuals(art.Find("07_천장_겹표지")));
   WallJoinery(ReplaceOwn(art.Find("03_서벽_단서와창"),Root),-1);
   WallJoinery(ReplaceOwn(art.Find("04_동벽_서가와창"),Root),1);
   DoorReveal(ReplaceOwn(art.Find("06_북벽_출구프레임"),Root));
   Corridor(ReplaceOwn(art.Find("13_출구뒤_연속장지복도"),Root));
   // Only replace the redundant prototype lamp cubes; keep their lights and all old shell colliders.
   foreach(var t in shell.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="다음 길의 등불").ToArray())
   {var cube=t.Find("종이등");if(cube){var r=cube.GetComponent<Renderer>();if(r)Object.DestroyImmediate(r);var f=cube.GetComponent<MeshFilter>();if(f)Object.DestroyImmediate(f);}
    var casing=ReplaceOwn(t,Root);Box(casing,"Folded lantern shade",Vector3.zero,V(.43f,.65f,.43f),paper,.08f);
    foreach(float y in new[]{-.36f,.36f})Box(casing,"Pressed collar",V(0,y,0),V(.54f,.09f,.54f),cover,.045f);
    foreach(float x in new[]{-.23f,.23f})foreach(float z in new[]{-.23f,.23f})Box(casing,"Binding rib",V(x,0,z),V(.045f,.7f,.045f),cover,.015f);
   }
   foreach(var group in groups)Combine(group);
   SaveMeshes();Physics.SyncTransforms();
   if(saved!=Snapshot(puzzle))throw new Exception("Optical transform or collider contract changed");
   Directory.CreateDirectory(ReleaseStructureReview.Output);File.WriteAllText(ReleaseStructureReview.Output+"/build.txt","PASS: optical transforms and page collider unchanged\n"+string.Join("\n",groups.Select(t=>$"{t.parent.name}: renderers={t.GetComponentsInChildren<Renderer>().Length}, triangles={t.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)}")));
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  }
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();ReleaseStructureReview.After();}
  static string Snapshot(FoldBridgePuzzle p)=>string.Join("|",new[]{p.hinge,p.source,p.receiver,p.shutter,p.entranceMirror,p.farMirror}.Concat(p.lightSurfaces.Select(x=>x.transform)).Select(t=>t.GetInstanceID()+":"+t.localPosition.ToString("F6")+":"+t.localRotation.ToString("F6")))+"|"+p.pageCollider.size+"|"+p.pageCollider.center;

  // Closed laminated sheet: the broad folds stay below the controller's existing flat collision.
  static UnityEngine.Mesh Sheet(float width,float depth,int seed,float relief,float thickness)
  {
   int nx=Mathf.CeilToInt(width*3),nz=Mathf.CeilToInt(depth*3);var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
   for(int layer=0;layer<2;layer++)for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
   {float u=x/(float)nx,t=z/(float)nz;float noise=Mathf.PerlinNoise(seed+u*7,seed*.3f+t*9)-.5f;
    float px=(u-.5f)*width+((x==0||x==nx)?noise*.04f:0),pz=t*depth+((z==0||z==nz)?noise*.035f:0);
    float fold=relief*(Mathf.Sin(u*7+t*2+seed)*.45f+Mathf.Sin(t*9-u*2)*.3f+noise*.25f);
    v.Add(V(px,fold-layer*thickness,pz));uv.Add(new Vector2(px*.53f+seed*.17f,pz*.53f));}
   int count=(nx+1)*(nz+1);void Q(int a,int b,int c,int d){tr.AddRange(new[]{a,b,c,a,c,d});}
   for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x;Q(a,a+nx+1,a+nx+2,a+1);Q(a+count,a+count+1,a+count+nx+2,a+count+nx+1);}
   for(int x=0;x<nx;x++){Q(x,x+1,x+1+count,x+count);int a=nz*(nx+1)+x;Q(a,a+count,a+count+1,a+1);}
   for(int z=0;z<nz;z++){int a=z*(nx+1),b=a+nx+1;Q(a,a+count,b+count,b);a+=nx;b+=nx;Q(a,b,b+count,a+count);}
   return Make("Broad folded laminated sheet",v,uv,tr);
  }
  static UnityEngine.Mesh Make(string name,List<Vector3> v,List<Vector2> uv,List<int> tr){var m=new UnityEngine.Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
  static void Floor(Transform g,float z0,float depth,bool far)
  {
   Box(g,"Continuous layered core",V(0,1.47f,z0+depth/2),V(32,2.94f,depth),edge,.05f);
   Box(g,"Unbroken paper backing",V(0,2.992f,z0+depth/2),V(32,.035f,depth),paper,.01f);
   int rows=far?1:4;float dz=depth/rows;
   float[] edges={-16,-9.1f,-2.9f,3.0f,9.6f,16};
   for(int row=0;row<rows;row++)for(int col=0;col<5;col++)
   {float w=edges[col+1]-edges[col],x=(edges[col]+edges[col+1])*.5f;
    Mesh(g,"Hand laid paper panel",Sheet(w-.022f,dz-.018f,13+row*17+col*3,.018f,.035f),paper,V(x,3.027f,z0+row*dz+.009f));
    // Paired stitches follow seams, instead of evenly sprinkling unrelated detail across every surface.
    if(col>0)for(float z=z0+row*dz+.45f;z<z0+(row+1)*dz-.25f;z+=.85f)
      Tube(g,"Sewn panel joint",new[]{V(edges[col]-.12f,3.045f,z-.035f),V(edges[col],3.064f,z),V(edges[col]+.12f,3.045f,z+.035f)},.012f,rope);
   }
   foreach(float x in new[]{-2.65f,2.65f})Tube(g,"Printed border",new[]{V(x,3.055f,z0+.08f),V(x,3.055f,z0+depth-.08f)},.019f,ink);
   for(int strand=-1;strand<=1;strand++)Tube(g,"Guiding book thread",new[]{V(strand*.09f,3.068f,z0+.08f),V(strand*.09f,3.068f,z0+depth-.08f)},.018f,rope);
   for(float z=z0+1.8f;z<z0+depth-.5f;z+=4.3f)foreach(int side in new[]{-1,1}){var mark=Group(g,"Pressed leaf",V(side*2.3f,3.061f,z));mark.localRotation=Quaternion.Euler(90,0,side*8);Sprig(mark,.68f,ink);}
   float rim=far?7.21f:-1.21f;
   foreach(int side in new[]{-1,1})
   {Box(g,"Folded bank binding",V(side*9.35f,2.94f,rim),V(13.2f,.19f,.23f),cover,.075f);
    for(int layer=0;layer<10;layer++)Tube(g,"Visible laminated edge",Enumerable.Range(0,17).Select(i=>V(side*(2.78f+i*.82f),2.77f-layer*.24f+Mathf.Sin(i*.6f+layer)*.014f,rim+(far?-.105f:.105f))).ToArray(),.016f,layer%3==0?paper:edge);
   }
  }
  static void Sprig(Transform p,float h,Material mat)
  {Tube(p,"Stem",new[]{Vector3.zero,V(0,h,0)},h*.011f,mat);for(int j=0;j<4;j++)foreach(int side in new[]{-1,1}){var leaf=Mesh(p,"Pressed leaf",F0Mesh.Leaf(h*.28f,h*.115f),mat,V(0,j*h*.2f,-.012f));leaf.localRotation=Quaternion.Euler(0,0,side*48);}}

  static void Ceiling(Transform g)
  {
   Box(g,"Sealed ceiling backing",V(0,16.17f,-2),V(32,.18f,36.2f),paper,.03f);
   for(int row=0;row<5;row++)for(int col=0;col<4;col++)
   {
    float x=-12+col*8,z=-20+row*7.2f;
    var sheet=Mesh(g,"Vaulted paper ceiling panel",Sheet(7.68f,6.88f,row*23+col,.10f,.16f),paper,V(x,15.94f,z+.16f));sheet.localRotation=Quaternion.Euler(0,0,180);
    // Recessed double borders create a readable thickness and generous quiet paper centres.
    foreach(float inset in new[]{0f,.19f})
    {float hw=3.88f-inset;var corners=new[]{V(x-hw,15.65f-inset*.15f,z+.12f+inset),V(x+hw,15.65f-inset*.15f,z+.12f+inset),V(x+hw,15.65f-inset*.15f,z+7.08f-inset),V(x-hw,15.65f-inset*.15f,z+7.08f-inset),V(x-hw,15.65f-inset*.15f,z+.12f+inset)};Tube(g,"Rolled coffer border",corners,inset==0?.14f:.038f,inset==0?cover:edge);}
    foreach(float sx in new[]{-1f,1f}){var leaf=Group(g,"Coffer corner imprint",V(x+sx*3.15f,15.74f,z+.7f));leaf.localRotation=Quaternion.Euler(-90,0,sx*26);Sprig(leaf,.65f,ink);}
   }
   foreach(float z in new[]{-19.9f,-12.8f,-5.6f,1.6f,8.8f,15.9f})
   {var path=Enumerable.Range(0,49).Select(i=>{float x=-16+i*32f/48;return V(x,15.18f+.43f*Mathf.Sin(i*Mathf.PI/48),z);}).ToArray();Tube(g,"Curved binding roof rib",path,.19f,cover);Tube(g,"Paper laminated rib edge",path.Select(p=>p+Vector3.down*.19f).ToArray(),.033f,edge);}
   // Curved corbels join the roof to the actual book pillars.
   foreach(int side in new[]{-1,1})foreach(float z in new[]{-11.8f,11.1f})
   {var p=new[]{V(side*10.65f,14.8f,z),V(side*10.3f,15.05f,z),V(side*9.6f,15.40f,z),V(side*8.3f,15.6f,z)};Tube(g,"Rolled paper corbel",p,.23f,cover);Tube(g,"Corbel page edge",p.Select(v=>v+V(0,-.20f,-.03f)).ToArray(),.036f,paper);}
  }
  static void WallJoinery(Transform g,int side)
  {
   foreach(float y in new[]{3.18f,3.43f})Tube(g,"Rolled wall footing",new[]{V(side*15.48f,y,-19.3f),V(side*15.48f,y,15.1f)},y<3.3f?.17f:.08f,cover);
   for(float z=-18.2f;z<15;z+=4.7f)
   {Box(g,"Folded wall heel",V(side*15.42f,3.55f,z),V(.62f,.8f,.9f),cover,.17f);Box(g,"Layered heel face",V(side*15.04f,3.57f,z),V(.11f,.55f,.69f),edge,.045f);}
   for(int layer=0;layer<3;layer++)Tube(g,"Wall roof laminated cornice",new[]{V(side*(15.70f-layer*.10f),15.18f+layer*.15f,-19.5f),V(side*(15.70f-layer*.10f),15.18f+layer*.15f,15.4f)},.09f,layer==1?edge:cover);
  }
  static void DoorReveal(Transform g)
  {
   foreach(int side in new[]{-1,1})
   {Box(g,"Thick outer jamb",V(side*6.30f,7.0f,15.48f),V(.55f,14.1f,1.22f),cover,.18f);
    for(int i=0;i<4;i++)Tube(g,"Jamb paper laminations",new[]{V(side*(6.04f+i*.06f),.2f,15.98f+i*.06f),V(side*(6.04f+i*.06f),13.9f,15.98f+i*.06f)},.029f,edge);
   }
   Box(g,"Continuous door soffit",V(0,14.22f,15.56f),V(12.9f,.36f,1.3f),cover,.15f);
   for(int i=0;i<4;i++)Tube(g,"Lintel page layers",new[]{V(-6.3f,14.04f+i*.055f,16.12f),V(6.3f,14.04f+i*.055f,16.12f)},.02f,edge);
  }
  static UnityEngine.Mesh Vault(float width,float length,float spring,float rise)
  {
   var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int n=40;
   for(int i=0;i<=n;i++){float u=i/(float)n,x=(u-.5f)*width,y=spring+rise*Mathf.Sin(u*Mathf.PI);foreach(float dy in new[]{0f,.23f})foreach(float z in new[]{-length*.5f,length*.5f}){v.Add(V(x,y+dy,z));uv.Add(new Vector2(u*width*.5f,z*.5f));}}
   void Q(int a,int b,int c,int d)=>tr.AddRange(new[]{a,b,c,a,c,d});
   for(int i=0;i<n;i++){int a=i*4,b=a+4;Q(a,a+1,b+1,b);Q(a+2,b+2,b+3,a+3);Q(a,b,b+2,a+2);Q(a+1,a+3,b+3,b+1);}Q(0,2,3,1);int e=n*4;Q(e,e+1,e+3,e+2);
   return Make("Closed broad paper vault",v,uv,tr);
  }
  static void Corridor(Transform g)
  {
   // The low vault is outside the double-door swing; the tall entry vestibule remains clear.
   var back=Group(g,"Bound book rear gallery",V(10.75f,0,18.35f));back.localRotation=Quaternion.Euler(0,90,0);
   Mesh(back,"Continuous rear gallery vault",Vault(5.75f,11f,7.15f,1.18f),paper);
   for(float z=-5.2f;z<5.5f;z+=3.5f)Arch(back,z,5.75f,7.10f,1.18f);
   Box(g,"Folded corner transition fascia",V(16.25f,7.80f,18.35f),V(.20f,1.65f,5.75f),cover,.065f);
   var side=Group(g,"Folded corner gallery",V(22.0f,0,14.5f));Mesh(side,"Continuous corner vault",Vault(11.5f,14.6f,7.05f,1.2f),paper);
   foreach(float z in new[]{-6.8f,-2.2f,2.4f,6.8f})Arch(side,z,11.5f,7.0f,1.2f);
   // Existing collision shell stays; added reveals are outside the walkable centreline.
   for(float x=6.8f;x<27;x+=3.6f)
   {
    var panel=Group(g,"Recessed gallery book panel",V(x,.12f,21.43f));
    Box(panel,"Panel backing",V(0,3.6f,0),V(3.48f,7.2f,.18f),cover,.07f);
    // Camera collision must follow the new inner face, not the older wall behind it.
    var stop=panel.gameObject.AddComponent<BoxCollider>();stop.center=V(0,3.6f,-.11f);stop.size=V(3.48f,7.2f,.4f);
    var sheet=Mesh(panel,"Curved inset paper",Sheet(3.10f,6.8f,(int)x,.045f,.04f),paper,V(0,.25f,-.13f));sheet.localRotation=Quaternion.Euler(-90,0,0);
    foreach(float sideX in new[]{-1.59f,1.59f})Tube(panel,"Rolled vertical binding",new[]{V(sideX,.15f,-.20f),V(sideX,7.1f,-.20f)},.065f,edge);
    foreach(float y in new[]{.20f,7.08f})Tube(panel,"Rolled panel edge",new[]{V(-1.6f,y,-.2f),V(1.6f,y,-.2f)},.065f,edge);
    var mark=Group(panel,"Faded leaf print",V(0,2.9f,-.21f));Sprig(mark,1.25f,ink);
   }
   // No floor colliders are replaced. Panels remain thin, below the existing safe step allowance.
   for(float x=5.6f;x<25.5f;x+=3.8f)Mesh(g,"Rear passage paper panel",Sheet(3.79f,5.3f,(int)x,.009f,.03f),paper,V(x,.055f,15.9f));
   for(float z=8.8f;z<15;z+=3.5f)Mesh(g,"Corner passage paper panel",Sheet(9.4f,3.48f,(int)(z*9),.009f,.03f),paper,V(22,.055f,z));
   // Raised guide ink follows the already authored walking route, without pointing into a dead end.
   var path=new[]{V(5,.092f,18.3f),V(20,.092f,18.3f),V(20,.092f,11.2f),V(27.8f,.092f,11.2f)};
   for(int i=0;i<path.Length-1;i++)Tube(g,"Continuous passage binding thread",new[]{path[i],path[i+1]},.035f,rope);
  }
  static void Arch(Transform g,float z,float width,float spring,float rise)
  {
   var path=Enumerable.Range(0,41).Select(i=>{float u=i/40f;return V((u-.5f)*width,spring+rise*Mathf.Sin(u*Mathf.PI)-.07f,z);}).ToArray();
   Tube(g,"Rolled gallery arch",path,.145f,cover);Tube(g,"Gallery arch page edge",path.Select(x=>x+V(0,-.15f,0)).ToArray(),.034f,edge);
  }
  static void Combine(Transform root)
  {
   var fs=root.GetComponentsInChildren<MeshFilter>().ToArray();
   foreach(var group in fs.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
   {var m=new UnityEngine.Mesh{name=root.parent.name+" "+group.Key.name,indexFormat=IndexFormat.UInt32};m.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
    Mesh(root,"Structure "+group.Key.name,m,group.Key);foreach(var f in group){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}}
  }
  static void SaveMeshes()
  {
   int i=0;foreach(var root in groups)foreach(var f in root.GetComponentsInChildren<MeshFilter>())
   {string path=Folder+"/Structure_"+(i++).ToString("000")+".asset";var m=f.sharedMesh;var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
    if(old){old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;old.normals=m.normals;old.tangents=m.tangents;old.uv=m.uv;old.triangles=m.triangles;old.bounds=m.bounds;old.name=m.name;EditorUtility.SetDirty(old);f.sharedMesh=old;}else AssetDatabase.CreateAsset(m,path);}
  }
 }
}
