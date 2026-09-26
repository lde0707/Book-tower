using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BookTower.Puzzles;
using BookTower.Interaction;
namespace BookTower.EditorTools
{
 public static partial class FoldBridgeBuilder
 {
  static void OpticalControl(Transform root,FoldBridgePuzzle puzzle,string name,Vector3 at,int action,Vector3 mechanism)
  {
   Control(root,puzzle,name,at,false);
   var control=root.Find(name).GetComponent<FoldBridgeControl>();control.opticalControl=action;
   var data=new SerializedObject(control.GetComponent<Interactable>());
   data.FindProperty("verb").stringValue=action==1?"빛 덮개 접기 / 펴기":"은박 책장 돌리기";data.ApplyModifiedPropertiesWithoutUndo();
   Line(root,name+"과 연결된 제본실",M("CottonRope"),.045f,at,at+Vector3.up*.8f,mechanism);
   // Three sewn markers expose discrete choices without numerical combination hints.
   if(action>1)for(int i=0;i<3;i++)Box(control.transform,"회전 눈금",V(-.3f+i*.3f,.55f,0),V(.12f,.15f,.08f),M("IvoryPaper"),false);
  }
  static F0LightSurface Mirror(Transform root,string name,Vector3 at,Vector3 incoming,Vector3 outgoing,Material foil)
  {
   var face=Box(root,name,at,V(1.65f,1.65f,.06f),foil);
   face.localRotation=Quaternion.LookRotation((incoming-outgoing).normalized);
   foreach(int sign in new[]{-1,1})
   {
    Box(face,"은박 책장의 종이 테두리",V(sign*.9f,0,0),V(.12f,1.95f,.1f),M("IvoryPaper"),false);
    Box(face,"은박 책장의 종이 테두리",V(0,sign*.9f,0),V(1.7f,.12f,.1f),M("IvoryPaper"),false);
   }
   var surface=face.gameObject.AddComponent<F0LightSurface>();surface.kind=F0LightSurface.Kind.Mirror;
   return surface;
  }
  static F0LightSurface Flower(Transform parent,string name,Vector3 at,Vector3 normal)
  {
   var flower=Box(parent,name,at,V(1.6f,1.6f,.1f),M("AgedBrass"));flower.localRotation=Quaternion.LookRotation(normal);
   var surface=flower.gameObject.AddComponent<F0LightSurface>();surface.kind=F0LightSurface.Kind.Flower;
   surface.petals=G(flower,"빛에 펼쳐지는 종이꽃",Vector3.zero);
   for(int i=0;i<6;i++)MeshObject(surface.petals,"종이 꽃잎",F0Mesh.Leaf(.95f,.5f),M("IvoryPaper"),V(0,0,.12f));
   surface.glow=flower.gameObject.AddComponent<Light>();surface.glow.range=4;surface.glow.color=new Color(1,.72f,.3f);surface.glow.intensity=0;
   return surface;
  }
  static void AddLightRouting(Transform root,FoldBridgePuzzle puzzle)
  {
   var surfaces=new List<F0LightSurface>();
   var foilPath=Dir+"/SilverPaper.mat";var foil=AssetDatabase.LoadAssetAtPath<Material>(foilPath);
   if(!foil){foil=new Material(M("IvoryPaper"));AssetDatabase.CreateAsset(foil,foilPath);}
   foil.SetColor("_BaseColor",new Color(.62f,.73f,.72f));foil.SetFloat("_Metallic",.75f);foil.SetFloat("_Smoothness",.68f);EditorUtility.SetDirty(foil);
   puzzle.shutter=G(root,"접히는 광원 종이 덮개",V(1.3f,9,3));
   Box(puzzle.shutter,"빛을 막는 불투명 종이",V(-1.3f,0,0),V(2.6f,.12f,2.6f),M("OxbloodBook"));
   OpticalControl(root,puzzle,"빛 덮개의 제본 고리",V(-5.5f,4,-5.8f),1,V(1.3f,9,3));
   var first=Mirror(root,"입구 은박 책장",V(2.1f,6,3),Vector3.forward,Vector3.back,foil);
   puzzle.entranceMirror=first.transform;surfaces.Add(first);
   surfaces.Add(Mirror(root,"고정된 모서리 은박",V(6,6,3),Vector3.right,Vector3.forward,foil));
   var last=Mirror(root,"건너편 은박 책장",V(6,6,9),Vector3.forward,Vector3.right,foil);
   puzzle.farMirror=last.transform;surfaces.Add(last);
   OpticalControl(root,puzzle,"입구 은박 회전 고리",V(-3.2f,4,-3),2,V(0,6,3));
   OpticalControl(root,puzzle,"건너편 은박 회전 고리",V(6,4,9),3,V(6,6,9));
   // Keep the original under-bridge receiver as a harmless learning response.
   var old=puzzle.receiver.Find("빛을 받는 인장");Object.DestroyImmediate(old.gameObject);
   surfaces.Add(Flower(puzzle.receiver,"골 아래 종이꽃",Vector3.zero,Vector3.up));
   surfaces.Add(Flower(root,"옆길의 종이꽃",V(-6,6,3),Vector3.right));
   surfaces.Add(Flower(root,"건너편 장식 종이꽃",V(10,6,9),Vector3.left));
   var exit=Flower(root,"문을 깨우는 잎 문양",V(0,6,9),Vector3.right);exit.kind=F0LightSurface.Kind.Exit;surfaces.Add(exit);
   puzzle.connection.positionCount=3;puzzle.connection.SetPositions(new[]{root.TransformPoint(V(0,6,9)),root.TransformPoint(V(0,6,10.3f)),root.TransformPoint(V(0,4,10.3f))});
   puzzle.receiverLight.intensity=0;
   puzzle.lightSurfaces=surfaces.ToArray();puzzle.lightSegments=new LineRenderer[8];
   for(int i=0;i<8;i++)puzzle.lightSegments[i]=Line(root,"실제로 반사되는 빛 "+i,M("LanternPaper"),.085f,Vector3.zero,Vector3.zero);
   // Slim hanging supports connect optics to the paper architecture, clear of walking space.
   foreach(var surface in surfaces)if(!surface.transform.IsChildOf(puzzle.receiver))
   {
    Vector3 p=root.InverseTransformPoint(surface.transform.position);
    Line(root,"천장에 매단 제본실",M("CottonRope"),.035f,p+Vector3.up,p+Vector3.up*8);
   }
  }
 }
}
