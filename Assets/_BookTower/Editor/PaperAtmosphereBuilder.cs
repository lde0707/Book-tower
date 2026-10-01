using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Presentation;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class PaperAtmosphereBuilder
    {
        public const string Folder = "Assets/_BookTower/Generated/Atmosphere";
        const string Root = "PaperAtmosphereCraft";
        [Serializable] public class LightEntry
        {
            public string id; public float intensity, strength, bias, normalBias;
            public Color color; public LightShadows shadows;
        }
        [Serializable] public class Archive { public LightEntry[] lights; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            Apply(); PaperAtmosphereReview.Run();
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(PaperAtmosphereReview.Output);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.GetActiveScene();
            Directory.CreateDirectory("Logs/AtmosphereBackups");
            EditorSceneManager.SaveScene(scene, "Logs/AtmosphereBackups/InGame-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target).ToArray();
            var owned = ReleaseLightingReview.PuzzleLights();
            string contract = Contract(owned);
            foreach (var room in rooms) { var old = room.transform.Find(Root); if (old) Object.DestroyImmediate(old.gameObject); }
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            string baseline = Folder + "/OriginalLights.json";
            var archive = File.Exists(baseline) ? JsonUtility.FromJson<Archive>(File.ReadAllText(baseline)) : new Archive
            {
                lights = lights.Where(l => ReleaseLightingReview.Target(l.GetComponentInParent<Room>()) && !owned.Contains(l)).Select(l => new LightEntry
                { id = GlobalObjectId.GetGlobalObjectIdSlow(l).ToString(), intensity = l.intensity, color = l.color, strength = l.shadowStrength, bias = l.shadowBias, normalBias = l.shadowNormalBias, shadows = l.shadows }).ToArray()
            };
            if (!File.Exists(baseline)) File.WriteAllText(baseline, JsonUtility.ToJson(archive, true));
            var log = new StringBuilder();
            foreach (var l in lights)
            {
                var r = l.GetComponentInParent<Room>(); if (!ReleaseLightingReview.Target(r) || owned.Contains(l)) continue;
                var e = archive.lights.FirstOrDefault(a => a.id == GlobalObjectId.GetGlobalObjectIdSlow(l).ToString()); if (e == null) continue;
                l.intensity = e.intensity; l.color = e.color; l.shadowStrength = e.strength; l.shadows = e.shadows; l.shadowBias = e.bias; l.shadowNormalBias = e.normalBias;
                if (e.intensity <= 0) continue;
                if (r.RoomId == "F0")
                {
                    if (l.name == "종이창_투광")
                    {
                        bool west = r.transform.InverseTransformPoint(l.transform.position).x < 0;
                        l.intensity = west ? 310 : 38;
                        l.color = west ? new Color(1, .82f, .59f) : new Color(.69f, .80f, 1);
                        l.shadowStrength = .72f;
                    }
                    else if (l.name.Contains("반사")) l.intensity = e.intensity * .72f;
                    else if (l.name == "매달린종이등") l.intensity = 12;
                }
                if (r.RoomId == "1A")
                {
                    if (l.name == "따뜻한 종이등 빛" && e.intensity > 15) l.intensity = e.intensity * .65f;
                    if (l.name == "ReleaseArt03_entrance-key") { l.intensity = 85; l.shadowStrength = .6f; }
                }
                if (r.RoomId == "1B")
                {
                    if (l.name == "shadow-casting reading light") { l.intensity = e.intensity * 1.2f; l.shadowStrength = .58f; }
                    if (l.name == "warm paper light" && e.intensity > 10) l.intensity = e.intensity * 1.15f;
                }
                EditorUtility.SetDirty(l); PrefabUtility.RecordPrefabInstancePropertyModifications(l);
                log.AppendLine($"{r.RoomId} {l.name}: {e.intensity} -> {l.intensity}, roomPos={r.transform.InverseTransformPoint(l.transform.position)}, forward={l.transform.forward}");
            }
            foreach (var room in rooms)
            {
                var root = new GameObject(Root).transform; root.SetParent(room.transform, false);
                var p = room.GetComponent<PaperAtmosphere>(); if (!p) p = room.gameObject.AddComponent<PaperAtmosphere>();
                p.room = room; p.clearDistance = 4.5f;
                if (room.RoomId == "F0")
                {
                    p.localMinimum = new(-18, 2.6f, -20); p.localMaximum = new(18, 22, 20);
                    p.density = .018f; p.air = new(.25f, .19f, .115f); p.shaftDensity = .028f;
                    p.indirectBase = new(.095f, .108f, .13f); p.indirectTop = new(.22f, .17f, .105f); p.indirectSide = new(.20f, .12f, .055f);
                    Bounce(root, "Warm paper return below window", new(-9, 4.2f, -5), new(0, 10, 0), 48, new(1, .83f, .64f), 25, 115);
                    Bounce(root, "Cool east paper return", new(12, 7, -10), new(0, 6, -9), 32, new(.63f, .77f, 1), 23, 110);
                    p.shafts = room.GetComponentsInChildren<Light>(true).Where(l => l.name == "종이창_투광" && l.intensity > 100).Take(2).ToArray();
                }
                else if (room.RoomId == "1A")
                {
                    p.localMinimum = new(-30, -.2f, -28); p.localMaximum = new(30, 15, 28);
                    p.density = .011f; p.air = new(.18f, .235f, .22f); p.shaftDensity = .012f;
                    p.indirectBase = new(.095f, .12f, .12f); p.indirectTop = new(.16f, .17f, .12f); p.indirectSide = new(.13f, .095f, .052f);
                    Bounce(root, "Teal alcove paper return", new(-20, 3, -11), new(-8, 7, -7), 44, new(.61f, .85f, .87f), 24, 108);
                    Bounce(root, "Entry page warm return", new(11, 2, -15), new(0, 6, -12), 40, new(1, .82f, .64f), 24, 115);
                    p.shafts = Array.Empty<Light>(); // Leave the three optical puzzle projectors physically legible.
                }
                else
                {
                    p.localMinimum = new(-21, -.2f, -22); p.localMaximum = new(19, 20, 23);
                    p.density = .018f; p.air = new(.115f, .15f, .21f); p.shaftDensity = .023f;
                    p.indirectBase = new(.065f, .085f, .12f); p.indirectTop = new(.13f, .155f, .21f); p.indirectSide = new(.11f, .075f, .04f);
                    Bounce(root, "Shelf reflected honey light", new(-12, 3, -5), new(0, 7, 0), 46, new(1, .77f, .50f), 25, 115);
                    Bounce(root, "Vault diffuse blue return", new(8, 8, 10), new(0, 11, 0), 70, new(.54f, .69f, 1), 30, 115);
                    p.shafts = room.GetComponentsInChildren<Light>(true).Where(l => l.name == "shadow-casting reading light").Take(2).ToArray();
                }
                Physics.SyncTransforms();
                p.shaftDepth = p.shafts.Select((l, i) => BakeBlockers(l, room, room.RoomId + "-shaft-" + i)).ToArray();
                EditorUtility.SetDirty(p);
            }
            var directorObject = GameObject.Find("PaperAtmosphereDirector"); if (!directorObject) directorObject = new GameObject("PaperAtmosphereDirector");
            if (!directorObject.GetComponent<PaperAtmosphereDirector>()) directorObject.AddComponent<PaperAtmosphereDirector>();
            ConfigureRenderer();
            var c = Camera.main; if (c) { var d = c.GetUniversalAdditionalCameraData(); d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; d.antialiasingQuality = AntialiasingQuality.High; EditorUtility.SetDirty(d); }
            // Keep ACES / brightness ownership / accessibility unchanged. No gameplay depth-of-field or motion blur.
            if (contract != Contract(owned)) throw new Exception("Atmosphere changed a collision or puzzle-owned light");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(PaperAtmosphereReview.Output + "/apply.txt", "PASS: optical light and collision contract unchanged. Authored bounce is not a GI bake.\n" + log);
        }
        static void Bounce(Transform root, string name, Vector3 at, Vector3 toward, float intensity, Color color, float range, float angle)
        {
            var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.localPosition = at;
            l.transform.LookAt(root.TransformPoint(toward)); l.type = LightType.Spot; l.intensity = intensity; l.color = color; l.range = range;
            l.spotAngle = angle; l.innerSpotAngle = angle * .32f; l.shadows = LightShadows.None;
        }
        static string Contract(HashSet<Light> lights) => string.Join("|", Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => $"{c.GetInstanceID()}:{c.bounds}:{c.enabled}").Concat(lights.OrderBy(l => l.GetInstanceID()).Select(l => $"{l.GetInstanceID()}:{l.transform.localToWorldMatrix}:{l.intensity}:{l.color}:{l.range}:{l.shadows}")));
        static Texture2D BakeBlockers(Light light, Room room, string name)
        {
            const int size = 64; var tex = new Texture2D(size, size, TextureFormat.RFloat, false, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size]; float tangent = Mathf.Tan(light.spotAngle * .5f * Mathf.Deg2Rad);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var ray = light.transform.TransformDirection(new Vector3(((x + .5f) / size * 2 - 1) * tangent, ((y + .5f) / size * 2 - 1) * tangent, 1).normalized);
                // Ignore trigger volumes and the source fixture; sample architecture, not puzzle geometry.
                float distance = light.range;
                foreach (var hit in Physics.RaycastAll(light.transform.position + ray * .45f, ray, light.range - .45f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Room>() != room || hit.collider.GetComponentInParent<BookTower.Puzzles.FoldBridgePuzzle>() || hit.collider.GetComponentInParent<BookTower.PlayerSystem.PlayerController>()) continue;
                    distance = Mathf.Min(distance, hit.distance + .45f);
                }
                pixels[y * size + x] = new Color(distance / light.range, 0, 0, 1);
            }
            tex.SetPixels(pixels); tex.Apply(); string path = Folder + "/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (old) { EditorUtility.CopySerialized(tex, old); Object.DestroyImmediate(tex); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(tex, path); return tex;
        }
        static void ConfigureRenderer()
        {
            string path = Folder + "/Air.mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("BookTower/InteriorAtmosphere"); if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Atmosphere shader failed");
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var feature = renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == "Paper interior atmosphere");
            if (!feature) { feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); feature.name = "Paper interior atmosphere"; AssetDatabase.AddObjectToAsset(feature, renderer); renderer.rendererFeatures.Add(feature); }
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true; feature.requirements = ScriptableRenderPassInput.Depth; feature.passMaterial = mat; feature.SetActive(true);
            foreach (var ao in renderer.rendererFeatures.Where(f => f.GetType().Name == "ScreenSpaceAmbientOcclusion"))
            {
                var so = new SerializedObject(ao); so.FindProperty("m_Settings.Intensity").floatValue = .65f;
                so.FindProperty("m_Settings.Radius").floatValue = .65f; so.FindProperty("m_Settings.DirectLightingStrength").floatValue = .12f;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(ao);
            }
            EditorUtility.SetDirty(feature); EditorUtility.SetDirty(renderer); renderer.SetDirty();
        }
    }
}
