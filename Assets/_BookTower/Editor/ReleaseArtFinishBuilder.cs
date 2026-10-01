using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Presentation;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 // Incremental finish pass. Existing structure, gameplay transforms and collision are authoritative.
 public static class ReleaseArtFinishBuilder
 {
  public const string Folder="Assets/_BookTower/Generated/ReleaseFinish";
  public const string RootName="ReleaseArt04_08_Craft";
  static Material paper,edge,ochre,red,teal,navy,thread;
  static readonly List<Transform> batches=new();
  static int serial;
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  static Transform Group(Transform p,string name,Vector3 at=default)
  {var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=at;return t;}
  static Transform Part(Transform p,string name,Mesh mesh,Material material,Vector3 at=default,Vector3 rotation=default)
  {var t=Group(p,name,at);t.localEulerAngles=rotation;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
  static void Box(Transform p,string n,Vector3 at,Vector3 size,Material m,float bevel=.07f)=>Part(p,n,F0Mesh.RoundedBox(size,bevel),m,at);
  static void Tube(Transform p,string n,Vector3[] path,float radius,Material m)=>Part(p,n,F0Mesh.Tube(path,radius,8),m);
  static Material Mat(string name,Color colour,string family="Paper")
  {
   string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.name=name;m.SetColor("_BaseColor",colour);m.SetFloat("_Smoothness",family=="Brass"?.27f:.13f);m.SetFloat("_Metallic",family=="Brass"?.55f:0);
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(family=="Paper"?"Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png":ReleasePaperMaps.Folder+"/"+family+"_Color.png"));
   m.SetTextureScale("_BaseMap",Vector2.one*.6f);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ReleasePaperMaps.Folder+"/"+family+"_Normal.png"));m.SetFloat("_BumpScale",.25f);m.EnableKeyword("_NORMALMAP");m.SetFloat("_Cull",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
  }
  public static void Run()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();ReleaseFinishReview.Run();
  }
  public static void SealAndReview(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();ReleaseFinishReview.CoverageOnly();}
  public static void FoyerAndReview(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();ReleaseFinishReview.FoyerOnly();}
  public static void Apply()
  {
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();batches.Clear();serial=0;
   string contract=CollisionContract();var scene=EditorSceneManager.GetActiveScene();
   Directory.CreateDirectory("Logs/ReleaseFinishBackups");EditorSceneManager.SaveScene(scene,"Logs/ReleaseFinishBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   paper=Mat("DeckledIvory",new Color(.78f,.69f,.53f));edge=Mat("ExposedPageEdge",new Color(.60f,.49f,.32f),"Edge");
   ochre=Mat("PressedOchre",new Color(.56f,.34f,.12f),"Cover");red=Mat("F0_OxbloodBinding",new Color(.24f,.067f,.032f),"Cover");
   teal=Mat("1A_CrayonTeal",new Color(.19f,.35f,.32f),"Cover");navy=Mat("1B_IndigoBinding",new Color(.055f,.105f,.16f),"Cover");thread=Mat("BindingCord",new Color(.68f,.57f,.39f),"Cotton");
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target).OrderBy(r=>r.RoomId))
   {
    var old=room.transform.Find(RootName);if(old)Object.DestroyImmediate(old.gameObject);
    var root=Group(room.transform,RootName);
    if(room.RoomId=="F0")Foyer(root);else if(room.RoomId=="1A")Theatre(root);else Reading(root);
   }
   foreach(var b in batches)Combine(b);
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target).OrderBy(r=>r.RoomId))
    foreach(var f in room.transform.Find(RootName).GetComponentsInChildren<MeshFilter>())SaveMesh(f);
   ProjectionCookie();Grade();
   if(CollisionContract()!=contract)throw new Exception("Finish pass changed existing collision geometry");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory(ReleaseFinishReview.Output);File.WriteAllText(ReleaseFinishReview.Output+"/apply.txt",$"PASS: collision contract preserved; {serial} mesh assets; {batches.Count} spatial material batches.\nThree-room craft pass plus bounded visual-only motion; optical object transforms unchanged.\n");
  }
  static bool OwnCollider(Collider c){for(var t=c.transform;t;t=t.parent)if(t.name==RootName)return true;return false;}
  static string CollisionContract()=>string.Join("|",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>!OwnCollider(c)).OrderBy(c=>c.GetInstanceID()).Select(c=>$"{c.GetInstanceID()}:{c.transform.localToWorldMatrix}:{c.bounds}:{c.enabled}"));
  static Transform Batch(Transform p,string n,Vector3 at=default){var g=Group(p,n,at);batches.Add(g);return g;}
  static void Arch(Transform p,float width,float spring,float rise,float depth,Material material,float band=.26f)
  {
   var path=new List<Vector3>{V(-width*.5f,0,0),V(-width*.5f,spring,0)};
   for(int i=1;i<=40;i++){float a=i/40f*Mathf.PI;path.Add(V(-Mathf.Cos(a)*width*.5f,spring+Mathf.Sin(a)*rise,0));}
   path.Add(V(width*.5f,0,0));
   Part(p,"Folded arch binding",Ribbon(path,band,depth),material);
   Tube(p,"Rolled ivory edge",path.Select(q=>q+V(0,0,-depth*.55f)).ToArray(),.037f,edge);
  }
  static Mesh Ribbon(IList<Vector3> path,float width,float depth)
  {
   var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();float length=0;
   for(int i=0;i<path.Count;i++)
   {
    var tangent=(path[Mathf.Min(path.Count-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;
    var side=Vector3.Cross(tangent,Vector3.forward).normalized*width*.5f;
    if(i>0)length+=Vector3.Distance(path[i],path[i-1]);
    vs.Add(path[i]-side-Vector3.forward*depth*.5f);vs.Add(path[i]+side-Vector3.forward*depth*.5f);vs.Add(path[i]+side+Vector3.forward*depth*.5f);vs.Add(path[i]-side+Vector3.forward*depth*.5f);
    for(int j=0;j<4;j++)uv.Add(new Vector2(j*width*.5f,length));
    if(i==0)continue;
    for(int j=0;j<4;j++){int a=(i-1)*4+j,b=(i-1)*4+(j+1)%4,c=i*4+j,d=i*4+(j+1)%4;ts.AddRange(new[]{a,c,b,b,c,d});}
   }
   ts.AddRange(new[]{0,1,2,0,2,3});int k=vs.Count-4;ts.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});
   var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  static void Sprig(Transform p,float height,Material colour)
  {
   Tube(p,"Pressed stem",Enumerable.Range(0,13).Select(i=>V(Mathf.Sin(i/12f*1.7f)*height*.08f,i/12f*height,0)).ToArray(),.018f,colour);
   for(int i=0;i<7;i++){float s=i%2==0?-1:1;Part(p,"Embossed paper leaf",F0Mesh.Leaf(height*.24f,height*.075f),colour,V(Mathf.Sin((i+1)/8f*1.7f)*height*.08f,(i+1)/8f*height,-.025f),V(0,0,s*48));}
  }
  static void Foyer(Transform root)
  {
   // Rear cover reads as several bound folios, not one oversized tiled wall.
   for(int i=0;i<5;i++)
   {
    var g=Batch(root,"Rear folio "+i,V((i-2)*5.4f,3.13f,-18.98f));g.localEulerAngles=V(0,180,0);
    float height=i==2?11.4f:i%2==0?10.4f:9.4f;
    // Clear the old wall's curled page tips and give the projected cover its own thin collision.
    var cover=Part(g,"Rounded oxblood inset",F0Mesh.RoundedBox(V(5.15f,height,.26f),.16f),red,V(0,height*.5f,.12f));
    cover.gameObject.AddComponent<BoxCollider>().size=V(5.15f,height,.26f);
    for(int layer=0;layer<3;layer++){var arch=Group(g,"Layered page moulding",V(0,.2f+layer*.06f,-.12f-layer*.09f));Arch(arch,4.65f-layer*.19f,height-2.4f,1.8f,.14f,layer==0?ochre:paper,.19f);}
    var mark=Group(g,"Faded binding motif",V(0,4.3f,-.31f));Sprig(mark,2.0f,ochre);
    for(int y=0;y<2;y++)Box(g,"Cover rib",V(0,1.3f+y*(height-3),-.16f),V(4.65f,.19f,.14f),ochre);
    for(int j=0;j<5;j++)Box(g,"Folio edge layers",V((i%2==0?1:-1)*(2.3f-j*.055f),height*.5f,-.20f-j*.025f),V(.04f,height-.8f,.045f),edge,.012f);
   }
   for(int side=-1;side<=1;side+=2)
   {
    var g=Batch(root,"Overhead binding "+side,V(side*11.8f,14.85f,-3));
    Box(g,"Layered ceiling spine",Vector3.zero,V(1.05f,.55f,30),red,.17f);
    for(int i=0;i<8;i++)Tube(g,"Sewn ceiling loop",new[]{V(-.57f,.1f,-13+i*3.8f),V(-.6f,-.35f,-13+i*3.8f),V(0,-.43f,-13+i*3.8f),V(.6f,-.35f,-13+i*3.8f),V(.57f,.1f,-13+i*3.8f)},.047f,thread);
   }
   // A real ribbon and loose page edge distinguish the work book without a hovering icon.
   var room=root.parent;var book=room.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="기울어진펼친책");
   if(book)
   {
    var g=Batch(root,"Workbench ribbon");g.position=book.position;g.rotation=book.rotation;
    Part(g,"Folded bookmark",Ribbon(new[]{V(.27f,.16f,-.2f),V(.26f,.12f,-.75f),V(.23f,-.02f,-1.25f),V(.18f,-.32f,-1.34f)},.24f,.025f),red);
    var tip=Part(g,"Loose bookmark tip",F0Mesh.Leaf(.48f,.12f),red,V(.18f,-.32f,-1.34f),V(160,0,6));
    Motion(root,"Workbench page breath",new[]{tip},.65f);
   }
   Sprays(root,new[]{V(-13.2f,9,-15),V(13.2f,8.5f,7)},ochre);
   foreach(int side in new[]{-1,1})
   {
    var bounce=Group(root,"Reflected window light on rear cover "+side,V(side*8.8f,9.5f,-11));bounce.LookAt(root.TransformPoint(V(side*6.5f,7,-19.5f)));
    var light=bounce.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.88f,.70f);light.intensity=95;light.range=18;light.spotAngle=85;light.innerSpotAngle=48;light.shadows=LightShadows.None;
   }
  }
  static void Theatre(Transform root)
  {
   var stage=Batch(root,"Layered proscenium",V(-5,.38f,25.4f));
   for(int i=0;i<4;i++){var layer=Group(stage,"Turned page arch "+i,V(0,i*.06f,i*.12f));Arch(layer,17.3f+i*.36f,5.1f,4.25f+i*.11f,.22f,i==0?teal:paper,.29f);}
   for(int side=-1;side<=1;side+=2)
   {
    var wing=Group(stage,"Printed side binding",V(side*9.35f,1,.05f));Sprig(wing,4.4f,teal);
    // Curved alcoves against the perimeter preserve the broad central staging/drag space.
    for(int bay=0;bay<2;bay++)
    {
     var g=Batch(root,"Side story alcove "+side+" "+bay,V(side*28,0,-12+bay*20));g.localEulerAngles=V(0,side*90,0);
     for(int l=0;l<3;l++){var frame=Group(g,"Nested page edge",V(0,l*.05f,l*.15f));Arch(frame,8.1f-l*.22f,5.4f,3.5f,.23f,l==0?teal:paper,.3f);}
     for(int i=0;i<6;i++){var leaf=Group(g,"Faint crayon relief",V((i%2==0?-1:1)*2.7f,1.5f+i*.75f,.35f));Sprig(leaf,.7f,teal);}
     Box(g,"Layered shelf lip",V(0,1.5f,-.2f),V(7.2f,.23f,1.25f),paper,.10f);
    }
   }
   // High swept bands break the flat roof silhouette without entering the projection cone.
   for(int j=0;j<3;j++)
   {
    var g=Batch(root,"Ceiling turning leaf "+j,V(0,0,-17+j*15));
    var path=Enumerable.Range(0,65).Select(i=>{float t=i/64f;return V((t-.5f)*54,8.8f+2.35f*Mathf.Sin(t*Mathf.PI),0);}).ToArray();
    Part(g,"Wide book arch",Ribbon(path,.52f,.32f),paper);Tube(g,"Teal book seam",path.Select(v=>v+V(0,-.12f,-.18f)).ToArray(),.045f,teal);
   }
   var trays=Batch(root,"Recorded paper wing trays",V(-5,.38f,14));
   foreach(float x in new[]{-6.8f,-5.9f,5.9f,6.8f})Box(trays,"Paper rest",V(x,.06f,Mathf.Abs(x)>6?3.4f:2.2f),V(.85f,.10f,.82f),teal,.04f);
   Sprays(root,new[]{V(-26,7,-10),V(26,7,9),V(-15,8,25)},teal);
  }
  static void Reading(Transform root)
  {
   // The rectangular lintel used to leave triangular exterior gaps above the curved passage.
   // Match its final arch profile and overlap the wall thickness, without lowering the walking opening.
   var wall=root.parent.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="door wall left");
   var cap=Part(root,"Bound arch spandrel seals entrance shoulders",EntrySpandrel(),wall.sharedMaterial,V(-10,0,0));
   cap.gameObject.AddComponent<MeshCollider>().sharedMesh=cap.GetComponent<MeshFilter>().sharedMesh;
   for(int j=0;j<6;j++)
   {
    var g=Batch(root,"Vault folded rib "+j,V(0,0,-20+j*8));
    var path=Enumerable.Range(0,65).Select(i=>{float t=i/64f;return V((t-.5f)*35.7f,15.85f+3.65f*Mathf.Sin(t*Mathf.PI),0);}).ToArray();
    Part(g,"Broad indigo page binding",Ribbon(path,.47f,.38f),navy);
    foreach(float z in new[]{-.25f,.25f})Tube(g,"Vault exposed paper layers",path.Select(v=>v+V(0,-.08f,z)).ToArray(),.065f,edge);
    var keystone=Group(g,"Folded vault rosette",V(0,19.28f,-.3f));
    for(int k=0;k<8;k++)Part(keystone,"Rosette page",F0Mesh.Leaf(.60f,.18f),paper,Vector3.zero,V(0,0,k*45));
   }
   foreach(int side in new[]{-1,1})for(int bay=0;bay<3;bay++)
   {
    var g=Batch(root,"Archival book alcove "+side+" "+bay,V(side*17.1f,7.0f,-14+bay*14));g.localEulerAngles=V(0,side*90,0);
    Arch(g,10.4f,4.5f,2.3f,.30f,navy,.42f);
    var nested=Group(g,"Ivory arch lining",V(0,.04f,-.2f));Arch(nested,9.8f,4.5f,2.1f,.12f,edge,.13f);
    for(int k=0;k<3;k++){var stamp=Group(g,"Pressed folio stamp",V(-3+k*3,4.6f,-.25f));Sprig(stamp,.7f,ochre);}
   }
   var rear=Batch(root,"Rear archive frieze",V(0,10.1f,-21.55f));rear.localEulerAngles=V(0,180,0);
   Box(rear,"Sculpted binding header",V(0,1,0),V(29,.44f,.30f),navy,.13f);
   for(int i=0;i<7;i++){var s=Group(rear,"Embossed memory leaf",V(-12+i*4,1.25f,-.21f));Sprig(s,1.6f,i%2==0?ochre:edge);}
   Sprays(root,new[]{V(-16,7,-5),V(16,9,8),V(-16,8,17)},edge);
  }
  static Mesh EntrySpandrel()
  {
   var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
   void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
   {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(a.x,a.y),new Vector2(b.x,b.y),new Vector2(c.x,c.y),new Vector2(d.x,d.y)});tri.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}
   const float front=-21.60f,back=-22.35f,top=6.06f;
   float Bottom(float x)=>2.55f+3.10f*Mathf.Sqrt(Mathf.Max(0,1-x*x/(3.5f*3.5f)));
   for(int i=0;i<64;i++)
   {
    float x=-3.8f+i*7.6f/64,xx=-3.8f+(i+1)*7.6f/64,y=Bottom(x),yy=Bottom(xx);
    Quad(V(x,y,front),V(xx,yy,front),V(x,top,front),V(xx,top,front));
    Quad(V(xx,yy,back),V(x,y,back),V(xx,top,back),V(x,top,back));
    Quad(V(x,top,front),V(xx,top,front),V(x,top,back),V(xx,top,back));
    Quad(V(x,y,back),V(xx,yy,back),V(x,y,front),V(xx,yy,front));
   }
   Quad(V(-3.8f,Bottom(-3.8f),back),V(-3.8f,Bottom(-3.8f),front),V(-3.8f,top,back),V(-3.8f,top,front));
   Quad(V(3.8f,Bottom(3.8f),front),V(3.8f,Bottom(3.8f),back),V(3.8f,top,front),V(3.8f,top,back));
   var mesh=new Mesh{name="Closed paper arch spandrel"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  static void Sprays(Transform root,Vector3[] points,Material colour)
  {
   foreach(var point in points)
   {
    var g=Group(root,"Loose page garland",point);var tips=new List<Transform>();
    for(int i=0;i<5;i++)tips.Add(Part(g,"Free leaf "+i,F0Mesh.Leaf(.52f+i*.06f,.15f),colour,V(Mathf.Sin(i)*.25f,-i*.18f,0),V(13+i*7,0,Mathf.Sin(i*1.7f)*45)));
    Motion(g,"Gentle paper motion",tips.ToArray(),1.3f);
   }
  }
  static void Motion(Transform root,string name,Transform[] tips,float amplitude)
  {var m=Group(root,name).gameObject.AddComponent<PaperRoomMotion>();m.tips=tips;m.amplitude=amplitude;m.period=7.3f+tips.Length*.23f;}
  static void Combine(Transform root)
  {
   var moving=new HashSet<Transform>(root.root.GetComponentsInChildren<PaperRoomMotion>().SelectMany(m=>m.tips));
   var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>!moving.Contains(f.transform)).ToArray();
   foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
   {
    var mesh=new Mesh{name=root.name+" "+group.Key.name,indexFormat=IndexFormat.UInt32};
    mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
    Part(root,"Craft batch "+group.Key.name,mesh,group.Key);
    foreach(var f in group){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
   }
  }
  static void SaveMesh(MeshFilter f)
  {
   string path=Folder+"/Craft_"+(serial++).ToString("000")+".asset";var mesh=f.sharedMesh;var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);f.sharedMesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var collider=f.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=f.sharedMesh;
  }
  static void ProjectionCookie()
  {
   const string path="Assets/_BookTower/Resources/ReleaseProjectionCookie.asset";
   var cookie=new Texture2D(128,128,TextureFormat.RGBA32,false,true){name="Paper projector floor cutoff",wrapMode=TextureWrapMode.Clamp};
   var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++)
   {float u=x/127f,v=y/127f;float low=Mathf.SmoothStep(.04f,1,Mathf.InverseLerp(.34f,.47f,v));float sides=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.12f,Mathf.Min(u,1-u)));float a=low*sides;pixels[y*128+x]=new Color(a,a,a,1);}
   cookie.SetPixels(pixels);cookie.Apply();var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(old){EditorUtility.CopySerialized(cookie,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(cookie);}else AssetDatabase.CreateAsset(cookie,path);
  }
  static void Grade()
  {
   var obj=GameObject.Find("PaperWorldGrade");if(!obj)obj=new GameObject("PaperWorldGrade");
   var volume=obj.GetComponent<Volume>();if(!volume)volume=obj.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.weight=1;
   string path=Folder+"/PaperWorldGrade.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
   if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
   if(!profile.TryGet<ColorAdjustments>(out var grade))grade=profile.Add<ColorAdjustments>(false);grade.postExposure.overrideState=false;grade.contrast.Override(5);grade.saturation.Override(-5);
   if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(false);tone.mode.Override(TonemappingMode.ACES);
   if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(false);bloom.intensity.Override(.13f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.6f);
   if(!profile.TryGet<Vignette>(out var vignette))vignette=profile.Add<Vignette>(false);vignette.intensity.Override(.10f);vignette.smoothness.Override(.65f);
   foreach(var component in profile.components){if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}
   volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
  }
 }
}
