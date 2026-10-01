using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 public static class ReleaseLightingBuilder
 {
  public const string Folder="Assets/_BookTower/Generated/ReleaseLighting";
  public static void Run()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();ReleaseLightingReview.After();
  }
  public static void Apply()
  {
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var scene=EditorSceneManager.GetActiveScene();Directory.CreateDirectory("Logs/ReleaseLightingBackups");EditorSceneManager.SaveScene(scene,"Logs/ReleaseLightingBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   // Extend the same material rules to the third room. Preserve previous stage's evidence.
   ReleaseMaterialAudit.RoomIds=new[]{"1B"};ReleaseMaterialAudit.Output=ReleaseLightingReview.Output+"/1B-Materials";
   ReleaseMaterialAudit.Write("Before");ReleaseMaterialBuilder.Apply();
   var owned=ReleaseLightingReview.PuzzleLights();string contract=Contract(owned);var baseline=Folder+"/LightBaseline.json";var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);
   // Original values are restored before each tuning pass to avoid multiplying twice.
   var archive=File.Exists(baseline)?JsonUtility.FromJson<Archive>(File.ReadAllText(baseline)):new Archive{entries=lights.Select(l=>new Entry{id=GlobalObjectId.GetGlobalObjectIdSlow(l).ToString(),intensity=l.intensity,color=l.color,range=l.range,shadows=l.shadows,bias=l.shadowBias,normalBias=l.shadowNormalBias}).ToArray()};
   if(!File.Exists(baseline))File.WriteAllText(baseline,JsonUtility.ToJson(archive,true));
   var rows=new StringBuilder();foreach(var l in lights)
   {
    var r=l.GetComponentInParent<Room>();if(!ReleaseLightingReview.Target(r)||owned.Contains(l))continue;
    var entry=archive.entries.FirstOrDefault(e=>e.id==GlobalObjectId.GetGlobalObjectIdSlow(l).ToString());if(entry==null)continue;
    l.intensity=entry.intensity;l.color=entry.color;l.range=entry.range;l.shadows=entry.shadows;l.shadowBias=entry.bias;l.shadowNormalBias=entry.normalBias;
    if(l.intensity<=0)continue;
    var name=l.name;
    if(r.RoomId=="F0")
    {
     if(name=="시작대지_장지반사")l.intensity=55;
     else if(name=="서쪽책등_부드러운반사")l.intensity=65;
     else if(name=="동쪽책등_부드러운반사")l.intensity=45;
     else if(name=="후면표지_반사")l.intensity=55;
     else if(name=="문과건너편_반사")l.intensity=80;
     else if(name=="종이창_투광"){bool left=r.transform.InverseTransformPoint(l.transform.position).x<0;l.intensity=left?155:30;l.color=left?new Color(1,.88f,.73f):new Color(.89f,.91f,1);if(left){l.shadowBias=.02f;l.shadowNormalBias=.045f;}}
     else if(name=="다음 길의 등불")l.intensity=25;
    }
    if(r.RoomId=="1A"&&name=="따뜻한 종이등 빛"){l.intensity=entry.intensity*.56f;l.shadows=LightShadows.None;l.color=new Color(1,.92f,.81f);}
    if(r.RoomId=="1B"&&name=="warm paper light"){l.intensity=entry.intensity*.62f;l.color=Color.Lerp(entry.color,new Color(1,.85f,.66f),.35f);}
    if(r.RoomId=="1B"&&name=="shadow-casting reading light"){l.intensity=entry.intensity*.8f;l.shadowBias=.025f;l.shadowNormalBias=.06f;}
    EditorUtility.SetDirty(l);PrefabUtility.RecordPrefabInstancePropertyModifications(l);rows.AppendLine($"{r.RoomId}: {ReleaseLightingReview.PathOf(l.transform)} {entry.intensity} -> {l.intensity}");
   }
   var rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target).ToArray();
   foreach(var room in rooms)
   {
    var so=new SerializedObject(room);so.FindProperty("sunIntensity").floatValue=room.RoomId=="F0"?.08f:room.RoomId=="1A"?.12f:.06f;so.FindProperty("ambient").floatValue=room.RoomId=="F0"?.48f:room.RoomId=="1A"?.43f:.30f;so.FindProperty("fog").floatValue=room.RoomId=="1B"?.001f:.0012f;so.ApplyModifiedPropertiesWithoutUndo();
    if(room.RoomId=="1A")
    {var found=room.transform.Find("ReleaseArt03_entrance-key");var key=found?found.GetComponent<Light>():new GameObject("ReleaseArt03_entrance-key").AddComponent<Light>();key.transform.SetParent(room.transform,false);key.transform.localPosition=new Vector3(-11,12,-8);key.transform.LookAt(room.transform.TransformPoint(new Vector3(0,0,-6)));key.type=LightType.Spot;key.intensity=60;key.range=25;key.spotAngle=70;key.innerSpotAngle=45;key.color=new Color(1,.91f,.77f);key.shadows=LightShadows.Soft;key.shadowBias=.025f;key.shadowNormalBias=.06f;}
   }
   if(contract!=Contract(owned))throw new Exception("Puzzle lights, transforms or collisions changed while tuning atmosphere");
   BakeReflections(rooms);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory(ReleaseLightingReview.Output);File.WriteAllText(ReleaseLightingReview.Output+"/apply.txt","PASS: existing geometry, collision, optical transforms and puzzle-owned light values unchanged.\n"+rows);
  }
  [Serializable]public class Entry{public string id;public float intensity,range,bias,normalBias;public Color color;public LightShadows shadows;}
  [Serializable]public class Archive{public Entry[] entries;}
  static string Contract(HashSet<Light> owned)=>string.Join("|",Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>$"{c.GetInstanceID()}:{c.bounds}:{c.enabled}").Concat(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Select(r=>$"{r.GetInstanceID()}:{r.transform.localToWorldMatrix}:{r.sharedMaterials.Length}")).Concat(owned.Select(l=>$"{l.GetInstanceID()}:{l.transform.localToWorldMatrix}:{l.intensity}:{l.color}:{l.shadows}:{l.range}")));
  static void BakeReflections(Room[] rooms)
  {
   var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToDictionary(r=>r,r=>r.enabled);var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).ToDictionary(l=>l,l=>(l.enabled,l.intensity));var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;var oldProbe=RenderSettings.ambientProbe;bool oldFog=RenderSettings.fog;
   var puzzleLights=ReleaseLightingReview.PuzzleLights();var exclude=new HashSet<Renderer>();void Hide(Transform t){if(t)foreach(var r in t.GetComponentsInChildren<Renderer>(true))exclude.Add(r);}
   foreach(var r in renderers.Keys)if(r is SkinnedMeshRenderer||r is LineRenderer||r is ParticleSystemRenderer)exclude.Add(r);
   foreach(var p in Object.FindObjectsByType<FoldBridgePuzzle>(FindObjectsSortMode.None))Hide(p.transform);
   foreach(var p in Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsSortMode.None)){Hide(p.body);Hide(p.ears);if(p.Duet){Hide(p.Duet.foxBody);Hide(p.Duet.foxTail);}}
   foreach(var p in Object.FindObjectsByType<ReadingRoomPuzzle>(FindObjectsSortMode.None)){Hide(p.lantern);Hide(p.servicePanel);Hide(p.shutter);Hide(p.door);}
   try
   {
    foreach(var room in rooms)
    {
     foreach(var r in renderers.Keys)r.enabled=renderers[r]&&!exclude.Contains(r)&&r.GetComponentInParent<Room>()==room;
     foreach(var l in lights.Keys){l.enabled=!puzzleLights.Contains(l)&&(l.GetComponentInParent<Room>()==room||l.type==LightType.Directional);if(l.type==LightType.Directional)l.intensity=room.SunIntensity;}
     RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.667f,.647f,.608f)*room.Ambient;var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(RenderSettings.ambientLight);RenderSettings.ambientProbe=sh;RenderSettings.fog=false;
     var t=room.transform.Find("ReleaseArt03_room-reflection");var probe=t?t.GetComponent<ReflectionProbe>():new GameObject("ReleaseArt03_room-reflection").AddComponent<ReflectionProbe>();probe.transform.SetParent(room.transform,false);probe.transform.localPosition=new Vector3(0,room.RoomId=="F0"?8:7,0);probe.size=room.RoomId=="F0"?new Vector3(32,16,36):room.RoomId=="1A"?new Vector3(60,16,56):new Vector3(36,20,44);probe.center=Vector3.zero;probe.boxProjection=true;probe.blendDistance=2;probe.intensity=.65f;probe.mode=ReflectionProbeMode.Custom;probe.resolution=256;probe.hdr=true;probe.renderDynamicObjects=true;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.10f,.08f,.055f);probe.nearClipPlane=.1f;probe.farClipPlane=65;
     string path=Folder+"/"+room.RoomId+"-interior.exr";if(!Lightmapping.BakeReflectionProbe(probe,path))throw new Exception("Reflection capture failed "+room.RoomId);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);if(!probe.customBakedTexture)throw new Exception("Missing cubemap "+path);EditorUtility.SetDirty(probe);
    }
   }
   finally{foreach(var p in renderers)p.Key.enabled=p.Value;foreach(var p in lights){p.Key.enabled=p.Value.Item1;p.Key.intensity=p.Value.Item2;}RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientProbe=oldProbe;RenderSettings.fog=oldFog;}
  }
 }
}
