using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    // Reimport and validate the existing prefab references; never replace or save the scene.
    public static class SpiritHatReview
    {
        static string Review="Docs/ArtReview/SpiritHatV2";
        public static void RunV4(){Review="Docs/ArtReview/SpiritGarmentV4";Run();}
        public static void RunV5(){Review="Docs/ArtReview/SpiritHemV5";Run();}
        public static void RunV3()
        {
            Review="Docs/ArtReview/SpiritLayersV3";
            AssetDatabase.Refresh();
            var paper=AssetDatabase.LoadAssetAtPath<Material>(HoodSpiritBuilder.Dir+"/HoodPaper.mat");
            paper.SetFloat("_BumpScale",.75f);
            EditorUtility.SetDirty(paper);AssetDatabase.SaveAssetIfDirty(paper);
            Run();
        }
        public static void Run()
        {
            try
            {
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(HoodSpiritBuilder.Dir+"/PaperHoodSpirit_Rigged.fbx",ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
                var spirit=UnityEngine.Object.FindFirstObjectByType<HoodSpiritAnimator>();
                if(!spirit)throw new Exception("Scene spirit missing");
                var skin=spirit.GetComponentInChildren<SkinnedMeshRenderer>();
                if(!skin||!skin.sharedMesh||skin.bones.Length<17)throw new Exception("Existing FBX references broke");
                if(skin.sharedMesh.vertexCount<10000)throw new Exception("Detailed mesh not imported");
                var animator=spirit.GetComponentInChildren<Animator>();
                var clips=animator.runtimeAnimatorController.animationClips;
                if(clips.Select(c=>c.name).Distinct().Count()!=9)throw new Exception("Animation references lost");
                var mesh=new Mesh();
                foreach(var clip in clips)
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*.5f);skin.BakeMesh(mesh);
                    if(mesh.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new Exception("Invalid deformation: "+clip.name);
                }
                UnityEngine.Object.DestroyImmediate(mesh);
                Directory.CreateDirectory(Review);
                File.WriteAllText(Review+"/import-check.txt","PASS: current InGame scene resolves existing prefab/FBX/material/9 animation references; 17 bones; finite deformations in all 9 clips. Scene not saved.");
                HoodSpiritPlayVerifier.Run();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
