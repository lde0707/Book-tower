using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    // Original meshes, persisted as assets so scenes and prefabs survive editor reloads.
    public static class PaperAssetBuilder
    {
        const string Dir = "Assets/_BookTower/Generated/Paper";
        static readonly Dictionary<string, Mesh> Cache = new();

        [MenuItem("Book Tower/종이 에셋 빌드와 미리보기")]
        public static void BuildAndReview()
        {
            var importer = AssetImporter.GetAtPath("Assets/_BookTower/Textures/T_Paper_Normal.png") as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            { importer.textureType = TextureImporterType.NormalMap; importer.SaveAndReimport(); }
            PrototypeSceneBuilder.Build();
            if (!Application.isBatchMode) return;
            ReviewRoom("1A");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            ReviewRoom("1D");
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/" + name + ".mat");
            var fairy = Fairy(null, Mat("M_Ghost_Body"), Mat("M_Ghost_Ink"), Mat("M_Ribbon"));
            fairy.transform.position = new Vector3(-1.1f, .12f, 0);
            fairy.transform.localRotation = Quaternion.Euler(0, 155, 0);
            var book = GameObject.CreatePrimitive(PrimitiveType.Cube);
            book.name = "BoundBook"; book.GetComponent<Renderer>().sharedMaterial = Mat("M_Ribbon");
            DressBook(book, new ObjDef { name = "장식_책", mat = "spine1", scale = new V3 { x=.28f,y=1.1f,z=.8f } }, Mat("M_Paper_Mid"));
            PrefabUtility.SaveAsPrefabAsset(book, Dir + "/BoundBook.prefab");
            book.transform.localScale = new Vector3(.28f, 1.1f, .8f);
            book.transform.position = new Vector3(.05f, .55f, 0);
            for (int i = 0; i < 3; i++)
            {
                var copy = Object.Instantiate(book);
                copy.transform.localRotation = Quaternion.Euler(0, 0, 90);
                copy.transform.position = new Vector3(1.15f, .14f + i*.28f, .05f);
            }
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "PaperWall"; wall.GetComponent<Renderer>().sharedMaterial = Mat("M_Paper_Mid");
            wall.transform.localScale = new Vector3(4, 2.2f, .2f);
            PrefabUtility.SaveAsPrefabAsset(wall, Dir + "/PaperWall.prefab");
            wall.transform.position = new Vector3(0,1.1f,1);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0,-.08f,0);
            floor.transform.localScale = new Vector3(200,.1f,200);
            floor.GetComponent<Renderer>().sharedMaterial = Mat("M_Paper_Light");
            var sun = new GameObject("ReviewSun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45,-35,0);
            RenderSettings.ambientLight = new Color(.55f,.53f,.50f);
            var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
            camera.transform.position = new Vector3(3,2.5f,-6);
            camera.transform.LookAt(new Vector3(0,.85f,0));
            camera.orthographic = true; camera.orthographicSize = 1.75f;
            camera.backgroundColor = new Color(.94f,.93f,.90f); camera.clearFlags = CameraClearFlags.SolidColor;
            var rt = new RenderTexture(1600,1000,24); camera.targetTexture = rt;
            camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1600,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
            System.IO.Directory.CreateDirectory("Docs/ArtReview");
            System.IO.File.WriteAllBytes("Docs/ArtReview/paper-assets.png", image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt);
            AssetDatabase.SaveAssets();
            Debug.Log("[PaperAssetBuilder] Asset preview saved to Docs/ArtReview/paper-assets.png");
        }

        static void ReviewRoom(string id)
        {
            BookTower.Rooms.Room target = null;
            foreach (var room in Object.FindObjectsByType<BookTower.Rooms.Room>(FindObjectsSortMode.None))
            {
                if (room.RoomId == id) target = room;
                else room.gameObject.SetActive(false);
            }
            if (target == null) throw new System.InvalidOperationException("Missing review room " + id);
            foreach (var bound in Object.FindObjectsByType<BookTower.Rooms.RoomBound>(FindObjectsSortMode.None))
                bound.gameObject.SetActive(false);
            foreach (var wall in target.GetComponentsInChildren<BookTower.Rooms.RoomWall>())
            {
                if (wall.Face != BookTower.Rooms.WallFace.Ceiling && wall.Face != BookTower.Rooms.RoomWall.HiddenFor(target.CamYaw)) continue;
                var renderer = wall.GetComponent<Renderer>();
                if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                var outline = wall.transform.Find("외곽선");
                if (outline != null) outline.gameObject.SetActive(false);
            }
            var camera = Camera.main;
            if (camera == null) throw new System.InvalidOperationException("Missing camera");
            camera.transform.rotation = Quaternion.Euler(30, target.CamYaw*90, 0);
            camera.transform.position = target.WorldBounds.center - camera.transform.forward*80;
            camera.orthographicSize = Mathf.Max(target.WorldBounds.size.x, target.WorldBounds.size.z)*.58f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { light.intensity = target.SunIntensity; light.shadowStrength = target.ShadowStrength; }
            var rt = new RenderTexture(1600,1000,24); camera.targetTexture = rt;
            camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1600,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,1000),0,0); image.Apply();
            System.IO.Directory.CreateDirectory("Docs/ArtReview");
            System.IO.File.WriteAllBytes("Docs/ArtReview/room-" + id + ".png",image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt);
        }

        public static Mesh FairyMesh()
        {
            const int rings = 32, sides = 48;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float h = j / (float)rings;
                float radius = .30f * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(2*h-1, 2))) * Mathf.Min(1, h*3.5f);
                for (int i = 0; i <= sides; i++)
                {
                    float u = i / (float)sides, p = u * Mathf.PI * 2;
                    float pleat = 1 + .035f * Mathf.Cos(p * 7) * (1 - h);
                    v.Add(new Vector3(Mathf.Cos(p) * radius * pleat,
                        h * 1.4f, Mathf.Sin(p) * radius * pleat + .11f * Mathf.Pow(1-h, 3)));
                    uv.Add(new Vector2(u, h));
                    if (j < rings && i < sides)
                    {
                        int k = j * (sides + 1) + i;
                        t.AddRange(new[] { k, k + sides + 1, k + 1, k + 1, k + sides + 1, k + sides + 2 });
                    }
                }
            }
            var mesh = new Mesh { name = "PaperFairy" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            // Weld the UV seam normals for a softly rounded silhouette.
            var normals = mesh.normals;
            for (int j = 0; j <= rings; j++)
            {
                int k = j * (sides + 1);
                normals[k] = normals[k + sides] = (normals[k] + normals[k + sides]).normalized;
            }
            mesh.normals = normals;
            return Save(mesh, "PaperFairy");
        }

        static Mesh Save(Mesh mesh, string name)
        {
            System.IO.Directory.CreateDirectory(Dir);
            string path = Dir + "/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                // Set the native buffers explicitly: CopySerialized can leave the
                // already-uploaded render buffer stale during repeated editor builds.
                old.Clear(); old.vertices = mesh.vertices; old.uv = mesh.uv;
                old.normals = mesh.normals; old.tangents = mesh.tangents;
                old.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) old.SetTriangles(mesh.GetTriangles(i), i);
                old.bounds = mesh.bounds; EditorUtility.SetDirty(old);
                Object.DestroyImmediate(mesh); mesh = old;
            }
            Cache[name] = mesh;
            return mesh;
        }

        public static GameObject Fairy(Transform parent, Material body, Material eyes, Material ribbon)
        {
            return ReferenceSpiritBuilder.Build(parent);
        }

        // Books remain inside the exact source box: no new collisions or shadow obstructions.
        public static void DressBook(GameObject go, ObjDef o, Material pages)
        {
            if (!o.name.StartsWith("장식_") || o.mat == null || !o.mat.StartsWith("spine")) return;
            Vector3 s = o.scale.V;
            int axis = s.x < s.y && s.x < s.z ? 0 : s.y < s.z ? 1 : 2;
            string key = "BoundBook_" + axis;
            if (!Cache.TryGetValue(key, out var mesh))
            {
                var verts = new List<Vector3>(); var uv = new List<Vector2>();
                var cover = new List<int>(); var paper = new List<int>();
                void Box(Vector3 center, Vector3 size, List<int> tris)
                {
                    Vector3[] normal = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                    foreach (var n in normal)
                    {
                        Vector3 u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                        Vector3 w = Vector3.Cross(n, u); int start = verts.Count;
                        foreach (var q in new[] { new Vector2(-1,-1), new Vector2(1,-1), new Vector2(1,1), new Vector2(-1,1) })
                        { verts.Add(center + Vector3.Scale(n + u * q.x + w * q.y, size) * .5f); uv.Add((q + Vector2.one)*.5f); }
                        tris.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
                    }
                }
                Vector3 dim = Vector3.one, pos = Vector3.zero;
                dim[axis] = .075f; pos[axis] = .4625f;
                Box(pos, dim, cover); Box(-pos, dim, cover);
                int spineAxis = axis == 2 ? 0 : 2;
                dim = Vector3.one * .92f; dim[axis] = .85f;dim[spineAxis]=.86f;
                pos=Vector3.zero;pos[spineAxis]=.03f;
                Box(pos, dim, paper);
                dim = Vector3.one; dim[spineAxis] = .10f;dim[axis]=.85f;
                pos = Vector3.zero; pos[spineAxis] = -.45f;
                Box(pos, dim, cover);
                mesh = new Mesh { name = key, subMeshCount = 2 };
                mesh.SetVertices(verts); mesh.SetUVs(0, uv);
                mesh.SetTriangles(cover, 0); mesh.SetTriangles(paper, 1);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                mesh = Save(mesh, key);
            }
            var renderer = go.GetComponent<MeshRenderer>();
            var coverMat = renderer.sharedMaterial;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            renderer.sharedMaterials = new[] { coverMat, pages };
        }
    }
}

