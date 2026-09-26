using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Presentation;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class DirectionReviewBuilder
    {
        const string Output = "Docs/ArtReview/Direction";
        const string AssetsDir = "Assets/_BookTower/Generated/Direction";
        const string ScenePath = "Assets/Scenes/InGame.unity";
        static readonly StringBuilder Report = new();
        static Room Room(string id) => Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r => r.RoomId == id);
        static Light Sun() => Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);

        [MenuItem("Book Tower/방향성 검증/캡처와 왜상 실험 생성")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(AssetsDir);
            AssetDatabase.Refresh();
            Report.Clear();
            Report.AppendLine("Generated evidence. Editor renders, not a novice playtest. No HUD or forced puzzle solve in the captures.");
            EditorSceneManager.OpenScene(ScenePath);
            if (!File.Exists(Output + "/1A-before-0.png")) CaptureRoom("1A", "before", true);
            EditorSceneManager.OpenScene(ScenePath);
            if (!File.Exists(Output + "/1B-before-0.png")) CaptureRoom("1B", "before", true);
            EditorSceneManager.OpenScene(ScenePath);
            AuditBridge();
            if (!File.Exists(Output + "/3A-before-0.png")) CaptureRoom("3A", "before", false);
            EditorSceneManager.OpenScene(ScenePath);
            Apply();
            ShadowBridgeBuilder.Apply();
            SurfaceAnamorphosisBuilder.Apply();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            CaptureRoom("1A", "after", true);
            EditorSceneManager.OpenScene(ScenePath);
            CaptureRoom("1B", "after", true);
            EditorSceneManager.OpenScene(ScenePath);
            BuildStudy();
            File.WriteAllText(Output + "/measurements.txt", Report.ToString());
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[DirectionReview] Evidence written to " + Output);
        }

        // Called by the scene generator too: the presentation changes survive rebuilds.
        public static void Apply()
        {
            Directory.CreateDirectory(AssetsDir);
            AssetDatabase.Refresh();
            var graphite = Material("CueGraphite", new Color(.27f, .26f, .25f), true);
            var paper = Material("CuePaper", new Color(.984f, .980f, .965f), false);
            var surface = Material("SurfacePaper", new Color(.95f,.94f,.91f), false);
            surface.SetTextureScale("_BaseMap",new Vector2(8,8));
            foreach (string name in new[] { "Paper", "Ribbon" })
            {
                var spirit = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/" + name + ".mat");
                if (spirit == null) continue;
                spirit.SetColor("_BaseColor", name == "Paper" ? new Color(.984f,.980f,.965f) : new Color(.70f,.33f,.25f));
                EditorUtility.SetDirty(spirit);
            }
            foreach (string id in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Select(r => r.RoomId))
            {
                var room = Room(id);
                var old = room.transform.Find("종이_시각단서");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var root = new GameObject("종이_시각단서").transform;
                root.SetParent(room.transform, false);
                // Reduce dressing contrast only in the reviewed rooms. Keep soot and puzzle clues intact.
                foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.name == "바닥" || renderer.name.StartsWith("바닥_") || renderer.GetComponent<RoomWall>() != null && !renderer.name.StartsWith("장식_"))
                    { renderer.sharedMaterial = surface; continue; }
                    if (!renderer.transform.name.StartsWith("장식_")) continue;
                    var source = renderer.sharedMaterial;
                    if (source != null && source.name.StartsWith("Soft_M_Dress_"))
                        source = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Materials/" + source.name.Substring(5) + ".mat");
                    if (source == null || !source.name.StartsWith("M_Dress_")) continue;
                    if (source.name.Contains("soot") || source.name.Contains("ink") || source.name.Contains("ribbon")) continue;
                    string path = AssetsDir + "/Soft_" + source.name + ".mat";
                    var soft = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (soft == null) { soft = new Material(source); AssetDatabase.CreateAsset(soft, path); }
                    soft.name = "Soft_" + source.name;
                    soft.SetColor("_BaseColor", Color.Lerp(source.GetColor("_BaseColor"), new Color(.965f, .956f, .929f), .68f));
                    soft.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Art/PaperSpirit/PaperFiber.png"));
                    soft.SetFloat("_BumpScale",.06f);
                    EditorUtility.SetDirty(soft);
                    renderer.sharedMaterial = soft;
                }
                foreach (var panel in room.GetComponentsInChildren<FoldPanel>())
                {
                    var leaf = panel.transform.Find("Panel");
                    if (leaf == null) continue;
                    leaf.GetComponent<Renderer>().sharedMaterial = paper;
                    // A dashed crease and folded corner are a shape cue, independent of color.
                    var existing = panel.transform.Find("접는선");
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);
                    var crease = new GameObject("접는선").transform;
                    crease.SetParent(panel.transform, false);
                    float width = leaf.localScale.x;
                    for (int i = 0; i < 12; i++)
                        Line(crease, "연필주름", new[] {new Vector3(-width*.48f+i*width/12,.09f,.12f), new Vector3(-width*.48f+(i+.55f)*width/12,.09f,.12f)}, graphite, .045f);
                    Line(crease, "접힌귀", new[] {new Vector3(width*.5f-.6f,.09f,.12f), new Vector3(width*.5f-.05f,.32f,.6f), new Vector3(width*.5f-.05f,.09f,1.05f)}, graphite, .045f);
                }
                if (id != "1A") continue;
                var fold = room.GetComponentsInChildren<FoldPanel>().Single(p => p.name == "P2_접이면");
                var marker = room.transform.Find("◎목표지점");
                Vector3[] shadow = ProjectShadow(fold.transform, 90, room.FloorY + .12f);
                Vector3 center = shadow.Aggregate(Vector3.zero, (sum, v) => sum + v) / shadow.Length;
                var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plaque.name = "그림자_목표종이";
                plaque.transform.SetParent(root, false);
                plaque.transform.localPosition = marker.localPosition + Vector3.up*.015f + Vector3.left*1.2f;
                plaque.transform.localScale = new Vector3(9.2f, .035f, 4.5f);
                plaque.GetComponent<Renderer>().sharedMaterial = paper;
                Object.DestroyImmediate(plaque.GetComponent<Collider>());
                Vector3 target = marker.localPosition + Vector3.up*.05f + Vector3.left*1.2f;
                var clearArea = new Bounds(room.transform.TransformPoint(target + Vector3.up*.8f), new Vector3(10,2,5.2f));
                foreach (var decoration in room.GetComponentsInChildren<MeshRenderer>())
                    if (decoration.name.StartsWith("장식_") && decoration.bounds.Intersects(clearArea))
                        decoration.gameObject.SetActive(false);
                var points = shadow.Select(v => v-center+target).ToList(); points.Add(points[0]);
                Line(root, "목표_그림자윤곽", points.ToArray(), graphite, .09f);
                // The same paired pencil notches identify the target and the fold without numbers/text.
                foreach (var at in new[] { target + new Vector3(0,0,-2), fold.transform.localPosition + new Vector3(0,.12f,-.45f) })
                    for (int n = 0; n < 2; n++)
                        Line(root, "짝표식", new[] {at+new Vector3(n*.3f-.15f,0,0), at+new Vector3(n*.3f-.15f,0,.35f)}, graphite, .075f);
                // Move only the hover effect off the hinge; a moving hinge changes its physical shadow.
                var interactable = fold.GetComponent<BookTower.Interaction.Interactable>();
                if (interactable != null)
                { var so = new SerializedObject(interactable); so.FindProperty("lift").floatValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        static Material Material(string name, Color color, bool unlit)
        {
            string path = AssetsDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Cull", 0);
            if (!unlit) { mat.SetFloat("_Smoothness", 0); mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Art/PaperSpirit/PaperFiber.png")); }
            EditorUtility.SetDirty(mat); return mat;
        }

        static void Line(Transform parent, string name, Vector3[] points, Material mat, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = points.Length; line.SetPositions(points); line.widthMultiplier = width;
            line.sharedMaterial = mat; line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false; line.numCornerVertices = 2;
        }

        static Vector3[] ProjectShadow(Transform fold, float angle, float floor)
        {
            var leaf = fold.Find("Panel"); float w = leaf.localScale.x, h = leaf.localScale.z;
            var corners = new[] {new Vector3(-w/2,0,0),new Vector3(w/2,0,0),new Vector3(w/2,0,h),new Vector3(-w/2,0,h)};
            Vector3 light = Sun().transform.forward;
            return corners.Select(v => {
                Vector3 world = fold.parent.TransformPoint(fold.localPosition + Quaternion.AngleAxis(angle,Vector3.left)*v);
                return world + light * ((floor-world.y)/light.y);
            }).ToArray();
        }

        static void AuditBridge()
        {
            var room = Room("1B"); var panel = room.GetComponentsInChildren<FoldPanel>().Single();
            var polygon = ProjectShadow(panel.transform,45,room.FloorY+.12f);
            float min = polygon.Min(v => v.z-room.transform.position.z), max = polygon.Max(v => v.z-room.transform.position.z);
            Report.AppendLine($"P3 longitudinal span [{min:F3}, {max:F3}], gap [-3.200, +3.200]: {(min <= -3.2f && max >= 3.2f ? "PASS" : "FAIL")}. Use play-verification.txt for mesh, interaction and crossing results.");
        }

        static void CaptureRoom(string id, string phase, bool states)
        {
            var room = Room(id);
            foreach (var r in Object.FindObjectsByType<Room>(FindObjectsSortMode.None)) if(r != room) r.gameObject.SetActive(false);
            foreach (var b in Object.FindObjectsByType<RoomBound>(FindObjectsSortMode.None)) b.gameObject.SetActive(false);
            foreach (var wall in room.GetComponentsInChildren<RoomWall>())
                if (wall.Face == WallFace.Ceiling || wall.Face == RoomWall.HiddenFor(0))
                {
                    foreach (var r in wall.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            var sun = Sun(); sun.intensity = room.SunIntensity; sun.shadowStrength = room.ShadowStrength;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f,.42f,.45f)*room.Ambient;
            RenderSettings.fog = false;
            var camera = Camera.main;
            camera.transform.rotation = Quaternion.Euler(30,0,0); camera.orthographic = true;
            var focus = room.transform.position + new Vector3(0,1,0);
            camera.transform.position = focus - camera.transform.forward*60;
            camera.orthographicSize = 10; // current gameplay cap; deliberately not a whole-room overview
            var player = Object.FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();
            if(player != null) player.transform.position = room.transform.position + new Vector3(-3,.2f,-3);
            var fold = room.GetComponentsInChildren<FoldPanel>().FirstOrDefault(p => p.name.StartsWith(id == "1A" ? "P2_" : "P3_"));
            if(fold != null)
            {
                var renderer=fold.transform.Find("Panel").GetComponent<MeshRenderer>();
                Report.AppendLine($"{id}/{phase}: panel enabled={renderer.enabled}, forceRenderingOff={renderer.forceRenderingOff}, shadowMode={renderer.shadowCastingMode}, mesh={renderer.GetComponent<MeshFilter>().sharedMesh.name}, bounds={renderer.bounds}, material={renderer.sharedMaterial.name}");
            }
            for(int i=0;i<(states?4:1);i++)
            {
                if (fold != null) fold.transform.localRotation = Quaternion.AngleAxis(fold.AngleAt(i),Vector3.left);
                if (fold != null) { var mr=fold.transform.Find("Panel").GetComponent<MeshRenderer>(); mr.ResetBounds(); mr.enabled=false; mr.enabled=true; }
                Render(camera,$"{id}-{phase}-{i}");
            }
            if (id == "1A")
            {
                camera.orthographicSize = 5;
                camera.transform.position = room.transform.TransformPoint(new Vector3(8,.5f,6.4f))-camera.transform.forward*60;
                Render(camera,$"{id}-{phase}-target");
            }
        }

        static void Render(Camera camera,string name)
        {
            var rt = new RenderTexture(1280,800,24); var old = RenderTexture.active;
            camera.targetTexture = rt; camera.aspect = 1.6f; camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1280,800,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt);
        }

        static void BuildStudy()
        {
            // Preserve 3A's room as context; this separate scene never replaces InGame or unlocks P15.
            var source = Room("3A");
            var copy = Object.Instantiate(source.gameObject); copy.name = "3A_ProjectionStudy";
            var production=copy.transform.Find("P15_왜상"); if(production!=null)Object.DestroyImmediate(production.gameObject);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            foreach(var other in Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s=>s!=scene).ToArray()) EditorSceneManager.CloseScene(other,true);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            foreach(var behaviour in copy.GetComponentsInChildren<MonoBehaviour>()) Object.DestroyImmediate(behaviour);
            foreach(var t in copy.GetComponentsInChildren<Transform>())
                if(t != null && t.name.StartsWith("왜상조각_")) Object.DestroyImmediate(t.gameObject);
            var camera=new GameObject("StudyCamera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=7.5f; camera.backgroundColor=new Color(.97f,.96f,.93f); camera.clearFlags=CameraClearFlags.SolidColor;
            var sun=new GameObject("StudySun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.15f; sun.transform.rotation=Quaternion.Euler(55,-30,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.65f,.65f); RenderSettings.fog=false;
            var root=new GameObject("FoldGeometryStudy").AddComponent<AnamorphosisStudy>(); root.transform.position=copy.transform.position;
            root.focus=copy.transform.position+new Vector3(0,5,0); root.studyCamera=camera;
            Quaternion view=Quaternion.Euler(30,0,0);
            var ink=Material("MentorGraphite",new Color(.28f,.27f,.26f),true);
            var ribbon=Material("MentorRibbon",new Color(.70f,.33f,.25f),true);
            var parts=new List<Transform>(); var aligned=new List<Vector3>(); var unfolded=new List<Vector3>();
            // Horizontal strips of the chosen round head / tapered curled tail silhouette.
            for(int i=0;i<12;i++)
            {
                float y0=-3.7f+i*.61f, y1=Mathf.Min(3.6f,y0+.61f);
                var verts=new List<Vector3>(); var tris=new List<int>();
                for(int j=0;j<=12;j++)
                {
                    float y=Mathf.Lerp(y0,y1,j/12f); float width, shift;
                    if(y> .15f) { width=Mathf.Sqrt(Mathf.Max(0,3.4f-(y-1.75f)*(y-1.75f))); shift=0; }
                    else { float t=Mathf.Clamp01((y+3.7f)/3.85f); width=.04f+1.28f*Mathf.Sin(t*Mathf.PI*.60f); shift=.85f*(1-t)*(1-t); }
                    verts.Add(view*new Vector3(shift-width,y,0)); verts.Add(view*new Vector3(shift+width,y,0));
                    if(j==0)continue; int k=j*2; tris.AddRange(new[]{k-2,k-1,k,k,k-1,k+1});
                }
                var mesh=new Mesh {name=$"MentorStrip{i}"}; mesh.SetVertices(verts); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path=AssetsDir+$"/MentorStrip{i}.asset"; var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old==null) AssetDatabase.CreateAsset(mesh,path); else {EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); mesh=old;}
                var part=new GameObject($"PaperFragment_{i:00}").transform; part.SetParent(root.transform,false);
                part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=part.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial=i==6?ribbon:ink; r.shadowCastingMode=ShadowCastingMode.Off;
                float depth=(i%3-1)*2.1f;
                Vector3 at=new Vector3(0,5,0)+view*Vector3.forward*depth;
                aligned.Add(at); unfolded.Add(at+view*new Vector3((i%2==0?1:-1)*(1.2f+i%3*.4f),Mathf.Sin(i*1.7f)*.65f,0)); parts.Add(part);
            }
            root.fragments=parts.ToArray(); root.aligned=aligned.ToArray(); root.unfolded=unfolded.ToArray();
            camera.transform.rotation=view; camera.transform.position=root.focus-camera.transform.forward*45;
            // Foreground wall/ceiling obscure the study; retain the floor and side/back surfaces.
            foreach(var r in copy.GetComponentsInChildren<Renderer>())
                if(r.bounds.center.z<copy.transform.position.z-18 || r.bounds.center.y>copy.transform.position.y+9.8f) r.enabled=false;
            for(int step=0;step<3;step++) {root.SetAmount(step*.5f); Render(camera,$"P15-study-fold-{step}");}
            root.SetAmount(1);
            float alignedError=ProjectionError(root,view);
            camera.transform.position+=new Vector3(2,1,0);
            float translatedError=ProjectionError(root,camera.transform.rotation);
            camera.transform.rotation=Quaternion.Euler(30,90,0); camera.transform.position=root.focus-camera.transform.forward*45;
            float wrongYaw=ProjectionError(root,camera.transform.rotation); Render(camera,"P15-study-wrong-yaw");
            root.SetAmount(0); float unfoldedError=ProjectionError(root,view);
            Report.AppendLine($"P15 geometric study RMS projection offsets (world screen-plane units): aligned={alignedError:F6}; translated camera={translatedError:F6}; wrong yaw={wrongYaw:F3}; unfolded={unfoldedError:F3}.");
            if(alignedError>.001f || translatedError>.001f || wrongYaw<.1f || unfoldedError<.5f) throw new Exception("P15 projection study regression");
            Report.AppendLine("P15 study is geometric evidence only. Production surface/progression verification is in play-verification.txt; physical back-light tracing and endings remain unfinished.");
            camera.transform.rotation=view; camera.transform.position=root.focus-camera.transform.forward*45;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/P15_ProjectionStudy.unity");
        }

        static float ProjectionError(AnamorphosisStudy study,Quaternion rotation)
        {
            float sum=0; Vector3 center=new Vector3(0,5,0);
            for(int i=0;i<study.fragments.Length;i++)
            { Vector3 delta=Quaternion.Inverse(rotation)*(study.fragments[i].localPosition-center); sum+=delta.x*delta.x+delta.y*delta.y; }
            return Mathf.Sqrt(sum/study.fragments.Length);
        }
    }
}
