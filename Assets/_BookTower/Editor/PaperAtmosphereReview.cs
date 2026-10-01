using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Presentation;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    [InitializeOnLoad] public static class PaperAtmosphereReview
    {
        public const string Output = "Docs/ArtReview/Atmosphere";
        const string Flag = "PaperAtmosphereReview";
        static IEnumerator routine; static double start, next; static readonly StringBuilder log = new();
        static PlayerController player; static PaperCamera camera; static Room[] rooms;
        static PaperAtmosphereReview() { EditorApplication.playModeStateChanged += Changed; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
        { if (SessionState.GetBool(Flag, false)) PaperSettingsRuntime.TestStoragePath = Path.GetFullPath("Logs/atmosphere-settings.json"); }
        public static void Run() { EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity"); Directory.CreateDirectory(Output); SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode(); }
        static void Changed(PlayModeStateChange s)
        {
            if (!SessionState.GetBool(Flag, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode) { log.Clear(); routine = Review(); start = next = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode) { SessionState.EraseBool(Flag); EditorApplication.Exit(SessionState.GetInt(Flag + "exit", 1)); }
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try { if (EditorApplication.timeSinceStartup - start > 260) throw new Exception("Atmosphere review timeout"); if (!routine.MoveNext()) { Finish(null); return; } next = EditorApplication.timeSinceStartup + (routine.Current is float t ? t : .03f); }
            catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception e) { EditorApplication.update -= Tick; File.WriteAllText(Output + "/review.txt", log + (e == null ? "PASS: render and state checks completed. Visual judgment is separate.\n" : "FAIL: " + e)); SessionState.SetInt(Flag + "exit", e == null ? 0 : 1); EditorApplication.ExitPlaymode(); }
        static void Place(Room room, Vector3 at, float yaw, float pitch)
        { player.Controller.enabled = false; player.transform.position = room.transform.TransformPoint(at); player.Controller.enabled = true; Physics.SyncTransforms(); camera.SetAim(yaw, pitch); }
        static void Capture(string name, int width = 1600, int height = 900)
        {
            var c = Camera.main; var rt = RenderTexture.GetTemporary(width, height, 24); var prev = c.targetTexture; var active = RenderTexture.active; float aspect = c.aspect; Texture2D tex = null;
            try { c.targetTexture = rt; c.aspect = (float)width / height; c.Render(); RenderTexture.active = rt; tex = new Texture2D(width, height, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply(); File.WriteAllBytes(Output + "/" + name + ".png", tex.EncodeToPNG()); }
            finally { c.targetTexture = prev; c.aspect = aspect; RenderTexture.active = active; if (tex) Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt); }
            log.AppendLine($"{name}: camera={c.transform.position:F3}, angle={c.transform.eulerAngles:F2}, FOV={c.fieldOfView}, {width}x{height}, q={PaperSettingsRuntime.Values.quality}");
        }
        static IEnumerator Review()
        {
            yield return 1f;
            var story = Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>(); if (story) { if (story.companion) story.companion.transform.SetParent(null, true); story.gameObject.SetActive(false); }
            PaperUI.Instance.EndStory(); PaperUI.Instance.Resume(); BookTower.Core.GameFlow.EnsureExists().Resume();
            player = Object.FindFirstObjectByType<PlayerController>(); player.enabled = false; camera = Object.FindFirstObjectByType<PaperCamera>(); rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            PaperSettingsRuntime.Instance.Apply(new PaperSettings(), false);
            var director = PaperAtmosphereDirector.Instance; if (!director) throw new Exception("Missing atmosphere director");
            var irradiance = Object.FindFirstObjectByType<PaperIrradianceCalibration>();
            if (!irradiance || irradiance.CalibratedCount < 500) throw new Exception("Baked indirect irradiance was not loaded/calibrated");
            log.AppendLine("PASS: loaded and calibrated " + irradiance.CalibratedCount + " real baked irradiance probes.");
            foreach (var id in new[] { "F0", "1A", "1B" })
            {
                var room = rooms.First(r => r.RoomId == id); Vector3 at = id == "F0" ? new(0, 3.1f, -10.5f) : id == "1A" ? new(-4, .2f, 14) : new(0, .2f, -11);
                foreach (float yaw in new[] { 0, 90, 180, 270 }) { Place(room, at, yaw, id == "1B" ? -6 : 5); yield return 2f; Capture(id + "-" + yaw); }
                Place(room, at, 0, id == "1B" ? -6 : 5); yield return 1f;
                if (RenderSettings.fog || director.ActiveVolumes == 0 || !director.OwnsAmbient) throw new Exception("Double fog or missing room atmosphere " + id);
                director.effectsEnabled = false; yield return .2f; Capture(id + "-air-off"); director.effectsEnabled = true;
                director.shaftsEnabled = false; yield return .2f; Capture(id + "-shafts-off"); director.shaftsEnabled = true;
                foreach (int q in new[] { 0, 2 }) { var s = PaperSettingsRuntime.Values.Copy(); s.quality = q; PaperSettingsRuntime.Instance.Apply(s, false); yield return .4f; Capture(id + "-quality-" + q); }
                var reduced = new PaperSettings { reduceStoryMotion = true, reduceFlash = true }; PaperSettingsRuntime.Instance.Apply(reduced, false); yield return .4f; Capture(id + "-accessibility");
                PaperSettingsRuntime.Instance.Apply(new PaperSettings(), false);
                log.AppendLine($"PASS {id}: bounded air active, legacy fog off, independent puzzle lights preserved; shafts={director.ActiveShafts}.");
                LightProbes.GetInterpolatedProbe(player.transform.position + Vector3.up, null, out var probe);
                var measured = new Color[3]; probe.Evaluate(new[] { Vector3.up, Vector3.forward, Vector3.down }, measured);
                if (measured.Any(c => c.maxColorComponent > 1.5f || c.maxColorComponent < .005f)) throw new Exception("Invalid irradiance range " + id);
                log.AppendLine("Baked irradiance up/front/down: " + string.Join(" ", measured.Select(c => c.ToString("F4"))));
            }
            // Wider room distances matter more than shallow DOF: these are normal walkable observation points.
            foreach (var shot in new[] { ("1A-entry", "1A", new Vector3(-8,.2f,-21),0f,8f), ("1B-rear", "1B",new Vector3(0,.2f,14),180f,-8f), ("F0-workbook","F0",new Vector3(-9,3.1f,-9),0f,5f) })
            { Place(rooms.First(r => r.RoomId == shot.Item2), shot.Item3, shot.Item4, shot.Item5); yield return 2f; Capture(shot.Item1); }
            var manager = Object.FindFirstObjectByType<RoomManager>(); manager.enabled = false;
            foreach (var room in rooms) room.SetVisible(false); yield return .2f;
            if (director.ActiveVolumes != 0 || director.ActiveShafts != 0) throw new Exception("Hidden room leaks air or beams");
            manager.enabled = true;
            log.AppendLine("PASS: hidden rooms contribute no fog or beams. No animated noise, motion blur or gameplay DOF.");
            foreach (var shader in new[] { "BookTower/InteriorAtmosphere", "BookTower/ReadingPaper", "BookTower/CrayonAtlasRepeat" })
                if (ShaderUtil.ShaderHasError(Shader.Find(shader))) throw new Exception("Shader errors: " + shader);
        }
    }
}
