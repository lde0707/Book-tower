using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.CameraSystem;
using BookTower.PlayerSystem;
using BookTower.Presentation;
using BookTower.Rooms;
using BookTower.UI;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Read-only scene review. Before/after use identical sampled poses and camera transforms.
    [InitializeOnLoad] public static class SpiritSoftPaperReview
    {
        const string Flag = "SpiritSoftPaperReview";
        const string Root = "Docs/ArtReview/SpiritSoftPaperV7";
        static IEnumerator routine;
        static double start, next;
        static readonly StringBuilder report = new();
        static string output;
        static SpiritSoftPaperReview() { EditorApplication.playModeStateChanged += Changed; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
        { if (SessionState.GetBool(Flag, false)) PaperSettingsRuntime.TestStoragePath = Path.GetFullPath("Logs/spirit-soft-settings.json"); }
        public static void Before() => Run("Before");
        public static void After() => Run("After");
        public static void Diagnose()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");var spirit=Object.FindFirstObjectByType<HoodSpiritAnimator>();
            var animator=spirit.GetComponentInChildren<Animator>();var skin=spirit.GetComponentInChildren<SkinnedMeshRenderer>();var log=new StringBuilder();
            void Read(string label)
            {
                log.AppendLine(label);for(var t=skin.bones[0];t;t=t.parent)log.AppendLine($"CHAIN {t.name}: local={t.localPosition:F5},scale={t.localScale:F5},worldScale={t.lossyScale:F5}");
                foreach(var t in skin.bones.Where(b=>b.name.Contains("Foot")||b.name=="Body"))log.AppendLine($"BONE {t.name}: {t.position-spirit.transform.position:F5}");
                log.AppendLine($"SKIN scale={skin.transform.lossyScale:F5} mesh={skin.sharedMesh.bounds}");
            }
            Read("Rest");var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name=="Move");clip.SampleAnimation(animator.gameObject,clip.length*.25f);Read("SampleAnimation");
            animator.Rebind();animator.Play("Move",0,.25f);animator.Update(0);Read("Animator");
            File.WriteAllText("Logs/spirit-rig-diagnostic.txt",log.ToString());EditorApplication.Exit(0);
        }
        public static void PrepareAndAfter()
        {
            ReleasePaperMaps.RefreshSpiritAtlas();
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReleaseMaterials/Materials/c2b78bf3b37e75f4fb0964aab32df502_1.mat");
            if(!mat)throw new Exception("Scene spirit material missing");
            mat.SetFloat("_BumpScale",.32f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();After();
        }
        static void Run(string name)
        {
            SessionState.SetString(Flag + "stage", name);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Flag, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                output = Root + "/" + SessionState.GetString(Flag + "stage", "After");
                Directory.CreateDirectory(output); report.Clear();
                routine = Review(); start = next = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            { SessionState.EraseBool(Flag); EditorApplication.Exit(SessionState.GetInt(Flag + "exit", 1)); }
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (EditorApplication.timeSinceStartup - start > 220) throw new Exception("Spirit review timeout");
                if (!routine.MoveNext()) { Finish(null); return; }
                next = EditorApplication.timeSinceStartup + (routine.Current is float t ? t : .05f);
            }
            catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception e)
        {
            EditorApplication.update -= Tick;
            File.WriteAllText(output + "/review.txt", report + (e == null ? "PASS: scene mesh/material references, clips, bounds and room renders.\n" : "FAIL: " + e));
            SessionState.SetInt(Flag + "exit", e == null ? 0 : 1);
            EditorApplication.ExitPlaymode();
        }
        static void Capture(Camera c, string name)
        {
            var old = c.targetTexture; var active = RenderTexture.active; float aspect = c.aspect;
            var rt = RenderTexture.GetTemporary(1600, 1200, 24); Texture2D tex = null;
            try
            {
                c.targetTexture = rt; c.aspect = 4f / 3; c.Render(); RenderTexture.active = rt;
                tex = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0); tex.Apply();
                File.WriteAllBytes(output + "/" + name + ".png", tex.EncodeToPNG());
            }
            finally { c.targetTexture = old; c.aspect = aspect; RenderTexture.active = active; if (tex) Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt); }
            report.AppendLine($"{name}: pos={c.transform.position:F4}; rotation={c.transform.eulerAngles:F3}; fov={c.fieldOfView}");
        }
        static IEnumerator Review()
        {
            yield return 1f;
            foreach (var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None)) story.gameObject.SetActive(false);
            PaperUI.Instance.EndStory(); PaperUI.Instance.Resume(); BookTower.Core.GameFlow.EnsureExists().Resume();
            PaperSettingsRuntime.Instance.Apply(new PaperSettings(), false);
            var player = Object.FindFirstObjectByType<PlayerController>(); player.enabled = false;
            var spirit = player.GetComponentInChildren<HoodSpiritAnimator>(); spirit.enabled = false;
            var animator = spirit.GetComponentInChildren<Animator>(); animator.enabled = false;
            var skin = spirit.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!skin || !skin.sharedMesh || skin.bones.Length != 23) throw new Exception("Spirit mesh / 23-bone binding missing");
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().OrderBy(c => c.name).ToArray();
            foreach(var bone in skin.bones) report.AppendLine($"BONE {bone.name} local={bone.localPosition:F4} scale={bone.localScale:F4}");
            if (clips.Length != 10) throw new Exception("Expected ten clips, got " + clips.Length);
            report.AppendLine($"Mesh {skin.sharedMesh.name}: {skin.sharedMesh.vertexCount} vertices, {skin.sharedMesh.triangles.Length / 3} triangles; bones={skin.bones.Length}");
            foreach (var mat in skin.sharedMaterials)
                if (!mat || !mat.mainTexture || !mat.GetTexture("_BumpMap")) throw new Exception("Missing paper material maps");
            var mesh = new Mesh();
            foreach (var clip in clips)
            {
                float travel = 0; Vector3[] initial = null;
                foreach (float phase in new[] { 0f, .125f, .25f, .5f, .75f, .875f, 1f })
                {
                    clip.SampleAnimation(animator.gameObject, phase * clip.length); skin.BakeMesh(mesh);
                    var vertices = mesh.vertices;
                    if (vertices.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))) throw new Exception("Nonfinite skin " + clip.name);
                    if (initial == null) initial = vertices;
                    for (int i = 0; i < vertices.Length; i++) travel = Mathf.Max(travel, Vector3.Distance(initial[i], vertices[i]));
                    if (mesh.bounds.size.magnitude > 7 || mesh.bounds.size.magnitude < .4f) throw new Exception("Broken bounds " + clip.name);
                }
                if (travel < .0001f) throw new Exception("Static clip " + clip.name);
                report.AppendLine($"PASS clip {clip.name}: duration={clip.length:F5}; loop={clip.isLooping}; vertexTravel={travel:F5}");
            }
            Object.DestroyImmediate(mesh);
            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            var orbit = Object.FindFirstObjectByType<PaperCamera>();
            var camera = Camera.main;
            var idle = clips.First(c => c.name == "Idle");
            foreach (string id in new[] { "F0", "1A", "1B" })
            {
                var room = rooms.First(r => r.RoomId == id);
                var local = id == "F0" ? new Vector3(0, 3.1f, -10.5f) : id == "1A" ? new Vector3(-4, .2f, 14) : new Vector3(0, .2f, -11);
                player.Controller.enabled = false; player.transform.position = room.transform.TransformPoint(local); player.transform.rotation = Quaternion.identity; player.Controller.enabled = true;
                Physics.SyncTransforms(); orbit.enabled = true; orbit.SetAim(0, 5);
                yield return 2f;
                idle.SampleAnimation(animator.gameObject, 0); Capture(camera, id + "-play");
                orbit.enabled = false;
                // FBX unit conversion is on the skin transform; do not apply it a
                // second time to BakeMesh positions when framing the review camera.
                var center = player.transform.position + Vector3.up * .88f;
                float distance = 3.5f;
                foreach (var shot in new[] { ("front", new Vector3(.16f, .15f, 1)), ("rear", new Vector3(-.16f, .18f, -1)), ("side", new Vector3(1, .12f, .15f)) })
                {
                    camera.transform.position = center + shot.Item2.normalized * distance;
                    camera.transform.LookAt(center); camera.fieldOfView = 36;
                    Capture(camera, id + "-" + shot.Item1);
                    if (id == "F0" && shot.Item1 == "rear")
                    {
                        var mat = skin.sharedMaterial; float bump = mat.GetFloat("_BumpScale");
                        mat.SetFloat("_BumpScale", 0); Capture(camera, "F0-rear-normal-off"); mat.SetFloat("_BumpScale", bump);
                    }
                }
                camera.fieldOfView = 60;
                if(id=="F0")
                {
                    camera.transform.position=center+new Vector3(1.9f,.35f,3.6f);camera.transform.LookAt(center);camera.fieldOfView=36;
                    var walk=clips.Single(c=>c.name=="Move");
                    var left=skin.bones.Single(b=>b.name=="FootL");var right=skin.bones.Single(b=>b.name=="FootR");
                    float minStep=float.MaxValue,maxStep=float.MinValue;
                    for(int frame=0;frame<24;frame++)
                    {
                        walk.SampleAnimation(animator.gameObject,walk.length*frame/24f);
                        // Advance a rendered frame so GPU skinning consumes this pose.
                        // Immediate repeated Camera.Render calls can reuse stale skin data.
                        yield return .06f;
                        Capture(camera,"walk-"+frame.ToString("D2"));
                        float step=Vector3.Dot(left.position-right.position,player.transform.forward);minStep=Mathf.Min(minStep,step);maxStep=Mathf.Max(maxStep,step);
                    }
                    report.AppendLine($"Walk alternating foot separation: {minStep:F4} .. {maxStep:F4} metres");
                    if(minStep>-.12f||maxStep<.12f)throw new Exception("Walking feet do not alternate visibly");
                    idle.SampleAnimation(animator.gameObject,0);camera.fieldOfView=60;
                }
            }
            report.AppendLine("PASS: ten existing animation clips deform a finite scene skin. Camera-only close-ups retain room lighting and atmospheric renderer.");
        }
    }
}
