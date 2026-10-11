using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BookTower.Puzzles;
using BookTower.UI;
namespace BookTower.EditorTools
{
    [InitializeOnLoad] public static class GravityPageReview
    {
        const string Flag="GravityPageReview";
        public static string OutputOverride {get=>SessionState.GetString(Flag+"output","");set=>SessionState.SetString(Flag+"output",value??"");}
        static GravityPageReview(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Tick;}
        static void Tick(){if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying)return;Application.runInBackground=true;if(EditorApplication.isPaused)EditorApplication.isPaused=false;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");AssetDatabase.DisallowAutoRefresh();SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/GravityMigration/review-settings.json");}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(!SessionState.GetBool(Flag,false))return;
            GravityPageBuildProbe.Finished=success=>{SessionState.SetInt(Flag+"exit",success?0:1);EditorApplication.ExitPlaymode();};
            if(UnityEngine.Object.FindFirstObjectByType<GravityPageBuildProbe>())return;
            var probe=new GameObject("Gravity review").AddComponent<GravityPageBuildProbe>();
            probe.output=Path.GetFullPath(string.IsNullOrEmpty(OutputOverride)?"Docs/ArtReview/GravityImplemented/Editor":OutputOverride);
            Debug.Log("[Gravity QA] Installed runtime probe: "+probe.output);
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                if(!UnityEngine.Object.FindFirstObjectByType<GravityPageBuildProbe>())Install();
            }
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}
        }
        public static void Build()
        {
            try
            {
                string path=Path.GetFullPath("Builds/GravityPage/AFoldOfLight.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
                PlayerSettings.enableFrameTimingStats=true;PlayerSettings.runInBackground=true;
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/InGame.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
                Directory.CreateDirectory("Docs/ArtReview/GravityImplemented");
                File.WriteAllText("Docs/ArtReview/GravityImplemented/build.txt",$"{result.summary.result}; bytes={result.summary.totalSize}; time={result.summary.totalTime}; errors={result.summary.totalErrors}; warnings={result.summary.totalWarnings}\n{path}");
                EditorApplication.Exit(result.summary.result==BuildResult.Succeeded?0:1);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
