using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
 public static class ReleaseMaterialAudit
 {
  public static string Output="Docs/ArtReview/ReleaseArt/02-Materials";
  public static string[] RoomIds={"F0","1A"};
  public static bool InScope(Renderer r)
  {
   var room=r.GetComponentInParent<Room>();if(room)return RoomIds.Contains(room.RoomId);
   var bound=r.GetComponentInParent<RoomBound>();if(bound)return RoomIds.Any(bound.Belongs);
   return r.GetComponentInParent<BookTower.Presentation.HoodSpiritAnimator>()||r.GetComponentInParent<BookTower.Story.MoaSculptMotion>();
  }
  public static Renderer[] Renderers()=>Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(InScope).ToArray();
  public static float Density(Renderer r)
  {
   var filter=r.GetComponent<MeshFilter>();var mesh=r is SkinnedMeshRenderer skin?skin.sharedMesh:filter?filter.sharedMesh:null;if(!mesh||!mesh.isReadable)return 0;
   var vertices=mesh.vertices;var uv=mesh.uv;if(uv.Length!=vertices.Length)return 0;var tri=mesh.triangles;double area=0,uvArea=0;
   int stride=Mathf.Max(1,tri.Length/3000)*3;
   for(int i=0;i+2<tri.Length;i+=stride){int a=tri[i],b=tri[i+1],c=tri[i+2];var x=uv[b]-uv[a];var y=uv[c]-uv[a];float ua=Mathf.Abs(x.x*y.y-x.y*y.x);float wa=Vector3.Cross(r.transform.TransformVector(vertices[b]-vertices[a]),r.transform.TransformVector(vertices[c]-vertices[a])).magnitude;if(ua<1e-9||wa<1e-9)continue;area+=wa;uvArea+=ua;}
   return area>0?(float)Math.Sqrt(uvArea/area):0;
  }
  static string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
  public static void Write(string stage)
  {
   Directory.CreateDirectory(Output+"/"+stage);var rows=new StringBuilder("material,shader,renderer_count,base_texture,normal_texture,normal_strength,smoothness,metallic,sample_objects\n");var rs=Renderers();
   foreach(var mat in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().OrderBy(AssetDatabase.GetAssetPath))
   {var users=rs.Where(r=>r.sharedMaterials.Contains(mat)).ToArray();string T(string key)=>mat.HasProperty(key)?AssetDatabase.GetAssetPath(mat.GetTexture(key)):"";float F(string key)=>mat.HasProperty(key)?mat.GetFloat(key):0;
    rows.AppendLine(string.Join(",",Q(AssetDatabase.GetAssetPath(mat)),Q(mat.shader.name),users.Length,Q(T("_BaseMap")),Q(T("_BumpMap")),F("_BumpScale"),F("_Smoothness"),F("_Metallic"),Q(string.Join(" | ",users.Take(4).Select(r=>r.name)))));}
   File.WriteAllText(Output+"/"+stage+"/materials.csv",rows.ToString(),new UTF8Encoding(false));
   var density=new StringBuilder("renderer,world_size,uv_units_per_world_unit,material_slots\n");foreach(var r in rs)density.AppendLine($"{Q(r.name)},{Q(r.bounds.size.ToString("F3"))},{Density(r):F5},{r.sharedMaterials.Length}");File.WriteAllText(Output+"/"+stage+"/density.csv",density.ToString(),new UTF8Encoding(false));
  }
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Write("Before");EditorApplication.Exit(0);}
 }
}
