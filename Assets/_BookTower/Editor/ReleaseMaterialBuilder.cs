using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 public static class ReleaseMaterialBuilder
 {
  public const string Folder="Assets/_BookTower/Generated/ReleaseMaterials";
  static readonly Dictionary<string,Material> replacements=new();
  static readonly Dictionary<Material,Material> originals=new();
  public static IReadOnlyDictionary<Material,Material> Originals=>originals;
  static Texture2D Tex(string family,string map)=>AssetDatabase.LoadAssetAtPath<Texture2D>(ReleasePaperMaps.Folder+"/"+family+"_"+map+".png");
  static Material Source(Material material)
  {
   string path=AssetDatabase.GetAssetPath(material);if(!path.StartsWith(Folder+"/Materials/"))return material;
   string guid=Path.GetFileNameWithoutExtension(path).Split('_')[0];var source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));return source?source:material;
  }
  public static string Family(Material m)
  {
   string path=AssetDatabase.GetAssetPath(m),n=m.name.ToLowerInvariant();
   if(m.shader.name=="BookTower/ReadingPaper"&&n=="ivory_atlas")return "Paper";
   if(m.shader.name!="Universal Render Pipeline/Lit"||path.StartsWith(Folder))return null;
   if(n=="hoodpaper")return "Spirit";if(n=="moapaper")return "Moa";
   string tex=AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"));
   if(tex.Contains("Atlas"))return n.Contains("crayonmural")?"Mural":null;
   if(!tex.Contains("MulberryFiber")&&!tex.Contains("PaperFiber")&&!tex.Contains("F0Reference"))return null;
   if(n.Contains("optical")||n.Contains("lantern")||n.Contains("window"))return null;
   if(n.Contains("foil")||n.Contains("silver"))return "Foil";
   if(n.Contains("brass")||n.Contains("gold"))return "Brass";
   if(n.Contains("thread")||n.Contains("rope")||n.Contains("cotton")&&!n.Contains("edge"))return "Cotton";
   if(n.Contains("edge")||n.Contains("foldedge"))return "Edge";
   if(n.Contains("dyed")||n.Contains("cover")||n.Contains("binding")||n.Contains("crayon")||n.Contains("book")||n.Contains("burgundy")||n.Contains("indigo"))return "Cover";
   return "Paper";
  }
  static Material Baseline(Material original)
  {
   if(originals.TryGetValue(original,out var previous))return previous;
   string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));string path=Folder+"/Baseline/"+id+".mat";
   var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(!saved){saved=new Material(original){name=original.name};AssetDatabase.CreateAsset(saved,path);}originals.Add(original,saved);return saved;
  }
  static Material Replacement(Material source,string family,float density)
  {
   bool atlas=family=="Spirit"||family=="Moa"||family=="Mural";
   // One 1K tile per ~2u. Quantised tiling avoids hundreds of unique per-object materials.
   float tiling=atlas||density<=0?1:Mathf.Pow(2,Mathf.Round(Mathf.Log(Mathf.Clamp(.5f/density,.25f,32),2)));
   string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));string key=id+"_"+tiling.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
   if(replacements.TryGetValue(key,out var found))return found;
   var original=Baseline(source);string path=Folder+"/Materials/"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(original);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(original);
   if(m.shader.name=="BookTower/ReadingPaper"){m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetFloat("_Cull",0);}
   m.name=key;m.SetFloat("_BumpScale",family=="Foil"?.7f:family=="Brass"?.55f:family=="Cotton"?.8f:.8f);m.SetFloat("_Smoothness",1);m.SetFloat("_Metallic",0);m.SetFloat("_SmoothnessTextureChannel",0);
   m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
   if(family=="Spirit"||family=="Moa")
   {m.SetTexture("_BaseMap",Tex(family,"Padded_BaseColor"));m.SetTexture("_BumpMap",Tex(family,"Padded_Normal"));m.SetTexture("_MetallicGlossMap",Tex(family,"Padded_MetallicSmoothness"));m.SetFloat("_BumpScale",family=="Spirit"?.12f:.20f);m.SetFloat("_Smoothness",.7f);}
   else if(family=="Mural")
   {m.SetTexture("_BumpMap",Tex("Paper","Normal"));m.SetFloat("_BumpScale",.3f);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.12f);}
   else
   {var colour=family=="Paper"?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png"):Tex(family,"Color");
    m.SetTexture("_BaseMap",colour);m.SetTexture("_BumpMap",Tex(family,"Normal"));m.SetTexture("_MetallicGlossMap",Tex(family,"Mask"));m.SetTextureScale("_BaseMap",Vector2.one*tiling);m.SetTextureOffset("_BaseMap",Vector2.zero);m.SetTexture("_MainTex",colour);
    // These are matt dyed papers, not leather. Preserve the authored room palette.
    if(family=="Brass"||family=="Foil"){m.SetFloat("_EnvironmentReflections",1);m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
   }
   m.enableInstancing=true;EditorUtility.SetDirty(m);replacements[key]=m;return m;
  }
  public static void Apply()
  {
   Directory.CreateDirectory(Folder+"/Baseline");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();replacements.Clear();originals.Clear();
   var scene=EditorSceneManager.GetActiveScene();Directory.CreateDirectory("Logs/ReleaseMaterialBackups");EditorSceneManager.SaveScene(scene,"Logs/ReleaseMaterialBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var renderers=ReleaseMaterialAudit.Renderers();string before=Contract();int slots=renderers.Sum(r=>r.sharedMaterials.Length);int changed=0;
   foreach(var renderer in renderers)
   {var mats=renderer.sharedMaterials;float density=-1;bool dirty=false;for(int i=0;i<mats.Length;i++){var original=Source(mats[i]);if(!original)continue;string family=Family(original);if(family==null)continue;if(density<0)density=ReleaseMaterialAudit.Density(renderer);mats[i]=Replacement(original,family,density);dirty=true;}if(dirty){renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);EditorUtility.SetDirty(renderer);changed++;}}
   // Runtime may instantiate these prefabs again, so apply the same palette to their renderers.
   var prefabPaths=new List<string>{"Assets/_BookTower/Resources/FoyerPassageCeiling.prefab","Assets/_BookTower/Art/PaperHoodSpirit/HoodSpirit.prefab","Assets/_BookTower/Art/MoaSculpt/MoaSculpt.prefab"};
   if(ReleaseMaterialAudit.RoomIds.Contains("1B"))prefabPaths.AddRange(new[]{"Assets/_BookTower/Resources/ReadingMemoryLantern.prefab","Assets/_BookTower/Resources/ReadingLanternDock.prefab"});
   foreach(string path in prefabPaths)
   {var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var r in root.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;float density=ReleaseMaterialAudit.Density(r);for(int i=0;i<mats.Length;i++){var source=Source(mats[i]);if(!source)continue;var family=Family(source);if(family!=null)mats[i]=Replacement(source,family,density);}r.sharedMaterials=mats;}PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
   if(before!=Contract()||slots!=renderers.Sum(r=>r.sharedMaterials.Length))throw new Exception("Geometry, collision, lights or material slots changed");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory(ReleaseMaterialAudit.Output);File.WriteAllText(ReleaseMaterialAudit.Output+"/apply.txt",$"PASS: geometry/colliders/light settings/slot counts unchanged.\nChanged renderers={changed}; material slots before=after={slots}; material variants={replacements.Count}; original materials={originals.Count}\n");
   var textures=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t=>t).Distinct().ToArray();File.WriteAllText(ReleaseMaterialAudit.Output+"/memory-after.txt",$"Referenced texture runtime bytes={textures.Sum(t=>Profiler.GetRuntimeMemorySizeLong(t))}\n"+string.Join("\n",textures.Select(t=>$"{AssetDatabase.GetAssetPath(t)}: {Profiler.GetRuntimeMemorySizeLong(t)} bytes")));
   ReleaseMaterialAudit.Write("After");
  }
  static string Contract()=>string.Join("|",ReleaseMaterialAudit.Renderers().Select(r=>$"{r.GetInstanceID()}:{r.transform.localToWorldMatrix}:{r.shadowCastingMode}:{r.receiveShadows}").Concat(Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>$"{c.GetInstanceID()}:{c.bounds}:{c.enabled}")).Concat(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Select(l=>$"{l.GetInstanceID()}:{l.transform.localToWorldMatrix}:{l.intensity}:{l.color}:{l.shadows}")));
 }
}
