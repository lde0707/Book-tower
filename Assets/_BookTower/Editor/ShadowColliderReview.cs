using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace BookTower.EditorTools {
public static class ShadowColliderReview {
 public static void Build(){try{
  var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-colliderBuildRoot");if(i<0||i+1>=args.Length)throw new Exception("Missing collider build output path");
  var folder=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(folder);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/InGame.unity"},locationPathName=Path.Combine(folder,"AFoldOfLight.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
  File.WriteAllText(Path.Combine(folder,"build-result.txt"),$"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; bytes={report.summary.totalSize}");
  EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
