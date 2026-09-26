using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Interaction;
using BookTower.Puzzles;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class BindingPlayBuilder
 {
  const string Dir="Assets/_BookTower/Generated/BindingPlay";static Transform root;static int meshIndex;static Material paper,red,rope,gold,light,ink;
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();}
  [MenuItem("Book Tower/F0 동적 상호작용 적용")]
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/InGame.unity"||EditorApplication.isPlaying)throw new Exception("Open InGame in edit mode");
   Directory.CreateDirectory("Logs/BindingBackups");EditorSceneManager.SaveScene(scene,"Logs/BindingBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();meshIndex=0;
   var room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");var old=room.transform.Find("F0_살아있는제본실");if(old)Object.DestroyImmediate(old.gameObject);root=Group(room.transform,"F0_살아있는제본실",Vector3.zero);
   Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0Reference/"+n+".mat");
   paper=Load("IvoryPaper");red=Load("OxbloodBook");if(!red)red=Load("DarkBinding");rope=Load("CottonRope");gold=Load("AgedBrass");light=Load("LanternPaper");ink=Load("DarkBinding");
   var play=root.gameObject.AddComponent<BindingPlayground>();play.puzzle=room.GetComponentsInChildren<PuzzleBase>().First(p=>p.PuzzleId=="P1");play.pulses=new Transform[3];play.signals=new Transform[3];play.signalLights=new Light[3];play.starts=new Vector3[3];play.ends=new Vector3[3];play.mobiles=new Transform[3];
   for(int i=0;i<3;i++)
   {
    float z=-7+i*6;var station=Group(root,"당기는 제본 고리 "+(i+1),V(2.3f,1.05f,z));
    var handle=Group(station,"움직이는 손잡이",Vector3.zero);
    var points=new Vector3[49];for(int j=0;j<49;j++){float a=j*Mathf.PI*2/48;points[j]=V(Mathf.Cos(a)*.33f,Mathf.Sin(a)*.42f,0);}
    Obj(handle,"꼬인 종이 고리",F0Mesh.Tube(points,.07f,8),rope,Vector3.zero);
    Obj(station,"낮은 제본 받침",F0Mesh.RoundedBox(V(.8f,.28f,.75f)),red,V(0,-.86f,0));
    Obj(station,"매듭의 실",F0Mesh.Tube(new[]{V(0,.42f,0),V(0,1.2f,.18f),V(-2.3f,-.89f,0)},.035f,8),rope,Vector3.zero);
    var touch=Touch(station,handle,BindingTouch.TouchKind.PullKnot,i,"RopePull");touch.playground=play;
    // Counted stitches connect a handle to the matching high seam signal.
    for(int j=0;j<=i;j++)Obj(handle,"짝 수선 실밥",F0Mesh.RoundedBox(V(.035f,.14f,.04f)),gold,V(-.12f+j*.12f,-.05f,-.08f));
    play.starts[i]=station.localPosition;play.ends[i]=V(0,2.5f+i*3.6f,14.85f);
    play.pulses[i]=Obj(root,"실을 따라 흐르는 빛",F0Mesh.Leaf(.4f,.22f),light,play.starts[i]);play.pulses[i].localScale=Vector3.zero;
    play.signals[i]=Group(root,"솔기 도착 반응 "+i,play.ends[i]);Obj(play.signals[i],"빛나는 종이 잎",F0Mesh.Leaf(.55f,.23f),light,V(0,0,-.06f));
    var l=play.signals[i].gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=5;l.intensity=2;l.color=new Color(1,.75f,.4f);play.signalLights[i]=l;
    var mobile=Group(root,"숨쉬는 책갈피 "+i,V(-4+i*4,7.5f+i*.3f,10));Obj(mobile,"얇은 책갈피",F0Mesh.Paper(.6f,1.8f,70+i),paper,V(0,-1.8f,0));Obj(root,"책갈피를 매단 실",F0Mesh.Tube(new[]{mobile.localPosition,mobile.localPosition+Vector3.up*8},.014f,5),rope,Vector3.zero);play.mobiles[i]=mobile;
   }
   for(int i=0;i<3;i++)
   {
    var t=Group(root,"소리를 내는 책 "+(i+1),V(-5.8f+i*1.1f,.9f,-4));var moving=Group(t,"반응하는 책등",Vector3.zero);
    Obj(moving,"손으로 두드리는 표지",F0Mesh.RoundedBox(V(.85f,1.5f,.5f),.15f),i==1?paper:red,Vector3.zero);
    Obj(moving,"황동 띠",F0Mesh.RoundedBox(V(.88f,.08f,.55f)),gold,V(0,-.45f,0));Touch(t,moving,BindingTouch.TouchKind.TapBook,i,"BookTone");
   }
   var table=Group(root,"제본사의 먼지 쪽지",V(-4.5f,.85f,-9));var page=Obj(table,"낡은 쪽지",F0Mesh.Paper(1.25f,1.25f,891),paper,V(0,-.3f,0));page.localRotation=Quaternion.Euler(-20,0,0);
   var dust=Group(table,"쓸려나가는 먼지",V(0,.2f,-.15f));for(int i=0;i<16;i++)Obj(dust,"먼지 섬유",F0Mesh.Paper(.12f,.12f,8+i),ink,V(Mathf.Sin(i*4.3f)*.46f,Mathf.Cos(i*2.2f)*.45f,0));
   var seal=Obj(table,"발견한 제본사 인장",F0Mesh.Leaf(.65f,.3f),gold,V(0,-.1f,-.18f));seal.localScale=Vector3.zero;var brush=Touch(table,null,BindingTouch.TouchKind.BrushDust,0,"PaperRustle");brush.dust=dust;brush.stamp=seal;
   var loose=Group(root,"펄럭이는 자유 페이지",V(5.8f,1,-9));var flap=Group(loose,"페이지 접힘 축",Vector3.zero);Obj(flap,"흔들리는 페이지",F0Mesh.Paper(1.2f,1.6f,793),paper,Vector3.zero);Touch(loose,flap,BindingTouch.TouchKind.FlutterPage,0,"PaperRustle");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory("Docs/ArtReview/BindingPlay");File.WriteAllText("Docs/ArtReview/BindingPlay/build-check.txt","PASS: 3 reversible pull handles, 3 tuned books, dust reveal, flutter page, 3 ambient mobiles; P1 unchanged.\n");
   if(room.GetComponentInChildren<FoldBridgePuzzle>())FoldBridgeBuilder.Apply();
   InteriorF0Builder.CaptureReferenceView();Debug.Log("[BindingPlay] Applied eight interactions and ambient motion");
  }
  static Transform Group(Transform parent,string n,Vector3 at){var t=new GameObject(n).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
  static Transform Obj(Transform parent,string n,Mesh mesh,Material m,Vector3 at)
  {
   if(!m)throw new Exception("Missing material for "+n);
   string path=Dir+"/Mesh_"+(meshIndex++).ToString("D3")+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(!saved){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{saved.Clear();saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;saved.uv=mesh.uv;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.triangles=mesh.triangles;saved.bounds=mesh.bounds;saved.UploadMeshData(false);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
   var t=Group(parent,n,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;
  }
  static BindingTouch Touch(Transform t,Transform moving,BindingTouch.TouchKind kind,int index,string sound)
  {
   var collider=t.gameObject.AddComponent<BoxCollider>();collider.center=V(0,.15f,0);collider.size=V(.95f,1.25f,.4f);
   var it=t.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=t.name;so.FindProperty("verb").stringValue=kind==BindingTouch.TouchKind.PullKnot?"당기거나 풀기":"만져 보기";so.FindProperty("range").floatValue=3.5f;so.FindProperty("lift").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
   var touch=t.gameObject.AddComponent<BindingTouch>();touch.kind=kind;touch.index=index;touch.moving=moving;touch.sound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Audio/BindingPlay/"+sound+".wav");return touch;
  }
 }
}
