using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class F0AtmosphereBuilder
 {
  const string Folder="Assets/_BookTower/Generated/F0Atmosphere";
  [MenuItem("Book Tower/F0 창빛과 분위기 적용")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path!="Assets/Scenes/InGame.unity")throw new InvalidOperationException("Open InGame first");
   Directory.CreateDirectory("Logs/F0DetailBackups");EditorSceneManager.SaveScene(scene,"Logs/F0DetailBackups/Lighting-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   var room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
   var architecture=room.transform.Find(InteriorF0Builder.RootName);
   var old=architecture.Find("F0_창빛과먼지");if(old)Object.DestroyImmediate(old.gameObject);
   var root=new GameObject("F0_창빛과먼지").transform;root.SetParent(architecture,false);
   var so=new SerializedObject(room);so.FindProperty("ambient").floatValue=.95f;so.FindProperty("fog").floatValue=.003f;so.ApplyModifiedPropertiesWithoutUndo();
   foreach(var light in architecture.GetComponentsInChildren<Light>(true))
   {
    if(light.name=="창문투광")
    {
     bool left=room.transform.InverseTransformPoint(light.transform.position).x<0;
     light.intensity=left?165:20;light.color=left?new Color(1,.87f,.68f):new Color(.83f,.87f,1);
     var at=room.transform.InverseTransformPoint(light.transform.position);light.transform.LookAt(room.transform.TransformPoint(new Vector3(left?2:-3,3.5f,at.z+7)));
     light.spotAngle=70;light.innerSpotAngle=45;light.shadowStrength=.8f;
    }
    else if(light.name=="실내반사광"){light.intensity=32;light.color=new Color(1,.92f,.8f);}
    else if(light.name=="문쪽반사광"){light.intensity=38;light.color=new Color(1,.89f,.73f);}
    else if(light.name=="매달린종이등"){light.intensity=12;light.color=new Color(1,.68f,.37f);}
   }
   AddFill(root,"기둥앞면_종이반사광",new Vector3(-3,8,-17),new Vector3(-11,8,-11),110,19,new Color(1,.9f,.76f));
   AddFill(root,"오른쪽_은은한반사광",new Vector3(4,9,-15),new Vector3(11,8,-11),75,18,new Color(.91f,.91f,1));
   AddFill(root,"후면기둥_반사광",new Vector3(0,9,4),new Vector3(11,9,11),75,20,new Color(1,.86f,.67f));
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_BookTower/Generated/F0Reference/F0_Grade.asset");
   if(profile.TryGet<ColorAdjustments>(out var grade)){grade.postExposure.Override(.2f);grade.contrast.Override(6);grade.saturation.Override(-3);}
   if(profile.TryGet<Bloom>(out var bloom)){bloom.threshold.Override(1.35f);bloom.intensity.Override(.24f);bloom.scatter.Override(.65f);}
   EditorUtility.SetDirty(profile);
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/WindowHaze.mat");
   if(!mat){mat=new Material(Shader.Find("BookTower/WindowHaze"));AssetDatabase.CreateAsset(mat,Folder+"/WindowHaze.mat");}
   mat.SetColor("_Color",new Color(1,.84f,.57f,.075f));EditorUtility.SetDirty(mat);
   int index=0;
   foreach(float z in new[]{-5.2f,5.4f})for(int band=0;band<3;band++)
   {
    Vector3 start=new(-15.35f,10.6f-band*.65f,z-1.1f+band*.8f),end=new(-.5f,3.25f,z+6+band*.6f);
    Vector3 across=Vector3.Cross((end-start).normalized,Vector3.forward).normalized;
    var mesh=new Mesh{name="Soft window light shaft"};float w=.24f+band*.05f;
    mesh.vertices=new[]{start-across*w,start+across*w,end-across*(w*2),end+across*(w*2)};
    mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateBounds();
    string path=Folder+"/Shaft"+index+++".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved){saved.Clear();saved.vertices=mesh.vertices;saved.uv=mesh.uv;saved.triangles=mesh.triangles;saved.UploadMeshData(false);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(mesh,path);
    var go=new GameObject("창가의_넓은빛줄기");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   }
   Dust(root);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  static void AddFill(Transform root,string name,Vector3 at,Vector3 target,float intensity,float range,Color color)
  {
   var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=at;go.transform.LookAt(root.TransformPoint(target));
   var l=go.AddComponent<Light>();l.type=LightType.Spot;l.intensity=intensity;l.range=range;l.spotAngle=95;l.innerSpotAngle=60;l.color=color;l.shadows=LightShadows.None;
  }
  static void Dust(Transform root)
  {
   var go=new GameObject("창가에_떠다니는종이먼지");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(-7,8,0);
   var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.duration=20;main.loop=true;main.prewarm=true;main.startLifetime=new ParticleSystem.MinMaxCurve(12,20);main.startSpeed=.035f;main.startSize=new ParticleSystem.MinMaxCurve(.013f,.034f);main.startColor=new Color(1,.86f,.59f,.22f);main.maxParticles=170;main.simulationSpace=ParticleSystemSimulationSpace.Local;
   var emission=ps.emission;emission.rateOverTime=7;
   var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(12,8,22);
   var noise=ps.noise;noise.enabled=true;noise.strength=.065f;noise.frequency=.18f;noise.scrollSpeed=.035f;
   var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(.65f,.75f),new GradientAlphaKey(0,1)});color.color=gradient;
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Dust.mat");if(!mat){mat=new Material(Shader.Find("BookTower/WindowHaze"));AssetDatabase.CreateAsset(mat,Folder+"/Dust.mat");}mat.SetColor("_Color",new Color(1,.86f,.6f,.18f));EditorUtility.SetDirty(mat);
   mat.SetFloat("_Particle",1);EditorUtility.SetDirty(mat);
   var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;
  }
  public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();F0PlayVerifier.Run();}
 }
}
