using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using BookTower.Presentation;
using BookTower.PlayerSystem;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class HoodSpiritBuilder
    {
        public const string Dir="Assets/_BookTower/Art/PaperHoodSpirit";
        public const string Review="Docs/ArtReview/SpiritRedesignV1/AnimationV1";
        const string Request="Temp/HoodSpiritApply.request";
        static readonly string[] Names={"Idle","Move","Run","JumpRise","Fall","Glide","Land","LookAround","TidyBookmark","Stretch"};
        [InitializeOnLoadMethod] static void Initialize() { EditorApplication.update-=Poll;EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(Request);
            try { Build();Apply();Verify();File.WriteAllText("Logs/hood-unity-apply.txt","PASS: prefab, scene replacement, skin, 9 clips and FSM verification complete."); }
            catch(Exception e) { File.WriteAllText("Logs/hood-unity-apply.txt","FAIL: "+e);Debug.LogException(e); }
        }
        public static void Batch()
        {
            try { Build();EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();Verify();EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
        static string ShortName(string s)=>Names.FirstOrDefault(n=>s==n||s.EndsWith("|"+n)||s.EndsWith("_"+n))??s;
        public static void Build()
        {
            Directory.CreateDirectory(Review);AssetDatabase.Refresh();
            string path=Dir+"/PaperHoodSpirit_Rigged.fbx";
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation=true;imp.optimizeGameObjects=false;imp.isReadable=true;imp.animationCompression=ModelImporterAnimationCompression.Off;
            imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.SaveAndReimport();
            var configs=imp.defaultClipAnimations;
            foreach(var c in configs)
            {
                c.name=ShortName(c.name);c.loopTime=new[]{"Idle","Move","Run","Fall","Glide"}.Contains(c.name);
                c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;
                c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;
            }
            imp.clipAnimations=configs;imp.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            foreach(string n in Names)if(!clips.Any(c=>c.name==n))throw new Exception("Missing clip "+n+" found: "+string.Join(",",clips.Select(c=>c.name)));
            foreach(string t in new[]{"Spirit_BaseColor","Spirit_Normal","Spirit_MetallicSmoothness"})
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+"/"+t+".png");
                ti.textureType=t=="Spirit_Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=t=="Spirit_BaseColor";ti.maxTextureSize=2048;ti.SaveAndReimport();
            }
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/HoodPaper.mat");
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Dir+"/HoodPaper.mat");}
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Spirit_BaseColor.png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Spirit_Normal.png"));mat.SetFloat("_BumpScale",.4f);mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/Spirit_MetallicSmoothness.png"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);mat.SetFloat("_Cull",0);
            EditorUtility.SetDirty(mat);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Dir+"/HoodSpirit.controller");
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Dir+"/HoodSpirit.controller");
            var sm=controller.layers[0].stateMachine;
            foreach(var s in sm.states)sm.RemoveState(s.state);
            foreach(string n in Names){var state=sm.AddState(n);state.motion=clips.First(c=>c.name==n);if(n=="Idle")sm.defaultState=state;}
            EditorUtility.SetDirty(controller);
            var root=new GameObject("종이요정");var sway=new GameObject("Sway");sway.transform.SetParent(root.transform,false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(sway.transform,false);
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=mat;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up,Vector3.one*4);}
            var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=root.AddComponent<HoodSpiritAnimator>();var so=new SerializedObject(driver);so.FindProperty("animator").objectReferenceValue=animator;so.FindProperty("sway").objectReferenceValue=sway.transform;so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Dir+"/HoodSpirit.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        }
        [MenuItem("Book Tower/Apply rigged hood spirit")]
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",OpenSceneMode.Additive);
            Directory.CreateDirectory("Logs/HoodSpiritBackup");
            EditorSceneManager.SaveScene(scene,"Logs/HoodSpiritBackup/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var player=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PlayerController>(true)).First();
            var existing=player.GetComponentsInChildren<HoodSpiritAnimator>(true);
            var previous=existing.Length>0?existing[0].transform:player.transform.Find("종이요정");
            float height=1.15f;Vector3 localPos=Vector3.zero;
            if(previous)
            {
                localPos=previous.localPosition;
                // Repeat application preserves the existing new model's scale instead of measuring inflated skin bounds.
                if(previous.GetComponent<HoodSpiritAnimator>())height=1.724f*previous.localScale.y;
                else
                {
                    var rs=previous.GetComponentsInChildren<Renderer>();if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);height=b.size.y/player.transform.lossyScale.y;}
                }
                Object.DestroyImmediate(previous.gameObject);
            }
            foreach(var duplicate in existing)if(duplicate)Object.DestroyImmediate(duplicate.gameObject);
            var fresh=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/HoodSpirit.prefab"),scene);
            fresh.name="종이요정";
            fresh.transform.SetParent(player.transform,false);fresh.transform.localPosition=localPos;fresh.transform.localScale=Vector3.one*(height/1.724f);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Review+"/integration.txt","Player="+player.name+"\nVisual height="+height+"\nScale="+fresh.transform.localScale+"\nMovement and colliders unchanged.");
        }
        public static void Verify()
        {
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/HoodSpirit.prefab"),preview);
                var driver=go.GetComponent<HoodSpiritAnimator>();var a=go.GetComponentInChildren<Animator>();var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();
                if(skin.bones.Length<17||!skin.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.002f))throw new Exception("Skin weights invalid");
                a.Rebind();a.Update(0);Vector3 start=go.transform.position;
                void Probe(HoodSpiritAnimator.Pose expected,float speed=0,bool air=false,bool rising=false,bool glide=false,bool blocked=false,bool input=false)
                {
                    driver.Step(.02f,speed,air,rising,glide,blocked,input);for(int i=0;i<30;i++)a.Update(1f/60);
                    if(driver.CurrentPose!=expected||!a.GetCurrentAnimatorStateInfo(0).IsName(expected.ToString()))throw new Exception("FSM failed "+expected);
                    if(go.transform.position!=start)throw new Exception("Root motion leaked");
                }
                Probe(HoodSpiritAnimator.Pose.Idle,blocked:true);Probe(HoodSpiritAnimator.Pose.Move,4);
                Probe(HoodSpiritAnimator.Pose.JumpRise,3,true,true);Probe(HoodSpiritAnimator.Pose.Fall,2,true);
                Probe(HoodSpiritAnimator.Pose.Glide,2,true,false,true);Probe(HoodSpiritAnimator.Pose.Land);
                driver.Step(.5f,0,false,false,false,false,false);Probe(HoodSpiritAnimator.Pose.Idle);
                driver.Step(16,0,false,false,false,false,false);
                if(driver.CurrentPose<HoodSpiritAnimator.Pose.LookAround)throw new Exception("Idle behavior did not start");
                Probe(HoodSpiritAnimator.Pose.Move,3);Probe(HoodSpiritAnimator.Pose.Idle,blocked:true);
                // Evaluate all skin poses to catch missing transforms / nonfinite deformation.
                var baked=new Mesh();
                foreach(string n in Names){a.Play(n,0,.5f);a.Update(0);skin.BakeMesh(baked);if(!baked.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)))throw new Exception("Invalid pose "+n);}
                Object.DestroyImmediate(baked);
                var camera=new GameObject("Review camera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,preview);
                camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.28f,.25f,.22f);camera.orthographic=true;camera.orthographicSize=1.15f;
                camera.transform.position=new Vector3(2.5f,1.7f,4);camera.transform.LookAt(Vector3.up*.98f);
                var light=new GameObject("Soft key").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,preview);light.type=LightType.Directional;light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(30,-35,0);
                var fill=new GameObject("Soft fill").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill.gameObject,preview);fill.type=LightType.Directional;fill.intensity=.85f;fill.color=new Color(.86f,.91f,1);fill.transform.rotation=Quaternion.Euler(15,155,0);
                foreach(string n in new[]{"Idle","Move","TidyBookmark","Stretch"})
                {
                    a.Play(n,0,.5f);a.Update(0);var rt=RenderTexture.GetTemporary(720,900,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
                    var tex=new Texture2D(720,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,900),0,0);tex.Apply();File.WriteAllBytes(Review+"/unity-"+n+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
                }
                File.WriteAllText(Review+"/unity-check.txt","PASS: 17 bones, normalized weights, 9 clips, idle behavior activation, movement cancellation, jump/fall/glide/landing FSM, finite skin deformation, no root motion.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
