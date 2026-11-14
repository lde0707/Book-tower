using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BookTower.CameraSystem;
using BookTower.Core;
using BookTower.PlayerSystem;
using BookTower.Rooms;
using BookTower.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Action = BookTower.Puzzles.ReadingRoomControl.Action;

namespace BookTower.Puzzles
{
    // Explicit build smoke test. Editor verifiers separately cover actual F/mouse input.
    public sealed class LegacyPuzzleBuildProbe : MonoBehaviour
    {
        readonly StringBuilder report = new StringBuilder();
        int failures;
        string output;
        PlayerController player;
        Room[] rooms;
        Keyboard keyboard;
        Mouse mouse;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Storage()
        {
            if (Environment.GetCommandLineArgs().Contains("-legacyQA"))
                PaperSettingsRuntime.TestStoragePath = Path.Combine(Application.temporaryCachePath, "legacy-light-qa.json");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Environment.GetCommandLineArgs().Contains("-legacyQA")) new GameObject("Legacy light QA").AddComponent<LegacyPuzzleBuildProbe>();
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath("Logs/LegacyPuzzleRestore/Standalone"); Directory.CreateDirectory(output);
            Application.runInBackground = true; Application.logMessageReceived += OnLog;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); keyboard.MakeCurrent(); mouse.MakeCurrent();
            yield return new WaitForSecondsRealtime(.8f);
            var story = FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();
            if (story) { if (story.companion) story.companion.transform.SetParent(null, true); story.enabled = false; }
            PaperUI.Instance.EndStory(); PaperUI.Instance.Resume(); GameFlow.EnsureExists().Resume();
            player = FindFirstObjectByType<PlayerController>(); player.enabled = false;
            FindFirstObjectByType<RoomManager>().enabled = false;
            rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            Check(FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None).Length == 0, "no active gravity cargo puzzles");
            Time.timeScale = 4;
            var foyer = Show("F0"); var bridge = foyer.GetComponentInChildren<FoldBridgePuzzle>();
            var controls = foyer.GetComponentsInChildren<BookTower.Interaction.FoldBridgeControl>();
            bool UseBridge(BookTower.Interaction.FoldBridgeControl control)
            { Place(control.transform.position - foyer.transform.forward); bool ok = control.TryUse(player.transform.position); Check(ok, "F0 control " + control.name); return ok; }
            Check(!bridge.LightConnected && !bridge.ShutterOpen, "F0 initially blocks optical path");
            UseBridge(controls.First(c => c.opticalControl == 1)); yield return new WaitForSeconds(2.3f);
            Check(bridge.DirectReceiverLit && !bridge.LightConnected, "F0 shutter opens actual lower light path");
            UseBridge(controls.First(c => c.name == "입구 제본 손잡이")); yield return new WaitForSeconds(2.3f);
            Check(bridge.BridgeDown && !bridge.DirectReceiverLit, "F0 lowered page blocks direct light and remains walkable");
            var first = controls.First(c => c.opticalControl == 2);
            UseBridge(first); yield return new WaitForSeconds(2.3f);
            Check(!bridge.LightConnected, "F0 wrong reflection does not solve");
            UseBridge(first); yield return new WaitForSeconds(2.3f);
            var last = controls.First(c => c.opticalControl == 3);
            UseBridge(last); yield return new WaitForSeconds(2.3f);
            UseBridge(last); yield return new WaitForSeconds(2.3f);
            Check(bridge.LightConnected && bridge.State != PuzzleBase.PuzzleState.Solved, "F0 reflected path awaits seal confirmation");
            UseBridge(controls.First(c => c.seal)); yield return new WaitForSeconds(5);
            Check(bridge.State == PuzzleBase.PuzzleState.Solved && foyer.GetComponentInChildren<BindingDoor>().IsOpen, "F0 optical solution opens existing binding door");
            yield return Capture("F0", foyer, new Vector3(0,3.1f,-10.5f), 0, 5);

            var theatre = Show("1A"); var shadow = theatre.GetComponentInChildren<StorybookLightPuzzle>();
            shadow.GetComponent<PuzzleBase>().Activate();
            shadow.SelectLight(0); shadow.DragPiece(2, ShadowDuet.FoxBodySolution); shadow.DragPiece(3, ShadowDuet.FoxTailSolution);
            if (!shadow.Duet.TailFlipped) shadow.TurnPiece(3);
            yield return new WaitForSeconds(3);
            Check(shadow.Duet.FoxRecorded && !shadow.Complete, "1A correct left-light fox is remembered without premature completion");
            shadow.SelectLight(1); shadow.DragPiece(0,new Vector3(-1.8f,1,12.5f/1.5f)); shadow.DragPiece(1,new Vector3(-.9f,1,12.5f/3));
            if (!shadow.Flipped) shadow.TurnPiece(1);
            yield return new WaitForSeconds(3);
            Check(shadow.Complete && shadow.Duet.AllRecorded && shadow.GetComponent<PuzzleBase>().State == PuzzleBase.PuzzleState.Solved, "1A both projected animals solve original P2");
            yield return Capture("1A", theatre, new Vector3(-4,.2f,14), 0, 4);

            var reading = Show("1B"); var lantern = reading.GetComponentInChildren<ReadingRoomPuzzle>();
            bool UseLamp(Action action, bool expected = true)
            {
                var control = lantern.GetComponentsInChildren<ReadingRoomControl>().First(c => c.action == action);
                Place(control.transform.position - reading.transform.forward);
                bool ok = control.TryUse(player.transform.position); Check(ok == expected, "1B control " + action + " expected=" + expected); return ok;
            }
            UseLamp(Action.Pickup, false); UseLamp(Action.Charge); UseLamp(Action.LeftRing); UseLamp(Action.RightRing); UseLamp(Action.Pickup);
            yield return new WaitForSeconds(1);
            Check(lantern.Charged && lantern.State == ReadingRoomPuzzle.LampState.Carried, "1B two locked rings preserve carried light");
            UseLamp(Action.RecordDock); UseLamp(Action.LeftRing); UseLamp(Action.RightRing); yield return new WaitForSeconds(2);
            Check(lantern.RecordOpen && !lantern.Charged, "1B docked release opens crank cabinet");
            UseLamp(Action.TakeCrank); UseLamp(Action.Crank); UseLamp(Action.Latch); UseLamp(Action.WindowCharge);
            UseLamp(Action.LeftRing); UseLamp(Action.RightRing); UseLamp(Action.Pickup); UseLamp(Action.ExitDock); UseLamp(Action.LeftRing); UseLamp(Action.RightRing);
            yield return new WaitForSeconds(3);
            Check(lantern.Complete && lantern.legacyPuzzle.State == PuzzleBase.PuzzleState.Solved && !lantern.doorBlocker.enabled, "1B repaired window and final lantern release open original P3 exit");
            yield return Capture("1B", reading, new Vector3(0,.2f,-11), 0, -5);

            Time.timeScale = 1;
            foreach (var link in FindObjectsByType<RampLink>(FindObjectsSortMode.None).Where(l =>
                (l.FromRoomId == "F0" && l.ToRoomId == "1A") || (l.FromRoomId == "1A" && l.ToRoomId == "1B")))
            {
                foreach (var room in rooms) room.SetVisible(room.RoomId == link.FromRoomId || room.RoomId == link.ToRoomId);
                foreach (var bound in FindObjectsByType<RoomBound>(FindObjectsSortMode.None)) bound.SetVisible(bound.Belongs(link.FromRoomId) || bound.Belongs(link.ToRoomId));
                foreach (bool reverse in new[] { false, true })
                {
                    var start = reverse ? link.ToPoint : link.FromPoint; var end = reverse ? link.FromPoint : link.ToPoint;
                    Place(start + Vector3.up * .25f); Physics.SyncTransforms();
                    bool clear = true;
                    for (int step = 1; step <= 180; step++)
                    {
                        var target = Vector3.Lerp(start,end,step/180f) + Vector3.up*.25f;
                        player.Controller.Move(target-player.transform.position);
                        clear &= Vector3.Distance(target,player.transform.position) < .8f;
                    }
                    Check(clear, link.FromRoomId + "–" + link.ToRoomId + " real capsule passage reverse=" + reverse);
                }
            }
            report.AppendLine($"RESULT: {(failures == 0 ? "PASS" : "FAIL")}; failures={failures}; editor={Application.isEditor}"); Flush();
            Application.logMessageReceived -= OnLog; InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
            Application.Quit(failures == 0 ? 0 : 1);
        }
        Room Show(string id)
        {
            var selected = rooms.First(r => r.RoomId == id); foreach (var room in rooms) room.SetVisible(room == selected);
            Place(selected.transform.TransformPoint(new Vector3(0,id == "F0" ? 3.2f : .3f,-10)));
            return selected;
        }
        void Place(Vector3 position)
        { player.Controller.enabled = false; player.transform.position = position; player.Controller.enabled = true; Physics.SyncTransforms(); }
        IEnumerator Capture(string name, Room room, Vector3 local, float yaw, float pitch)
        {
            Place(room.transform.TransformPoint(local)); var camera = FindFirstObjectByType<PaperCamera>(); camera.SetAim(yaw,pitch); camera.ReturnToFollow();
            yield return new WaitForSecondsRealtime(.8f);
            var c = Camera.main; var rt = RenderTexture.GetTemporary(1600,900,24); var old = c.targetTexture; var active = RenderTexture.active; float aspect = c.aspect;
            Texture2D image = null;
            try { c.targetTexture=rt;c.aspect=16f/9;c.Render();RenderTexture.active=rt;image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG()); }
            finally { c.targetTexture=old;c.aspect=aspect;RenderTexture.active=active;if(image)Destroy(image);RenderTexture.ReleaseTemporary(rt); }
        }
        void Check(bool condition, string message) { if(!condition)failures++;report.AppendLine((condition?"PASS ":"FAIL ")+message);Flush(); }
        void Flush() => File.WriteAllText(Path.Combine(output,"checks.txt"),report.ToString());
        void OnLog(string message,string stack,LogType type) { if(type is LogType.Error or LogType.Exception){failures++;report.AppendLine("RUNTIME ERROR "+message+"\n"+stack);Flush();} }
    }
}
