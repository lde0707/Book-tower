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
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Bake indirect irradiance for moving characters without freezing any optical puzzle shadow.
    public static class PaperIndirectBake
    {
        static readonly Dictionary<GameObject, StaticEditorFlags> flags = new();
        static readonly Dictionary<MeshRenderer, ReceiveGI> receive = new();
        static double started, lastLog;
        static bool completed;
        public static string OutputOverride;
        public static bool ForceCpu;
        static string Output=>string.IsNullOrEmpty(OutputOverride)?PaperAtmosphereReview.Output:OutputOverride;
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, "Logs/AtmosphereBackups/BeforeIndirect.unity", true);
            Directory.CreateDirectory(Output);
            var dynamic = new HashSet<Renderer>();
            void Exclude(Transform t) { if (t) foreach (var r in t.GetComponentsInChildren<Renderer>(true)) dynamic.Add(r); }
            foreach (var p in Object.FindObjectsByType<FoldBridgePuzzle>(FindObjectsSortMode.None)) Exclude(p.transform);
            foreach (var p in Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsSortMode.None)) { Exclude(p.body); Exclude(p.ears); if (p.Duet) { Exclude(p.Duet.foxBody); Exclude(p.Duet.foxTail); } }
            foreach (var p in Object.FindObjectsByType<ReadingRoomPuzzle>(FindObjectsSortMode.None)) { Exclude(p.lantern); Exclude(p.shutter); Exclude(p.door); Exclude(p.servicePanel); }
            foreach(var p in Object.FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None))
            {foreach(var body in p.cargo)Exclude(body.transform);Exclude(p.exitLeaf);Exclude(p.completedPage);Exclude(p.arrow);}
            int contributors = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = r.gameObject; if (!flags.ContainsKey(go)) flags.Add(go, GameObjectUtility.GetStaticEditorFlags(go));
                receive.Add(r, r.receiveGI);
                bool include = ReleaseLightingReview.Target(r.GetComponentInParent<Room>()) && !dynamic.Contains(r) && r.enabled && r.gameObject.activeInHierarchy;
                // Tiny wire, print and leaf detail does not meaningfully contribute to diffuse transport.
                include &= r.bounds.size.sqrMagnitude > .3f;
                var f = flags[go] & ~StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(go, include ? f | StaticEditorFlags.ContributeGI : f);
                if (include) { r.receiveGI = ReceiveGI.LightProbes; contributors++; }
            }
            var optical = ReleaseLightingReview.PuzzleLights(); int sources = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!ReleaseLightingReview.Target(l.GetComponentInParent<Room>()) || optical.Contains(l) || l.intensity <= 0) continue;
                l.lightmapBakeType = LightmapBakeType.Mixed; l.bounceIntensity = .75f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(l); sources++;
            }
            var oldGroup = GameObject.Find("PaperIndirectProbeGrid"); if (oldGroup) Object.DestroyImmediate(oldGroup);
            var group = new GameObject("PaperIndirectProbeGrid").AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target))
            {
                bool foyer = room.RoomId == "F0"; float minX = foyer ? -13 : room.RoomId == "1A" ? -24 : -16;
                float maxX = foyer ? 13 : room.RoomId == "1A" ? 24 : 16;
                float minZ = foyer ? -16 : -20, maxZ = foyer ? 16 : 20;
                foreach (float y in foyer ? new[] { 3.8f, 5.5f, 9f, 14f } : new[] { .8f, 2.5f, 6f, 11f })
                    for (float x = minX; x <= maxX; x += 5) for (float z = minZ; z <= maxZ; z += 5)
                        positions.Add(room.transform.TransformPoint(new Vector3(x, y, z)));
            }
            group.probePositions = positions.ToArray();
            string path = PaperAtmosphereBuilder.Folder + "/IndirectSettings.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (!settings) { settings = new LightingSettings(); AssetDatabase.CreateAsset(settings, path); }
            settings.bakedGI = true; settings.realtimeGI = false; settings.lightmapper = ForceCpu?LightingSettings.Lightmapper.ProgressiveCPU:LightingSettings.Lightmapper.ProgressiveGPU;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly; settings.lightmapResolution = 2; settings.lightmapMaxSize = 1024;
            settings.directSampleCount = 32; settings.indirectSampleCount = 128; settings.environmentSampleCount = 64; settings.maxBounces = 3;
            settings.lightProbeSampleCountMultiplier = 2; settings.indirectScale = 1;
            Lightmapping.lightingSettings = settings;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new(.08f, .10f, .13f);
            RenderSettings.ambientEquatorColor = new(.055f, .049f, .043f); RenderSettings.ambientGroundColor = new(.025f, .018f, .012f);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            File.WriteAllText(Output + "/indirect-bake.txt", $"Requested: {contributors} static transport meshes, {sources} environment sources, {positions.Count} irradiance probes. Dynamic puzzle geometry excluded; 3 bounces, 128 indirect samples x probe multiplier 2.\n");
            started = lastLog = EditorApplication.timeSinceStartup; completed = false;
            Lightmapping.bakeCompleted += Complete; EditorApplication.update += Tick;
            if (!Lightmapping.BakeAsync()) Fail("Unity refused to start the indirect bake");
        }
        static void Complete() { completed = true; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup - lastLog > 25) { lastLog = EditorApplication.timeSinceStartup; Debug.Log("Paper indirect bake " + Lightmapping.buildProgress.ToString("P0")); }
            if (EditorApplication.timeSinceStartup - started > 720) { Lightmapping.Cancel(); Fail("Indirect bake exceeded 12-minute budget; previous scene preserved"); return; }
            if (!completed) return;
            EditorApplication.update -= Tick; Lightmapping.bakeCompleted -= Complete;
            try
            {
                if (!Lightmapping.lightingDataAsset || !LightmapSettings.lightProbes || LightmapSettings.lightProbes.count == 0) throw new Exception("Bake produced no light probes");
                foreach (var p in flags) if (p.Key) GameObjectUtility.SetStaticEditorFlags(p.Key, p.Value);
                foreach (var p in receive) if (p.Key) p.Key.receiveGI = p.Value;
                LightProbes.Tetrahedralize();
                var text = new StringBuilder();
                foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Where(ReleaseLightingReview.Target))
                {
                    foreach (float x in new[] { -10f, 0, 10f })
                    {
                        var at = room.transform.TransformPoint(new Vector3(x, room.RoomId == "F0" ? 4.5f : 1.5f, -8));
                        LightProbes.GetInterpolatedProbe(at, null, out var sh);
                        var directions = new[] { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
                        var colors = new Color[6]; sh.Evaluate(directions, colors);
                        text.AppendLine($"{room.RoomId} x={x}: " + string.Join(" ", colors.Select(c => c.ToString("F4"))));
                    }
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
                File.AppendAllText(Output + "/indirect-bake.txt", $"PASS: {LightmapSettings.lightProbes.count} baked probes, {LightmapSettings.lightmaps.Length} lightmap atlases; {EditorApplication.timeSinceStartup - started:F1}s.\nLighting data: {AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset)}\n" + text);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Fail(e.ToString()); }
        }
        static void Fail(string message)
        {
            EditorApplication.update -= Tick; Lightmapping.bakeCompleted -= Complete;
            File.AppendAllText(Output + "/indirect-bake.txt", "FAIL: " + message + "\n");
            // Unsaved bake preparation does not replace the saved playable scene.
            EditorApplication.Exit(1);
        }
    }
}
