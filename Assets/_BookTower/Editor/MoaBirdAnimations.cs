using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class MoaBirdBuilder
    {
        static AnimatorController Controller()
        {
            var idle=Locomotion("Idle",2.6f,false);var run=Locomotion("Run",.48f,true);
            var rest=Wings("WingRest",2.6f,false);var flutter=Wings("Flutter",1.4f,true);
            string path=Dir+"/Animation/PaperMoa.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.layers=Array.Empty<AnimatorControllerLayer>();
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset&&asset!=controller)Object.DestroyImmediate(asset,true);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Flutter",AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine Layer(string name)
            {
                var sm=new AnimatorStateMachine{name=name};AssetDatabase.AddObjectToAsset(sm,controller);
                controller.AddLayer(new AnimatorControllerLayer{name=name,stateMachine=sm,defaultWeight=1,blendingMode=AnimatorLayerBlendingMode.Override});return sm;
            }
            var main=Layer("Locomotion");var state=main.AddState("IdleRun");main.defaultState=state;state.writeDefaultValues=false;
            var blend=new BlendTree{name="Smooth Idle Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            blend.AddChild(idle,0);blend.AddChild(run,1);AssetDatabase.AddObjectToAsset(blend,controller);state.motion=blend;
            var wings=Layer("Paper Wings");var calm=wings.AddState("Rest");calm.motion=rest;calm.writeDefaultValues=false;
            var flap=wings.AddState("Flutter");flap.motion=flutter;flap.writeDefaultValues=false;wings.defaultState=calm;
            var start=calm.AddTransition(flap);start.hasExitTime=false;start.hasFixedDuration=true;start.duration=.12f;start.AddCondition(AnimatorConditionMode.If,0,"Flutter");
            var end=flap.AddTransition(calm);end.hasExitTime=true;end.exitTime=.94f;end.hasFixedDuration=true;end.duration=.18f;
            EditorUtility.SetDirty(controller);return controller;
        }
        static AnimationClip NewClip(string name,float seconds,bool loop)
        {
            string path=Dir+"/Animation/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(!clip){clip=new AnimationClip{name=name};AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
            clip.frameRate=60;clip.legacy=false;
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=false;settings.startTime=0;settings.stopTime=seconds;AnimationUtility.SetAnimationClipSettings(clip,settings);return clip;
        }
        static void Curve(AnimationClip clip,string path,string property,float seconds,Func<float,float> at)
        {
            int frames=Mathf.RoundToInt(seconds*60);var keys=new Keyframe[frames+1];
            for(int i=0;i<=frames;i++)
            {
                float t=i/(float)frames,h=.0001f;float tangent=(at(t+h)-at(t-h))/(2*h*seconds);
                keys[i]=new Keyframe(t*seconds,at(t),tangent,tangent);
            }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),new AnimationCurve(keys));
        }
        static AnimationClip Locomotion(string name,float duration,bool running)
        {
            var clip=NewClip(name,duration,true);float Tau=2*Mathf.PI;
            float Bob(float t)=>running?.017f+.019f*Mathf.Cos(t*Tau*2):.006f*(1-Mathf.Cos(t*Tau));
            void C(string path,string prop,Func<float,float> at)=>Curve(clip,path,prop,duration,at);
            C("Rig","m_LocalPosition.y",Bob);
            C("Rig/Body","localEulerAnglesRaw.x",t=>running?9+3*Mathf.Sin(t*Tau*2):1.5f*Mathf.Sin(t*Tau));
            C("Rig/Body","localEulerAnglesRaw.z",t=>Mathf.Sin(t*Tau)*(running?3.5f:.7f));
            C("Rig/Body","m_LocalScale.y",t=>1+Mathf.Sin(t*Tau)*(running?.02f:.012f));
            C("Rig/Head","m_LocalPosition.y",t=>.655f+Mathf.Sin(t*Tau*(running?2:1))*(running?.007f:.004f));
            C("Rig/Head","localEulerAnglesRaw.x",t=>Mathf.Sin(t*Tau)*(running?3:2));
            C("Rig/Head","localEulerAnglesRaw.y",t=>Mathf.Sin(t*Tau)*(running?2:6));
            C("Rig/Head","localEulerAnglesRaw.z",t=>Mathf.Sin(t*Tau+.4f)*(running?1:1.8f));
            C("Rig/Tail","localEulerAnglesRaw.x",t=>Mathf.Sin(t*Tau+.7f)*(running?7:2.5f));
            C("Rig/Tail","localEulerAnglesRaw.y",t=>Mathf.Sin(t*Tau)*(running?5:2));
            foreach(int side in new[]{-1,1})
            {
                string foot=side<0?"Rig/FootL":"Rig/FootR";
                float Phase(float t)=>t*Tau+(side<0?0:Mathf.PI);
                // Smooth raised swing and planted return; root motion remains owned by follower.
                C(foot,"m_LocalPosition.y",t=>.155f-Bob(t)+(running?.05f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Phase(t))),2):0));
                C(foot,"m_LocalPosition.z",t=>.01f+(running?.054f*Mathf.Cos(Phase(t)):0));
                C(foot,"localEulerAnglesRaw.x",t=>running?19*Mathf.Cos(Phase(t)):0);
            }
            EditorUtility.SetDirty(clip);return clip;
        }
        static AnimationClip Wings(string name,float duration,bool flutter)
        {
            var clip=NewClip(name,duration,!flutter);float Tau=2*Mathf.PI;
            float Envelope(float t)=>t<=0||t>=1?0:Mathf.Pow(Mathf.Sin(t*Mathf.PI),2);
            foreach(int side in new[]{-1,1})
            {
                string path=side<0?"Rig/WingL":"Rig/WingR";
                Curve(clip,path,"localEulerAnglesRaw.z",duration,t=>side*(flutter?Envelope(t)*(31+29*Mathf.Sin(t*Tau*3)):1.2f*Mathf.Sin(t*Tau)));
                Curve(clip,path,"localEulerAnglesRaw.y",duration,t=>side*(flutter?Envelope(t)*7*Mathf.Sin(t*Tau*3+.4f):.6f*Mathf.Sin(t*Tau)));
                for(int layer=0;layer<3;layer++)
                {
                    int n=layer;string leaf=path+"/Wing"+(side<0?"L":"R")+"Sheet"+layer;
                    Curve(clip,leaf,"localEulerAnglesRaw.x",duration,t=>flutter?Envelope(t)*(2+n)*Mathf.Sin(t*Tau*3-n*.24f):.3f*Mathf.Sin(t*Tau-n*.24f));
                }
            }
            EditorUtility.SetDirty(clip);return clip;
        }
    }
}
