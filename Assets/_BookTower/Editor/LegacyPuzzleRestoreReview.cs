using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BookTower.Presentation;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.Story;
using BookTower.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class LegacyPuzzleRestoreReview
    {
        const string Output = "Docs/ArtReview/LegacyPuzzleRestore";
        public static void Build() => TitleScreenReview.Build();
        public static void AuditWorkbookRay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity"); Physics.SyncTransforms();
            var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            var note=room.GetComponentsInChildren<ReadableNote>().First(n=>n.id==OpeningStoryDirector.WorkNote);
            var eye=room.transform.TransformPoint(new Vector3(-8,3.15f,-9))+Vector3.up*1.1f;
            var ray=new Ray(eye,(note.transform.position-eye).normalized);
            var text=new StringBuilder($"NOTE {note.transform.position:F4}; eye={eye:F4}; range={note.GetComponent<BookTower.Interaction.Interactable>().Range}\n");
            foreach(var hit in Physics.RaycastAll(ray,15,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                text.AppendLine($"HIT {StructureCollisionFit.PathOf(hit.transform)} at={hit.point:F4}; fromNote={Vector3.Distance(hit.point,note.transform.position):F4}; closest={Vector3.Distance(hit.collider.ClosestPoint(note.transform.position),note.transform.position):F4}; bounds={hit.collider.bounds}");
            Directory.CreateDirectory("Logs/LegacyPuzzleRestore"); File.WriteAllText("Logs/LegacyPuzzleRestore/workbook-ray.txt",text.ToString());
            EditorApplication.Exit(0);
        }
        static void Require(bool condition, string message, StringBuilder report)
        {
            report.AppendLine((condition ? "PASS " : "FAIL ") + message);
            if (!condition) throw new Exception(message);
        }

        public static void AuditAndRefreshDepth()
        {
            Directory.CreateDirectory(Output);
            var report = new StringBuilder();
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                Require(roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "scene has no missing scripts", report);
                Require(Object.FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None).Length == 0, "no active gravity puzzle", report);
                var rooms = Object.FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Require(rooms.Count(r => r.transform.Find("GravityPage_RebuiltRoom")) == 16 && rooms.All(r =>
                    !r.transform.Find("GravityPage_RebuiltRoom") || !r.transform.Find("GravityPage_RebuiltRoom").gameObject.activeSelf), "16 gravity rooms retained inactive for recovery", report);
                var foyer = rooms.First(r => r.RoomId == "F0"); var theatre = rooms.First(r => r.RoomId == "1A"); var reading = rooms.First(r => r.RoomId == "1B");
                var bridge = foyer.GetComponentInChildren<FoldBridgePuzzle>();
                var shadow = theatre.GetComponentInChildren<StorybookLightPuzzle>();
                var lantern = reading.GetComponentInChildren<ReadingRoomPuzzle>();
                Require(bridge && bridge.enabled && bridge.lightSurfaces.Length > 0, "F0 optical bridge and light surfaces restored", report);
                Require(shadow && shadow.enabled && shadow.realShadows && shadow.stage.gameObject.activeInHierarchy, "1A real projected shadow stage restored", report);
                Require(lantern && lantern.enabled && lantern.legacyPuzzle && lantern.lantern.gameObject.activeInHierarchy, "1B lantern and original progression restored", report);
                var story = Object.FindFirstObjectByType<OpeningStoryDirector>();
                Require(story && !story.primaryPuzzle && story.bridge == bridge && story.theatrePuzzle == shadow.GetComponent<PuzzleBase>(), "opening story and memory rewards point to legacy puzzles", report);
                Require(foyer.GetComponentsInChildren<BindingDoor>().Any(d => d.puzzle == bridge), "F0 door listens to optical bridge", report);
                Require(!Object.FindObjectsByType<ReadableNote>(FindObjectsSortMode.None).Any(n => n.pages != null && n.pages.Any(p => p.Contains("중력 페이지"))), "active workbooks no longer teach gravity cargo rules", report);
                foreach (var puzzle in Object.FindObjectsByType<PuzzleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p is not GravityPagePuzzle))
                    report.AppendLine($"RESTORED {puzzle.PuzzleId}: {puzzle.GetType().Name}; active={puzzle.gameObject.activeInHierarchy}; enabled={puzzle.enabled}");

                // Refresh only architecture depth masks; preserve authored lights, art,
                // current probe data and render settings. The removed cargo boards must
                // no longer occlude the room's atmosphere shafts.
                var bake = typeof(PaperAtmosphereBuilder).GetMethod("BakeBlockers", BindingFlags.Static | BindingFlags.NonPublic);
                int refreshed = 0;
                Physics.SyncTransforms();
                foreach (var atmosphere in Object.FindObjectsByType<PaperAtmosphere>(FindObjectsSortMode.None))
                {
                    atmosphere.shaftDepth = atmosphere.shafts.Select((light, i) =>
                    {
                        refreshed++;
                        return (Texture2D)bake.Invoke(null, new object[] { light, atmosphere.room, atmosphere.room.RoomId + "-shaft-" + i });
                    }).ToArray();
                    EditorUtility.SetDirty(atmosphere);
                }
                report.AppendLine("PASS refreshed atmosphere architecture masks=" + refreshed + "; puzzle lights and indirect probe bake retained");
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                File.WriteAllText(Output + "/structure-checks.txt", report + "RESULT: PASS\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                File.WriteAllText(Output + "/structure-checks.txt", report + "RESULT: FAIL\n" + error);
                Debug.LogException(error); EditorApplication.Exit(1);
            }
        }
    }
}
