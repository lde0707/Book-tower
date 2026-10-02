using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BookTower.CameraSystem;
using BookTower.Interaction;
using BookTower.PlayerSystem;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    [InitializeOnLoad]
    public static class ShadowFoxAlignmentVerifier
    {
        const string Flag = "FoxAlignmentQA";
        const string Output = "Docs/ArtReview/ShadowFoxAlignment";
        static IEnumerator routine;
        static double next, started;
        static readonly StringBuilder report = new();
        static InputSettings original;
        static Mouse mouse;
        static Keyboard keyboard;

        static ShadowFoxAlignmentVerifier() => EditorApplication.playModeStateChanged += Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateSettings()
        {
            if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/fox-alignment-settings.json");
        }
        public static void Run() => Start(false);
        public static void Baseline() => Start(true);
        static void Start(bool baseline)
        {
            SessionState.SetBool(Flag + "baseline", baseline);
            SessionState.SetBool(Flag, true);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Flag, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                report.Clear(); routine = Test(); started = EditorApplication.timeSinceStartup;
                next = started + 1; EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(Flag);
                EditorApplication.Exit(SessionState.GetInt(Flag + "exit", 1));
            }
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 100) throw new Exception("Timeout");
                if (!routine.MoveNext()) { Finish(null); return; }
                next = EditorApplication.timeSinceStartup + (routine.Current is float f ? f : .05);
            }
            catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception e)
        {
            EditorApplication.update -= Tick;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (original) InputSystem.settings = original;
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + (SessionState.GetBool(Flag + "baseline", false) ? "/before.txt" : "/after.txt"), report + (e == null ? "PASS" : "FAIL " + e));
            SessionState.SetInt(Flag + "exit", e == null ? 0 : 1);
            EditorApplication.ExitPlaymode();
        }
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            report.AppendLine("PASS: " + message);
        }
        static void Place(StorybookLightPuzzle p, Vector3 bodyOffset, Vector3 tailOffset)
        {
            p.DragPiece(2, ShadowDuet.FoxBodySolution + bodyOffset);
            p.DragPiece(3, ShadowDuet.FoxTailSolution + tailOffset);
            p.Duet.foxBody.localPosition = p.PiecePosition(2);
            p.Duet.foxTail.localPosition = p.PiecePosition(3);
            p.Duet.Tick(0);
        }
        static void Capture(StorybookLightPuzzle p, string name)
        {
            var c = Camera.main;
            c.transform.position = p.stage.TransformPoint(new Vector3(0, 5, -8));
            c.transform.LookAt(p.stage.TransformPoint(new Vector3(0, 2.8f, 9)));
            Directory.CreateDirectory(Output);
            InteriorF0Builder.Capture(c, Output + "/" + name + ".png");
        }
        static IEnumerator Test()
        {
            foreach (var s in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
            PaperUI.Instance.EndStory(); PaperUI.Instance.Resume(); BookTower.Core.GameFlow.EnsureExists().Resume();
            Object.FindFirstObjectByType<RoomManager>().enabled = false;
            var p = Object.FindObjectsByType<StorybookLightPuzzle>(FindObjectsSortMode.None).Single(x => x.realShadows);
            var room = p.GetComponentInParent<Room>(); room.SetVisible(true);
            var player = Object.FindFirstObjectByType<PlayerController>(); player.enabled = false;
            Object.FindFirstObjectByType<PaperCamera>().enabled = false;
            p.enabled = false;
            var duet = p.Duet;
            p.SelectLight(0); if (!duet.TailFlipped) p.TurnPiece(3);
            Place(p, Vector3.zero, Vector3.zero);
            Check(duet.FoxAligned, "Exact left-light solution still matches");
            // A few centimetres of planar mouse placement: visually near the wall line,
            // yet the old single-worst-vertex threshold rejects it.
            var bodyOffset = new Vector3(.06f, 0, .02f);
            var tailOffset = new Vector3(-.04f, 0, -.02f);
            Place(p, bodyOffset, tailOffset);
            float bodyError = duet.ProjectionError(duet.foxBody, duet.bodyProbes, duet.bodyTargets);
            float tailError = duet.ProjectionError(duet.foxTail, duet.tailProbes, duet.tailTargets);
            report.AppendLine($"Near match screen error: body={bodyError:F5}, tail={tailError:F5}; legacy max tolerance=0.10");
            yield return .25f;
            bool baseline = SessionState.GetBool(Flag + "baseline", false);
            Capture(p, baseline ? "near-match-before" : "near-match-after");
            if (baseline) { Check(!duet.FoxAligned, "Reproduced visually near alignment rejected by old rule"); yield break; }
            Check(duet.FoxAligned, "Visible near match under left light is accepted");
            var savedProbes = duet.bodyProbes;
            duet.bodyProbes = Array.Empty<Vector3>();
            Check(!duet.FoxAligned, "Missing contour data fails closed");
            duet.bodyProbes = savedProbes;
            p.Use(StorybookLightControl.Action.Lamp);
            Check(!duet.FoxAligned, "Dark projector cannot record");
            p.Use(StorybookLightControl.Action.Lamp);
            p.TurnPiece(3); duet.Tick(0);
            Check(!duet.FoxAligned, "Mirrored tail rejected");
            p.TurnPiece(3); duet.Tick(0);
            Place(p, Vector3.right * .35f, tailOffset);
            Check(!duet.FoxAligned, "Shifted body rejected");
            Place(p, bodyOffset, Vector3.forward * .5f);
            Check(!duet.FoxAligned, "Wrong tail size rejected");
            foreach (int index in new[] { 1, 2 })
            {
                p.SelectLight(index);
                float shift = index * 4;
                Place(p, Vector3.right * shift * (1 - 1 / 1.8f), Vector3.right * shift * (1 - 1 / 2.5f));
                Check(!duet.FoxAligned, "Folded contour rejects wrong light after lateral compensation: " + index);
            }
            p.SelectLight(0); Place(p, bodyOffset, tailOffset);
            // Exercise the real hold/release gesture at a valid, slightly imperfect match.
            original = InputSystem.settings;
            var settings = Object.Instantiate(original);
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            var target = duet.foxTail.GetComponent<BoxCollider>().bounds.center;
            Camera.main.transform.position = target - p.stage.forward * 2;
            Camera.main.transform.LookAt(target);
            player.Controller.enabled = false; player.transform.position = target - p.stage.forward * 6; player.Controller.enabled = true;
            Physics.SyncTransforms(); yield return .15f;
            Check(PaperShadowDrag.Aimed == duet.foxTail.GetComponent<PaperShadowHandle>(), "Aim reaches fox tail at matching placement");
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); yield return .15f;
            Check(PaperShadowDrag.Holding, "Actual mouse click holds fox cutout");
            p.enabled = true; yield return 1.5f;
            Check(!duet.FoxRecorded, "Held paper remains adjustable before release");
            var toast = typeof(PaperUI).GetField("toast", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Check(((string)toast.GetValue(PaperUI.Instance)).Contains("놓으면"), "Matched held paper tells player to release");
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return 1.65f;
            Check(duet.FoxRecorded && !p.Complete, "Releasing visible near match records fox first without opening exit");
            Capture(p, "fox-recorded");
            p.SelectLight(1);
            p.DragPiece(0, new Vector3(-1.8f, 1, 12.5f / 1.5f));
            p.DragPiece(1, new Vector3(-.9f, 1, 12.5f / 3));
            if (!p.Flipped) p.TurnPiece(1);
            yield return 2.5f;
            Check(duet.AllRecorded && p.Complete, "Fox-first then rabbit completes both memories");
            var gate = p.GetComponent<FoldCombinationPuzzle>(); gate.Activate(); yield return .2f;
            Check(gate.State == PuzzleBase.PuzzleState.Solved, "Completed pair reaches existing P2 gate");
        }
    }
}
