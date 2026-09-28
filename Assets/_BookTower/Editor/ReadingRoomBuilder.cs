using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.Interaction;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class ReadingRoomBuilder
 {
  static Transform root;static Room room;static ReadingRoomPuzzle puzzle;static int pass;
  public static void RunBatch()
  {
   try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();Capture();Structure();EditorApplication.Exit(0);}
   catch(Exception e){Debug.LogException(e);File.WriteAllText(Out+"/build-error.txt",e.ToString());EditorApplication.Exit(1);}
  }
  public static void RunVerifiedBatch()
  {
   try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();Capture();Structure();ReadingRoomVerifier.RunBatch();}
   catch(Exception e){Debug.LogException(e);File.WriteAllText(Out+"/build-error.txt",e.ToString());EditorApplication.Exit(1);}
  }
  [MenuItem("Book Tower/1B 찢어진 열람실 제작")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
   var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-readingPass");pass=index>=0?int.Parse(args[index+1]):4;
   Directory.CreateDirectory(Dir+"/Meshes");Directory.CreateDirectory(Dir+"/Materials");Directory.CreateDirectory(Dir+"/Modules");Directory.CreateDirectory(Out+"/Iterations/Pass"+pass);Directory.CreateDirectory("Logs/ReadingRoomBackups");AssetDatabase.Refresh();
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,"Logs/ReadingRoomBackups/before-pass"+pass+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="1B");
   var legacy=room.GetComponentsInChildren<PuzzleBase>(true).First(p=>p.PuzzleId=="P3");
   var old=room.transform.Find("ReadingRoom1B");if(old)Object.DestroyImmediate(old.gameObject);
   foreach(Transform child in room.transform)child.gameObject.SetActive(false);
   var keep=room.transform.Find("1B_PreservedProgress");if(!keep)keep=Group(room.transform,"1B_PreservedProgress",Vector3.zero);
   legacy.transform.SetParent(keep,true);keep.gameObject.SetActive(true);legacy.gameObject.SetActive(true);legacy.enabled=false;
   foreach(Transform c in legacy.transform)c.gameObject.SetActive(false);
   foreach(var r in legacy.GetComponentsInChildren<Renderer>(true))r.enabled=false;
   foreach(var c in legacy.GetComponentsInChildren<Collider>(true))c.enabled=false;
   meshId=0;seed=510;cache.Clear();modules.Clear();Materials();if(pass>=2)ArtMaterials();
   root=Group(room.transform,"ReadingRoom1B",Vector3.zero);puzzle=root.gameObject.AddComponent<ReadingRoomPuzzle>();puzzle.legacyPuzzle=legacy;
   Shell();Furnish();Mechanism();Passages();
   if(pass>=2){Detail();ArchitecturalArt();}
   var so=new SerializedObject(room);so.FindProperty("boundsCenter").vector3Value=V(-5,10,0);so.FindProperty("boundsSize").vector3Value=V(56,23,68);so.FindProperty("ambient").floatValue=pass>=3?.36f:.26f;so.FindProperty("sunIntensity").floatValue=.12f;so.FindProperty("fog").floatValue=.0018f;so.FindProperty("goal").stringValue="모아와 마지막 장으로 · 닫힌 열람실에 빛 되돌리기";so.ApplyModifiedPropertiesWithoutUndo();
   foreach(var module in modules)PrefabUtility.SaveAsPrefabAsset(module.gameObject,Dir+"/Modules/"+module.name+".prefab");
   PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"/ReadingRoom1B.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllText(Out+"/Iterations/Pass"+pass+"/build.txt","Built pass "+pass+"\nMeshes="+meshId+"\nRenderers="+root.GetComponentsInChildren<Renderer>().Length+"\nModules="+modules.Count+"\n");
  }
  static void Shell()
  {
   // A continuous collision foundation remains below a shallow, book-filled display trench.
   Box(root,"continuous sealed foundation",V(0,-.8f,0),V(36,1,44),navy,true);
   foreach(var spec in new[]{(V(0,-.18f,-13),V(36,.36f,18)),(V(0,-.18f,14),V(36,.36f,16)),(V(-13,-.18f,1),V(10,.36f,10)),(V(13,-.18f,1),V(10,.36f,10)),(V(0,-.18f,1),V(4.4f,.36f,10))})Box(root,"walkable page island",spec.Item1,spec.Item2,paper,true);
   foreach(float x in new[]{-18f,18f})Box(root,"sealed side wall",V(x,9,0),V(.8f,18.5f,44),navy,true);
   WallWithDoor(-22,-10,7);WallWithDoor(22,0,5.6f);
   Box(root,"sealed ceiling backing",V(0,20.7f,0),V(36.8f,.6f,44.8f),navy,true);
   // Barrel vault is a connected two-sided mesh, not disconnected slats.
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   for(int z=0;z<=11;z++)for(int x=0;x<=36;x++){float px=-18+x,py=16+4*Mathf.Sqrt(Mathf.Max(0,1-px*px/324));vertices.Add(V(px,py,-22+z*4));uv.Add(new Vector2(px*.3f,z*1.2f));if(x<36&&z<11){int a=z*37+x;tris.AddRange(new[]{a,a+37,a+1,a+1,a+37,a+38});}}
   var m=new Mesh();m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();var vault=MeshAt(root,"continuous folded paper vault",SaveMesh(m),paper,Vector3.zero);vault.gameObject.AddComponent<MeshCollider>().sharedMesh=vault.GetComponent<MeshFilter>().sharedMesh;
   for(int z=-20;z<=20;z+=8){var path=new List<Vector3>();for(int x=-18;x<=18;x++)path.Add(V(x,15.8f+4*Mathf.Sqrt(Mathf.Max(0,1-x*x/324f)),z));Tube(root,"vault binding rib",path,.18f,navy);}
   for(int x=-15;x<=15;x+=6)for(int z=-19;z<22;z+=6)
   {if(z>-4&&z<6&&Mathf.Abs(x)<8)continue;Sheet(root,"layered deckled floor",V(x,.065f,z-3),5.98f,5.98f,paper,V(90,0,0));}
   var bridge=Group(root,"07_RunnerBridge",Vector3.zero);modules.Add(bridge);
   Box(bridge,"bookboard bridge",V(0,-.04f,1),V(4.5f,.15f,10.2f),navy);
   for(int z=-20;z<=18;z+=4){Sheet(root,"burgundy paper runner",V(0,.15f,z),3.65f,4,wine,V(90,0,0));for(int side=-1;side<=1;side+=2)Tube(root,"golden runner border",new[]{V(side*1.55f,.20f,z),V(side*1.55f,.20f,z+4)},.027f,gold);}
   for(int s=-1;s<=1;s+=2)for(int z=-4;z<=6;z+=5){Box(bridge,"brass stanchion",V(s*2.2f,.57f,z),V(.13f,1.15f,.13f),gold,true);Ring(bridge,"rope eye",V(s*2.2f,1.2f,z),.13f,.03f,gold);if(z<6)Rope(bridge,V(s*2.2f,1.16f,z),V(s*2.2f,1.16f,z+5),.045f);}
   for(int s=-1;s<=1;s+=2)for(int i=0;i<9;i++){var g=Group(root,"books lining shallow trench",V(s*(3+i*.55f),-.58f,1));Book(g,Vector3.zero,.42f,.7f+(.1f*(i%3)),3,i%4);}
   Merge(bridge);
  }
  static void WallWithDoor(float z,float center,float width)
  {
   float left=center-width*.5f,right=center+width*.5f;
   Box(root,"door wall left",V((-18+left)*.5f,10.5f,z),V(left+18,21,.7f),paper,true);
   Box(root,"door wall right",V((18+right)*.5f,10.5f,z),V(18-right,21,.7f),paper,true);
   Box(root,"sealed lintel",V(center,13.5f,z),V(width,15,.7f),paper,true);
   var arc=new List<Vector3>();for(int i=0;i<=32;i++){float a=Mathf.PI*i/32;arc.Add(V(center+Mathf.Cos(a)*(width*.5f+.15f),3.8f+Mathf.Sin(a)*2.2f,z+(z<0?.45f:-.45f)));}Tube(root,"layered entry arch",arc,.18f,navy);
  }
  static void Furnish()
  {
   var shelf=Shelf(root,V(-16.1f,0,-16),-90);modules.Add(shelf);
   foreach(float z in new[]{-9f,-2,5,12,18})Copy(shelf,root,V(-16.1f,0,z),-90);
   foreach(float z in new[]{-17f,-10,-3,4,11,18})Copy(shelf,root,V(16.1f,0,z),90);
   foreach(float x in new[]{-13f,-7,7,13}){var copy=Copy(shelf,root,V(x,0,20.6f),0);copy.localScale=V(1,.6f,1);}
   var desk=Desk(root,V(-10.4f,0,-8),-90);modules.Add(desk);
   var banner=Banner(root,V(-5,8,21.5f),0);modules.Add(banner);Copy(banner,root,V(5,8,21.5f),0);
   Copy(banner,root,V(17.4f,7,-12),90);Copy(banner,root,V(-17.4f,7,10),-90);
   var lamp=Chandelier(root,V(-7,13,-6));modules.Add(lamp);Copy(lamp,root,V(7,13,9),0);
   var table=Desk(root,V(10,0,10),90);table.localScale=V(.6f,.68f,.7f);
   Sheet(table,"draped burgundy paper",V(0,5.75f,-2.24f),5,3.5f,wine,V(-90,0,0));
   Lantern(root,V(10,3.95f,10));Point(root,V(10,5,10),5,10,new Color(1,.65f,.3f));
   foreach(var v in new[]{V(-14,0,-2),V(14,0,3),V(-13,0,13)})
   {var ladder=Group(root,"09_LibraryLadder",v);if(!modules.Any(m=>m.name=="09_LibraryLadder"))modules.Add(ladder);ladder.localEulerAngles=V(-10,0,-5);foreach(float x in new[]{-.7f,.7f})Box(ladder,"ladder rail",V(x,4,0),V(.16f,8,.2f),navy);for(int i=0;i<12;i++)Box(ladder,"ladder rung",V(0,.4f+i*.61f,-.1f),V(1.55f,.13f,.25f),edge);}
   foreach(var v in new[]{V(-8,3,-17),V(8,3,-17),V(-8,3,15),V(8,3,15)})Point(root,v,pass>=2?24:3,20,new Color(1,.71f,.42f));
  }
  static Transform Copy(Transform original,Transform p,Vector3 at,float yaw)
  {var t=Object.Instantiate(original,p);t.name=original.name+" instance";t.localPosition=at;t.localEulerAngles=V(0,yaw,0);return t;}
  static Transform Control(Transform parent,string name,Vector3 at,ReadingRoomControl.Action action,Vector3 size,string verb="살펴보기")
  {
   var g=Group(parent,name,at);
   if(pass>=4){
    if(action==ReadingRoomControl.Action.RecordDock||action==ReadingRoomControl.Action.ExitDock||action==ReadingRoomControl.Action.WindowCharge||action==ReadingRoomControl.Action.Charge){
     Box(g,"stacked bookboard pedestal",V(0,-.25f,0),V(size.x*1.6f,.5f,size.z*1.7f),navy);var ring=Ring(g,"recessed brass receiving cup",V(0,.03f,0),size.x*.55f,.06f,gold);ring.localEulerAngles=V(90,0,0);
    }else if(action==ReadingRoomControl.Action.LeftRing||action==ReadingRoomControl.Action.RightRing){Box(g,"locking tongue",V(0,-.13f,0),V(.13f,.18f,.10f),gold);}
    else if(action==ReadingRoomControl.Action.Pickup||action==ReadingRoomControl.Action.Recover){Ring(g,"usable handle",Vector3.zero,size.x*.4f,.035f,gold);}
    else {Box(g,"attached axle plate",Vector3.zero,size*.8f,gold);}
   }else Box(g,"gold clasp",Vector3.zero,size,gold);
   g.gameObject.AddComponent<BoxCollider>().size=size+Vector3.one*.14f;
   var it=g.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=name;so.FindProperty("verb").stringValue=verb;so.FindProperty("range").floatValue=3.6f;so.FindProperty("animatePosition").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
   var c=g.gameObject.AddComponent<ReadingRoomControl>();c.puzzle=puzzle;c.action=action;return g;
  }
  static void Mechanism()
  {
   puzzle.chargingSocket=Group(root,"charging socket",V(-8,0,-16));
   var charge=Group(root,"08_ChargingNiche",V(-8,0,-16));Box(charge,"inset plinth",V(0,.12f,0),V(2.4f,.24f,2),navy);puzzle.chargingSocket.localPosition+=V(0,.25f,0);
   Control(charge,"채광 받침",V(-1.1f,.7f,-.5f),ReadingRoomControl.Action.Charge,V(.4f,.5f,.35f),"기억등 충전하기");
   Control(charge,"등 회수 고리",V(1.1f,.65f,-.5f),ReadingRoomControl.Action.Recover,V(.3f,.4f,.3f),"기억등 되돌리기");
   puzzle.lantern=Lantern(root,puzzle.chargingSocket.localPosition,true);modules.Add(puzzle.lantern);
   puzzle.leftRing=puzzle.lantern.Find("left locking ring");puzzle.rightRing=puzzle.lantern.Find("right locking ring");puzzle.chargeRibbon=puzzle.lantern.Find("charge ribbon");
   Control(puzzle.leftRing,"왼쪽 보존 고리",Vector3.zero,ReadingRoomControl.Action.LeftRing,V(.32f,.36f,.25f),"잠그기 / 열기");
   Control(puzzle.rightRing,"오른쪽 보존 고리",Vector3.zero,ReadingRoomControl.Action.RightRing,V(.32f,.36f,.25f),"잠그기 / 열기");
   Control(puzzle.lantern,"기억등 손잡이",V(0,1.6f,-.2f),ReadingRoomControl.Action.Pickup,V(.6f,.35f,.5f),"들기");puzzle.lanternLight=Point(puzzle.lantern,V(0,1,0),.3f,8,new Color(1,.70f,.35f));
   // The alcove is reachable through the actual desk arch; a partition prevents seeing through its back.
   var alcove=Group(root,"09_RecordAlcove",V(-14,0,-8));
   Box(alcove,"record lectern",V(0,.55f,0),V(2.3f,1.1f,1.5f),navy,true);
   puzzle.recordSocket=Group(root,"record dock socket",V(-14,1.2f,-8));
   Control(alcove,"먼지 쌓인 점검대",V(0,1.3f,1.2f),ReadingRoomControl.Action.RecordDock,V(.7f,.4f,.4f),"빛 비춰보기");
   puzzle.servicePanel=Box(alcove,"sliding paper service panel",V(0,1.5f,-1.1f),V(3,3,.15f),wine,true);puzzle.serviceBlocker=puzzle.servicePanel.GetComponent<Collider>();
   foreach(float side in new[]{-1f,1f})Box(alcove,"enclosed service cabinet side",V(side*1.6f,1.5f,-1.8f),V(.2f,3,1.6f),navy,true);
   Box(alcove,"enclosed service cabinet back",V(0,1.5f,-2.55f),V(3.4f,3,.2f),navy,true);Box(alcove,"enclosed service cabinet canopy",V(0,3.1f,-1.8f),V(3.4f,.2f,1.7f),navy,true);
   Tube(puzzle.servicePanel,"pressed maintenance crank trace",new[]{V(-.45f,-.3f,.09f),V(-.45f,.15f,.09f),V(.35f,.15f,.09f),V(.35f,.65f,.09f)},.025f,gold);
   puzzle.crank=Control(alcove,"보관된 도르래 손잡이",V(1,1.8f,-1.4f),ReadingRoomControl.Action.TakeCrank,V(.7f,.3f,.3f),"손잡이 꺼내기");
   Ring(puzzle.crank,"crank round grip",V(.15f,.2f,-.2f),.17f,.05f,gold);
   puzzle.recordLight=Point(alcove,V(0,2,-.4f),0,6,new Color(1,.8f,.5f));
   var window=Group(root,"04_DoorWindowPulley",V(0,0,21.35f));modules.Add(window);
   puzzle.door=Group(window,"sliding book door",Vector3.zero);
   Box(puzzle.door,"closed book door",V(0,2.95f,0),V(6,6.3f,.45f),navy,true);puzzle.doorBlocker=puzzle.door.GetComponentInChildren<Collider>();
   foreach(float x in new[]{-2.45f,2.45f})Box(puzzle.door,"gilt door seam",V(x,2.8f,-.2f),V(.06f,5.2f,.06f),gold);
   foreach(float x in new[]{-1.2f,1.2f})Sprig(puzzle.door,V(x,1,-.24f),3.4f,gold);
   Ring(puzzle.door,"door ring",V(0,2,-.3f),.36f,.065f,gold);
   Box(window,"blue paper window",V(0,12.2f,.01f),V(5.6f,8,.2f),blue);
   foreach(float x in new[]{-2.9f,0,2.9f})Box(window,"window mullion",V(x,12.2f,-.23f),V(.14f,8.4f,.2f),navy);
   foreach(float y in new[]{8.2f,10,14.5f,16.2f})Box(window,"window crossbar",V(0,y,-.24f),V(5.9f,.13f,.2f),navy);
   if(pass>=2)Print(window,"etched owl glazing",V(0,8.25f,-.27f),5.6f,7.9f,windowPrint,Vector3.zero);else Bird(window,V(0,12.3f,-.3f),1.4f,navy);
   puzzle.shutter=Group(window,"accordion window shutter",V(0,7.9f,-.38f));for(int i=0;i<18;i++){var slat=Box(puzzle.shutter,"folded page slat",V(0,i*.45f,.06f*(i%2)),V(5.65f,.28f,.15f),navy);slat.localEulerAngles=V(i%2==0?12:-12,0,0);}
   puzzle.pulley=Ring(window,"window pulley",V(3.5f,16.4f,-.3f),.6f,.12f,gold);
   Rope(window,V(3.5f,16.2f,-.4f),V(3.5f,1.5f,-.4f));Rope(window,V(3.9f,16.2f,-.4f),V(3.9f,1.5f,-.4f));
   var axle=Control(window,"손잡이가 빠진 도르래 축",V(3.6f,1.5f,-.6f),ReadingRoomControl.Action.Crank,V(.5f,.5f,.4f),"손잡이 연결 / 창 올리기");Ring(axle,"empty axle hole",V(0,0,-.24f),.12f,.04f,ink);
   puzzle.latch=Control(window,"창문 고정 걸쇠",V(3.7f,2.3f,-.6f),ReadingRoomControl.Action.Latch,V(.55f,.22f,.25f),"걸어 고정하기");
   puzzle.exitSocket=Group(root,"exit dock socket",V(-3.7f,.4f,18.5f));
   puzzle.windowChargeSocket=Group(root,"window charging socket",V(5.7f,.4f,18.4f));
   Control(root,"출구의 기억등 받침",V(-3.7f,1,18.2f),ReadingRoomControl.Action.ExitDock,V(.8f,.6f,.6f),"빛 건네기");
   Control(root,"새로 열린 창의 충전대",V(5.7f,1,18.4f),ReadingRoomControl.Action.WindowCharge,V(.8f,.6f,.6f),"창빛 담기");
   puzzle.windowLight=Point(window,V(0,11,-2),0,30,new Color(.42f,.60f,1));
   Point(root,V(0,15,17),pass>=2?35:4,35,new Color(.46f,.60f,1));
   Note(root,V(-4,1.1f,-17),"1B.Entry","보존 고리",new[]{"빛을 옮길 때는 두 고리를 잠글 것. 받을 자리에 놓기 전 다시 연다.","등은 빛을 기억한다. 걸어가는 동안 빛이 닳지는 않는다."});
   Note(root,V(8,1.3f,18),"1B.Next","반대편의 기록",new[]{"같은 도장이라도 반대편에서 보면 방향이 바뀐다.","글보다 먼저, 종이의 찢긴 모서리와 제본 구멍을 맞춰 볼 것."});
  }
  static void Note(Transform p,Vector3 pos,string id,string title,string[] pages)
  {var g=Group(p,"펼친 작업책 "+title,pos);Sheet(g,"left page",V(-.5f,0,0),1,1.2f,paper,V(55,-12,0));Sheet(g,"right page",V(.5f,0,0),1,1.2f,paper,V(55,12,0));g.gameObject.AddComponent<BoxCollider>().size=V(2,1.2f,1);var it=g.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=title;so.FindProperty("verb").stringValue="읽기";so.FindProperty("animatePosition").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();var n=g.gameObject.AddComponent<ReadableNote>();n.id=id;n.title=title;n.pages=pages;}
  static void Detail()
  {
   for(int i=0;i<28;i++)
   {float x=i%2==0?-7.5f:8.5f,z=-18+(i/2)*2.7f;if(z>-5&&z<7)continue;var pile=Group(root,"varied stacked volumes",V(x,0,z));for(int j=0;j<3+i%3;j++){var b=Group(pile,"slumped volume",V(0,.15f+j*.28f,0));b.localEulerAngles=V(0,j*11-15,90);Book(b,Vector3.zero,.25f,1.1f+(j%2)*.3f,.85f,i+j);}}
   for(int z=-19;z<=17;z+=6){var motif=Group(root,"raven runner medallion",V(0,.23f,z));motif.localEulerAngles=V(90,0,0);if(pass>=4)Print(motif,"woven raven medallion",V(0,-.7f,0),1.2f,1.4f,bannerPrint,Vector3.zero);else Bird(motif,Vector3.zero,.8f,gold);}
   for(int i=0;i<20;i++){float z=-20+i*2.1f;foreach(float s in new[]{-1f,1f})Sheet(root,"loose page by wall",V(s*(11.5f+i%3),.06f,z),.8f,1.2f,paper,V(90,(i*31)%180,0));}
   if(pass>=4)for(int z=-20;z<22;z++)foreach(float side in new[]{-1f,1f})Tube(root,"runner stitching",new[]{V(side*1.75f,.22f,z),V(side*1.88f,.22f,z+.15f)},.014f,paper);
  }
  static void Passages()
  {
   var prior=GameObject.Find("ReadingRoom1B_Connections");if(prior)Object.DestroyImmediate(prior);
   var g=Group(null,"ReadingRoom1B_Connections",Vector3.zero);var b=g.gameObject.AddComponent<RoomBound>();var so=new SerializedObject(b);var ids=so.FindProperty("roomIds");ids.arraySize=3;for(int i=0;i<3;i++)ids.GetArrayElementAtIndex(i).stringValue=new[]{"1A","1B","1C"}[i];so.ApplyModifiedPropertiesWithoutUndo();
   Corridor(g,V(142,8,-132),V(160,8,-122),7);
   Corridor(g,V(170,8,-78),V(170,8,-68),6);
   Corridor(g,V(170,8.1f,-68),V(170,12.1f,-30),6);
  }
  static void Corridor(Transform parent,Vector3 a,Vector3 b,float width)
  {var d=b-a;var g=Group(parent,"sealed approach passage",(a+b)*.5f);g.rotation=Quaternion.LookRotation(d.normalized);float len=d.magnitude;
   Box(g,"continuous approach floor",V(0,-.2f,0),V(width,.4f,len+1.5f),paper,true);Box(g,"continuous approach ceiling",V(0,6,0),V(width+1,.5f,len+2),navy,true);
   foreach(float s in new[]{-1f,1f})Box(g,"approach wall",V(s*(width*.5f+.15f),3,0),V(.5f,6,len-1.5f),navy,true);
   for(float z=-len*.5f;z<len*.5f;z+=2)Sheet(g,"stitched approach runner",V(0,.02f,z),2.6f,2.05f,wine,V(90,0,0));
   Point(g,V(0,4,0),6,15,new Color(1,.74f,.45f));
  }
 }
}
