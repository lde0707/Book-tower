using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using BookTower.Core;
using BookTower.Fold;
using BookTower.Presentation;
using BookTower.Puzzles;
using BookTower.Rooms;

namespace BookTower.EditorTools
{
    /// <summary>
    /// Classifies the build's scene copy, never the authoring scene or collision meshes.
    /// Unity keeps per-renderer culling, materials, probes and shadow passes in a static batch.
    /// Unknown behaviours are excluded until their transform ownership is reviewed.
    /// </summary>
    public sealed class RoomStaticBatching : IProcessSceneWithReport
    {
        // Unity's own static mesh combiner runs at order 0 (UnityBuildPostprocessor).
        public int callbackOrder => -1000;
        static readonly HashSet<Type> stationaryOwners = new()
        {
            typeof(Room), typeof(LevelNote), typeof(PaperAtmosphere), typeof(PaperRoomMotion),
            typeof(StorybookPaperMotion), typeof(FoldGroup), typeof(RoomWall), typeof(PencilOutline),
            typeof(BindingDoor)
        };

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || scene.name != "InGame") return;
            var summary = new StringBuilder();
            bool disable = Array.IndexOf(Environment.GetCommandLineArgs(), "-disableRoomBatching") >= 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var room in root.GetComponentsInChildren<Room>(true))
            {
                if (room.RoomId is not ("F0" or "1A" or "1B")) continue;
                var moving = new HashSet<Transform>();
                foreach (var motion in room.GetComponentsInChildren<PaperRoomMotion>(true))
                    foreach (var tip in motion.tips) if (tip) moving.Add(tip);
                foreach (var motion in room.GetComponentsInChildren<StorybookPaperMotion>(true))
                    foreach (var paper in motion.papers) if (paper) moving.Add(paper);
                foreach (var door in room.GetComponentsInChildren<BindingDoor>(true))
                {
                    if (door.left) moving.Add(door.left);
                    if (door.right) moving.Add(door.right);
                }
                int batched = 0, excluded = 0;
                long vertices = 0;
                foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    bool eligible = !disable && renderer.gameObject.activeInHierarchy && renderer.enabled &&
                        filter && filter.sharedMesh && !renderer.HasPropertyBlock() &&
                        IsStationary(renderer.transform, room.transform, moving);
                    var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                        eligible ? flags | StaticEditorFlags.BatchingStatic : flags & ~StaticEditorFlags.BatchingStatic);
                    if (eligible) { batched++; vertices += filter.sharedMesh.vertexCount; }
                    else excluded++;
                }
                summary.AppendLine($"{room.RoomId}: batchable={batched}, excluded={excluded}, vertices={vertices}");
            }
            Directory.CreateDirectory("Docs/ArtReview/Optimization");
            File.WriteAllText("Docs/ArtReview/Optimization/static-batching.txt", summary.ToString());
        }

        static bool IsStationary(Transform candidate, Transform room, HashSet<Transform> moving)
        {
            for (var at = candidate; at && at != room; at = at.parent)
            {
                if (moving.Contains(at) || at.GetComponent<Rigidbody>() || at.GetComponent<Animator>() || at.GetComponent<Animation>()) return false;
                foreach (var behaviour in at.GetComponents<MonoBehaviour>())
                    if (!behaviour || !stationaryOwners.Contains(behaviour.GetType())) return false;
            }
            return true;
        }
    }

    public sealed class RoomStaticBatchingValidation : IProcessSceneWithReport
    {
        public int callbackOrder => 1000;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || scene.name != "InGame" ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "-disableRoomBatching") >= 0) return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var room in root.GetComponentsInChildren<Room>(true))
            {
                if (room.RoomId is not ("F0" or "1A" or "1B")) continue;
                int count = 0;
                foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.isPartOfStaticBatch) count++;
                    foreach (var material in renderer.sharedMaterials)
                        if (material && (material.shader.name is "BookTower/ReadingPaper" or "BookTower/CrayonAtlasRepeat") && material.FindPass("DepthNormals") < 0)
                            throw new BuildFailedException("Paper shader needs a depth/normals pass for depth priming: " + material.name);
                }
                if (count == 0) throw new BuildFailedException($"Static batching produced no geometry for {room.RoomId}.");
                foreach (var puzzle in room.GetComponentsInChildren<GravityPagePuzzle>(true))
                    foreach (var renderer in puzzle.GetComponentsInChildren<MeshRenderer>(true))
                        if (renderer.isPartOfStaticBatch) throw new BuildFailedException("Puzzle geometry must remain dynamic: " + renderer.name);
                File.AppendAllText("Docs/ArtReview/Optimization/static-batching.txt", $"VERIFIED {room.RoomId}: {count} batched renderers\n");
            }
        }
    }
}
