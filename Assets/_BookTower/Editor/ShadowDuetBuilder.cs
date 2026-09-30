using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 public static class ShadowDuetBuilder
 {
  const string Dir="Assets/_BookTower/Generated/ShadowDuet";
  static Material cream,coral,teal,honey,ink,edge;static int id;
  public static void Run()
  {
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();id=0;
   Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>(StorybookRoomBuilder.WorkshopDir+"/"+name+".mat");
   cream=M("02_Ivory_LayeredPaper");coral=M("03_Coral_CrayonPaper");teal=M("04_Teal_CrayonPaper");honey=M("05_Honey_Cotton");ink=M("09_FadedOutline");edge=M("08_DeckledEdges");
   var root=new GameObject("토끼와 여우의 세 빛 그림자 극장");var duet=root.AddComponent<ShadowDuet>();
   Vector2[] body={new(-.8f,.05f),new(-1.0f,.5f),new(-.8f,1.3f),new(-.4f,1.8f),new(-.52f,2.35f),new(-.9f,3.45f),new(-.18f,3.04f),new(.42f,3.36f),new(.74f,3.82f),new(.92f,2.83f),new(1.42f,2.48f),new(1.65f,2.35f),new(1.19f,2.08f),new(.68f,1.9f),new(.45f,.72f),new(.89f,.15f),new(.63f,.02f)};
   Vector2[] tail={new(.13f,.07f),new(-.7f,.03f),new(-1.48f,.35f),new(-2.05f,1.09f),new(-2.22f,2.5f),new(-1.72f,2.2f),new(-1.05f,1.91f),new(-.72f,1.32f),new(-.67f,.73f),new(-.26f,.45f)};
   body=Round(body,new[]{5,8,10,11});tail=Round(tail,new[]{4,5});
   duet.foxBody=Fox(root.transform,"여우 몸통 종이 꽂이",body,ShadowDuet.FoxBodySolution,coral,out duet.bodyProbes,out duet.bodyTargets);
   duet.foxTail=Fox(root.transform,"여우 꼬리 종이 꽂이",tail,ShadowDuet.FoxTailSolution,teal,out duet.tailProbes,out duet.tailTargets);
   duet.foxBody.GetComponent<PaperShadowHandle>().extraPart=2;duet.foxTail.GetComponent<PaperShadowHandle>().extraPart=3;
   var guides=new List<Renderer>();
   foreach(var outline in new[]{body,tail}){var path=outline.Select(v=>new Vector3(v.x+ShadowDuet.FoxX,v.y+1,12.36f)).ToArray();guides.Add(Stroke(root.transform,"맞춰야 할 여우 윤곽",path,.024f,ink,true).GetComponent<Renderer>());}
   duet.foxRecord=guides.ToArray();duet.rabbitRecord=Array.Empty<Renderer>();
   // Small framed source diagram: both the folds and projected angle are visible clues.
   foreach(float x in new[]{ShadowDuet.RabbitX,ShadowDuet.FoxX}){
    Stroke(root.transform,"그림자를 기억하는 종이 액자",new[]{new Vector3(x-2.25f,.5f,12.33f),new Vector3(x-2.25f,5.5f,12.33f),new Vector3(x+2.25f,5.5f,12.33f),new Vector3(x+2.25f,.5f,12.33f)},.045f,honey,true);
    for(int k=0;k<3;k++)Leaf(root.transform,new Vector3(x-.25f+k*.25f,5.73f,12.31f),.24f,k==1?teal:coral,k*35-35);
   }
   duet.lamps=new Light[3];duet.indicators=new Renderer[3];
   for(int i=0;i<3;i++){
    float x=(i-1)*4;var g=Group(root.transform,new[]{"왼쪽 창의 투사등","가운데 창의 투사등","오른쪽 창의 투사등"}[i],new Vector3(x,0,0));
    var control=g.gameObject.AddComponent<StorybookLightControl>();control.action=(StorybookLightControl.Action)((int)StorybookLightControl.Action.LightLeft+i);
    var it=g.GetComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=g.name;so.FindProperty("verb").stringValue="이 방향에서 빛 비추기";so.FindProperty("range").floatValue=12;so.FindProperty("lift").floatValue=0;so.FindProperty("animatePosition").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
    Box(g,"누빈 종이 등 받침",new Vector3(0,.16f,0),new Vector3(1.45f,.32f,1.35f),honey);
    Stroke(g,"둘러 감은 종이띠",new[]{new Vector3(-.67f,.22f,-.64f),new Vector3(.67f,.22f,-.64f),new Vector3(.67f,.22f,.64f),new Vector3(-.67f,.22f,.64f)},.025f,cream,true);
    for(int k=0;k<8;k++){
     float a=k*Mathf.PI/4;var petal=Leaf(g,new Vector3(Mathf.Cos(a)*.25f,.34f,Mathf.Sin(a)*.25f),.75f,cream,0);
     petal.localRotation=Quaternion.Euler(35,k*45,0);
    }
    Ring(g,"장지 렌즈 테두리",new Vector3(0,1.02f,.20f),.4f,honey);
    Ring(g,"겹친 장지 렌즈",new Vector3(0,1.02f,.24f),.33f,cream);
    var bulb=Leaf(g,new Vector3(0,.77f,-.08f),.50f,M("10_StarLantern"),0);duet.indicators[i]=bulb.GetComponent<Renderer>();
    Box(g,"종이 렌즈 축",new Vector3(0,.52f,0),new Vector3(.18f,.7f,.18f),honey);
    var col=g.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.65f,0);col.size=new Vector3(1.5f,1.4f,1.3f);
    var l=new GameObject("실제 그림자 광원").AddComponent<Light>();l.transform.SetParent(g,false);l.transform.localPosition=new Vector3(0,1,.30f);l.type=LightType.Spot;l.transform.localPosition=new Vector3(0,1,0);l.range=24;l.spotAngle=100;l.innerSpotAngle=85;l.intensity=i==1?160:0;l.color=new Color(1,.87f,.64f);l.shadows=LightShadows.Hard;
    var data=l.GetUniversalAdditionalLightData();var ld=new SerializedObject(data);ld.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;ld.ApplyModifiedPropertiesWithoutUndo();duet.lamps[i]=l;
    foreach(var r in g.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
    // Directional chevrons are raised paper inlays, not labels floating over the room.
    for(int k=0;k<3;k++)Stroke(g,"빛 방향을 가리키는 제본 문양",new[]{new Vector3(-.2f,.055f,.95f+k*.30f),new Vector3(0,.055f,1.10f+k*.30f),new Vector3(.2f,.055f,.95f+k*.30f)},.022f,i==0?coral:i==1?honey:teal,false);
   }
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/_BookTower/Resources/ShadowDuet.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("ShadowDuet art created");if(Application.isBatchMode)EditorApplication.Exit(0);
  }
  static Vector2[] Round(Vector2[] points,int[] tips)
  {
   var list=new List<Vector2>();for(int i=0;i<points.Length;i++){
    float trim=tips.Contains(i)?.015f:.22f;
    var a=Vector2.Lerp(points[i],points[(i+points.Length-1)%points.Length],trim);var b=Vector2.Lerp(points[i],points[(i+1)%points.Length],trim);
    for(int k=0;k<=4;k++){float t=k/4f;list.Add((1-t)*(1-t)*a+2*t*(1-t)*points[i]+t*t*b);}
   }return list.ToArray();
  }
  static Transform Fox(Transform parent,string name,Vector2[] outline,Vector3 solved,Material material,out Vector3[] probes,out Vector3[] targets)
  {
   var g=Group(parent,name,Vector3.zero);targets=outline.Select(p=>new Vector3(p.x+ShadowDuet.FoxX,p.y+1,12.5f)).ToArray();
   probes=new Vector3[outline.Length];var lamp=new Vector3(-4,1,0);
   // A creased 3D silhouette, reverse-projected from the actual left lamp. Other lamps deform its outline.
   for(int i=0;i<outline.Length;i++){float depth=solved.z+.78f*Mathf.Sin(outline[i].y*2.8f+outline[i].x*2.4f);probes[i]=lamp+(targets[i]-lamp)*(depth/12.5f)-solved;}
   Face(g,"잘라낸 입체 장지 오림",probes,outline,material);
   Face(g,"겹친 오림 뒷면",probes.Select(v=>v+Vector3.forward*.025f).ToArray(),outline,cream);
   Stroke(g,"구김 있는 오림의 섬유 가장자리",probes,.018f,edge,true);
   for(int i=2;i<probes.Length;i+=4)Stroke(g,"접힌 면의 얇은 주름",new[]{probes[0]+Vector3.back*.015f,probes[i]+Vector3.back*.015f},.007f,honey,false);
   Box(g,"잡을 수 있는 종이 꽂이 발",new Vector3(0,-.78f,0),new Vector3(1.1f,.2f,.65f),honey);
   Stroke(g,"잡을 수 있는 긴 종이 기둥",new[]{new Vector3(0,-.75f,.05f),new Vector3(0,.55f,.05f)},.07f,cream,false);
   var h=g.gameObject.AddComponent<PaperShadowHandle>();var it=g.GetComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=name;so.FindProperty("verb").stringValue="클릭 유지: 좌우 · 휠: 거리 · 우클릭: 뒤집기";so.FindProperty("range").floatValue=12;so.FindProperty("lift").floatValue=0;so.FindProperty("animatePosition").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();h.FitWholeObject();
   foreach(var r in g.GetComponentsInChildren<Renderer>())r.shadowCastingMode=r.name.StartsWith("잘라낸")||r.name.StartsWith("겹친 오림")?ShadowCastingMode.TwoSided:ShadowCastingMode.Off;
   return g;
  }
  static Transform Group(Transform parent,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
  static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material mat){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(p,false);t.localPosition=at;t.localScale=size;Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=mat;return t;}
  static Transform Face(Transform p,string name,Vector3[] vertices,Vector2[] flat,Material mat)
  {var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=flat;mesh.triangles=Triangles(flat);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh);var t=Group(p,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
  static Mesh Save(Mesh m){string path=Dir+"/part_"+(id++).ToString("D3")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}else AssetDatabase.CreateAsset(m,path);return m;}
  static Transform Stroke(Transform p,string name,Vector3[] path,float radius,Material mat,bool loop)
  {
   int count=path.Length;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
   for(int i=0;i<count;i++){var d=(path[(i+1)%count]-path[(i+count-1)%count]).normalized;var side=Vector3.Cross(d,Vector3.forward);if(side.sqrMagnitude<.01f)side=Vector3.Cross(d,Vector3.up);side.Normalize();var other=Vector3.Cross(d,side).normalized;for(int j=0;j<6;j++){float a=j*Mathf.PI/3;v.Add(path[i]+radius*(side*Mathf.Cos(a)+other*Mathf.Sin(a)));uv.Add(new Vector2(j/6f,i*.15f));}}
   for(int i=0;i<(loop?count:count-1);i++)for(int j=0;j<6;j++){int a=i*6+j,b=i*6+(j+1)%6,c=((i+1)%count)*6+j,d=((i+1)%count)*6+(j+1)%6;tri.AddRange(new[]{a,c,b,b,c,d});}
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh);var t=Group(p,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;
  }
  static Transform Leaf(Transform p,Vector3 at,float size,Material mat,float angle){var flat=new[]{new Vector2(0,0),new Vector2(-.38f,.35f),new Vector2(-.27f,.8f),new Vector2(0,1),new Vector2(.27f,.8f),new Vector2(.38f,.35f)};var t=Face(p,"겹종이 꽃잎",flat.Select((v,i)=>new Vector3(v.x*size,v.y*size,i%2*.035f)).ToArray(),flat,mat);t.localPosition=at;t.localRotation=Quaternion.Euler(0,0,angle);return t;}
  static void Ring(Transform p,string name,Vector3 at,float radius,Material mat){Stroke(p,name,Enumerable.Range(0,32).Select(i=>at+new Vector3(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16),0)*radius).ToArray(),.055f,mat,true);}
  static int[] Triangles(Vector2[] p)
  {
   float area=0;for(int i=0;i<p.Length;i++)area+=p[i].x*p[(i+1)%p.Length].y-p[(i+1)%p.Length].x*p[i].y;var work=Enumerable.Range(0,p.Length).ToList();if(area<0)work.Reverse();var tri=new List<int>();
   float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
   while(work.Count>2){bool clipped=false;for(int i=0;i<work.Count;i++){int a=work[(i+work.Count-1)%work.Count],b=work[i],c=work[(i+1)%work.Count];if(Cross(p[a],p[b],p[c])<=.00001f)continue;if(work.Any(k=>k!=a&&k!=b&&k!=c&&Cross(p[a],p[b],p[k])>=0&&Cross(p[b],p[c],p[k])>=0&&Cross(p[c],p[a],p[k])>=0))continue;tri.AddRange(new[]{a,c,b});work.RemoveAt(i);clipped=true;break;}if(!clipped)throw new Exception("Invalid silhouette");}return tri.ToArray();
  }
 }
}
