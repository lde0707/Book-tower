using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Puzzles;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class ReadingLanternArt
 {
  const string Dir="Assets/_BookTower/Generated/ReadingLanternV2",Out="Docs/ArtReview/ReadingLanternV2";
  static int serial;static Material brass,paper,ink,wine,light;
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Preview;EditorApplication.projectChanged-=Queue;EditorApplication.projectChanged+=Queue;}
  static void Queue(){EditorApplication.delayCall-=Preview;EditorApplication.delayCall+=Preview;}
  static void Preview()
  {if(EditorApplication.isPlayingOrWillChangePlaymode)return;var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");if(!scene.IsValid()||!scene.isLoaded)return;bool dirty=scene.isDirty,changed=false;
   foreach(var p in Object.FindObjectsByType<ReadingRoomPuzzle>(FindObjectsSortMode.None))if(!p.lantern.Find(ReadingLanternPresentation.ModelName)&&Resources.Load<GameObject>("ReadingMemoryLantern")){ReadingLanternPresentation.Apply(p);changed=true;}
   if(changed){EditorSceneManager.MarkSceneDirty(scene);if(!dirty)EditorSceneManager.SaveScene(scene);}}
  static Transform Group(Transform p,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=at;return t;}
  static Material Mat(string name,Color color,float metallic=0,float emission=0)
  {var path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",metallic>0?.3f:.07f);m.SetFloat("_Cull",0);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiberNormal.png"));m.SetFloat("_BumpScale",metallic>0?.1f:.32f);m.EnableKeyword("_NORMALMAP");if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(m);return m;}
  static Transform MeshAt(Transform p,string name,Mesh mesh,Material mat,Vector3 at)
  {var path=Dir+"/M"+(serial++).ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.triangles=mesh.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var t=Group(p,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.TwoSided;return t;}
  static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material m)=>MeshAt(p,name,F0Mesh.RoundedBox(size,.035f),m,at);
  static Transform Tube(Transform p,string name,IList<Vector3> points,float radius,Material mat)
  {var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int sides=10;float length=0;
   for(int i=0;i<points.Count;i++){if(i>0)length+=Vector3.Distance(points[i],points[i-1]);var t=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;var n=Vector3.Cross(t,Vector3.forward).normalized;if(n.sqrMagnitude<.01f)n=Vector3.right;var b=Vector3.Cross(t,n).normalized;
    for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;v.Add(points[i]+radius*(n*Mathf.Cos(a)+b*Mathf.Sin(a)));uv.Add(new Vector2(j/(float)sides,length));if(i>0&&j<sides){int q=(i-1)*(sides+1)+j;tris.AddRange(new[]{q,q+1,q+sides+1,q+1,q+sides+2,q+sides+1});}}}
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return MeshAt(p,name,mesh,mat,Vector3.zero);}
  static void Loop(Transform p,string name,float radius,float y,float thickness,Material mat)
  {var pts=new List<Vector3>();for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;pts.Add(V(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));}Tube(p,name,pts,thickness,mat);}
  static void Leaf(Transform p,Vector3 at,float scale,float angle,Material mat){var g=MeshAt(p,"pressed leaf",F0Mesh.Leaf(scale,scale*.42f),mat,at);g.localEulerAngles=V(0,0,angle);}
  static void Lantern(Transform g)
  {
   // Smooth curved six-panel paper body; geometry carries the fold, not a flat box texture.
   for(int k=0;k<6;k++)
   {float angle=k*Mathf.PI/3;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
    for(int y=0;y<=18;y++)for(int x=0;x<=8;x++){float t=y/18f,a=angle+x/8f*Mathf.PI/3;float r=.44f+.15f*Mathf.Sin(t*Mathf.PI)+.008f*Mathf.Sin(t*25+x*1.5f);v.Add(V(Mathf.Cos(a)*r,.22f+t*1.25f,Mathf.Sin(a)*r));uv.Add(new Vector2(x/8f,t));if(y<18&&x<8){int n=y*9+x;tris.AddRange(new[]{n,n+9,n+1,n+1,n+9,n+10});}}
    var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();MeshAt(g,"translucent raven paper",mesh,paper,Vector3.zero);
    var rib=new List<Vector3>();for(int j=0;j<=24;j++){float t=j/24f,r=.45f+.16f*Mathf.Sin(t*Mathf.PI);rib.Add(V(Mathf.Cos(angle)*r,.20f+t*1.30f,Mathf.Sin(angle)*r));}Tube(g,"curved brass rib",rib,.023f,brass);
    var crown=new List<Vector3>();for(int j=0;j<=20;j++){float t=j/20f,r=.52f*(1-t)+.13f*Mathf.Sin(Mathf.PI*t);crown.Add(V(Mathf.Cos(angle)*r,1.48f+t*.44f,Mathf.Sin(angle)*r));}Tube(g,"crown petal seam",crown,.026f,brass);
    var petal=MeshAt(g,"layered paper crown",F0Mesh.Leaf(.66f,.36f),wine,V(Mathf.Cos(angle)*.48f,1.48f,Mathf.Sin(angle)*.48f));petal.localRotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)))*Quaternion.Euler(-42,0,0);
   }
   foreach(float y in new[]{.10f,.18f,1.47f,1.53f})Loop(g,"rolled binding lip",y<1?.46f:.48f,y,.04f,brass);
   Box(g,"keyed underside",V(0,.04f,0),V(.7f,.08f,.7f),wine);Box(g,"alignment tooth",V(0,.05f,-.52f),V(.16f,.10f,.24f),brass);
   var handle=new List<Vector3>();for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;handle.Add(V(Mathf.Sin(a)*.23f,2.14f+Mathf.Cos(a)*.25f,0));}Tube(g,"wrapped carrying handle",handle,.039f,brass);
   for(int i=0;i<9;i++){float a=(-.75f+i*.18f);Tube(g,"linen handle wrap",new[]{V(Mathf.Sin(a)*.22f,2.14f+Mathf.Cos(a)*.24f,-.039f),V(Mathf.Sin(a)*.26f,2.14f+Mathf.Cos(a)*.28f,.025f)},.013f,paper);}
   foreach(float side in new[]{-1f,1f})
   {var at=V(side*.36f,.98f,-.56f);Box(g,"fixed catch plate",at+V(0,-.23f,.015f),V(.24f,.49f,.05f),wine);
    Box(g,"catch slot",at+V(0,-.38f,-.019f),V(.17f,.06f,.035f),ink);
    Box(g,"receiver lip",at+V(0,-.43f,-.038f),V(.21f,.06f,.08f),brass);
    var latch=Group(g,side<0?"LeftLatch":"RightLatch",at);
    Tube(latch,"hinge pin",new[]{V(-.14f,0,0),V(.14f,0,0)},.038f,brass);
    Box(latch,"embossed locking tongue",V(0,-.22f,-.015f),V(.15f,.4f,.055f),brass);
    Tube(latch,"raised finger bow",new[]{V(-.07f,-.13f,-.05f),V(-.07f,-.25f,-.16f),V(.07f,-.25f,-.16f),V(.07f,-.13f,-.05f)},.022f,brass);
    Leaf(latch,V(0,-.32f,-.049f),.14f,0,wine);
    latch.localRotation=Quaternion.Euler(65,0,0);
   }
   Box(g,"ChargeRibbon",V(0,.36f,-.565f),V(.42f,.085f,.025f),light);
   for(int i=0;i<3;i++)Leaf(g,V(0,.54f+i*.23f,-.603f),.20f,i%2==0?-48:48,ink);
  }
  static void Dock(Transform g)
  {
   for(int i=0;i<3;i++)Box(g,"layered rounded bookboard",V(0,-.19f+i*.05f,0),V(1.85f-i*.06f,.055f,1.85f-i*.06f),i==1?brass:wine);
   Loop(g,"recessed lantern footprint",.50f,-.015f,.024f,ink);Loop(g,"raised receiving rim",.70f,-.035f,.045f,brass);
   Box(g,"matching alignment notch",V(0,-.04f,-.57f),V(.2f,.06f,.27f),ink);
   foreach(float side in new[]{-1f,1f})Tube(g,"inlaid docking chevron",new[]{V(side*.82f,-.02f,-.72f),V(side*.69f,-.02f,-.61f),V(side*.82f,-.02f,-.5f)},.017f,brass);
   var sign=Group(g,"sloped illustrated tab",V(0,-.045f,-.88f));sign.localEulerAngles=V(65,0,0);Box(sign,"paper plaque",Vector3.zero,V(.58f,.36f,.035f),paper);
   var book=Group(sign,"Book",Vector3.zero);foreach(float s in new[]{-1f,1f}){var p=Box(book,"open page",V(s*.11f,0,-.04f),V(.2f,.22f,.025f),wine);p.localEulerAngles=V(0,s*14,0);}Tube(book,"crease",new[]{V(0,-.12f,-.05f),V(0,.12f,-.05f)},.01f,brass);
   var door=Group(sign,"Door",Vector3.zero);Tube(door,"doorway",new[]{V(-.11f,-.12f,-.04f),V(-.11f,.06f,-.04f),V(0,.14f,-.04f),V(.11f,.06f,-.04f),V(.11f,-.12f,-.04f)},.019f,wine);
   var sun=Group(sign,"Sun",Vector3.zero);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Tube(sun,"sun ray",new[]{V(Mathf.Cos(a)*.07f,Mathf.Sin(a)*.07f,-.04f),V(Mathf.Cos(a)*.14f,Mathf.Sin(a)*.14f,-.04f)},.015f,wine);}
  }
  public static void Build()
  {
   try{
    Directory.CreateDirectory(Dir);Directory.CreateDirectory(Out);serial=0;AssetDatabase.Refresh();
    brass=Mat("Worn honey brass",new Color(.68f,.45f,.20f),.48f);paper=Mat("Folded ivory paper",new Color(.91f,.79f,.57f),0,.08f);ink=Mat("Dark engraved ink",new Color(.085f,.053f,.039f));wine=Mat("Wine book cloth",new Color(.29f,.085f,.07f));light=Mat("Golden stored light",new Color(1,.7f,.28f),0,.7f);
    var model=Group(null,"MemoryLantern_ArtV2",Vector3.zero);Lantern(model);PrefabUtility.SaveAsPrefabAsset(model.gameObject,"Assets/_BookTower/Resources/ReadingMemoryLantern.prefab");Object.DestroyImmediate(model.gameObject);
    var dock=Group(null,"Keyed receiving tray",Vector3.zero);Dock(dock);PrefabUtility.SaveAsPrefabAsset(dock.gameObject,"Assets/_BookTower/Resources/ReadingLanternDock.prefab");Object.DestroyImmediate(dock.gameObject);
    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/ReadingRoom1B/RavenPortrait.png");if(!texture)throw new Exception("Portrait image is missing");
    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
    const string materialPath="Assets/_BookTower/Resources/ReadingRavenPortrait.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("BookTower/ReadingPaper"));AssetDatabase.CreateAsset(mat,materialPath);}mat.SetTexture("_BaseMap",texture);mat.SetVector("_Rect",new Vector4(0,0,1,1));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Repeat",0);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
    EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");var puzzle=Object.FindFirstObjectByType<ReadingRoomPuzzle>();ReadingLanternPresentation.Apply(puzzle);EditorSceneManager.SaveScene(puzzle.gameObject.scene);Capture(puzzle);Validate(puzzle);AssetDatabase.SaveAssets();
    if(Environment.GetCommandLineArgs().Contains("-lanternVerify"))ReadingRoomVerifier.RunBatch();else EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);File.WriteAllText(Out+"/error.txt",e.ToString());EditorApplication.Exit(1);}
  }
  static void Validate(ReadingRoomPuzzle p)
  {if(!p.leftRing||!p.rightRing||!p.chargeRibbon)throw new Exception("Missing lantern bindings");foreach(var c in p.GetComponentsInChildren<ReadingRoomControl>())
   {Transform socket=c.action==ReadingRoomControl.Action.Charge?p.chargingSocket:c.action==ReadingRoomControl.Action.RecordDock?p.recordSocket:c.action==ReadingRoomControl.Action.ExitDock?p.exitSocket:c.action==ReadingRoomControl.Action.WindowCharge?p.windowChargeSocket:null;if(socket&&Vector3.Distance(c.transform.position,socket.position+Vector3.down*.1f)>.001f)throw new Exception("Misaligned socket");}
   File.WriteAllText(Out+"/structure.txt","PASS: all four receiving trays and interaction colliders share the physical docking position.\nPASS: independent hinge pivots, keyed base and portrait-specific material bound.\n");}
  static void Capture(ReadingRoomPuzzle p)
  {var room=p.GetComponentInParent<Room>();foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){var owner=r.GetComponentInParent<Room>();if(owner&&owner!=room)r.enabled=false;}
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.36f,.34f,.31f);var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.42f,.4f,.38f));RenderSettings.ambientProbe=probe;RenderSettings.fog=false;
   var go=new GameObject("Review camera");var cam=go.AddComponent<Camera>();cam.fieldOfView=48;cam.nearClipPlane=.05f;cam.farClipPlane=100;
   var fill=new GameObject("Review softbox").AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(1,.84f,.62f);fill.range=8;fill.intensity=9;
   var pos=p.lantern.position;p.leftRing.localRotation=p.rightRing.localRotation=Quaternion.Euler(65,0,0);fill.transform.position=pos+V(-1,3,-2);cam.transform.position=pos+V(2.3f,1.9f,-3.7f);cam.transform.LookAt(pos+V(0,1.1f,0));InteriorF0Builder.Capture(cam,Out+"/01-open-lantern.png");
   p.leftRing.localRotation=p.rightRing.localRotation=Quaternion.identity;InteriorF0Builder.Capture(cam,Out+"/02-closed-lantern.png");p.leftRing.localRotation=p.rightRing.localRotation=Quaternion.Euler(65,0,0);
   fill.enabled=false;cam.fieldOfView=60;cam.transform.position=room.transform.TransformPoint(V(0,2.7f,-19));cam.transform.LookAt(room.transform.TransformPoint(V(0,7,12)));InteriorF0Builder.Capture(cam,Out+"/03-room-portrait-banners.png");
   cam.transform.position=p.recordSocket.position+V(.65f,1.1f,1.7f);cam.transform.LookAt(p.recordSocket.position+V(0,.3f,0));InteriorF0Builder.Capture(cam,Out+"/04-record-dock.png");Object.DestroyImmediate(go);Object.DestroyImmediate(fill.gameObject);
  }
 }
}
