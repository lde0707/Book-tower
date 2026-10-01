using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace BookTower.EditorTools
{
 public static class ReleaseArtBuild
 {
  public static void Run()
  {
   UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ReleaseArtFinishBuilder.Apply();
   const string destination="../ReleaseFinishBuild/AFoldOfLight.exe";
   string path=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),destination));Directory.CreateDirectory(Path.GetDirectoryName(path));
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.enableFrameTimingStats=true;PlayerSettings.runInBackground=true;
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/InGame.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   File.WriteAllText(ReleaseFinishReview.Output+"/build.txt",$"{result.summary.result}, {result.summary.totalSize} bytes, {result.summary.totalTime}, errors={result.summary.totalErrors}, warnings={result.summary.totalWarnings}\n{path}\nMono, Windows64, non-development QA build; InGame scene only.\n");
   EditorApplication.Exit(result.summary.result==BuildResult.Succeeded?0:1);
  }
 }
}
