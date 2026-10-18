using System;
using System.IO;
using System.Text;
using BookTower.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Batch-only disposable fixtures; never saves the game scene.
    public static class GravityWallMotionReview
    {
        public static void Run()
        {
            var report = new StringBuilder();
            var originalMode = Physics.simulationMode;
            int failures = 0, reproduced = 0;
            void Check(bool ok, string message)
            {
                report.AppendLine((ok ? "PASS " : "FAIL ") + message);
                if (!ok) failures++;
            }
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                foreach (bool corrected in new[] { false, true })
                foreach (float cell in new[] { .8f, 1f, 1.2f })
                foreach (int size in new[] { 1, 2 })
                foreach (var axis in new[] { Vector3.down, Vector3.left, Vector3.up, Vector3.right })
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var root = new GameObject("Rail motion fixture");
                    var puzzle = root.AddComponent<GravityPagePuzzle>();
                    var material = new PhysicsMaterial("Frictionless fixture")
                    {
                        staticFriction = 0, dynamicFriction = 0, bounciness = 0,
                        frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                    var side = new Vector3(-axis.y, axis.x, 0);
                    var wallSize = new Vector3(cell, cell, 1.5f);
                    var wallMesh = F0Mesh.RoundedBox(wallSize, .045f);
                    void Wall(Vector3 position)
                    {
                        var go = new GameObject("고정 선반"); go.transform.SetParent(root.transform);
                        go.transform.position = position;
                        var box = go.AddComponent<BoxCollider>(); box.size = wallSize; box.sharedMaterial = material; box.enabled = false;
                        var mesh = go.AddComponent<MeshCollider>(); mesh.sharedMesh = wallMesh; mesh.sharedMaterial = material;
                    }
                    for (int i = -3; i < 15; i++) Wall(axis * (i * cell) - side * ((size + 1) * cell * .5f));
                    for (int i = -2; i <= 2; i++) Wall(axis * (12 * cell) + side * (i * cell));
                    var cargoObject = new GameObject("Cargo"); cargoObject.transform.SetParent(root.transform);
                    float frame = (size - .006f) * cell;
                    var cargoBox = cargoObject.AddComponent<BoxCollider>(); cargoBox.size = new Vector3(frame, frame, .95f); cargoBox.sharedMaterial = material;
                    var body = cargoObject.AddComponent<Rigidbody>(); body.useGravity = false; body.linearDamping = 0;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation |
                        (axis.x == 0 ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionY);
                    puzzle.cargo = new[] { body };
                    if (corrected)
                    {
                        GravityPageRailPhysics.Configure(puzzle);
                        int count = root.GetComponentsInChildren<Collider>().Length;
                        GravityPageRailPhysics.Configure(puzzle);
                        Check(root.GetComponentsInChildren<Collider>().Length == count, "repeated setup reuses colliders");
                    }
                    Physics.SyncTransforms();
                    for (int tick = 0; tick < 50; tick++)
                    {
                        body.AddForce(axis * 12, ForceMode.Acceleration); Physics.defaultPhysicsScene.Simulate(.02f);
                    }
                    float distance = Vector3.Dot(body.position, axis), speed = Vector3.Dot(body.linearVelocity, axis);
                    string context = $"cell={cell} size={size} axis={axis} distance={distance:F5} speed={speed:F5}";
                    if (!corrected)
                    {
                        if (distance < 6.12f * .9f) reproduced++;
                        report.AppendLine("BEFORE " + context);
                    }
                    else
                    {
                        Check(Mathf.Abs(distance - 6.12f) < .01f && Mathf.Abs(speed - 12) < .01f, "wall travel matches free acceleration: " + context);
                        Check(Mathf.Abs(Vector3.Dot(body.position, side)) < .001f, "side wall cannot push cargo off its rail");
                        float stop = 12 * cell - cell * .5f - frame * .5f;
                        float furthest = distance;
                        for (int tick = 0; tick < 100; tick++)
                        {
                            body.AddForce(axis * 12, ForceMode.Acceleration); Physics.defaultPhysicsScene.Simulate(.02f);
                            furthest = Mathf.Max(furthest, Vector3.Dot(body.position, axis));
                        }
                        Check(furthest < 12 * cell && Mathf.Abs(Vector3.Dot(body.position, axis) - stop) < .005f,
                            $"front wall stops cargo without tunnelling: limit={stop:F4} peak={furthest:F4} final={Vector3.Dot(body.position, axis):F4}");
                        Check(Mathf.Abs(Vector3.Dot(body.linearVelocity, axis)) < .01f, "blocked cargo settles");
                    }
                    Object.DestroyImmediate(root); Object.DestroyImmediate(wallMesh); Object.DestroyImmediate(material);
                }
                Check(reproduced > 0, $"original rounded-mesh slowdown reproduced in {reproduced}/24 cases");
            }
            catch (Exception e) { failures++; report.AppendLine(e.ToString()); }
            finally { Physics.simulationMode = originalMode; }
            report.AppendLine($"RESULT: {(failures == 0 ? "PASS" : "FAIL")}; failures={failures}");
            Directory.CreateDirectory("Logs/WallMotion"); File.WriteAllText("Logs/WallMotion/rail-regression.txt", report.ToString());
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}
