using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BookTower.Core;
using BookTower.PlayerSystem;
using BookTower.Story;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BookTower.UI
{
    public sealed class PaperTitleBuildProbe : MonoBehaviour
    {
        public static Action<bool> Finished;
        readonly StringBuilder report = new StringBuilder();
        int failures;
        Keyboard keyboard;
        Mouse mouse;
        InputSettings originalInput;
        string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Storage()
        {
            if (Environment.GetCommandLineArgs().Any(a => a is "-titleQA" or "-titleExitQA"))
                PaperSettingsRuntime.TestStoragePath = Path.Combine(Application.temporaryCachePath, "title-qa-settings.json");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Environment.GetCommandLineArgs().Any(a => a is "-titleQA" or "-titleExitQA")) new GameObject("Title QA").AddComponent<PaperTitleBuildProbe>();
        }
        IEnumerator Start()
        {
            bool exitTest = Environment.GetCommandLineArgs().Contains("-titleExitQA");
            output = Path.GetFullPath("Logs/TitleScreenV2/" + (exitTest ? "Exit" : Application.isEditor ? "Editor" : "Standalone"));
            Directory.CreateDirectory(output); Application.runInBackground = true; Application.logMessageReceived += OnLog;
            originalInput = InputSystem.settings;
            var settings = Instantiate(originalInput);
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            yield return new WaitForSecondsRealtime(1);
            var ui = PaperUI.Instance; var player = FindFirstObjectByType<PlayerController>();
            var story = FindFirstObjectByType<OpeningStoryDirector>();
            Check(ui && ui.TitleMode && ui.ScreenState == PaperScreen.Title, "launch opens title before story");
            Check(ui.TitleArtReady, "generated title illustration loaded from project asset");
            Check(ui.TitleMenuArtReady, "separate paper plaques, glowing leaf and animation shader loaded");
            Check(Time.timeScale == 0 && !GameFlow.Instance.AcceptsPlayerInput && PaperUI.BlocksGameplay, "title freezes gameplay and blocks input");
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "title frees pointer");
            Check(!story || story.ActiveSequence.Length == 0, "opening story waits for Start");
            Check(Camera.main && Camera.main.cullingMask == 0, "title avoids rendering hidden 3D world");
            Check(ui.TitleMusic && ui.TitleMusic.clip && ui.TitleMusic.isPlaying && ui.TitleMusic.ignoreListenerPause, "dedicated title music plays while world is paused");
            if (exitTest)
            {
                yield return ClickButton(TitleButton("나가기"));
                Check(ui.ScreenState == PaperScreen.ConfirmQuit, "actual title Quit click opens confirmation");
                var confirm = ui.GetComponentsInChildren<Button>().First(b => b.name == "나가기" && b.transform.parent.name == "Modal paper");
                EventSystem.current.SetSelectedGameObject(confirm.gameObject);
                report.AppendLine("EXPECT_APPLICATION_QUIT: confirm real Quit; parent process must observe exit code 0"); Flush();
                yield return PressKey(Key.Enter);
                yield return new WaitForSecondsRealtime(3);
                Check(false, "confirmed Quit failed to terminate player"); Application.Quit(1); yield break;
            }
            Check(EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.name == "게임 시작", "start selected for keyboard navigation");
            var motion = ui.GetComponentInChildren<PaperTitleMotion>();
            var values = PaperSettingsRuntime.Values.Copy(); values.reduceStoryMotion = false;
            PaperSettingsRuntime.Instance.Apply(values, false);
            float age = motion.Age;
            Capture("title-motion-0", 1920, 1080);
            yield return new WaitForSecondsRealtime(2);
            Capture("title-motion-1", 1920, 1080);
            Check(motion && motion.MotionEnabled && motion.Age > age + 1, "title world animates while gameplay time is zero");
            var startPosition = ((RectTransform)TitleButton("게임 시작").transform).anchoredPosition;
            yield return HoverButton(TitleButton("옵션"));
            Check(TitleButton("옵션").GetComponent<PaperTitleFocus>().Highlight > .95f && TitleButton("게임 시작").GetComponent<PaperTitleFocus>().Highlight < .05f, "hover transfers golden edge and leaf to exactly one button");
            Check(((RectTransform)TitleButton("게임 시작").transform).anchoredPosition == startPosition, "animation keeps menu hit rectangles fixed");
            Capture("title-hover-options", 1920, 1080);
            yield return ClickButton(TitleButton("옵션"));
            Check(ui.ScreenState == PaperScreen.Settings && ui.TitleMode, "actual mouse Options click opens settings");
            ui.Back(); yield return null;
            var position = player.transform.position;
            yield return PressKey(Key.W);
            Check(Vector3.Distance(position, player.transform.position) < .001f, "title key cannot move player");
            EventSystem.current.SetSelectedGameObject(ui.GetComponentsInChildren<Button>().First(b => b.name == "게임 시작").gameObject);
            yield return PressKey(Key.DownArrow);
            Check(EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.name == "옵션", "down arrow navigates to Settings");
            yield return PressKey(Key.Enter);
            Check(ui.ScreenState == PaperScreen.Settings && ui.TitleMode, "Enter opens title Settings");
            Capture("settings", 1920, 1080);
            ui.Back(); yield return null;
            Check(ui.ScreenState == PaperScreen.Title, "unchanged Settings returns to title");
            ui.OpenSettings(); ui.Draft.master = ui.Draft.master > .5f ? .25f : .75f;
            ui.Back(); yield return null;
            Check(ui.ScreenState == PaperScreen.ConfirmDiscard, "dirty title settings asks before discarding");
            ui.Discard(); yield return null;
            Check(ui.ScreenState == PaperScreen.Title && ui.TitleMode, "discard returns to title and keeps gameplay paused");
            yield return ClickButton(TitleButton("나가기"));
            Check(ui.ScreenState == PaperScreen.ConfirmQuit, "actual mouse Quit click opens confirmation");
            Check(EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.name == "취소", "quit confirmation selects Cancel, not hidden title menu");
            Capture("quit", 1920, 1080);
            ui.Back(); yield return null;
            Check(ui.ScreenState == PaperScreen.Title && Time.timeScale == 0, "quit cancel restores title");
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(1280,1024), new Vector2Int(2560,1080) })
            {
                Capture("title-" + size.x + "x" + size.y, size.x, size.y);
            }
            values = PaperSettingsRuntime.Values.Copy(); values.textSize = 2; values.reduceStoryMotion = true;
            PaperSettingsRuntime.Instance.Apply(values, false); ui.OpenSettings(); ui.Back(); yield return null;
            age = motion.Age;
            Capture("title-reduced-motion-0", 1920, 1080);
            yield return new WaitForSecondsRealtime(.5f);
            Capture("title-reduced-motion-1", 1920, 1080);
            Check(!motion.MotionEnabled && Mathf.Abs(motion.Age - age) < .001f, "reduced motion freezes title animation");
            Capture("title-large-text", 1920, 1080);
            Check(ui.ScreenState == PaperScreen.Title, "large text and reduced motion keep title usable");
            yield return ClickButton(TitleButton("게임 시작")); yield return new WaitForSecondsRealtime(1.5f);
            Check(!ui.TitleMode && ui.ScreenState is PaperScreen.Story or PaperScreen.Playing, "Start enters actual game or opening story");
            Check(Camera.main && Camera.main.cullingMask != 0, "Start restores world camera layers");
            Check(!ui.TitleMusic || !ui.TitleMusic.isPlaying, "Start stops title music");
            Check(story && story.ActiveSequence.Length > 0 && ui.StoryActive, "opening story starts only after title is dismissed");
            ui.Pause(); ui.OpenSettings(); ui.Back();
            Check(ui.ScreenState == PaperScreen.Paused && !ui.TitleMode, "in-game Settings still returns to pause menu");
            ui.Resume();
            report.AppendLine($"RESULT: {(failures == 0 ? "PASS" : "FAIL")}; failures={failures}; editor={Application.isEditor}"); Flush();
            Application.logMessageReceived -= OnLog;
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
            InputSystem.settings = originalInput; Destroy(settings);
            if (Finished != null) Finished(failures == 0);
            else Application.Quit(failures == 0 ? 0 : 1);
        }
        IEnumerator PressKey(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.15f);
        }
        Button TitleButton(string name) => PaperUI.Instance.GetComponentsInChildren<Button>().First(b => b.name == name && b.transform.parent.name == "Title menu");
        IEnumerator HoverButton(Button button)
        {
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator ClickButton(Button button)
        {
            yield return HoverButton(button);
            var position = mouse.position.ReadValue();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 }); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); yield return null;
            yield return new WaitForSecondsRealtime(.2f);
        }
        void OnLog(string message, string stack, LogType type)
        { if (type is LogType.Error or LogType.Exception) { failures++; report.AppendLine("RUNTIME ERROR " + message + "\n" + stack); Flush(); } }
        void Check(bool condition, string message) { if (!condition) failures++; report.AppendLine((condition ? "PASS " : "FAIL ") + message); Flush(); }
        void Flush() => File.WriteAllText(Path.Combine(output, "checks.txt"), report.ToString());

        void Capture(string name, int width, int height)
        {
            var canvas = PaperUI.Instance.GetComponentInChildren<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>(); bool scalerEnabled = scaler.enabled; float previousScale = canvas.scaleFactor;
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera; float previousDistance = canvas.planeDistance;
            var owner = new GameObject("Title QA capture camera"); var camera = owner.AddComponent<Camera>();
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            float renderScale = pipeline ? pipeline.renderScale : 1;
            camera.cullingMask = 1 << 5; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.94f,.91f,.84f);
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 100; camera.transform.position = new Vector3(0,0,-10);
            var rt = RenderTexture.GetTemporary(width,height,24); var previousActive = RenderTexture.active; Texture2D image = null;
            try
            {
                camera.targetTexture = rt; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                scaler.enabled = false; canvas.scaleFactor = Mathf.Min(width / 1920f, height / 1080f);
                // Overlay UI is native-resolution in the game. Do not downsample it with
                // the world's quality preset when routing it through a QA capture camera.
                if (pipeline) pipeline.renderScale = 1;
                foreach (var label in canvas.GetComponentsInChildren<Text>()) label.SetAllDirty();
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                image = new Texture2D(width,height,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"), image.EncodeToPNG());
                var buttons = canvas.GetComponentsInChildren<Button>().Where(b => b.name is "게임 시작" or "옵션" or "나가기");
                foreach (var button in buttons)
                {
                    var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                    Check(corners.All(c => { var v = camera.WorldToViewportPoint(c); return v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1; }), name+" button within viewport: "+button.name);
                }
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                canvas.scaleFactor = previousScale; scaler.enabled = scalerEnabled;
                if (pipeline) pipeline.renderScale = renderScale;
                Canvas.ForceUpdateCanvases(); RenderTexture.active = previousActive;
                if (image) Destroy(image); Destroy(owner); RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
