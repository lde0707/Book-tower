using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static partial class ReadingEntranceRepair
 {
  const string Dir="Assets/_BookTower/Generated/ReadingEntrance",Asset="Assets/_BookTower/Resources/ReadingEntrancePassage.prefab";
  static readonly Vector3[] centers={new(140.6f,8.02f,-133.6f),new(142.5f,8.15f,-132),new(150,8.15f,-129),new(158,8.15f,-126),new(160,8.15f,-124),new(160,8.15f,-120.8f)};
  static readonly Vector3[] sides={new(.752f,0,-.659f),new(.45f,0,-.90f),new(.351f,0,-.936f),new(.546f,0,-.838f),new(.924f,0,-.383f),Vector3.right};
  static readonly float[] widths={2.85f,3.35f,3.5f,3.58f,3.79f,3.5f};
  static int serial;static Material paper,navy,gold,wine;
  [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Preview;EditorApplication.projectChanged-=Queue;EditorApplication.projectChanged+=Queue;}
  static void Queue(){EditorApplication.delayCall-=Preview;EditorApplication.delayCall+=Preview;}
  static void Preview(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");if(!scene.IsValid()||!scene.isLoaded)return;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Asset);if(!prefab)return;bool dirty=scene.isDirty,exists=scene.GetRootGameObjects().Any(x=>x.name==ReadingEntrancePassage.RootName);if(!exists){PrefabUtility.InstantiatePrefab(prefab,scene);ReadingEntrancePassage.ClearOldJunction();EditorSceneManager.MarkSceneDirty(scene);if(!dirty)EditorSceneManager.SaveScene(scene);}else ReadingEntrancePassage.ClearOldJunction();}
  static Transform Group(Transform p,string n,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=at;return t;}
  static Transform MeshAt(Transform p,string n,Mesh mesh,Material mat,bool solid=false)
  {var path=Dir+"/M"+(serial++).ToString("D3")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){old.Clear();old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.triangles=mesh.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var t=Group(p,n,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
   if(solid){var collision=Object.Instantiate(mesh);var front=mesh.triangles;var both=new List<int>(front);for(int i=0;i<front.Length;i+=3)both.AddRange(new[]{front[i+2],front[i+1],front[i]});collision.triangles=both.ToArray();var collisionPath=Dir+"/C"+(serial-1).ToString("D3")+".asset";var oldCollision=AssetDatabase.LoadAssetAtPath<Mesh>(collisionPath);if(oldCollision){oldCollision.Clear();oldCollision.vertices=collision.vertices;oldCollision.triangles=collision.triangles;oldCollision.RecalculateBounds();EditorUtility.SetDirty(oldCollision);Object.DestroyImmediate(collision);collision=oldCollision;}else AssetDatabase.CreateAsset(collision,collisionPath);t.gameObject.AddComponent<MeshCollider>().sharedMesh=collision;}return t;}
  static Mesh Quads(List<Vector3> vertices,List<Vector2> uv,List<int> indices)
  {var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
  static void Quad(List<Vector3> v,List<Vector2> uv,List<int> tri,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
  {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(Vector3.Distance(a,b)*.4f,0),new Vector2(0,Vector3.Distance(a,c)*.4f),new Vector2(Vector3.Distance(a,b)*.4f,Vector3.Distance(a,c)*.4f)});tri.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
  static void Tube(Transform root,string name,List<Vector3> points,float radius,Material mat)
  {var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();for(int i=0;i<points.Count;i++){var tangent=(points[Math.Min(i+1,points.Count-1)]-points[Math.Max(0,i-1)]).normalized;var n=Vector3.Cross(tangent,Vector3.forward).normalized;if(n.sqrMagnitude<.01f)n=Vector3.right;var b=Vector3.Cross(tangent,n).normalized;for(int j=0;j<=10;j++){float a=j*Mathf.PI/5;v.Add(points[i]+radius*(n*Mathf.Cos(a)+b*Mathf.Sin(a)));uv.Add(new Vector2(j*.1f,i*.1f));if(i>0&&j<10){int q=(i-1)*11+j;tri.AddRange(new[]{q,q+1,q+11,q+1,q+12,q+11});}}}MeshAt(root,name,Quads(v,uv,tri),mat);}
  static Vector3 Profile(int station,int step,float thickness=0)
  {float a=step*Mathf.PI/40;return centers[station]+sides[station]*(Mathf.Cos(a)*(widths[station]+thickness))+Vector3.up*(2.5f+Mathf.Sin(a)*(3.15f+thickness));}
  static void Shell(Transform root)
  {
   var wall=new List<Vector3>();var wuv=new List<Vector2>();var wt=new List<int>();var roof=new List<Vector3>();var ruv=new List<Vector2>();var rt=new List<int>();var floor=new List<Vector3>();var fuv=new List<Vector2>();var ft=new List<int>();
   for(int i=0;i<centers.Length-1;i++)
   {
    foreach(float s in new[]{-1f,1f})
    {var a=centers[i]+s*sides[i]*widths[i];var b=centers[i+1]+s*sides[i+1]*widths[i+1];Quad(wall,wuv,wt,a-Vector3.up*.24f,b-Vector3.up*.24f,a+Vector3.up*2.5f,b+Vector3.up*2.5f);
     var ao=a+s*sides[i]*.24f;var bo=b+s*sides[i+1]*.24f;Quad(wall,wuv,wt,ao-Vector3.up*.24f,bo-Vector3.up*.24f,ao+Vector3.up*2.5f,bo+Vector3.up*2.5f);}
    for(int j=0;j<40;j++){
     Quad(roof,ruv,rt,Profile(i,j),Profile(i,j+1),Profile(i+1,j),Profile(i+1,j+1));
     float run=0;for(int k=0;k<i;k++)run+=Vector3.Distance(centers[k],centers[k+1]);float end=run+Vector3.Distance(centers[i],centers[i+1]);int u=ruv.Count-4;
     ruv[u]=new Vector2(j*.2474f*.4f,run*.4f);ruv[u+1]=new Vector2((j+1)*.2474f*.4f,run*.4f);ruv[u+2]=new Vector2(j*.2474f*.4f,end*.4f);ruv[u+3]=new Vector2((j+1)*.2474f*.4f,end*.4f);
     Quad(roof,ruv,rt,Profile(i,j,.24f),Profile(i+1,j,.24f),Profile(i,j+1,.24f),Profile(i+1,j+1,.24f));}
    var l=centers[i]-sides[i]*(widths[i]+.26f);var r=centers[i]+sides[i]*(widths[i]+.26f);var nl=centers[i+1]-sides[i+1]*(widths[i+1]+.26f);var nr=centers[i+1]+sides[i+1]*(widths[i+1]+.26f);
    Quad(floor,fuv,ft,l,r,nl,nr);Quad(floor,fuv,ft,l-Vector3.up*.25f,r-Vector3.up*.25f,nl-Vector3.up*.25f,nr-Vector3.up*.25f);
   }
   MeshAt(root,"연속 벽과 맞물린 옆면",Quads(wall,wuv,wt),navy,true);MeshAt(root,"틈 없는 아치 천장",Quads(roof,ruv,rt),paper,true);MeshAt(root,"넓혀 이어 붙인 연속 바닥",Quads(floor,fuv,ft),paper,true);
   for(int i=0;i<centers.Length;i++)
   {
    for(int layer=0;layer<2;layer++){var arc=new List<Vector3>{centers[i]-sides[i]*(widths[i]-.1f)};for(int j=40;j>=0;j--)arc.Add(Profile(i,j)-sides[i]*Mathf.Cos(j*Mathf.PI/40)*.1f);arc.Add(centers[i]+sides[i]*(widths[i]-.1f));Tube(root,"접힌 종이 문틀",arc,layer==0?.095f:.045f,layer==0?wine:gold);}
    if(i>0&&i<centers.Length-1)foreach(float side in new[]{-1f,1f})
    {var pos=centers[i]+sides[i]*side*(widths[i]-.15f)+Vector3.up*.5f;var stem=new List<Vector3>{pos,pos+Vector3.up*1.4f};Tube(root,"벽면 제본 줄기",stem,.025f,gold);
     for(int n=0;n<4;n++){var leaf=MeshAt(root,"종이 잎 부조",F0Mesh.Leaf(.3f,.13f),gold);leaf.position=pos+Vector3.up*(n*.3f);leaf.rotation=Quaternion.LookRotation(-sides[i]*side)*Quaternion.Euler(0,0,n%2==0?45:-45);}}
   }
   var runner=new List<Vector3>();var uvs=new List<Vector2>();var indices=new List<int>();
   for(int i=0;i<centers.Length-1;i++)Quad(runner,uvs,indices,centers[i]-sides[i]*1.05f+Vector3.up*.025f,centers[i]+sides[i]*1.05f+Vector3.up*.025f,centers[i+1]-sides[i+1]*1.05f+Vector3.up*.025f,centers[i+1]+sides[i+1]*1.05f+Vector3.up*.025f);
   MeshAt(root,"문 안까지 이어지는 책갈피",Quads(runner,uvs,indices),wine);
   foreach(float side in new[]{-1f,1f})Tube(root,"러너 황금 제본선",centers.Select((p,i)=>p+sides[i]*side*1.07f+Vector3.up*.045f).ToList(),.018f,gold);
   for(int i=1;i<centers.Length;i+=2){var light=Group(root,"회랑의 은은한 등빛",centers[i]+Vector3.up*3.7f).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.78f,.51f);light.intensity=4;light.range=9;light.shadows=LightShadows.None;}
  }
  public static void Build()
  {try{
   Directory.CreateDirectory(Dir);Directory.CreateDirectory(Out);EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");serial=0;var prior=GameObject.Find(ReadingEntrancePassage.RootName);if(prior)Object.DestroyImmediate(prior);
   paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReadingRoom1B/Materials/Ivory_Atlas.mat");navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReadingRoom1B/Materials/Indigo_QuietFiber.mat");gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReadingRoom1B/Materials/Worn_Brass.mat");wine=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReadingRoom1B/Materials/Warm_Wine_Cover.mat");
   var root=Group(null,ReadingEntrancePassage.RootName,Vector3.zero);Shell(root);var bound=root.gameObject.AddComponent<RoomBound>();var so=new SerializedObject(bound);var ids=so.FindProperty("roomIds");ids.arraySize=2;ids.GetArrayElementAtIndex(0).stringValue="1A";ids.GetArrayElementAtIndex(1).stringValue="1B";so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Asset);AssetDatabase.SaveAssets();ReadingEntrancePassage.ClearOldJunction();EditorSceneManager.SaveScene(root.gameObject.scene);Physics.SyncTransforms();Capture("after");VerifyShell(root);if(Environment.GetCommandLineArgs().Contains("-entryVerify"))ReadingEntranceVerifier.Run();else EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);File.WriteAllText(Out+"/structure.txt","FAIL: "+e);EditorApplication.Exit(1);}}
  static void VerifyShell(Transform root)
  {
   int count=0;for(int i=0;i<centers.Length-1;i++)for(int j=1;j<=15;j++)foreach(float lateral in new[]{-.65f,0,.65f})
   {float t=j/16f;var p=Vector3.Lerp(centers[i],centers[i+1],t)+Vector3.Lerp(sides[i]*widths[i],sides[i+1]*widths[i+1],t)*lateral+Vector3.up*1.6f;foreach(var direction in new[]{Vector3.up,Vector3.down,Vector3.Lerp(sides[i],sides[i+1],t).normalized,-Vector3.Lerp(sides[i],sides[i+1],t).normalized}){if(!Physics.RaycastAll(p,direction,12).Any(h=>h.collider.transform.IsChildOf(root)))throw new Exception("Shell leak "+p+" direction "+direction);count++;}}
   File.WriteAllText(Out+"/structure.txt","PASS: "+count+" floor, roof and side rays across 3 lanes and 5 bays.\n");
  }
 }
}
