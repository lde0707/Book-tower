using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class RetiredContentCleanup
 {
  public static void RunBatch()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Directory.CreateDirectory("Logs/CleanupBackups");EditorSceneManager.SaveScene(scene,"Logs/CleanupBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var candidates=new List<GameObject>();
   foreach(var room in Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None))
    if(room.RoomId=="F0"||room.RoomId=="1A")foreach(Transform t in room.transform)
     if(!t.gameObject.activeSelf&&t.name!="F0_Gameplay"&&t.name!="F0_살아있는제본실"&&t.name!="1A_Gameplay"&&t.name!="V3_제본현관"&&t.name!="V4_팝업북회랑")candidates.Add(t.gameObject);
   var owners=new Dictionary<int,GameObject>();
   foreach(var c in candidates)foreach(var t in c.GetComponentsInChildren<Transform>(true)){owners[t.gameObject.GetInstanceID()]=c;foreach(var component in t.GetComponents<Component>())if(component)owners[component.GetInstanceID()]=c;}
   var protectedRoots=new HashSet<GameObject>();
   foreach(var component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include,FindObjectsSortMode.None))
   {
    if(!component||component is Transform)continue;
    var so=new SerializedObject(component);var it=so.GetIterator();
    while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue)
    {
     if(!owners.TryGetValue(it.objectReferenceValue.GetInstanceID(),out var target))continue;
     // Protect any root with incoming references from a different hierarchy.
     if(!owners.TryGetValue(component.GetInstanceID(),out var origin)||origin!=target)protectedRoots.Add(target);
    }
   }
   var report=new List<string>{"# 사용 종료 콘텐츠 정리","","범위: F0·1A의 비활성 구버전 직계 계층. 다른 방 및 외부 참조가 있는 항목은 보존."};int nodes=0;
   foreach(var c in candidates){if(protectedRoots.Contains(c)){report.Add("- 보존(외부 참조): "+c.name);continue;}nodes+=c.GetComponentsInChildren<Transform>(true).Length;report.Add("- 삭제 계층: "+c.name);Object.DestroyImmediate(c);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
   // Only numbered generated meshes: no hand-authored asset, texture, dynamic resource or script deletion.
   var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
   var referenced=new HashSet<string>();foreach(var path in paths)foreach(var dependency in AssetDatabase.GetDependencies(path,false))if(dependency!=path)referenced.Add(dependency);
   var unused=paths.Where(p=>(p.StartsWith("Assets/_BookTower/Generated/F0Reference/Mesh_")||p.StartsWith("Assets/_BookTower/Generated/Storybook1A/Mesh_"))&&p.EndsWith(".asset")&&!referenced.Contains(p)).ToArray();
   foreach(var path in unused){report.Add("- 삭제 미참조 생성 메시: "+path);AssetDatabase.DeleteAsset(path);}
   int missing=0;foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
   report.Insert(3,$"삭제 오브젝트 {nodes}개 / 미참조 생성 메시 {unused.Length}개 / Missing Script {missing}개");Directory.CreateDirectory("Docs/Maintenance");File.WriteAllLines("Docs/Maintenance/2026-09-26-cleanup.md",report);
   if(missing>0)throw new Exception("Missing scripts remain: "+missing);Debug.Log($"[Cleanup] PASS: removed {nodes} objects and {unused.Length} orphan meshes, no missing scripts");
  }
 }
}
