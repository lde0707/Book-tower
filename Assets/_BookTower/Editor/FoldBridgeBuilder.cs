using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.Interaction;
using BookTower.Info;
using BookTower.PlayerSystem;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class FoldBridgeBuilder
 {
  const string Dir="Assets/_BookTower/Generated/FoldBridge";static int index;
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  static Transform G(Transform p,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=at;return t;}
  static Transform MeshObject(Transform p,string name,Mesh mesh,Material mat,Vector3 at){string path=Dir+"/Mesh_"+(index++)+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);var t=G(p,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
  static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool solid=true){var t=MeshObject(p,name,F0Mesh.RoundedBox(size,.06f),mat,at);if(solid)t.gameObject.AddComponent<BoxCollider>().size=size;return t;}
  static Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0Reference/"+name+".mat");
  static LineRenderer Line(Transform p,string name,Material mat,float width,params Vector3[] points){var t=G(p,name,Vector3.zero);var l=t.gameObject.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.widthMultiplier=width;l.positionCount=points.Length;l.useWorldSpace=true;l.SetPositions(points.Select(v=>p.TransformPoint(v)).ToArray());return l;}
  static void Control(Transform root,FoldBridgePuzzle puzzle,string name,Vector3 at,bool seal){var t=G(root,name,at);var body=Box(t,"접는 제본 손잡이",Vector3.zero,V(.8f,.8f,.4f),M("AgedBrass"));body.gameObject.GetComponent<Collider>().enabled=false;var c=t.gameObject.AddComponent<BoxCollider>();c.size=V(1,1,.6f);var it=t.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("range").floatValue=3;so.FindProperty("lift").floatValue=0;so.FindProperty("label").stringValue=name;so.FindProperty("verb").stringValue=seal?"빛을 고정한다":"페이지를 접는다";so.ApplyModifiedPropertiesWithoutUndo();var ctrl=t.gameObject.AddComponent<FoldBridgeControl>();ctrl.puzzle=puzzle;ctrl.seal=seal;if(seal)puzzle.sealVisual=body;}
  [MenuItem("Book Tower/F0 접는 다리 퍼즐 적용")]
  public static void Apply()
  {
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();index=0;
   var room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");var art=room.transform.Find(InteriorF0Builder.RootName);
   var previous=art.Find("접는 다리와 빛");if(previous)Object.DestroyImmediate(previous.gameObject);
   var root=G(art,"접는 다리와 빛",Vector3.zero);
   var old=room.GetComponentsInChildren<PuzzleBase>(true).First(p=>p.PuzzleId=="P1");var puzzle=old as FoldBridgePuzzle;
   if(!puzzle){puzzle=old.gameObject.AddComponent<FoldBridgePuzzle>();var so=new SerializedObject(puzzle);so.FindProperty("puzzleId").stringValue="P1";so.FindProperty("targetSeconds").floatValue=180;so.FindProperty("activateRadius").floatValue=70;so.ApplyModifiedPropertiesWithoutUndo();
    foreach(var component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(!component||component==old)continue;var data=new SerializedObject(component);var prop=data.GetIterator();bool changed=false;while(prop.Next(true))if(prop.propertyType==SerializedPropertyType.ObjectReference&&prop.objectReferenceValue==old){prop.objectReferenceValue=puzzle;changed=true;}if(changed)data.ApplyModifiedPropertiesWithoutUndo();}Object.DestroyImmediate(old);}
   foreach(var p in room.GetComponentsInChildren<CollectiblePiece>(true))if(p.PieceId=="P1_a"||p.PieceId=="P1_b")p.gameObject.SetActive(false);
   foreach(var spot in room.GetComponentsInChildren<AssemblyPoint>(true))spot.gameObject.SetActive(false);
   // The retained P1 object owns the replacement component; hide old assembly visuals only.
   puzzle.gameObject.SetActive(true);puzzle.roomSpace=room.transform;
   var paper=M("IvoryPaper");var cover=M("OxbloodBook");var edges=M("PaperEdges");if(!edges)edges=paper;var glow=M("LanternPaper");
   Box(root,"시작 책장 대지",V(0,1.5f,-10.6f),V(32,3,18.8f),edges);
   Box(root,"건너편 책장 대지",V(0,1.5f,9.4f),V(32,3,4.4f),edges);
   for(int i=0;i<6;i++)Box(root,"문으로 내려가는 책장 계단",V(0,(2.5f-i*.5f)/2,11.8f+i*.5f),V(11.4f,Mathf.Max(.04f,2.5f-i*.5f),.54f),paper);
   // A continuous, shallow ramp avoids steps higher than the controller's step offset.
   var ramp=Box(root,"문 앞 접힌 경사면",V(0,1.5f,13.2f),V(11.4f,.16f,4.6f),cover);ramp.localRotation=Quaternion.Euler(42,0,0);
   Box(root,"책장 사이 어두운 골",V(0,.09f,3),V(31.8f,.1f,8.2f),M("DarkBinding"),false);
   foreach(float z in new[]{-1.25f,7.25f})foreach(int side in new[]{-1,1})Box(root,"골 가장자리 제본 턱",V(side*9.2f,3.25f,z),V(13.6f,.5f,.23f),cover);
   puzzle.hinge=G(root,"실제 접히는 페이지",V(0,3.04f,-1.2f));var page=Box(puzzle.hinge,"밟는 종이 다리",V(0,-.10f,4.2f),V(4.4f,.2f,8.4f),paper);puzzle.pageCollider=page.GetComponent<BoxCollider>();
   var skin=MeshObject(puzzle.hinge,"거칠게 뜯긴 장지 표면",F0Mesh.Paper(4.35f,8.35f,734),paper,V(0,.07f,0));skin.localRotation=Quaternion.Euler(90,0,0);
   for(int i=0;i<8;i++)foreach(int side in new[]{-1,1})Box(puzzle.hinge,"가장자리 제본 실밥",V(side*2,.1f,.4f+i),V(.22f,.045f,.045f),M("CottonRope"),false);
   puzzle.hinge.localRotation=Quaternion.Euler(-90,0,0);
   puzzle.source=G(root,"위에서 내려오는 빛",V(0,12.2f,3));Box(puzzle.source,"종이 광원",Vector3.zero,V(1,.6f,1),glow,false);
   var lamp=puzzle.source.gameObject.AddComponent<Light>();lamp.type=LightType.Spot;lamp.spotAngle=20;lamp.range=15;lamp.intensity=80;lamp.color=new Color(1,.8f,.4f);lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.shadows=LightShadows.Soft;
   puzzle.receiver=G(root,"골 아래 빛 받침",V(0,.35f,3));Box(puzzle.receiver,"빛을 받는 인장",Vector3.zero,V(1.8f,.3f,1.8f),M("AgedBrass"));puzzle.receiverLight=puzzle.receiver.gameObject.AddComponent<Light>();puzzle.receiverLight.range=7;puzzle.receiverLight.color=new Color(1,.8f,.3f);
   puzzle.beam=Line(root,"페이지에 막히는 빛줄기",glow,.07f,V(0,12,3),V(0,.5f,3));
   puzzle.connection=Line(root,"받침에서 걸쇠까지 빛",glow,.06f,V(0,.55f,3),V(2.8f,.55f,3),V(2.8f,3.4f,8.8f),V(0,4,10.3f));
   Control(root,puzzle,"입구 제본 손잡이",V(3.2f,4,-3),false);Control(root,puzzle,"반대편 제본 손잡이",V(3.2f,4,9),false);Control(root,puzzle,"빛으로 여는 걸쇠",V(0,4,10.3f),true);
   // Matching physical threads make both handles visibly operate the same hinge.
   Line(root,"두 손잡이를 잇는 제본실",M("CottonRope"),.065f,V(3.2f,4,-3),V(3.2f,3.25f,-1.2f),V(2.3f,3.25f,-1.2f),V(3.2f,3.25f,7.5f),V(3.2f,4,9));
   puzzle.foldSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Audio/BindingPlay/PaperRustle.wav");
   var toys=room.transform.Find("F0_살아있는제본실");if(toys){foreach(Transform t in toys){if(t.name.StartsWith("당기는 제본")||t.name.StartsWith("솔기 도착")||t.name.StartsWith("실을 따라"))t.gameObject.SetActive(false);if(t.GetComponent<BindingTouch>()&&t.GetComponent<BindingTouch>().kind!=BindingTouch.TouchKind.PullKnot){var p=t.localPosition;p.y=p.y%3+3;t.localPosition=p;}}}
   var player=Object.FindFirstObjectByType<PlayerController>();if(player)player.transform.position=room.transform.TransformPoint(V(-5.5f,3.15f,-10.5f));
   AddLightRouting(root,puzzle);
   var sr=new SerializedObject(room);sr.FindProperty("goal").stringValue="종이 덮개로 빛을 깨우고, 은박 책장으로 다리를 우회해 문양에 빛을 보낸다.";sr.ApplyModifiedPropertiesWithoutUndo();
   PaperUIBuilder.RefreshSymbols();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  }
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory("Logs/FoldBridgeBackups");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Logs/FoldBridgeBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);Apply();F0PlayVerifier.Run();}
 }
}
