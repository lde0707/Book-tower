using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class InteriorEnvelopeRepair
 {
  const string Out="Docs/ArtReview/Envelope";
  static Vector3 V(float x,float y,float z)=>new(x,y,z);
  static Transform Group(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
  static void Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool collision=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());}
  static Material Mat(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
  public static void Apply()
  {
   var rooms=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None);
   var f=rooms.First(r=>r.RoomId=="F0").transform.Find(InteriorF0Builder.RootName);
   var a=rooms.First(r=>r.RoomId=="1A").transform.Find("V4_팝업북회랑");
   if(!f||!a)return;
   foreach(var p in new[]{f,a}){var old=p.Find("연속 외피 보강");if(old)Object.DestroyImmediate(old.gameObject);}
   var exitHeader=a.Find("출구 윗벽");if(exitHeader){var scale=exitHeader.localScale;scale.x=8.8f/exitHeader.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;exitHeader.localScale=scale;}
   var fg=Group(f,"연속 외피 보강");var ag=Group(a,"연속 외피 보강");
   var paper=Mat("Assets/_BookTower/Generated/F0Reference/IvoryPaper.mat");var wall=Mat("Assets/_BookTower/Generated/F0Reference/UmberBook.mat");var cream=Mat("Assets/_BookTower/Generated/Storybook1A/CreamPaper.mat");
   Box(fg,"후면 복도 천장",V(5.85f,16.45f,21),V(44.9f,.6f,2.9f),paper);
   Box(fg,"동측 복도 천장",V(22.25f,16.45f,0),V(12.1f,.6f,41.5f),paper);
   Box(fg,"후면 서쪽 막음",V(-16.3f,8,21),V(.55f,16.4f,2.5f),wall);
   Box(fg,"동측 복도 남쪽 막음",V(22.2f,8,-20.3f),V(12.2f,16.4f,.6f),wall);
   Box(fg,"출구 위 외벽 연장",V(28,12,0),V(.55f,8.4f,44),wall);
   Box(ag,"서쪽 입구 상부 막음",V(-32,9,-12.75f),V(.4f,6,9.5f),cream);
   // A low paper soffit seals the visual gap between the tall entrance and tunnel roof.
   Box(ag,"입구 안쪽 덮개",V(-30.5f,6.25f,-12.75f),V(3.4f,.3f,9.5f),cream);
   Box(fg,"후면 복도 밝은 장지",V(5.8f,5,21.69f),V(43.8f,10,.04f),paper,false);
   var guide=Mat("Assets/_BookTower/Generated/F0Reference/LanternPaper.mat");
   foreach(float x in new[]{0f,8f,16f}){Box(fg,"오른쪽 길 리본",V(x,2.6f,21.64f),V(2,.12f,.035f),guide,false);foreach(int sign in new[]{-1,1}){var t=Group(fg,"길을 가리키는 접힌 리본");t.localPosition=V(x+.8f,2.6f+sign*.24f,21.62f);t.localRotation=Quaternion.Euler(0,0,-sign*35);Box(t,"리본 끝",Vector3.zero,V(.85f,.12f,.035f),guide,false);}}
   Box(ag,"입구 남쪽 접합 기둥",V(-32,3,-16.7f),V(.45f,6,1.6f),cream);
   Box(ag,"입구 북쪽 접합 기둥",V(-32,3,-8.9f),V(.45f,6,1.8f),cream);
   var ink=Mat("Assets/_BookTower/Generated/F0Reference/TerracottaInk.mat");
   Vector3[] trail={V(0,.075f,13),V(0,.075f,18.1f),V(20,.075f,18.1f),V(20,.075f,11.2f),V(27.8f,.075f,11.2f)};
   for(int i=0;i<trail.Length-1;i++){var d=trail[i+1]-trail[i];int steps=Mathf.CeilToInt(d.magnitude/1.2f);for(int j=0;j<steps;j++){var at=Vector3.Lerp(trail[i],trail[i+1],(j+.5f)/steps);Box(fg,"출구를 잇는 인쇄 리본",at,d.x==0?V(.18f,.014f,.65f):V(.65f,.014f,.18f),ink,false);}}
   foreach(var pos in new[]{V(1,3,18.9f),V(19,3,18.9f),V(25.5f,3,11.2f)}){var t=Group(fg,"다음 길의 등불");t.localPosition=pos;Box(t,"종이등",Vector3.zero,V(.35f,.6f,.35f),paper,false);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.73f,.4f);l.range=14;l.intensity=45;}
   var passage=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="V4_서문에서동화로");
   if(passage)foreach(var mesh in passage.GetComponentsInChildren<MeshFilter>())if(mesh.name=="휘어진 책장 천장"&&!mesh.GetComponent<Collider>())mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
   if(passage){var old=passage.Find("입구 겹침 덮개");if(old)Object.DestroyImmediate(old.gameObject);var collars=Group(passage,"입구 겹침 덮개");var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(r=>r.FromRoomId=="F0"&&r.ToRoomId=="1A");var so=new SerializedObject(link);float length=Vector3.Distance(so.FindProperty("fromPoint").vector3Value,so.FindProperty("toPoint").vector3Value);foreach(int end in new[]{-1,1}){float z=end*length*.5f;foreach(int side in new[]{-1,1})Box(collars,"겹친 연결부 벽",V(side*2.93f,3.2f,z),V(.3f,6.4f,3),cream);Box(collars,"겹친 연결부 천장",V(0,6.4f,z),V(6.16f,.3f,3),cream);}}
   Physics.SyncTransforms();Directory.CreateDirectory(Out);
   int count=0;
   foreach(var pair in new[]{(f,28,22,16.9f),(a,31,27,13f)})for(int x=pair.Item1==f?-15:-30;x<pair.Item2;x+=2)for(int z=pair.Item1==f?-19:-26;z<pair.Item3;z+=2){var origin=pair.Item1.TransformPoint(V(x,.5f,z));if(!Physics.Raycast(origin,Vector3.up,out _,pair.Item4))throw new Exception("Ceiling gap: "+pair.Item1.name+" "+x+","+z);if(!Physics.Raycast(origin,Vector3.down,out _,1))throw new Exception("Floor gap: "+origin);count++;}
   int walls=0;
   void WallProbe(Transform r,Vector3 at,Vector3 dir){if(!Physics.Raycast(r.TransformPoint(at),dir,out _,1.5f))throw new Exception("Wall gap: "+r.name+" "+at);walls++;}
   for(float y=1;y<16;y+=2){for(float z=-19;z<22;z+=1){WallProbe(f,V(-15.4f,y,z),Vector3.left);if(!(z>8.7f&&z<13.6f&&y<6))WallProbe(f,V(27.1f,y,z),Vector3.right);}for(float x=-15;x<28;x+=1){WallProbe(f,V(x,y,-19.4f),Vector3.back);WallProbe(f,V(x,y,21.1f),Vector3.forward);}}
   for(float y=1;y<12;y+=2){for(float z=-27;z<28;z+=1){if(!(z>-17.6f&&z<-7.9f&&y<7.8f))WallProbe(a,V(-31.1f,y,z),Vector3.left);WallProbe(a,V(31.1f,y,z),Vector3.right);}for(float x=-31;x<32;x+=1){WallProbe(a,V(x,y,-27.1f),Vector3.back);if(!(x>20.3f&&x<29.1f&&y<9))WallProbe(a,V(x,y,27.1f),Vector3.forward);}}
   WallProbe(a,V(28.975f,10,27.1f),Vector3.forward);
   File.WriteAllText(Out+"/structure.txt","PASS: "+count+" floor/ceiling sample pairs; "+walls+" wall rays (intentional doorways excluded); corridor roof colliders added. Finite samples are not proof of every seam.\n");
  }
  public static void StructureBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
  public static void Verify1A(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());StorybookPlayVerifier.RunBatch();}
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory("Logs/EnvelopeBackups");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Logs/EnvelopeBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();F0PlayVerifier.Run();}
 }
}
