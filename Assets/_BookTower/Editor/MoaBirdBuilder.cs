using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.Story;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class MoaBirdBuilder
    {
        public const string Dir="Assets/_BookTower/Art/PaperMoa";
        public const string PrefabPath=Dir+"/PaperMoa.prefab";
        static Material paper,wrap,ochre,ink,gold;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        [MenuItem("Book Tower/모아/둥근 종이새와 애니메이션 적용")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/InGame.unity")throw new InvalidOperationException("Open InGame first");
            var director=Object.FindFirstObjectByType<OpeningStoryDirector>();if(!director||!director.companion)throw new InvalidOperationException("Opening story companion missing");
            Directory.CreateDirectory("Logs/MoaBackups");EditorSceneManager.SaveScene(scene,"Logs/MoaBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var old=director.companion;var bird=Create(old.transform.parent);
            bird.transform.SetPositionAndRotation(director.player.transform.TransformPoint(V(.72f,.025f,-.48f)),director.player.transform.rotation);
            bird.player=director.player;director.companion=bird;Object.DestroyImmediate(old.gameObject);EditorUtility.SetDirty(director);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("[Moa] Rounded layered-paper bird, idle/run blend and flutter layer applied.");
        }
        public static void BuildAndVerifyBatch()
        {EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();MoaBirdVerifier.RunBatch();}
        public static PaperCompanion Create(Transform parent)
        {
            Directory.CreateDirectory(Dir);Directory.CreateDirectory(Dir+"/Animation");AssetDatabase.Refresh();
            paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/Paper.mat");
            wrap=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/PaperWrap.mat");
            ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/Ink.mat");
            if(!paper||!wrap||!ink)throw new InvalidOperationException("Paper spirit materials are required");
            // These are closed volumes with an explicit inner surface. Cull hidden reverse
            // faces while retaining the spirit's exact fibre maps and rough paper response.
            paper=ClosedPaper("MoaPaper",paper);wrap=ClosedPaper("MoaPaperLayers",wrap);
            ochre=Tint("FoldedOchre",new Color(.72f,.43f,.16f));gold=Tint("FadedGoldInk",new Color(.77f,.49f,.15f));
            var root=Group(parent,"모아_둥근종이새",Vector3.zero);var c=root.gameObject.AddComponent<PaperCompanion>();
            var visual=Group(root,"Visual",Vector3.zero);var rig=Group(visual,"Rig",Vector3.zero);
            BuildNaturalAnatomy(rig,c);
            var light=Group(root,"Borrowed light",V(0,.39f,0)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.8f,.5f);light.intensity=.18f;light.range=1.3f;light.shadows=LightShadows.None;c.glow=light;
            c.lightSeed=Put(root,"HiddenLightSeed",Ellipsoid(V(.03f,.045f,.025f),0,12,8),gold,V(0,.4f,.12f));c.lightSeed.gameObject.SetActive(false);
            var animator=visual.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=Controller();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=root.gameObject.AddComponent<PaperMoaMotion>();driver.companion=c;driver.animator=animator;
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);AssetDatabase.SaveAssets();return c;
        }
        static Material Tint(string name,Color color)
        {
            var path=Dir+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(wrap);AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_BumpScale",.65f);mat.SetFloat("_Cull",2);EditorUtility.SetDirty(mat);return mat;
        }
        static Material ClosedPaper(string name,Material source)
        {
            string path=Dir+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else mat.CopyPropertiesFromMaterial(source);
            mat.SetFloat("_Cull",2);mat.SetFloat("_BumpScale",.48f);mat.SetColor("_BaseColor",new Color(.985f,.96f,.90f));EditorUtility.SetDirty(mat);return mat;
        }
        static Transform Group(Transform parent,string name,Vector3 at)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
        static Transform Put(Transform parent,string name,Mesh mesh,Material mat,Vector3 at)
        {
            string path=Dir+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){saved.Clear();saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;saved.triangles=mesh.triangles;saved.uv=mesh.uv;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.bounds=mesh.bounds;saved.UploadMeshData(false);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}else{saved=mesh;AssetDatabase.CreateAsset(mesh,path);}
            var t=Group(parent,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            return t;
        }
        static Mesh Make(string name,List<Vector3> v,List<Vector2> uv,List<int> triangles)
        {
            var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();
            var sums=new Dictionary<Vector3Int,Vector3>();var ns=m.normals;
            Vector3Int Key(Vector3 p)=>new(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
            for(int i=0;i<v.Count;i++){var k=Key(v[i]);sums.TryGetValue(k,out var n);sums[k]=n+ns[i];}
            for(int i=0;i<v.Count;i++)ns[i]=sums[Key(v[i])].normalized;
            m.normals=ns;m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        static Mesh Ellipsoid(Vector3 radii,int seed,int lon=56,int lat=36)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int j=0;j<=lat;j++)for(int i=0;i<=lon;i++)
            {
                float a=i*Mathf.PI*2/lon,b=(j/(float)lat-.5f)*Mathf.PI;
                var d=V(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Cos(a)*Mathf.Cos(b));
                float r=seed==0?1:1+.010f*Mathf.Sin(d.x*17+seed)*Mathf.Sin(d.y*13+seed*.3f)*Mathf.Sin(d.z*11);
                v.Add(Vector3.Scale(d,radii)*r);uv.Add(new Vector2(i/(float)lon,j/(float)lat));
                if(i<lon&&j<lat){int q=j*(lon+1)+i;tr.AddRange(new[]{q,q+1,q+lon+1,q+1,q+lon+2,q+lon+1});}
            }
            return Make("Rounded handmade paper",v,uv,tr);
        }
        static Mesh Patch(Vector3 radii,float a0,float a1,float b0,float b1,int seed)
        {
            return Sheet((t,s)=>
            {
                float a=Mathf.Lerp(a0,a1,(s+1)/2),b=Mathf.Lerp(b0,b1,t)+.016f*Mathf.Sin(a*7+seed);
                return Vector3.Scale(V(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Cos(a)*Mathf.Cos(b)),radii);
            },.0018f,24,30);
        }
        // Closed thin sheet with separate top/bottom and a real edge, suitable for back views.
        static Mesh Sheet(Func<float,float,Vector3> at,float thickness,int rows,int cols)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();int count=(rows+1)*(cols+1);
            for(int side=0;side<2;side++)for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float t=j/(float)rows,s=i*2f/cols-1;Vector3 p=at(t,s);
                var tangent=at(Mathf.Min(1,t+.001f),s)-at(Mathf.Max(0,t-.001f),s);
                var across=at(t,Mathf.Min(1,s+.001f))-at(t,Mathf.Max(-1,s-.001f));
                Vector3 normal=Vector3.Cross(across,tangent).normalized;if(normal.sqrMagnitude<.01f)normal=Vector3.up;
                v.Add(p+normal*thickness*(side==0?.5f:-.5f));uv.Add(new Vector2((s+1)*.5f,t));
                if(j<rows&&i<cols){int q=side*count+j*(cols+1)+i;int[] face={q,q+1,q+cols+1,q+1,q+cols+2,q+cols+1};if(side==1)Array.Reverse(face);tr.AddRange(face);}
            }
            void Edge(int a,int b){tr.AddRange(new[]{a,b,a+count,b,b+count,a+count});}
            for(int i=0;i<cols;i++){Edge(i+1,i);Edge(rows*(cols+1)+i,rows*(cols+1)+i+1);}
            for(int j=0;j<rows;j++){Edge(j*(cols+1),(j+1)*(cols+1));Edge((j+1)*(cols+1)+cols,j*(cols+1)+cols);}
            return Make("Layered mulberry paper sheet",v,uv,tr);
        }
        static Mesh Beak(bool upper)
        {
            float sign=upper?1:-1;
            var v=new List<Vector3>{V(-.067f,0,0),V(0,sign*.035f,0),V(.067f,0,0),V(0,-.011f*sign,.117f)};
            var uv=new List<Vector2>{new(0,0),new(.5f,0),new(1,0),new(.5f,1)};
            var tr=new List<int>{0,1,3,1,2,3,0,3,2};if(upper)tr.Reverse();
            return Make("Folded paper beak",v,uv,tr);
        }
        static Mesh Tube(Vector3[] points,float radius,int sides)
        {return F0Mesh.Tube(points,radius,sides);}
    }
}
