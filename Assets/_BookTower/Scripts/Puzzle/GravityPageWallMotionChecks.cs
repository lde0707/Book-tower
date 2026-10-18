using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BookTower.Puzzles
{
    public sealed partial class GravityPageBuildProbe
    {
        IEnumerator VerifyWallSliding()
        {
            var source = FindObjectsByType<GravityPagePuzzle>(FindObjectsSortMode.None)
                .First(p => Mathf.Abs(p.railSpacing - 1) < .001f);
            var sourceWall = source.GetComponentsInChildren<MeshCollider>().First(c => c.name == "고정 선반");
            var sourceBox = sourceWall.GetComponent<BoxCollider>();
            var scene = SceneManager.CreateScene("QA wall sliding", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var physics = scene.GetPhysicsScene();
            foreach (int walls in new[] { 0, 1, 2 })
            foreach (float clearance in new[] { .006f, 0f })
            foreach (var axis in new[] { Vector3.down, Vector3.left, Vector3.up, Vector3.right })
            {
                var root = new GameObject("QA sliding fixture"); SceneManager.MoveGameObjectToScene(root, scene);
                var puzzle = root.AddComponent<GravityPagePuzzle>(); puzzle.enabled = false;
                var side = new Vector3(-axis.y, axis.x, 0);
                const float frame = .994f;
                for (int wall = 0; wall < walls; wall++)
                    for (int i = -2; i < 16; i++)
                    {
                        var go = new GameObject("고정 선반"); go.transform.SetParent(root.transform, false);
                        go.transform.position = axis * i + side * ((wall == 0 ? -1 : 1) * (frame + 1 + clearance) * .5f);
                        var rail = go.AddComponent<BoxCollider>(); rail.size = sourceBox.size; rail.sharedMaterial = sourceBox.sharedMaterial; rail.enabled = false;
                        var mesh = go.AddComponent<MeshCollider>(); mesh.sharedMesh = sourceWall.sharedMesh; mesh.sharedMaterial = sourceWall.sharedMaterial;
                    }
                var cargoObject = new GameObject("QA cargo"); cargoObject.transform.SetParent(root.transform, false);
                var box = cargoObject.AddComponent<BoxCollider>(); box.size = new Vector3(frame, frame, .95f);
                box.sharedMaterial = source.cargo[0].GetComponent<Collider>().sharedMaterial;
                var body = cargoObject.AddComponent<Rigidbody>(); body.useGravity = false; body.linearDamping = 0;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation |
                    (axis.x == 0 ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionY);
                puzzle.cargo = new[] { body };
                body.isKinematic = true;
                GravityPageRailPhysics.Configure(puzzle);
                body.isKinematic = false; body.interpolation = RigidbodyInterpolation.Interpolate;
                Physics.SyncTransforms();
                for (int tick = 0; tick < 50; tick++)
                {
                    body.AddForce(axis * 12, ForceMode.Acceleration); physics.Simulate(.02f);
                }
                float travel = Vector3.Dot(body.position, axis), speed = Vector3.Dot(body.linearVelocity, axis);
                Check(Mathf.Abs(travel - 6.12f) < .015f && Mathf.Abs(speed - 12) < .015f,
                    $"PLAY MODE wall sliding walls={walls} clearance={clearance} direction={axis} distance={travel:F5} speed={speed:F5}");
                DestroyImmediate(root);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
