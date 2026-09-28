using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using BookTower.Story;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static class MoaSculptBuilder
    {
        public const string Dir="Assets/_BookTower/Art/MoaSculpt",Out="Docs/ArtReview/MoaModelViewsV1/UnityV3";
        static readonly string[] Clips={"Hover","Fly","Flutter"};
        [InitializeOnLoadMethod]static void Init(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/MoaSculptApply.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);try{Build();HoodSpiritBuilder.Apply();Apply();File.WriteAllText("Logs/moa-sculpt-apply.txt","PASS: approved sculpt companion applied.");}catch(Exception e){File.WriteAllText("Logs/moa-sculpt-apply.txt",e.ToString());Debug.LogException(e);}
        }
        public static void Batch()
        {try{Build();EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");HoodSpiritBuilder.Apply();Apply();Verify();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void Build()
        {
            Directory.CreateDirectory(Out);AssetDatabase.Refresh();string path=Dir+"/MoaSculpt_Rigged.fbx";
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.importAnimation=true;imp.isReadable=true;imp.optimizeGameObjects=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.SaveAndReimport();
            var configs=imp.defaultClipAnimations;foreach(var c in configs){c.name=Clips.FirstOrDefault(n=>c.name.EndsWith("|"+n)||c.name==n)??c.name;c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}imp.clipAnimations=configs;imp.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>Clips.Contains(c.name)).ToArray();if(clips.Length!=3)throw new Exception("Flight clips missing");
            foreach(string t in new[]{"BaseColor","Normal","MetallicSmoothness"}){var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+"/Moa_"+t+".png");ti.textureType=t=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=t=="BaseColor";ti.SaveAndReimport();}
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/MoaPaper.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Dir+"/MoaPaper.mat");}
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Moa_BaseColor.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Moa_Normal.png"));mat.SetFloat("_BumpScale",.45f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Moa_MetallicSmoothness.png"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);EditorUtility.SetDirty(mat);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Dir+"/MoaFlight.controller")??AnimatorController.CreateAnimatorControllerAtPath(Dir+"/MoaFlight.controller");var sm=controller.layers[0].stateMachine;foreach(var c in sm.states)sm.RemoveState(c.state);
            foreach(string n in Clips){var state=sm.AddState(n);state.motion=clips.First(c=>c.name==n);if(n=="Hover")sm.defaultState=state;}EditorUtility.SetDirty(controller);
            var root=new GameObject("모아_종이새_SculptV3");var companion=root.AddComponent<PaperCompanion>();companion.airborneFollow=true;
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);model.transform.localScale=Vector3.one*.63f;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=mat;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up*.5f,Vector3.one*3);}
            var a=model.GetComponent<Animator>();a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var motion=root.AddComponent<MoaSculptMotion>();motion.companion=companion;motion.animator=a;
            PrefabUtility.SaveAsPrefabAsset(root,Dir+"/MoaSculpt.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",OpenSceneMode.Additive);
            var director=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OpeningStoryDirector>(true)).First();var old=director.companion;if(!old)throw new Exception("Story companion missing");
            Directory.CreateDirectory("Logs/MoaSculptBackup");EditorSceneManager.SaveScene(scene,"Logs/MoaSculptBackup/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/MoaSculpt.prefab"),scene);go.transform.SetParent(old.transform.parent,false);go.transform.position=director.player.transform.TransformPoint(new Vector3(.70f,.55f,-.40f));go.transform.rotation=old.transform.rotation;
            var bird=go.GetComponent<PaperCompanion>();bird.player=director.player;director.companion=bird;Object.DestroyImmediate(old.gameObject);EditorUtility.SetDirty(director);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void Verify()
        {
            var director=Object.FindFirstObjectByType<OpeningStoryDirector>();var bird=director.companion;
            if(!bird.GetComponent<MoaSculptMotion>()||!bird.airborneFollow||bird.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Companion wiring invalid");
            var skin=bird.GetComponentInChildren<SkinnedMeshRenderer>();if(!skin||skin.bones.Length!=7)throw new Exception("Bird skin missing");
            var a=bird.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);var mesh=new Mesh();
            foreach(string n in Clips){a.Play(n,0,.3f);a.Update(0);skin.BakeMesh(mesh);if(!mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)))throw new Exception("Bad skin "+n);}
            Object.DestroyImmediate(mesh);
            if(OpeningStoryData.Beats(OpeningStoryData.Promise).Length!=16)throw new Exception("Opening dialogue not refreshed");
            File.WriteAllText(Out+"/unity-check.txt","PASS: scene references approved V3 sculpt, 7 skin bones, 3 flight clips, airborne follower, no solid collider, 16 opening lines.");
        }
        public static void CaptureBatch()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Verify();
                var d=Object.FindFirstObjectByType<OpeningStoryDirector>();var p=d.player;var bird=d.companion;
                var a=bird.GetComponentInChildren<Animator>();a.Play("Hover",0,.18f);a.Update(0);
                var cam=new GameObject("Moa model review").AddComponent<Camera>();cam.transform.position=bird.transform.position+new Vector3(.9f,.6f,1.5f);cam.transform.LookAt(bird.transform.position+Vector3.up*.30f);cam.fieldOfView=35;
                var rt=RenderTexture.GetTemporary(960,720,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                var tex=new Texture2D(960,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(Out+"/unity-moa.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
