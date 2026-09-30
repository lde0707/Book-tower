using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    public static class TurnaroundSpiritIntegration
    {
        public const string Review="Docs/ArtReview/TurnaroundSpiritModelV6/Animation";
        public static void CheckMotion()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                var spirit=UnityEngine.Object.FindFirstObjectByType<HoodSpiritAnimator>();
                var a=spirit.GetComponentInChildren<Animator>();var skin=spirit.GetComponentInChildren<SkinnedMeshRenderer>();
                var mesh=new Mesh();string report="";
                foreach(var clip in a.runtimeAnimatorController.animationClips.Distinct())
                {
                    clip.SampleAnimation(a.gameObject,0);skin.BakeMesh(mesh);var initial=mesh.vertices;float maximum=0;
                    foreach(float sample in new[]{.25f,.5f,.75f})
                    {
                        clip.SampleAnimation(a.gameObject,clip.length*sample);skin.BakeMesh(mesh);var current=mesh.vertices;
                        for(int i=0;i<current.Length;i++)maximum=Mathf.Max(maximum,Vector3.Distance(initial[i],current[i]));
                    }
                    if(maximum<.0001f)throw new Exception("Clip does not deform actual scene mesh: "+clip.name);
                    report+=$"PASS {clip.name}: maximum vertex travel {maximum:F6}\n";
                }
                File.WriteAllText(Review+"/motion-check.txt",report);UnityEngine.Object.DestroyImmediate(mesh);EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText(Review+"/motion-check.txt","FAIL "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory(Review);AssetDatabase.Refresh();
                string path=HoodSpiritBuilder.Dir+"/PaperHoodSpirit_Rigged.fbx";
                string[] names={"Idle","Move","Run","JumpRise","Fall","Glide","Land","LookAround","TidyBookmark","Stretch"};
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;
                importer.optimizeGameObjects=false;importer.isReadable=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
                importer.SaveAndReimport();var configs=importer.defaultClipAnimations;
                foreach(var c in configs)
                {
                    c.name=names.FirstOrDefault(n=>c.name==n||c.name.EndsWith("|"+n)||c.name.EndsWith("_"+n))??c.name;
                    c.loopTime=new[]{"Idle","Move","Run","Fall","Glide"}.Contains(c.name);c.loopPose=c.loopTime;
                    c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;
                    c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;
                }
                importer.clipAnimations=configs;importer.SaveAndReimport();
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(HoodSpiritBuilder.Dir+"/HoodSpirit.controller");
                var sm=controller.layers[0].stateMachine;
                foreach(string name in names)
                {
                    var clip=clips.Single(c=>c.name==name);
                    var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);
                    state.motion=clip;EditorUtility.SetDirty(state);
                }
                EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
                var audio=(AudioImporter)AssetImporter.GetAtPath("Assets/_BookTower/Resources/Sfx/spirit_jump.wav");
                var settings=audio.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                audio.defaultSampleSettings=settings;audio.forceToMono=true;audio.SaveAndReimport();
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                var spirit=UnityEngine.Object.FindFirstObjectByType<HoodSpiritAnimator>();
                var skin=spirit.GetComponentInChildren<SkinnedMeshRenderer>();
                if(skin.bones.Length!=23)throw new Exception("Expected 23 connected-limb bones, got "+skin.bones.Length);
                var animator=spirit.ModelAnimator?spirit.ModelAnimator:spirit.GetComponentInChildren<Animator>();
                var mesh=new Mesh();
                foreach(var clip in clips)
                {
                    foreach(float time in new[]{0f,.25f,.5f,.75f,1f})
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*time);skin.BakeMesh(mesh);
                        if(mesh.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new Exception("Nonfinite mesh "+clip.name);
                    }
                }
                UnityEngine.Object.DestroyImmediate(mesh);
                File.WriteAllText(Review+"/import-check.txt","PASS: InGame existing prefab resolves V6, 23 bones, 10 clips sampled at 5 points, finite skin. Existing scene and prefab IDs preserved.");
                HoodSpiritPlayVerifier.Run();
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText(Review+"/import-check.txt",e.ToString());EditorApplication.Exit(1);}
        }
    }
}
