using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Presentation;


namespace BookTower.EditorTools
{
    public static class ReferenceSpiritBuilder
    {
        const string Dir = "Assets/_BookTower/Art/PaperSpirit";
        [Serializable] class Model { public Part[] parts; }
        [Serializable] class Part { public string name, material; public Vector3[] positions; public Vector2[] uv; public int[] triangles; }

        static Material Material(string name)
        {
            string path = Dir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor", name == "Ribbon" ? new Color(.70f,.33f,.25f) : name == "Ink" ? new Color(.12f,.10f,.075f) : new Color(.984f,.980f,.965f));
            mat.SetFloat("_Smoothness",0); mat.SetFloat("_Metallic",0); mat.SetFloat("_Cull",0);
            mat.SetFloat("_SpecularHighlights",0);
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/PaperFiber.png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/PaperFiberNormal.png"));
            mat.SetFloat("_BumpScale",.65f); mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat); return mat;
        }
        public static GameObject Build(Transform parent)
        {
            var importer = AssetImporter.GetAtPath(Dir+"/PaperFiberNormal.png") as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            { importer.textureType=TextureImporterType.NormalMap; importer.SaveAndReimport(); }
            var data=JsonUtility.FromJson<Model>(File.ReadAllText(Dir+"/paper-spirit.json"));
            var root=new GameObject("종이요정"); root.transform.SetParent(parent,false);
            var materials=new Dictionary<string,Material>();
            var tails=new List<Transform>(); int count=0;
            foreach(var p in data.parts)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/"+p.name+".asset");
                bool fresh=mesh==null; if(fresh) mesh=new Mesh { name=p.name }; else mesh.Clear();
                mesh.vertices=p.positions; mesh.uv=p.uv; mesh.triangles=p.triangles;
                mesh.RecalculateNormals();
                // Smooth duplicated UV seam vertices without changing UV coordinates.
                var sums=new Dictionary<Vector3Int,Vector3>(); var normals=mesh.normals;
                Vector3Int Key(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
                for(int i=0;i<p.positions.Length;i++) { var key=Key(p.positions[i]); sums.TryGetValue(key,out var sum); sums[key]=sum+normals[i]; }
                for(int i=0;i<p.positions.Length;i++) normals[i]=sums[Key(p.positions[i])].normalized;
                mesh.normals=normals; mesh.RecalculateTangents(); mesh.RecalculateBounds();
                if(fresh) AssetDatabase.CreateAsset(mesh,Dir+"/"+p.name+".asset"); else EditorUtility.SetDirty(mesh);
                foreach(var v in mesh.vertices)
                    if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)) throw new Exception("Invalid mesh "+p.name);
                count+=p.triangles.Length/3;
                var go=new GameObject(p.name); go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                if(!materials.ContainsKey(p.material)) materials[p.material]=Material(p.material);
                go.AddComponent<MeshRenderer>().sharedMaterial=materials[p.material];
                if(p.name.StartsWith("RibbonTail"))
                {
                    // Pivot at the neck attachment, so the ribbon can flutter naturally.
                    Vector3 pivot=new Vector3(p.name.EndsWith("-1") ? -.12f : .12f,.8f,-.025f);
                    var vertices=mesh.vertices; for(int i=0;i<vertices.Length;i++) vertices[i]-=pivot;
                    mesh.vertices=vertices; mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                    go.transform.localPosition=pivot; tails.Add(go.transform);
                }
            }
            SpiritAnimationBuilder.Attach(root);
            PrefabUtility.SaveAsPrefabAsset(root,Dir+"/PaperSpirit.prefab");
            Debug.Log("[PaperSpirit] "+data.parts.Length+" mesh parts, "+count+" triangles; finite geometry verified.");
            return root;
        }

        [MenuItem("Book Tower/참조 이미지 종이 정령 적용")]
        public static void ApplyAndReview()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            var player=GameObject.Find("BookmarkBird");
            if(player==null) throw new Exception("Player not found");
            var old=player.transform.Find("종이요정");
            if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Build(player.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            if(!Application.isBatchMode) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var model=Build(null); model.transform.position=Vector3.up*.12f;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.04f,0); floor.transform.localScale=new Vector3(200,.05f,200);
            floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Paper.mat");
            var light=new GameObject("Key").AddComponent<Light>(); light.type=LightType.Directional;
            light.intensity=1.15f; light.color=new Color(1,.94f,.83f); light.shadows=LightShadows.Soft;
            light.transform.rotation=Quaternion.Euler(40,-35,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.72f,.70f,.66f);
            var camera=new GameObject("Camera").AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=.90f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.88f,.85f,.78f);
            foreach(int yaw in new[]{25,90,180})
            {
                Vector3 direction=Quaternion.Euler(-10,yaw,0)*Vector3.forward;
                camera.transform.position=new Vector3(0,.86f,0)+direction*4;
                camera.transform.LookAt(new Vector3(0,.86f,0));
                var rt=new RenderTexture(1200,1400,24); camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                var image=new Texture2D(1200,1400,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1200,1400),0,0); image.Apply();
                File.WriteAllBytes("Docs/ArtReview/spirit-model-"+yaw+".png",image.EncodeToPNG());
                camera.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
            SpiritAnimationBuilder.Review(model,camera);
            AssetDatabase.SaveAssets(); Debug.Log("[PaperSpirit] Saved player, animation clips and review renders.");
        }
    }
}
