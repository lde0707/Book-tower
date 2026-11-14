using System;
using System.Diagnostics;
using System.IO;
using BookTower.Story;
using BookTower.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Opt-in editor recording: plays the authored opening without changing the saved scene.
    public static class OpeningStoryVideoCapture
    {
        const string Flag = "OpeningStoryVideoCapture";
        public const string Output = "Logs/OpeningStoryCapture";
        [InitializeOnLoadMethod]
        static void Initialize() => EditorApplication.playModeStateChanged += Changed;
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Storage()
        {
            if (!SessionState.GetBool(Flag, false)) return;
            PaperSettingsRuntime.TestStoragePath = Path.GetFullPath(Output + "/isolated-settings.json");
            File.WriteAllText(PaperSettingsRuntime.TestStoragePath, JsonUtility.ToJson(new PaperSettings { muteBackground = false, vsync = false, fps = -1 }));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SessionState.GetBool(Flag, false)) new GameObject("Opening story video capture").AddComponent<OpeningStoryCaptureDriver>();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Flag, false)) return;
            SessionState.SetBool(Flag, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Flag + "Exit", 1));
        }
        public static void Finish(bool success)
        {
            SessionState.SetInt(Flag + "Exit", success ? 0 : 1);
            EditorApplication.ExitPlaymode();
        }
    }

    [DefaultExecutionOrder(10000)]
    public sealed class OpeningStoryCaptureDriver : MonoBehaviour
    {
        const int Width = 1920, Height = 1080, Fps = 30;
        OpeningStoryDirector story;
        PaperUI ui;
        Camera captureCamera;
        Canvas canvas;
        RenderTexture target;
        Texture2D pixels;
        Process encoder;
        StreamWriter events;
        int frames, previousBeat = -1;
        float readHold, ending;
        bool recording, finished;
        double started;
        string Folder => OpeningStoryVideoCapture.Output;

        void Start()
        {
            try
            {
                Application.runInBackground = true;
                started = EditorApplication.timeSinceStartup;
                story = FindFirstObjectByType<OpeningStoryDirector>();
                ui = PaperUI.Instance;
                captureCamera = Camera.main;
                canvas = ui.GetComponentInChildren<Canvas>();
                if (!story || !captureCamera || !canvas) throw new Exception("Missing story/captureCamera/canvas");
                Time.captureFramerate = Fps;
                AudioListener.volume = 0;
                var ffmpeg = File.ReadAllText(Folder + "/ffmpeg-path.txt").Trim();
                var info = new ProcessStartInfo(ffmpeg,
                    "-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 1920x1080 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -movflags +faststart \"" + Path.GetFullPath(Folder + "/opening-story-1x-silent.mp4") + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true };
                encoder = Process.Start(info);
                events = new StreamWriter(Folder + "/capture-events.txt", false) { AutoFlush = true };
                target = new RenderTexture(Width, Height, 24); target.Create();
                pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            }
            catch (Exception error) { End(error); }
        }
        void LateUpdate()
        {
            if (finished) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 600) throw new Exception("Recording timeout");
                if (!recording)
                {
                    if (story.ActiveSequence != OpeningStoryData.Promise)
                    {
                        return;
                    }
                    recording = true;
                }
                if (frames > Fps * 120) throw new Exception("Opening exceeded 120 seconds");
                if (story.ActiveSequence == OpeningStoryData.Promise && previousBeat != story.BeatIndex)
                {
                    previousBeat = story.BeatIndex; readHold = 0;
                    events.WriteLine($"BEAT {previousBeat}; seconds={frames / (float)Fps:F3}; text={ui.StoryText.Replace('\n', ' ')}");
                }
                CaptureFrame(); frames++;
                if (story.Completed(OpeningStoryData.Promise))
                {
                    ending += 1f / Fps;
                    if (ending >= 2.5f) End(null);
                    return;
                }
                if (ui.StoryTextComplete)
                {
                    readHold += 1f / Fps;
                    if (readHold >= 2.6f) { ui.AdvanceStoryText(); readHold = 0; }
                }
                else readHold = 0;
            }
            catch (Exception error) { End(error); }
        }
        void CaptureFrame()
        {
            var active = RenderTexture.active;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = captureCamera;
            captureCamera.nearClipPlane = .01f; canvas.planeDistance = .02f;
            captureCamera.targetTexture = target; captureCamera.aspect = 16f / 9;
            canvas.scaleFactor = 1;
            var data = captureCamera.GetComponent<UniversalAdditionalCameraData>();
            bool post = data.renderPostProcessing; var aa = data.antialiasing;
            try
            {
                // Same UI capture path as the story verifier, keeping text outside post effects.
                data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
                Canvas.ForceUpdateCanvases(); captureCamera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply();
                var bytes = pixels.GetRawTextureData<byte>().ToArray();
                encoder.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                if (frames % (Fps * 5) == 0) File.WriteAllBytes(Folder + "/frame-" + frames.ToString("D5") + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                captureCamera.targetTexture = null; RenderTexture.active = active;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                data.renderPostProcessing = post; data.antialiasing = aa;
            }
        }
        void End(Exception error)
        {
            if (finished) return; finished = true;
            Time.captureFramerate = 0;
            events?.Dispose();
            if (encoder != null)
            {
                encoder.StandardInput.Close();
                if (!encoder.WaitForExit(30000)) { encoder.Kill(); error = error ?? new Exception("Encoder timeout"); }
                else if (encoder.ExitCode != 0) error = error ?? new Exception("Encoder failed: " + encoder.ExitCode);
                encoder.Dispose();
            }
            if (target) { target.Release(); Destroy(target); }
            if (pixels) Destroy(pixels);
            string result = $"{(error == null ? "PASS" : "FAIL")} frames={frames}; fps={Fps}; duration={frames / (float)Fps:F3}; audio=False; completed={story && story.Completed(OpeningStoryData.Promise)}\n{error}";
            File.WriteAllText(Folder + "/capture-result.txt", result);
            if (error != null) Debug.LogException(error); else Debug.Log(result);
            OpeningStoryVideoCapture.Finish(error == null);
        }
    }
}
