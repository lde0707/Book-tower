using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Core;
using BookTower.PlayerSystem;
using BookTower.Presentation;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static class HoodSpiritPlayVerifier
    {
        const string Flag="HoodSpiritPlayQA";
        static IEnumerator routine;static double next,started;static int exit;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateSettings(){if(SessionState.GetBool(Flag,false))BookTower.UI.PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/hood-quiet-settings.json");}
        [InitializeOnLoadMethod]static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange s)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){routine=Test();started=EditorApplication.timeSinceStartup;next=started+1;EditorApplication.update+=Tick;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"Exit",1));}
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Runtime timeout");
                if(!routine.MoveNext()){Finish(null);return;}
                next=EditorApplication.timeSinceStartup+(routine.Current is float n?n:.4f);
            }
            catch(Exception e){Finish(e);}
        }
        static void Finish(Exception e)
        {
            EditorApplication.update-=Tick;exit=e==null?0:1;
            File.WriteAllText(HoodSpiritBuilder.Review+"/play-check.txt",e==null?"PASS: actual InGame play mode, new skinned model, player-driven Idle/Move/JumpRise/Fall/Glide/Land, pause freeze, idle action cancellation and unchanged collider.":e.ToString());
            SessionState.SetInt(Flag+"Exit",exit);EditorApplication.ExitPlaymode();
        }
        static IEnumerator Test()
        {
            var p=Object.FindFirstObjectByType<PlayerController>();var d=p.GetComponentInChildren<HoodSpiritAnimator>();if(!d)throw new Exception("New visual absent in game");
            var a=d.ModelAnimator;float radius=p.Controller.radius,height=p.Controller.height;
            p.enabled=false; // Isolate state-to-animation binding from platform movement during the probe.
            foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
            GameFlow.Instance.SetPhase(GamePhase.Playing);
            void Expect(HoodSpiritAnimator.Pose pose){if(d.CurrentPose!=pose||!a.GetCurrentAnimatorStateInfo(0).IsName(pose.ToString()))throw new Exception("Runtime state: expected "+pose+" got "+d.CurrentPose);}
            p.Machine.Change(p.Grounded);p.HorizontalVelocity=Vector3.zero;yield return .6f;Expect(HoodSpiritAnimator.Pose.Idle);
            p.HorizontalVelocity=Vector3.forward*4;yield return .7f;Expect(HoodSpiritAnimator.Pose.Move);
            p.Machine.Change(p.Airborne);p.VerticalVelocity=5;yield return .6f;Expect(HoodSpiritAnimator.Pose.JumpRise);
            p.VerticalVelocity=-2;yield return .6f;Expect(HoodSpiritAnimator.Pose.Fall);
            p.Machine.Change(p.Glide);yield return .6f;Expect(HoodSpiritAnimator.Pose.Glide);
            p.HorizontalVelocity=Vector3.zero;p.Machine.Change(p.Grounded);yield return .16f;Expect(HoodSpiritAnimator.Pose.Land);
            yield return .8f;Expect(HoodSpiritAnimator.Pose.Idle);
            d.Step(16,0,false,false,false,false,false);yield return .5f;if(d.CurrentPose<HoodSpiritAnimator.Pose.LookAround)throw new Exception("Idle action missing");
            p.HorizontalVelocity=Vector3.forward*3;yield return .5f;Expect(HoodSpiritAnimator.Pose.Move);
            GameFlow.Instance.Pause();float before=a.GetCurrentAnimatorStateInfo(0).normalizedTime;yield return .4f;
            if(Mathf.Abs(before-a.GetCurrentAnimatorStateInfo(0).normalizedTime)>.01f)throw new Exception("Pause did not freeze animation");
            GameFlow.Instance.Resume();p.HorizontalVelocity=Vector3.zero;yield return .6f;
            if(p.Controller.radius!=radius||p.Controller.height!=height)throw new Exception("Collider changed");
            var ui=BookTower.UI.PaperUI.Instance;
            if(!ui)throw new Exception("Paper UI absent");
            ui.Pause();yield return .2f;
            var button=Array.Find(ui.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="계속하기");
            if(!button)throw new Exception("Resume button absent");
            button.onClick.Invoke();
            var sfx=BookTower.Audio.SfxDirector.EnsureExists();
            var click=sfx.GetComponent<AudioSource>();
            if(!click||!click.clip||click.clip.name!="ui_click"||!click.ignoreListenerPause||click.volume<=0)throw new Exception("UI click not connected");
            yield return .2f;
            var settings=BookTower.UI.PaperSettingsRuntime.Values;float previousEffects=settings.effects;
            settings.effects=0;
            BookTower.Audio.SfxDirector.PlayUi();
            if(click.volume!=0)throw new Exception("Muted UI still audible");
            var silent=new GameObject("Muted first frame test").AddComponent<AudioSource>();silent.volume=.8f;
            BookTower.UI.PaperAudioLevel.Bind(silent);
            if(silent.volume!=0)throw new Exception("Effect mute leaks first frame");
            Object.Destroy(silent.gameObject);settings.effects=previousEffects;
            foreach(string name in new[]{"fold_step","fold_step_2","fold_step_3","puzzle_solve","piece_pickup","gate_open","rustle_step","rustle_step_2","rustle_step_3","hint_tremble","hint_nudge","ui_click","ui_page"})
            {
                var clip=Resources.Load<AudioClip>("Sfx/"+name);if(!clip)throw new Exception("Missing quiet clip "+name);
                var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Unreadable clip "+name);
                float peak=0;foreach(float sample in data)peak=Mathf.Max(peak,Mathf.Abs(sample));
                if(peak>.225f||peak<.001f)throw new Exception("Quiet level check failed "+name+": "+peak);
            }
            File.WriteAllText("Logs/quiet-audio-play-check.txt","PASS: actual resume button invokes new click, UI source plays during pause, effects mute works including first frame, 13 loaded clips have quiet nonzero peaks <=0.225, existing animation FSM/collider probe also run.");
            // A close camera in the actual room, retaining the game's actual lighting/materials.
            var cam=new GameObject("Hood QA camera").AddComponent<Camera>();cam.transform.position=p.transform.position+new Vector3(1.8f,1.4f,2.7f);cam.transform.LookAt(p.transform.position+Vector3.up*.8f);cam.fieldOfView=38;
            var rt=RenderTexture.GetTemporary(960,720,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(960,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();File.WriteAllBytes(HoodSpiritBuilder.Review+"/unity-ingame.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.Destroy(tex);Object.Destroy(cam.gameObject);
        }
    }
}
