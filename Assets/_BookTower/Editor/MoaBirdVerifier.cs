using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Story;
using BookTower.UI;
using BookTower.Core;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class MoaBirdVerifier
    {
        const string Flag="MoaBirdVerify";
        public const string Out="Docs/ArtReview/MoaModelViewsV1/NaturalV2";
        static IEnumerator routine;static int frame;static float next;static double started;
        static readonly StringBuilder report=new();
        [InitializeOnLoadMethod]static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
        {if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/moa-test-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".json");}
        public static void RunBatch()
        {
            Directory.CreateDirectory(Out);report.Clear();CheckAssets();Studio();
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
        public static void PreviewBatch()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var bird=MoaBirdBuilder.Create(null);Object.DestroyImmediate(bird.gameObject);
            Directory.CreateDirectory(Out);report.Clear();CheckAssets();Studio();
            Debug.Log("[Moa] Natural sculpt preview saved; InGame scene was not changed.");EditorApplication.Exit(0);
        }
        static void Check(bool value,string description){if(!value)throw new Exception(description);report.AppendLine("PASS: "+description);}
        static void CheckAssets()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MoaBirdBuilder.PrefabPath);var visual=prefab.transform.Find("Visual");
            var c=prefab.GetComponent<PaperCompanion>();var driver=prefab.GetComponent<PaperMoaMotion>();
            Check(c&&c.head&&c.leftWing&&c.rightWing&&driver&&driver.animator,"Prefab has articulated body, head, wings and animation driver");
            Check(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Any(f=>f.name=="ContinuousPaperBody"),"Head, throat and breast share one continuous skinned surface");
            Check(c.leftWing.childCount==6&&c.rightWing.childCount==3,"Three sheet layers per wing and asymmetric left-wing flecks");
            Check(prefab.GetComponentsInChildren<Collider>().Length==0,"Companion cannot block player or intercept light rays");
            var spirit=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/PaperWrap.mat");
            Check(prefab.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial.GetTexture("_BaseMap")==spirit.GetTexture("_BaseMap")&&r.sharedMaterial.GetTexture("_BumpMap")==spirit.GetTexture("_BumpMap")),"Bird shares actual paper spirit fibre and normal textures");
            long tris=0;
            foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {Check(f.sharedMesh&&f.sharedMesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Valid saved mesh: "+f.name);tris+=f.sharedMesh.triangles.Length/3;}
            foreach(var f in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Check(f.sharedMesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Valid sculpted skin: "+f.name);
                Check(f.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1-1)<.0001f),"Normalized smooth skin weights: "+f.name);tris+=f.sharedMesh.triangles.Length/3;
            }
            report.AppendLine("Mesh triangles: "+tris);
            foreach(string name in new[]{"Idle","Run","WingRest","Flutter"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MoaBirdBuilder.Dir+"/Animation/"+name+".anim");
                Check(clip&&clip.frameRate==60&&clip.length>0,"Continuous 60 Hz authored clip: "+name);
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if(!visual.Find(binding.path))throw new Exception("Unbound animation: "+binding.path);
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);
                    if(name!="Flutter"&&Mathf.Abs(curve.Evaluate(0)-curve.Evaluate(clip.length))>.0001f)throw new Exception("Loop seam: "+name+" "+binding.path);
                }
            }
            File.WriteAllText(Out+"/asset-check.txt",report.ToString());
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){report.Clear();started=EditorApplication.timeSinceStartup;frame=-1;next=0;routine=Play();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+"Exit",1));}
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>140)throw new Exception("Moa verification timeout");
                if(frame==Time.frameCount||Time.unscaledTime<next)return;frame=Time.frameCount;
                if(!routine.MoveNext()){Finish(null);return;}next=Time.unscaledTime+(routine.Current is float f?f:0);
            }catch(Exception e){Finish(e);}
        }
        static void Finish(Exception error)
        {
            EditorApplication.update-=Tick;report.AppendLine(error==null?"PASS: Moa model, follow and animation integration":"FAIL: "+error);
            File.WriteAllText(Out+"/play-check.txt",report.ToString());Debug.Log(report.ToString());SessionState.SetInt(Flag+"Exit",error==null?0:1);EditorApplication.ExitPlaymode();
        }
        static IEnumerator Play()
        {
            yield return 1f;var story=Object.FindFirstObjectByType<OpeningStoryDirector>();var ui=PaperUI.Instance;
            var bird=story.companion;var driver=bird.GetComponent<PaperMoaMotion>();var player=story.player;var room=story.foyer;
            Check(Object.FindObjectsByType<PaperCompanion>(FindObjectsSortMode.None).Length==1,"Exactly one in-game companion");
            Check(driver&&driver.animator&&driver.animator.runtimeAnimatorController,"Scene uses new bird and animation controller after reload");
            float pausedClock=driver.AnimationTime;Vector3 pausedPosition=bird.transform.position;yield return .3f;
            Check(driver.AnimationTime==pausedClock&&bird.transform.position==pausedPosition,"2D opening keeps 3D bird frozen");
            int guard=0;while(ui.StoryActive&&guard++<30){ui.AdvanceStoryText();yield return .07f;}
            Check(!ui.StoryActive,"Story releases new companion normally");
            void Place(float x,float z)
            {player.Controller.enabled=false;player.transform.position=room.transform.TransformPoint(new Vector3(x,3.05f,z));player.Controller.enabled=true;player.VerticalVelocity=0;player.HorizontalVelocity=Vector3.zero;player.transform.rotation=Quaternion.identity;Physics.SyncTransforms();}
            Place(0,-12);yield return 1.5f;
            Check(bird.Grounded&&Mathf.Abs(bird.transform.position.y-3.04f)<.12f,"Bird feet settle on actual F0 floor");
            Check(driver.RunBlend<.08f,"Stationary player returns companion to idle");
            float time=Time.time;while(Time.time-time<2)
            {player.Controller.Move(Vector3.forward*3*Time.deltaTime+Vector3.down*2*Time.deltaTime);yield return 0f;}
            Check(driver.RunBlend>.35f&&bird.MovementSpeed>.8f,"Actual follow movement smoothly drives run blend");
            Check(Vector3.Distance(player.transform.position,bird.transform.position)<2,"Running bird stays alongside player");
            yield return 1.7f;Check(driver.RunBlend<.08f,"Stopping eases run back to idle");
            yield return 1.8f;Check(driver.FlutterCount>0,"Natural intermittent flutter is scheduled");
            driver.FlutterNow();yield return .65f;
            Check(Quaternion.Angle(bird.leftWing.localRotation,Quaternion.identity)>5,"Flutter visibly opens articulated wing");
            ui.Pause();yield return .05f;pausedClock=driver.AnimationTime;pausedPosition=bird.transform.position;var wing=bird.leftWing.localRotation;
            yield return .4f;Check(driver.AnimationTime==pausedClock&&bird.transform.position==pausedPosition&&bird.leftWing.localRotation==wing,"Pause freezes follow and wings without pose reset");
            ui.Resume();yield return .4f;Check(driver.AnimationTime>pausedClock,"Resume continues animation");
            var near=room.GetComponentsInChildren<FoldBridgeControl>().First(c=>c.name=="입구 제본 손잡이");Place(3.2f,-5);yield return .7f;
            Check(near.TryUse(player.transform.position),"Original bridge control remains usable with new bird");yield return 2f;
            Place(0,-3);yield return 1.2f;time=Time.time;
            while(room.transform.InverseTransformPoint(player.transform.position).z<8.5f)
            {
                if(Time.time-time>8)throw new Exception("Bridge walk timed out");
                player.Controller.Move(Vector3.forward*2.5f*Time.deltaTime+Vector3.down*2*Time.deltaTime);
                if(bird.transform.position.y<2.8f)throw new Exception("Bird fell below bridge");yield return 0f;
            }
            yield return 1.2f;Check(room.transform.InverseTransformPoint(bird.transform.position).z>7.2f,"Bird crosses narrow bridge onto far bank");
            Place(0,-10);yield return .6f;Check(Vector3.Distance(player.transform.position,bird.transform.position)<2,"Checkpoint-sized teleport safely reunites companion");
            story.storyCamera.SetAim(0,10);yield return .7f;F0PaperIdentityVerifier.Capture(Camera.main,Out+"/in-game-follow.png");
            Check(bird.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"Companion stays visible independently of room static batching");
        }
        static void Studio()
        {
            var previous=EditorSceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var enabled=lights.Select(l=>l.enabled).ToArray();
            foreach(var light in lights)light.enabled=false;
            var originalAmbient=RenderSettings.ambientProbe;var ambient=new UnityEngine.Rendering.SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.32f,.31f,.28f));RenderSettings.ambientProbe=ambient;
            try
            {
                var bird=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MoaBirdBuilder.PrefabPath),scene);
                foreach(var t in bird.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                bird.GetComponentInChildren<Light>().enabled=false;var visual=bird.transform.Find("Visual").gameObject;
                var animator=visual.GetComponent<Animator>();animator.enabled=false;
                // Explicit CPU baking makes each sampled preview frame current even when
                // several Camera.Render calls occur inside one editor update.
                var skins=bird.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=new Mesh[skins.Length];
                for(int i=0;i<skins.Length;i++)
                {
                    baked[i]=new Mesh();var proxy=new GameObject("Preview bake");proxy.layer=31;proxy.transform.SetParent(skins[i].transform,false);
                    proxy.AddComponent<MeshFilter>().sharedMesh=baked[i];proxy.AddComponent<MeshRenderer>().sharedMaterial=skins[i].sharedMaterial;skins[i].enabled=false;
                }
                void Light(string name,Vector3 angles,float power,Color color)
                {var l=new GameObject(name).AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(l.gameObject,scene);l.gameObject.layer=31;l.cullingMask=1<<31;l.type=LightType.Directional;l.intensity=power;l.color=color;l.transform.rotation=Quaternion.Euler(angles);if(name=="Key"){l.shadows=LightShadows.Soft;l.shadowBias=.01f;l.shadowNormalBias=.015f;}}
                Light("Key",new Vector3(38,145,0),1.1f,new Color(1,.96f,.90f));Light("Fill",new Vector3(10,-100,0),.85f,new Color(.95f,.96f,1));Light("Rim",new Vector3(30,0,0),.65f,new Color(1,.94f,.81f));
                var camera=new GameObject("Moa preview camera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);
                camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.gameObject.layer=31;camera.cullingMask=1<<31;
                camera.orthographic=true;camera.orthographicSize=.60f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.70f,.68f,.61f);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ground,scene);ground.layer=31;ground.transform.position=new Vector3(0,.018f,0);ground.transform.localScale=Vector3.one*20;
                var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.SetColor("_BaseColor",new Color(.64f,.61f,.55f));groundMaterial.SetFloat("_Smoothness",0);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                var target=new Vector3(0,.48f,0);
                void Shot(string path,int size){for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(baked[i]);Capture(camera,path,size);}
                void View(Vector3 direction,string name){camera.transform.position=target+direction.normalized*4;camera.transform.LookAt(target);Shot(Out+"/"+name+".png",1000);}
                View(new Vector3(-3,.7f,3),"model-three-quarter");View(Vector3.left,"model-left");View(Vector3.right,"model-right");View(Vector3.forward,"model-front");
                camera.transform.position=target+new Vector3(-3,.7f,3).normalized*4;camera.transform.LookAt(target);
                var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(MoaBirdBuilder.Dir+"/Animation/Idle.anim");
                var rest=AssetDatabase.LoadAssetAtPath<AnimationClip>(MoaBirdBuilder.Dir+"/Animation/WingRest.anim");
                foreach(string action in new[]{"Idle","Run","Flutter"})
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MoaBirdBuilder.Dir+"/Animation/"+action+".anim");
                    int count=action=="Idle"?63:action=="Run"?48:48;string directory=Out+"/Frames/"+action;Directory.CreateDirectory(directory);
                    for(int i=0;i<count;i++)
                    {
                        float t=i/24f;idle.SampleAnimation(visual,t%idle.length);rest.SampleAnimation(visual,t%rest.length);
                        clip.SampleAnimation(visual,action=="Flutter"?Mathf.Min(t,clip.length):t%clip.length);Shot(directory+"/"+i.ToString("000")+".png",512);
                    }
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=enabled[i];RenderSettings.ambientProbe=originalAmbient;}
        }
        static void Capture(Camera camera,string path,int size)
        {
            var rt=new RenderTexture(size,size,24);var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=rt;camera.aspect=1;camera.Render();RenderTexture.active=rt;var image=new Texture2D(size,size,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size,size),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);}
            finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
