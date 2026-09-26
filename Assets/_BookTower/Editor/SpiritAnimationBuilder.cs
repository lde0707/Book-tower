using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    public static class SpiritAnimationBuilder
    {
        public const string Dir = "Assets/_BookTower/Art/PaperSpirit/Animation";
        // One-shot editor request; preserve the user's loaded and unsaved scenes.
        [InitializeOnLoadMethod]
        static void QueueRefresh() => EditorApplication.delayCall += ApplyPendingRefresh;

        // Batch entry point also works with an empty startup scene.
        public static void RebuildAndVerify()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/SpiritAnimationRefresh.request","verify");
            ApplyPendingRefresh();
            string result=File.ReadAllText("Logs/spirit-animation-smooth-result.txt");
            if(!result.StartsWith("PASS:")) throw new Exception(result);
        }

        static void ApplyPendingRefresh()
        {
            const string request="Temp/SpiritAnimationRefresh.request";
            if(!File.Exists(request)) return;
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            { EditorApplication.delayCall += ApplyPendingRefresh; return; }
            File.Delete(request);
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                Controller(); AssetDatabase.SaveAssets();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BookTower/Art/PaperSpirit/PaperSpirit.prefab");
                var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
                foreach(var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer=31;
                var light=new GameObject("AnimationPreviewLight").AddComponent<Light>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,preview);
                light.gameObject.layer=31; light.cullingMask=1<<31; light.type=LightType.Directional;
                light.intensity=1.2f; light.transform.rotation=Quaternion.Euler(40,-35,0);
                var camera=new GameObject("AnimationPreviewCamera").AddComponent<Camera>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,preview);
                camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);
                camera.gameObject.layer=31; camera.cullingMask=1<<31;
                camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.9f,.87f,.8f);
                Review(model,camera);
                File.WriteAllText("Logs/spirit-animation-smooth-result.txt","PASS: 72/48/44 frames; continuous curves; loop seams; Animator transitions; root unchanged.\n");
            }
            catch(Exception e)
            { File.WriteAllText("Logs/spirit-animation-smooth-result.txt",e.ToString()); Debug.LogException(e); }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            }
        }
        public static void Review(GameObject model, Camera camera)
        {
            var animator=model.GetComponent<Animator>();
            animator.Rebind(); animator.Update(0);
            // Record the former two-tick probe: the first tick starts blending;
            // the second advances it only 0.12s, short of the new 0.14s blend.
            animator.SetBool("Moving",true);
            animator.Update(.12f); animator.Update(.12f);
            Debug.Log($"[SpiritAnimation] Former probe: currentIdle={animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")}, nextMove={animator.GetNextAnimatorStateInfo(0).IsName("Move")}, blending={animator.IsInTransition(0)}, progress={animator.GetAnimatorTransitionInfo(0).normalizedTime:F3}");
            animator.Rebind(); animator.Update(0);
            void CheckState(string expected)
            {
                // Simulate normal game frames, allowing the documented 0.14s blend
                // to finish, with a bounded margin rather than an unbounded wait.
                const int ticks=18;
                for(int i=0;i<ticks;i++) animator.Update(1f/60f);
                if(animator.IsInTransition(0) || !animator.GetCurrentAnimatorStateInfo(0).IsName(expected))
                    throw new Exception($"Animation transition failed after 0.30s: {expected}; current={animator.GetCurrentAnimatorStateInfo(0).shortNameHash}; next={animator.GetNextAnimatorStateInfo(0).shortNameHash}");
            }
            CheckState("Idle"); animator.SetBool("Moving",true); CheckState("Move");
            animator.SetBool("Jumping",true); CheckState("Jump");
            animator.SetBool("Jumping",false); CheckState("Move");
            animator.SetBool("Moving",false); CheckState("Idle");
            animator.SetBool("Jumping",true); CheckState("Jump");
            animator.SetBool("Jumping",false); CheckState("Idle");
            animator.enabled=false;
            camera.transform.position=new Vector3(2,1.5f,4);
            camera.transform.LookAt(new Vector3(0,.95f,0)); camera.orthographicSize=1.02f;
            Directory.CreateDirectory("Docs/ArtReview/AnimationSmooth");
            Vector3 rest=model.transform.position;
            foreach(string name in new[]{"Idle","Move","Jump"})
            {
                int count=name=="Idle"?72:name=="Move"?48:44;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"/"+name+".anim");
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if(!binding.path.StartsWith("Motion")) throw new Exception("Animation modifies gameplay root");
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);
                    if(curve.length!=count+1) throw new Exception("Incorrect frame count");
                    foreach(var key in curve.keys)
                        if(float.IsInfinity(key.inTangent)||float.IsInfinity(key.outTangent)||float.IsNaN(key.value)) throw new Exception("Non-smooth animation curve");
                    if(name!="Jump")
                    {
                        var first=curve.keys[0]; var last=curve.keys[curve.length-1];
                        if(Mathf.Abs(first.value-last.value)>.0001f || Mathf.Abs(first.outTangent-last.inTangent)>.02f)
                            throw new Exception("Loop seam mismatch: "+binding.propertyName);
                    }
                }
                int columns=6;
                int rows=Mathf.CeilToInt(count/(float)columns);
                var sheet=new Texture2D(columns*300,rows*360,TextureFormat.RGB24,false);
                for(int i=0;i<count;i++)
                {
                    clip.SampleAnimation(model,(i+.01f)/clip.frameRate);
                    if(model.transform.position!=rest) throw new Exception("Animation moved player root");
                    var rt=new RenderTexture(300,360,24); camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                    var frame=new Texture2D(300,360,TextureFormat.RGB24,false);
                    frame.ReadPixels(new Rect(0,0,300,360),0,0); frame.Apply();
                    File.WriteAllBytes($"Docs/ArtReview/AnimationSmooth/{name}-{i+1:00}.png",frame.EncodeToPNG());
                    sheet.SetPixels((i%columns)*300,(rows-1-i/columns)*360,300,360,frame.GetPixels());
                    camera.targetTexture=null; RenderTexture.active=null;
                    UnityEngine.Object.DestroyImmediate(frame); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                }
                sheet.Apply(); File.WriteAllBytes($"Docs/ArtReview/AnimationSmooth/{name}-sheet.png",sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            Debug.Log("[SpiritAnimation] PASS: Idle/Move/Jump transitions, 72/48/44 samples, smooth interpolation, loop seams and root immobility; 164 frames rendered.");
        }
        public static void Attach(GameObject root)
        {
            Directory.CreateDirectory(Dir);
            var motion = new GameObject("Motion").transform;
            motion.SetParent(root.transform,false); motion.localPosition = Vector3.up*.75f;
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in root.transform) if(child != motion) children.Add(child);
            foreach (var child in children) { child.SetParent(motion,false); child.localPosition -= Vector3.up*.75f; }
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller(); animator.applyRootMotion=false;
            root.AddComponent<PaperSpiritAnimator>();
        }
        static AnimationClip Clip(string name, int frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Dir+"/"+name+".anim");
            bool fresh=clip==null; if(fresh) clip=new AnimationClip { name=name };
            clip.ClearCurves(); clip.frameRate=fps;
            // Dense 60 Hz samples of a continuous curve; matching loop derivatives
            // avoid the hitch produced by holding discrete poses or a mismatched seam.
            void Curve(string path,string prop,Func<float,float> value)
            {
                var keys=new Keyframe[frames+1];
                float duration=frames/fps, epsilon=.0001f;
                for(int i=0;i<=frames;i++)
                {
                    float t=i/(float)frames;
                    float slope=(!loop && (i==0 || i==frames)) ? 0f :
                        (value(t+epsilon)-value(t-epsilon))/(2*epsilon*duration);
                    keys[i]=new Keyframe(i/fps,value(t),slope,slope);
                }
                var curve=new AnimationCurve(keys);
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),prop),curve);
            }
            float[] bob, pitch, scale, roll;
            if(name=="Idle")
            {
                bob=new[]{0f,.025f,.04f,.018f,-.018f,-.03f};
                pitch=new[]{0f,-1f,-2f,0f,1f,1f};
                scale=new[]{1f,1.018f,1.025f,1f,.982f,.988f};
                roll=new[]{0f,1f,1.5f,0f,-1f,-1.5f};
            }
            else if(name=="Move")
            {
                bob=new[]{0f,.024f,.04f,.024f,0f,-.02f,-.03f,-.015f};
                pitch=new[]{9f,12f,15f,13f,10f,8f,7f,8f};
                scale=new[]{1f,1.025f,1.045f,1.025f,1f,.98f,.97f,.985f};
                roll=new[]{-2f,0f,2f,3f,2f,0f,-2f,-3f};
            }
            else
            {
                bob=new[]{-.035f,0f,.01f,0f,-.01f,0f};
                pitch=new[]{-5f,-9f,-3f,5f,8f,0f};
                scale=new[]{.90f,1.12f,1.05f,1f,.94f,1f};
                roll=new[]{0f,-2f,-1f,1f,0f,0f};
            }
            Curve("Motion","m_LocalPosition.x",i=>0);
            Curve("Motion","m_LocalPosition.y",i=>.91f+Smooth(bob,i,loop));
            Curve("Motion","m_LocalPosition.z",i=>0);
            Curve("Motion","localEulerAnglesRaw.x",i=>Smooth(pitch,i,loop));
            Curve("Motion","localEulerAnglesRaw.y",i=>0);
            Curve("Motion","localEulerAnglesRaw.z",i=>Smooth(roll,i,loop));
            Curve("Motion","m_LocalScale.y",i=>Smooth(scale,i,loop));
            Curve("Motion","m_LocalScale.x",i=>1/Mathf.Sqrt(Smooth(scale,i,loop)));
            Curve("Motion","m_LocalScale.z",i=>1/Mathf.Sqrt(Smooth(scale,i,loop)));
            foreach(int side in new[]{-1,1})
            {
                string path="Motion/RibbonTail"+side;
                Curve(path,"localEulerAnglesRaw.x",i=>(name=="Move"?-22f:name=="Jump"?-12f:0f)+Mathf.Sin(i*2*Mathf.PI+side)*9f);
                Curve(path,"localEulerAnglesRaw.y",i=>Mathf.Sin(i*2*Mathf.PI+side)*12f);
                Curve(path,"localEulerAnglesRaw.z",i=>side*Mathf.Cos(i*2*Mathf.PI)*8f);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            if(fresh) AssetDatabase.CreateAsset(clip,Dir+"/"+name+".anim"); else EditorUtility.SetDirty(clip);
            return clip;
        }
        // Cubic interpolation through the original acting poses, periodic for loops.
        static float Smooth(float[] poses,float t,bool loop)
        {
            float x=(loop?Mathf.Repeat(t,1):Mathf.Clamp01(t))*(loop?poses.Length:poses.Length-1);
            int i=Mathf.FloorToInt(x); float u=x-i;
            float At(int k) => poses[loop?((k%poses.Length)+poses.Length)%poses.Length:Mathf.Clamp(k,0,poses.Length-1)];
            float p=At(i),q=At(i+1);
            float m0=(!loop && i==0)?0:(q-At(i-1))*.5f;
            float m1=(!loop && i>=poses.Length-2)?0:(At(i+2)-p)*.5f;
            return (2*u*u*u-3*u*u+1)*p+(u*u*u-2*u*u+u)*m0+(-2*u*u*u+3*u*u)*q+(u*u*u-u*u)*m1;
        }
        static AnimatorController Controller()
        {
            var idle=Clip("Idle",72,60,true); var move=Clip("Move",48,60,true); var jump=Clip("Jump",44,60,false);
            string path=Dir+"/PaperSpirit.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm=controller.layers[0].stateMachine;
            foreach(var state in sm.states) sm.RemoveState(state.state);
            foreach(var transition in sm.anyStateTransitions) sm.RemoveAnyStateTransition(transition);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jumping",AnimatorControllerParameterType.Bool);
            var a=sm.AddState("Idle"); a.motion=idle;
            var b=sm.AddState("Move"); b.motion=move;
            var c=sm.AddState("Jump"); c.motion=jump;
            sm.defaultState=a;
            void Transition(AnimatorState from,AnimatorState to,bool moving)
            {
                var t=from.AddTransition(to); t.hasExitTime=false; t.duration=.14f; t.hasFixedDuration=true;
                t.AddCondition(moving?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Moving");
                t.AddCondition(AnimatorConditionMode.IfNot,0,"Jumping");
            }
            Transition(a,b,true); Transition(b,a,false); Transition(c,a,false); Transition(c,b,true);
            var takeoff=sm.AddAnyStateTransition(c); takeoff.hasExitTime=false; takeoff.duration=.05f; takeoff.hasFixedDuration=true;
            takeoff.canTransitionToSelf=false; takeoff.AddCondition(AnimatorConditionMode.If,0,"Jumping");
            EditorUtility.SetDirty(controller); return controller;
        }
    }
}
