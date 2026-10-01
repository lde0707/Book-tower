using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace BookTower.EditorTools
{
    public static class PaperAtmosphereBuild
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            string path = Path.GetFullPath("../AtmosphereBuild/AFoldOfLight.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.enableFrameTimingStats = true; PlayerSettings.runInBackground = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/InGame.unity" }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            File.WriteAllText(PaperAtmosphereReview.Output + "/build.txt", $"{report.summary.result}; {report.summary.totalSize} bytes; {report.summary.totalTime}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}\nWin64 Mono nondevelopment, InGame-only QA build.\n{path}\n");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
