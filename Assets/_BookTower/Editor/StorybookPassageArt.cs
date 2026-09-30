using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class StorybookPassageArt
 {
  const string Dir="Assets/_BookTower/Generated/StorybookReadingPassage",Prefab="Assets/_BookTower/Resources/StorybookReadingPassage.prefab",Review="Docs/ArtReview/StorybookReadingPassage";
  static int serial;static Material paper,gold,wine,teal,navy,lightPaper;static readonly List<Vector3> checkpoints=new();
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  [InitializeOnLoadMethod]static void Init(){EditorApplication.projectChanged-=Queue;EditorApplication.projectChanged+=Queue;EditorApplication.playModeStateChanged-=Mode;EditorApplication.playModeStateChanged+=Mode;Queue();}
  static void Mode(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode)Queue();}
  static void Queue(){EditorApplication.delayCall-=Preview;EditorApplication.delayCall+=Preview;}
  static void Preview(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");if(!scene.IsValid()||!scene.isLoaded||scene.GetRootGameObjects().Any(g=>g.name==StorybookReadingPassage.RootName))return;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(!prefab)return;bool dirty=scene.isDirty;PrefabUtility.InstantiatePrefab(prefab,scene);StorybookReadingPassage.ClearArrivalRails();EditorSceneManager.MarkSceneDirty(scene);if(!dirty)EditorSceneManager.SaveScene(scene);}
  static Transform Group(Transform p,string name,Vector3 position){var g=new GameObject(name).transform;g.SetParent(p,false);g.localPosition=position;return g;}
  static Material Mat(string name,Color color,float emission=0)
  {string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.09f);m.SetFloat("_Cull",0);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiberNormal.png"));m.SetFloat("_BumpScale",.32f);m.EnableKeyword("_NORMALMAP");if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(m);return m;}
  static Transform MeshAt(Transform p,string name,Mesh mesh,Material mat,Vector3 at)
  {var g=Group(p,name,at);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
  static Transform Tube(Transform p,string name,IList<Vector3> points,float radius,Material mat)
  {
   var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();float length=0;const int sides=10;
   for(int i=0;i<points.Count;i++){if(i>0)length+=Vector3.Distance(points[i],points[i-1]);var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;var normal=Vector3.Cross(tangent,Vector3.forward).normalized;if(normal.sqrMagnitude<.01f)normal=Vector3.up;var binormal=Vector3.Cross(tangent,normal).normalized;
    for(int j=0;j<=sides;j++){float angle=j*Mathf.PI*2/sides;v.Add(points[i]+radius*(normal*Mathf.Cos(angle)+binormal*Mathf.Sin(angle)));uv.Add(new Vector2((float)j/sides,length));if(i>0&&j<sides){int q=(i-1)*(sides+1)+j;tri.AddRange(new[]{q,q+1,q+sides+1,q+1,q+sides+2,q+sides+1});}}}
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();return MeshAt(p,name,mesh,mat,Vector3.zero);
  }
  static Transform Page(Transform p,string name,Vector3 at,float w,float h,Material mat,Vector3 rotation)
  {var g=MeshAt(p,name,F0Mesh.Paper(w,h,serial++),mat,at);g.localEulerAngles=rotation;return g;}
  static void Sprig(Transform p,Vector3 at,float height,Material mat)
  {var g=Group(p,"눌러 붙인 잎 문양",at);Tube(g,"휘어진 줄기",new[]{V(0,0,0),V(.07f,height*.5f,-.03f),V(0,height,0)},.017f,mat);for(int i=0;i<4;i++)foreach(float s in new[]{-1f,1f}){var leaf=MeshAt(g,"접힌 종이 잎",F0Mesh.Leaf(height*.23f,height*.11f),mat,V(s*.015f,height*(.12f+i*.20f),-.025f));leaf.localEulerAngles=V(0,0,s*48);}}
  static void Book(Transform p,Vector3 at,int index,Material color)
  {float h=.95f+(index%3)*.19f;var g=Group(p,"둥근 책등과 겹친 페이지",at);g.localEulerAngles=V(0,0,index%2==0?-4:3);MeshAt(g,"볼록한 책등",F0Mesh.Spine(.27f,h,.08f),color,Vector3.zero);for(int k=0;k<4;k++)Tube(g,"제본 가로 주름",new[]{V(-.14f,.1f+k*(h-.2f)/3,-.09f),V(0,.1f+k*(h-.2f)/3,-.115f),V(.14f,.1f+k*(h-.2f)/3,-.09f)},.018f,gold);}
  static void Panel(Transform parent,float side,float x,float z,int index,float progress,bool approach)
  {
   var p=Group(parent,"겹친 동화책 벽감",V(side*x,.6f,z));p.localEulerAngles=V(0,side*90,0);
   p.localScale=Vector3.one*(approach?.72f:.90f);
   Page(p,"뜯긴 장지 가장자리",V(0,0,0),2.5f,3.65f,paper,Vector3.zero);
   Page(p,"물든 안쪽 페이지",V(0,.13f,-.075f),2.23f,3.35f,progress<.45f?teal:navy,Vector3.zero);
   var arch=new List<Vector3>{V(-1.03f,.2f,-.15f),V(-1.03f,2.45f,-.15f)};
   for(int i=0;i<=24;i++){float a=Mathf.PI-i*Mathf.PI/24;arch.Add(V(Mathf.Cos(a)*1.03f,2.45f+Mathf.Sin(a)*.8f,-.15f));}arch.Add(V(1.03f,.2f,-.15f));Tube(p,"가늘게 말린 종이 테두리",arch,.05f,gold);
   Sprig(p,V(-.74f,.7f,-.16f),1.35f,progress<.5f?paper:gold);Sprig(p,V(.64f,1.0f,-.16f),1.1f,paper);
   if(index%2==0){var scroll=new List<Vector3>();for(int j=0;j<=24;j++){float a=j*Mathf.PI*1.55f/24;scroll.Add(V(Mathf.Sin(a)*.45f,2.25f+Mathf.Cos(a)*.45f,-.18f));}Tube(p,"이야기의 나선 문양",scroll,.037f,gold);}
   else {var crescent=new List<Vector3>();for(int j=0;j<=28;j++){float a=j*Mathf.PI*1.65f/28;crescent.Add(V(Mathf.Cos(a)*.38f,2.55f+Mathf.Sin(a)*.38f,-.20f));}Tube(p,"열람실의 초승달 인장",crescent,.07f,paper);}
   var shelf=MeshAt(p,"완만하게 휜 책받침",F0Mesh.RoundedBox(V(2.2f,.13f,.48f),.06f),wine,V(0,.32f,-.26f));
   for(int i=0;i<5;i++)Book(p,V(-.65f+i*.32f,.40f,-.4f),i,i%2==0?wine:teal);
  }
  static void Lantern(Transform p,float y,float z)
  {
   var g=Group(p,"꽃봉오리 종이등",V(0,y,z));Tube(g,"종이등 실",new[]{V(0,.2f,0),V(0,1.05f,0)},.02f,gold);
   for(int k=0;k<6;k++)
   {float a=Mathf.PI*2*k/6;var outline=new List<Vector3>();for(int j=0;j<=20;j++){float t=j/20f;float radius=.1f+.32f*Mathf.Sin(Mathf.PI*t);outline.Add(V(Mathf.Cos(a)*radius,.65f-t*1.0f,Mathf.Sin(a)*radius));}Tube(g,"접힌 등살",outline,.025f,gold);
    var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();for(int j=0;j<=12;j++)for(int i=0;i<2;i++){float t=j/12f,angle=a+i*Mathf.PI/3,rad=.1f+.32f*Mathf.Sin(Mathf.PI*t);v.Add(V(Mathf.Cos(angle)*rad,.65f-t,Mathf.Sin(angle)*rad));uv.Add(new Vector2(i,t));if(j<12&&i==0){int n=j*2;tri.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}}var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();MeshAt(g,"빛을 머금은 굽은 장지",mesh,lightPaper,Vector3.zero);}
   var l=g.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.79f,.51f);l.intensity=2.6f;l.range=6;l.shadows=LightShadows.None;
  }
  static void Merge(Transform root)
  {
   var source=root.GetComponentsInChildren<MeshFilter>();
   foreach(var group in source.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
   {var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=root.name+" paper layers"};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();if(mesh.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new Exception("Invalid geometry");string path=Dir+"/Part"+(serial++).ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.triangles=mesh.triangles;old.RecalculateBounds();EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);MeshAt(root,"병합된 종이 장식",mesh,group.Key,Vector3.zero);}
   foreach(var f in source){var mesh=f.sharedMesh;Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);if(!AssetDatabase.Contains(mesh))Object.DestroyImmediate(mesh);}
  }
  static Transform Segment(Transform parent,Vector3 a,Vector3 b,float width,bool approach)
  {
   var g=Group(parent,approach?"열람실 앞 굽은 회랑":"동화에서 기억으로 이어지는 회랑",(a+b)/2);g.rotation=Quaternion.LookRotation(b-a);float len=Vector3.Distance(a,b),r=width*.5f-.20f,spring=approach?2.25f:3.25f;
   int bays=Mathf.Max(2,Mathf.CeilToInt(len/4.5f));float step=len/bays;
   for(int n=0;n<bays;n++)
   {
    float z=-len/2+n*step,t=approach?.9f:(float)n/Mathf.Max(1,bays-1);var color=Mat((approach?"Approach":"Ramp")+n,Color.Lerp(new Color(.68f,.58f,.40f),new Color(.22f,.29f,.34f),t));
    var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
    for(int j=0;j<=40;j++)for(int k=0;k<2;k++){float angle=j*Mathf.PI/40;v.Add(V(Mathf.Cos(angle)*r,spring+Mathf.Sin(angle)*r,z+k*(step+.02f)));uv.Add(new Vector2(angle*r*.4f,(z+k*step)*.4f));if(j<40&&k==0){int q=j*2;tri.AddRange(new[]{q,q+1,q+2,q+1,q+3,q+2});}}
    var roof=new Mesh{name="Curled paper canopy"};roof.SetVertices(v);roof.SetUVs(0,uv);roof.SetTriangles(tri,0);roof.RecalculateNormals();MeshAt(g,"말려 올라간 종이 천장",roof,color,Vector3.zero);
    foreach(float s in new[]{-1f,1f})
    {Page(g,"주름진 연속 종이 벽",V(s*(r+.055f),0,z+step/2),step+.09f,spring+.22f,color,V(0,s*90,0));Panel(g,s,r-.18f,z+step*.52f,n,t,approach);}
    for(int layer=0;layer<3;layer++)
    {float radius=r-.045f-layer*.045f;var arch=new List<Vector3>{V(-radius,.03f,z+.14f+layer*.10f),V(-radius,spring,z+.14f+layer*.10f)};for(int j=0;j<=48;j++){float angle=Mathf.PI-j*Mathf.PI/48;arch.Add(V(Mathf.Cos(angle)*radius,spring+Mathf.Sin(angle)*radius,z+.14f+layer*.10f));}arch.Add(V(radius,.03f,z+.14f+layer*.10f));Tube(g,"여러 장을 말아 만든 입체 아치",arch,layer==0?.09f:.045f,layer==1?gold:paper);}
    float floor=approach?.11f:.34f;
    Page(g,"가장자리가 뜯긴 책갈피 길",V(0,floor,z),2.3f,step+.035f,t<.45f?teal:wine,V(90,0,0));
    for(float dz=.2f;dz<step-.1f;dz+=.42f)foreach(float s in new[]{-1f,1f})Tube(g,"책갈피 바느질",new[]{V(s*1.06f,floor+.05f,z+dz),V(s*1.16f,floor+.05f,z+dz+.12f)},.012f,gold);
    var motif=Group(g,"발밑 안내 잎",V(0,floor+.06f,z+step*.4f));motif.localEulerAngles=V(90,0,0);Sprig(motif,Vector3.zero,.65f,paper);
    if(n%2==0)Lantern(g,spring+r-1.0f,z+step*.6f);
   }
   Merge(g);for(int i=0;i<=20;i++)checkpoints.Add(Vector3.Lerp(a,b,i/20f));return g;
  }
  public static void Run()
  {try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/storybook-passage-check.txt",e.ToString());EditorApplication.Exit(1);}}
  static void Build()
  {
   Directory.CreateDirectory(Dir);Directory.CreateDirectory(Review);EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");serial=0;checkpoints.Clear();StorybookReadingPassage.ClearArrivalRails();var previous=GameObject.Find(StorybookReadingPassage.RootName);if(previous)Object.DestroyImmediate(previous);
   paper=Mat("Ivory paper",new Color(.89f,.79f,.60f));gold=Mat("Dull gold ink",new Color(.65f,.43f,.19f));wine=Mat("Faded wine bookmark",new Color(.39f,.15f,.17f));teal=Mat("Crayon teal",new Color(.23f,.48f,.43f));navy=Mat("Reading indigo",new Color(.17f,.24f,.33f));lightPaper=Mat("Warm lantern paper",new Color(1,.73f,.38f),.6f);
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="1A"&&l.ToRoomId=="1B");var so=new SerializedObject(link);var a=so.FindProperty("fromPoint").vector3Value;var b=so.FindProperty("toPoint").vector3Value;float width=link.transform.lossyScale.x;
   var root=Group(null,StorybookReadingPassage.RootName,Vector3.zero);var first=Segment(root,a,b,width,false);var last=Segment(root,V(142,8,-132),V(160,8,-122),7,true);
   var bound=root.gameObject.AddComponent<RoomBound>();var rb=new SerializedObject(bound);var ids=rb.FindProperty("roomIds");ids.arraySize=2;ids.GetArrayElementAtIndex(0).stringValue="1A";ids.GetArrayElementAtIndex(1).stringValue="1B";rb.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefab);AssetDatabase.SaveAssets();StorybookReadingPassage.ClearArrivalRails();Physics.SyncTransforms();
   int rays=0;foreach(var point in checkpoints){if(!Physics.Raycast(point+Vector3.up*1.5f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))throw new Exception("Missing floor "+point);if(!Physics.Raycast(point+Vector3.up*2,Vector3.up,10,~0,QueryTriggerInteraction.Ignore))throw new Exception("Missing structural roof "+point);rays+=2;}
   var cam=new GameObject("Passage art review").AddComponent<Camera>();cam.fieldOfView=66;cam.nearClipPlane=.08f;
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))foreach(var renderer in room.GetComponentsInChildren<Renderer>())renderer.enabled=room.RoomId=="1A"||room.RoomId=="1B";
   var frames=new[]{(a+Vector3.up*1.65f+(b-a).normalized*2,b+Vector3.up*2.7f,"01-storybook-entry"),(b+Vector3.up*1.7f-(b-a).normalized*2,a+Vector3.up*2.8f,"02-reading-return"),(V(143,9.7f,-131),V(158,11,-123),"03-reading-approach")};
   foreach(var shot in frames){cam.transform.position=shot.Item1;cam.transform.LookAt(shot.Item2);InteriorF0Builder.Capture(cam,Review+"/"+shot.Item3+".png");if(shot.Item3=="03-reading-approach"){string audit="";foreach(float x in new[]{.55f,.65f,.75f,.85f})if(Physics.Raycast(cam.ViewportPointToRay(V(x,.5f,0)),out var hit,30))audit+=$"{x}: {hit.collider.name} parent={hit.collider.transform.parent?.name} at={hit.point} size={hit.collider.bounds.size}\n";File.WriteAllText("Logs/passage-obstructions.txt",audit);}}Object.DestroyImmediate(cam.gameObject);
   File.WriteAllText("Logs/storybook-passage-check.txt",$"PASS: A={a}, B={b}, width={width}; ramp plus reading-room dogleg dressed. {rays} floor/structural-roof rays. {root.GetComponentsInChildren<MeshRenderer>().Length} merged renderers, no added obstacle colliders.\n");
  }
 }
}
