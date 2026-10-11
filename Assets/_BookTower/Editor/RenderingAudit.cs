using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;

namespace BookTower.EditorTools
{
    public static class RenderingAudit
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            var report = new StringBuilder();
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None).OrderBy(r => r.RoomId))
            {
                var renderers = room.GetComponentsInChildren<MeshRenderer>();
                report.AppendLine($"ROOM {room.RoomId}: renderers={renderers.Length}, materials={renderers.SelectMany(r => r.sharedMaterials).Distinct().Count()}, batchingStatic={renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic))}");
                foreach (var group in renderers.GroupBy(r => r.sharedMaterial ? r.sharedMaterial.shader.name : "null").OrderByDescending(g => g.Count()))
                    report.AppendLine($"  shader {group.Key}: {group.Count()}");
                foreach (var light in room.GetComponentsInChildren<Light>())
                    report.AppendLine($"  light {light.name}: {light.type}, intensity={light.intensity}, range={light.range}, shadows={light.shadows}, mode={light.lightmapBakeType}");
                foreach (var group in room.GetComponentsInChildren<MonoBehaviour>().GroupBy(c => c.GetType().Name))
                    report.AppendLine($"  component {group.Key}: {group.Count()}");
            }
            Directory.CreateDirectory("Docs/ArtReview/Optimization");
            File.WriteAllText("Docs/ArtReview/Optimization/rendering-audit.txt", report.ToString());
            EditorApplication.Exit(0);
        }
    }
}
