using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using BookTower.Rooms;
using BookTower.Presentation;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Final presentation pass: deterministic and reapplied by the scene generator.
    public static class SketchbookArtBuilder
    {
        const string Output = "Docs/ArtReview/Sketchbook";
        const string MeshPath = "Assets/_BookTower/Generated/Direction/CurledPage.asset";
        public static void Apply()
        {
            var fiber=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Art/PaperSpirit/PaperFiber.png");
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_BookTower/Materials","Assets/_BookTower/Generated/Direction"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(!material.HasProperty("_BaseMap") || material.name.Contains("Graphite") || material.name.Contains("Outline") || material.name.Contains("Ink"))continue;
                if(material.name.StartsWith("M_Paper") || material.name.StartsWith("M_Dress") || material.name.StartsWith("Soft_M_Dress") || material.name.Contains("Paper") || material.name=="M_Sea")
                {
                    material.SetTexture("_BaseMap",fiber);
                    material.SetTextureScale("_BaseMap", material.name.Contains("Surface") ? new Vector2(6,6) : Vector2.one);
                    material.SetFloat("_BumpScale",.035f);
                    material.SetFloat("_Smoothness",0);
                    if(material.name=="SurfacePaper")material.SetColor("_BaseColor",new Color(.984f,.980f,.965f));
                    // A restrained fill represents diffuse light through paper; direct shadows remain visible.
                    if(material.HasProperty("_EmissionColor") && !material.name.Contains("ink") && !material.name.Contains("soot"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor",material.GetColor("_BaseColor")*.22f);
                        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
                    }
                    if(material.name=="M_Sea")
                    {
                        material.shader=Shader.Find("Universal Render Pipeline/Unlit");
                        material.SetTexture("_BaseMap",null);
                        material.SetColor("_BaseColor",new Color(.992f,.988f,.980f));
                    }
                    EditorUtility.SetDirty(material);
                }
            }
            var ribbon=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/Ribbon.mat");
            if(ribbon!=null){ribbon.SetColor("_BaseColor",new Color(.851f,.412f,.310f));EditorUtility.SetDirty(ribbon);}
            var spirit=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/Paper.mat");
            if(spirit!=null){spirit.EnableKeyword("_EMISSION");spirit.SetColor("_EmissionColor",new Color(.13f,.128f,.12f));spirit.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;EditorUtility.SetDirty(spirit);}
            var mesh=CurledPage();
            var outline=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/M_Outline.mat");
            foreach(var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(renderer==null || renderer.name=="외곽선")continue;
                    if(renderer.name.StartsWith("장식_핀") && !renderer.name.Contains("끈") && renderer.GetComponent<MeshFilter>()!=null)
                    {
                        renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
                        renderer.sharedMaterial.SetFloat("_Cull",0);
                        EditorUtility.SetDirty(renderer.sharedMaterial);
                        // Inverted hulls cover the back face of an open sheet. Its curled edge supplies the silhouette.
                        var oldOutline=renderer.transform.Find("외곽선");
                        if(oldOutline!=null)Object.DestroyImmediate(oldOutline.gameObject);
                        continue;
                    }
                    if(renderer.transform.Find("외곽선")!=null && outline!=null)
                        PencilOutline.Attach(renderer.gameObject,outline,renderer.name.StartsWith("장식_")?.009f:.018f);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static Mesh CurledPage()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,MeshPath);}
            mesh.Clear();mesh.name="Thin curled paper leaf";
            const int n=12;var vertices=new Vector3[(n+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[n*6];
            for(int i=0;i<=n;i++)
            {
                float t=(float)i/n;
                float curl=Mathf.Pow(Mathf.Max(0,(t-.65f)/.35f),2)*5;
                vertices[i*2]=new Vector3(-.5f,-.5f+t,curl);
                vertices[i*2+1]=new Vector3(.5f,-.5f+t,curl*.6f);
                uv[i*2]=new Vector2(0,t);uv[i*2+1]=new Vector2(1,t);
                if(i==n)continue;
                int k=i*6,a=i*2;triangles[k]=a;triangles[k+1]=a+2;triangles[k+2]=a+1;
                triangles[k+3]=a+1;triangles[k+4]=a+2;triangles[k+5]=a+3;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }

        [MenuItem("Book Tower/아트/전체 방 렌더 검증")]
        public static void CaptureAll()
        {
            // Offline captures must wait for shader variants, otherwise the first room can render magenta.
            bool previousAsync=ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation=false;
            try { CaptureRooms(); }
            finally { ShaderUtil.allowAsyncCompilation=previousAsync; }
        }

        static void CaptureRooms()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            var ids=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Select(r=>r.RoomId).OrderBy(id=>id).ToArray();
            foreach(string id in ids)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                var rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
                var room=rooms.Single(r=>r.RoomId==id);
                foreach(var other in rooms)if(other!=room)other.gameObject.SetActive(false);
                foreach(var bound in Object.FindObjectsByType<RoomBound>(FindObjectsSortMode.None))bound.gameObject.SetActive(false);
                foreach(var wall in room.GetComponentsInChildren<RoomWall>())
                    if(wall.Face==WallFace.Ceiling || wall.Face==RoomWall.HiddenFor(room.CamYaw))
                        foreach(var renderer in wall.GetComponentsInChildren<Renderer>())
                            if(renderer.shadowCastingMode==ShadowCastingMode.Off)renderer.enabled=false;else renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
                var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
                sun.intensity=room.SunIntensity;sun.shadowStrength=room.ShadowStrength;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.667f,.647f,.608f)*room.Ambient;RenderSettings.fog=false;
                var camera=Camera.main;camera.orthographic=true;camera.aspect=1.6f;camera.transform.rotation=Quaternion.Euler(30,room.CamYaw*90,0);
                camera.orthographicSize=Mathf.Max(10,Mathf.Max(room.WorldBounds.size.x/1.6f,room.WorldBounds.size.z)*.48f);
                camera.transform.position=room.transform.position+Vector3.up*2-camera.transform.forward*100;
                var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;var previous=RenderTexture.active;camera.Render();RenderTexture.active=rt;
                var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
                File.WriteAllBytes(Output+"/"+id+".png",texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);
            }
            File.WriteAllText(Output+"/index.html","<!doctype html><meta charset='utf-8'><title>종이 세계 — 전체 방</title><style>body{background:#fdfcfa;color:#3a3733;font:18px sans-serif;margin:32px}main{display:grid;grid-template-columns:repeat(2,1fr);gap:24px}img{width:100%}figure{margin:0}h1{font-size:26px}</style><h1>흰 종이 · 연필 · 크레파스</h1><p>전체 방의 에디터 조감 렌더. 실제 플레이 검증은 Direction 폴더의 runtime 이미지와 보고서 참조.</p><main>"+string.Join("",ids.Select(id=>$"<figure><img src='{id}.png'><figcaption>{id}</figcaption></figure>"))+"</main>");
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        }
    }
}
