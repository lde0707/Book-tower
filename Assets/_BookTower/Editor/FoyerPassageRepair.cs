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
 public static class FoyerPassageRepair
 {
  const string Dir="Assets/_BookTower/Generated/FoyerPassageSeal",Prefab="Assets/_BookTower/Resources/FoyerPassageCeiling.prefab";
  [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=PreviewInEditor;EditorApplication.projectChanged-=QueuePreview;EditorApplication.projectChanged+=QueuePreview;EditorApplication.playModeStateChanged-=ModeChanged;EditorApplication.playModeStateChanged+=ModeChanged;}
  static void QueuePreview(){EditorApplication.delayCall-=PreviewInEditor;EditorApplication.delayCall+=PreviewInEditor;}
  static void ModeChanged(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)QueuePreview();}
  static void PreviewInEditor()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||Application.isBatchMode)return;
   var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
   if(!scene.IsValid()||!scene.isLoaded||scene.GetRootGameObjects().Any(x=>x.name==FoyerPassageCeiling.RootName||x.name=="FoyerPassageCeiling"))return;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(!prefab)return;
   var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.name=FoyerPassageCeiling.RootName;
   // Preview must not silently save a user's open scene on every asset import.
   EditorSceneManager.MarkSceneDirty(scene);
  }
  static void Box(Transform p,string name,Vector3 at,Vector3 size,Material m,bool solid=true)
  {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());}
  public static void Run()
  {
   try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/foyer-ceiling-check.txt",e.ToString());EditorApplication.Exit(1);}
  }
  static void Build()
  {
   Directory.CreateDirectory(Dir);EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
   var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(x=>x.FromRoomId=="F0"&&x.ToRoomId=="1A");
   var so=new SerializedObject(link);var a=so.FindProperty("fromPoint").vector3Value;var b=so.FindProperty("toPoint").vector3Value;
   var old=GameObject.Find(FoyerPassageCeiling.RootName);if(old)Object.DestroyImmediate(old);
   var root=new GameObject(FoyerPassageCeiling.RootName);root.transform.position=(a+b)/2;root.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
   float width=link.transform.lossyScale.x,length=Vector3.Distance(a,b)+12,r=width*.5f+.12f;
   var passage=link.transform.parent.Find("V4_서문에서동화로");
   var source=passage?passage.GetComponentsInChildren<Renderer>().FirstOrDefault(x=>x.name=="휘어진 책장 천장"):null;
   var mat=new Material(source?source.sharedMaterial:link.GetComponent<Renderer>().sharedMaterial);mat.name="Warm paper vault";if(mat.HasProperty("_Cull"))mat.SetFloat("_Cull",0);
   string materialPath=Dir+"/CeilingPaper.mat";var prior=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(prior){EditorUtility.CopySerialized(mat,prior);Object.DestroyImmediate(mat);mat=prior;}else AssetDatabase.CreateAsset(mat,materialPath);
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int count=64;
   for(int j=0;j<=count;j++){float angle=Mathf.PI*j/count;foreach(float radius in new[]{r,r+.3f})foreach(float z in new[]{-length/2,length/2}){vertices.Add(new Vector3(Mathf.Cos(angle)*radius,3.5f+Mathf.Sin(angle)*radius,z));uv.Add(new Vector2(angle*r*.35f,z*.35f));}}
   void Quad(int x,int y,int z,int w){triangles.AddRange(new[]{x,z,y,x,w,z});}
   for(int j=0;j<count;j++){int k=j*4,n=k+4;Quad(k,n,n+1,k+1);Quad(k+2,k+3,n+3,n+2);Quad(k,k+2,n+2,n);Quad(k+1,n+1,n+3,k+3);}
   Quad(0,1,3,2);int last=count*4;Quad(last,last+2,last+3,last+1);
   var mesh=new Mesh{name="Continuous thick paper barrel vault"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   var previousMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/Vault.asset");if(previousMesh){EditorUtility.CopySerialized(mesh,previousMesh);Object.DestroyImmediate(mesh);mesh=previousMesh;}else AssetDatabase.CreateAsset(mesh,Dir+"/Vault.asset");
   var vault=new GameObject("이음새 없는 두꺼운 종이 아치 천장");vault.transform.SetParent(root.transform,false);vault.AddComponent<MeshFilter>().sharedMesh=mesh;vault.AddComponent<MeshRenderer>().sharedMaterial=mat;vault.AddComponent<MeshCollider>().sharedMesh=mesh;
   foreach(float side in new[]{-1f,1f})Box(root.transform,"입구까지 이어진 틈막이 벽",new Vector3(side*(r+.20f),1.85f,0),new Vector3(.4f,3.9f,length),mat);
   // Shallow leaf inlays under the barrel, with actual offset to prevent z fighting.
   var ink=new Material(mat);ink.name="Faded terracotta leaf ink";ink.SetColor("_BaseColor",new Color(.38f,.20f,.12f));string inkPath=Dir+"/LeafInk.mat";var oldInk=AssetDatabase.LoadAssetAtPath<Material>(inkPath);if(oldInk){EditorUtility.CopySerialized(ink,oldInk);Object.DestroyImmediate(ink);ink=oldInk;}else AssetDatabase.CreateAsset(ink,inkPath);
   for(float z=-length/2+1;z<length/2-1;z+=3.5f)
   {Box(root.transform,"천장 잎줄기",new Vector3(0,3.5f+r-.19f,z),new Vector3(.025f,.02f,1.2f),ink,false);
    foreach(float s in new[]{-1f,1f})for(int j=0;j<3;j++){var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);leaf.name="천장의 작은 잎 문양";leaf.transform.SetParent(root.transform,false);leaf.transform.localPosition=new Vector3(s*.16f,3.5f+Mathf.Sqrt(r*r-.16f*.16f)-.195f,z-.4f+j*.32f);leaf.transform.localScale=new Vector3(.24f,.02f,.11f);leaf.transform.localRotation=Quaternion.Euler(0,s*35,0);leaf.GetComponent<Renderer>().sharedMaterial=ink;Object.DestroyImmediate(leaf.GetComponent<Collider>());}}
   var bound=root.AddComponent<RoomBound>();var rb=new SerializedObject(bound);var ids=rb.FindProperty("roomIds");ids.arraySize=2;ids.GetArrayElementAtIndex(0).stringValue="F0";ids.GetArrayElementAtIndex(1).stringValue="1A";rb.ApplyModifiedPropertiesWithoutUndo();
   Physics.SyncTransforms();int checkedRays=0;var roof=vault.GetComponent<Collider>();
   for(float z=-length/2+.05f;z<length/2;z+=.5f)for(int i=-8;i<=8;i++){var start=root.transform.TransformPoint(new Vector3(r*i/10,2,z));if(!roof.Raycast(new Ray(start,root.transform.up),out _,r+4))throw new Exception("Roof gap x="+i+" z="+z);checkedRays++;}
   foreach(var wall in root.GetComponentsInChildren<BoxCollider>())for(float z=-length/2+.05f;z<length/2;z+=.5f)for(float y=.1f;y<3.5f;y+=.5f){float sign=Mathf.Sign(wall.transform.localPosition.x);if(!wall.Raycast(new Ray(root.transform.TransformPoint(new Vector3(0,y,z)),root.transform.right*sign),out _,r+1))throw new Exception("Side gap");checkedRays++;}
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
   Directory.CreateDirectory("Docs/ArtReview/FoyerPassageSeal");var camera=new GameObject("Ceiling audit camera").AddComponent<Camera>();camera.fieldOfView=75;camera.transform.position=root.transform.TransformPoint(new Vector3(0,1.5f,-length/2+3));camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,4.1f,0)));InteriorF0Builder.Capture(camera,"Docs/ArtReview/FoyerPassageSeal/ceiling.png");Object.DestroyImmediate(camera.gameObject);
   File.WriteAllText("Logs/foyer-ceiling-check.txt",$"PASS: {checkedRays} roof/side rays. A={a} B={b}, width={width}, covered length={length}, 6m overlap at both entrances. Closed thick mesh and side colliders, leaf motifs.\n");
  }
 }
}
