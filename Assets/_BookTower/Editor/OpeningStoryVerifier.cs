using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Story;
using BookTower.UI;
using BookTower.Core;
using BookTower.Rooms;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class OpeningStoryVerifier
    {
        const string Flag="OpeningStoryVerify", Out="Docs/ArtReview/Cutscene2D/Play";
        static IEnumerator routine; static float next; static int frame; static double started;
        static readonly StringBuilder report=new();
        static Keyboard keyboard;static Mouse mouse;static InputSettings original;
        static OpeningStoryDirector story;static PaperUI ui;
        [InitializeOnLoadMethod] static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateStorage()
        {
            if(!SessionState.GetBool(Flag,false))return;
            PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/story-test-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".json");
        }
        public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void BuildAndRunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");OpeningStoryBuilder.Apply();RunBatch();}
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {Directory.CreateDirectory(Out);report.Clear();frame=-1;next=0;started=EditorApplication.timeSinceStartup;routine=Verify();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool(Flag,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Flag+"Code",1));}
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>240)throw new Exception("Story test timeout");
                if(Time.frameCount==frame||Time.unscaledTime<next)return;frame=Time.frameCount;
                if(!routine.MoveNext()){Finish(null);return;}next=Time.unscaledTime+(routine.Current is float f?f:0);
            }catch(Exception e){Finish(e);}
        }
        static void Finish(Exception e)
        {
            EditorApplication.update-=Tick;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(original)InputSystem.settings=original;
            report.AppendLine(e==null?"PASS: F0–1A illustrated cutscene integration":"FAIL: "+e);File.WriteAllText(Out+"/play-check.txt",report.ToString());Debug.Log(report.ToString());
            SessionState.SetInt(Flag+"Code",e==null?0:1);EditorApplication.ExitPlaymode();
        }
        static void Check(bool valid,string label){if(!valid)throw new Exception(label);report.AppendLine("PASS: "+label);}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static void Pointer(ushort buttons=0,Vector2? position=null)=>InputSystem.QueueStateEvent(mouse,new MouseState{buttons=buttons,position=position??new Vector2(30,30)});
        static IEnumerator Click(Vector2? position=null)
        {Pointer(1,position);yield return .05f;Pointer(0,position);yield return .07f;}
        static void Capture(string name)
        {
            var cam=Camera.main;
            if(name=="01-first-meeting") {var ray=new Ray(cam.transform.position,(story.player.transform.position+Vector3.up*.8f-cam.transform.position).normalized);File.WriteAllText(Out+"/camera-audit.txt","Player "+story.player.transform.position+" Camera "+cam.transform.position+"\n"+string.Join("\n",Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.bounds.IntersectRay(ray,out float d)&&d<5).Select(r=>r.name+" @ "+r.bounds)));}
            var canvas=ui.GetComponentInChildren<Canvas>();var rt=new RenderTexture(1600,900,24);var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect,near=cam.nearClipPlane;var data=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();bool post=data.renderPostProcessing;var aa=data.antialiasing;float scale=canvas.scaleFactor;
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;cam.nearClipPlane=.01f;canvas.planeDistance=.02f;cam.targetTexture=rt;cam.aspect=1600f/900;canvas.scaleFactor=1600f/1920;data.renderPostProcessing=false;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;foreach(var label in canvas.GetComponentsInChildren<Text>())label.SetAllDirty();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}
            finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.scaleFactor=scale;data.renderPostProcessing=post;data.antialiasing=aa;cam.targetTexture=old;cam.aspect=aspect;cam.nearClipPlane=near;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Canvas.ForceUpdateCanvases();}
        }
        static IEnumerator Wait(Func<bool> condition,float seconds=12)
        {float end=Time.unscaledTime+seconds;while(!condition()){if(Time.unscaledTime>end)throw new Exception("Wait failed: "+story.ActiveSequence+" beat "+story.BeatIndex);yield return 0f;}}
        static void Teleport(Vector3 p){var actor=story.player;actor.Controller.enabled=false;actor.transform.position=p;actor.Controller.enabled=true;actor.VerticalVelocity=0;actor.HorizontalVelocity=Vector3.zero;Physics.SyncTransforms();}
        static IEnumerator Tap()
        {
            Keys((Key)PaperSettingsRuntime.Values.keys[5]);yield return .05f;
            Keys();yield return .07f;
        }
        static IEnumerator ReadRest()
        {
            string id=story.ActiveSequence;int guard=0;
            while(story.ActiveSequence==id&&guard++<30)
            {
                if(id==OpeningStoryData.Promise&&story.BeatIndex==2)
                {foreach(var w in Each(Wait(()=>story.BeatIndex!=2)))yield return w;continue;}
                if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;
                foreach(var w in Each(Tap()))yield return w;
            }
            Check(story.ActiveSequence!=id,"Last dialogue immediately releases "+id);
        }
        static IEnumerator Verify()
        {
            yield return 1f;story=Object.FindFirstObjectByType<OpeningStoryDirector>();ui=PaperUI.Instance;
            Check(PaperSettingsRuntime.TestStoragePath!=null,"Test storage isolated from player records/settings");
            original=InputSystem.settings;var settings=Object.Instantiate(original);settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();Pointer();
            Check(story.ActiveSequence==OpeningStoryData.Promise,"Play automatically starts illustrated first meeting");
            Check(ui.CutsceneView&&ui.CutsceneView.IsVisible&&ui.cutsceneArt.IsReady,"All five 2D sprites are present and cutscene covers gameplay");
            if(ui.cutsceneArt.HasFirstPerson)
                Check(ui.CutsceneView.IsFirstPerson&&ui.CutsceneView.FrameIndex==0&&ui.CutsceneView.CurrentFrame==ui.cutsceneArt.firstPersonAwakening[0],"First-person entry starts on dark bird frame");
            Check(!Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.name=="M01_돌아올자리"||t.name=="M02_이름"),"Old 3D memory tableaux removed from scene");
            Check(ui.ScreenState==PaperScreen.Story&&GameFlow.Instance.Phase==GamePhase.Cutscene&&Time.timeScale==1,"Story owns input while illustrations animate");
            Check(!story.NameKnown,"Bird name is hidden before 1A");
            Check(ui.PortraitIsSilhouette&&!ui.BirdRevealed&&ui.CutsceneView.Continuous.BirdVisibility==0&&!ui.CutsceneView.Continuous.HandsVisible,"Before receiving light only dialogue bird silhouette is shown; no bird or hands on stage");
            var pos=story.player.transform.position;var clearSpawn=pos;var cameraPos=Camera.main.transform.position;var cameraRot=Camera.main.transform.rotation;var worldBird=story.companion.transform.position;var scale=story.player.transform.Find("종이요정").localScale;
            Vector2 spriteBefore=ui.CutsceneView.SpiritPosition;
            Keys(Key.W);yield return .4f;Keys();yield return .1f;
            Check(Vector3.Distance(pos,story.player.transform.position)<.001f,"Movement blocked during story");
            Check(Vector2.Distance(spriteBefore,ui.CutsceneView.SpiritPosition)>.1f,"2D spirit moves gently while text waits");
            Check(Camera.main.transform.position==cameraPos&&Quaternion.Angle(Camera.main.transform.rotation,cameraRot)<.01f&&story.companion.transform.position==worldBird&&story.player.transform.Find("종이요정").localScale==scale,"Cutscene does not animate 3D camera, bird or spirit scale");
            yield return .5f;Capture("01-first-meeting");
            Check(ui.StoryTextComplete&&story.BeatIndex==0,"Completed text waits indefinitely for input");
            Keys(Key.F);yield return 1.65f;
            Check(story.BeatIndex==1&&story.ActiveSequence==OpeningStoryData.Promise,"Holding F advances exactly one line and never skips the scene");
            Keys();yield return .1f;Check(story.BeatIndex==1,"Releasing F does not advance text");Capture("02-sharing-light");
            Keys(Key.Escape);yield return .15f;Keys();yield return .15f;
            Check(ui.ScreenState==PaperScreen.Paused&&Time.timeScale==0&&ui.CutsceneView.IsVisible,"Escape pauses with illustration retained behind menu");
            float elapsed=story.BeatElapsed,artTime=ui.CutsceneView.Elapsed;int beat=story.BeatIndex;Vector2 bird=ui.CutsceneView.BirdPosition;
            yield return .5f;
            Check(Mathf.Abs(story.BeatElapsed-elapsed)<.001f&&ui.CutsceneView.Elapsed==artTime&&ui.CutsceneView.BirdPosition==bird,"Pause freezes illustration and dialogue timeline");
            ui.OpenSettings();ui.SelectTab(3);yield return .2f;
            Check(!ui.GetComponentsInChildren<Text>().Any(t=>t.text=="대사 자동 진행"),"Obsolete automatic dialogue setting removed");
            var changed=PaperSettingsRuntime.Values.Copy();changed.keys[5]=(int)Key.R;changed.dialogueSpeed=0;changed.textSize=2;
            PaperSettingsRuntime.Instance.Apply(changed,false);ui.Draft.keys[5]=(int)Key.R;
            ui.Resume();yield return .15f;
            Check(story.BeatIndex==beat&&ui.CutsceneView.Elapsed<artTime+.5f,"Settings returns to same illustration and line");
            Check(ui.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("R / 좌클릭")),"Story prompt uses remapped interaction key and left click");
            foreach(var w in Each(Tap()))yield return w;
            Check(story.BeatIndex==2,"Remapped press advances exactly one completed dialogue");
            if(ui.cutsceneArt.HasFirstPerson)
            {
                Check(ui.CutsceneView.CurrentFrame==ui.cutsceneArt.firstPersonAwakening[1],"Light offering starts on bird's gift frame");
                yield return 2.6f;
                Check(story.BeatIndex==2&&(ui.cutsceneArt.HasContinuous?ui.CutsceneView.Continuous.GiftTime>2:ui.CutsceneView.CurrentFrame==ui.cutsceneArt.firstPersonAwakening[2]),"Light moves into first-person hands without advancing dialogue");
                Capture("02b-light-received");
                Check(!ui.BirdRevealed&&ui.CutsceneView.Continuous.BirdVisibility==0,"Approaching light does not reveal bird early");
                yield return 1.9f;
                Check(story.BeatIndex==2&&ui.CutsceneView.Continuous.BirdVisibility>.99f,"Bird becomes fully visible before the hold");
                Capture("02c-bird-hold");
                foreach(var w in Each(Tap()))yield return w;
                yield return 2.5f;
                Check(story.BeatIndex==2&&ui.CutsceneView.IsVisible,"Bird stays visible for three seconds; early F cannot skip the hold");
            }
            while(story.BeatIndex<4)
            {
                if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;
                foreach(var w in Each(Tap()))yield return w;
            }
            Check(!ui.StoryTextComplete,"Long dialogue begins with typewriter text");
            Check(ui.WalkingStory&&!ui.CutsceneView.IsVisible&&GameFlow.Instance.Phase==GamePhase.Playing&&!PaperUI.BlocksGameplay,"Awakening returns to gameplay with dialogue retained");
            Check(ui.BirdRevealed&&!ui.PortraitIsSilhouette&&ui.SpeakerPortrait==ui.cutsceneArt.continuousBird,"Revealed bird portrait appears during gameplay dialogue");
            pos=story.player.transform.position;Keys(Key.W);yield return .35f;Keys();yield return .1f;
            Check(Vector3.Distance(pos,story.player.transform.position)>.1f&&ui.StoryActive,"Player walks while dialogue is still active");
            Check(!story.companion.Directed&&story.player.Camera.Mode!=BookTower.CameraSystem.PaperCamera.CameraMode.Cinematic,"Camera and companion follow during walking dialogue");
            ui.Pause();yield return .15f;pos=story.player.transform.position;Keys(Key.W);yield return .2f;Keys();
            Check(Time.timeScale==0&&Vector3.Distance(pos,story.player.transform.position)<.01f,"Walking dialogue pauses movement");
            ui.Resume();yield return .15f;Check(ui.WalkingStory&&GameFlow.Instance.Phase==GamePhase.Playing,"Resume retains walking dialogue mode");
            pos=story.player.transform.position;Keys(Key.W);yield return .3f;Keys();yield return .1f;
            Check(Vector3.Distance(pos,story.player.transform.position)>.1f,"Walking resumes without requiring another mouse click");
            Capture("03a-walking-dialogue");
            foreach(var w in Each(Tap()))yield return w;
            Check(story.BeatIndex==4&&ui.StoryTextComplete,"One press reveals all text without changing dialogue");
            var walkInput=story.player.GetComponent<BookTower.PlayerSystem.PlayerInputReader>();
            Check(!walkInput.InteractPressed&&!walkInput.PointerPressed,"Dialogue advance is not forwarded to world interaction");
            foreach(var w in Each(Tap()))yield return w;
            Check(story.BeatIndex==5&&story.BeatElapsed<1,"Next press advances immediately without animation minimum-time gate");
            Check(ui.SpeakerPortrait==ui.cutsceneArt.spiritPortrait,"Spirit dialogue selects current hooded spirit portrait");
            Capture("03b-spirit-portrait");
            while(story.BeatIndex<6)
            {if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;foreach(var w in Each(Tap()))yield return w;}
            changed.keys[5]=(int)Key.F;PaperSettingsRuntime.Instance.Apply(changed,false);
            if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;
            yield return 1.3f;Capture("03-promise-large-text");
            Check(ui.GetComponentsInChildren<Text>().Where(t=>t.text==ui.StoryText).All(t=>t.cachedTextGenerator.characterCountVisible>=ui.StoryText.Replace("\n","").Length),"Large dialogue text fits the paper panel");
            foreach(var w in Each(ReadRest()))yield return w;
            Check(story.Completed(OpeningStoryData.Promise)&&ui.HasRecord("Story.Promise")&&!ui.HasRecord("Goal.Promise"),"Witnessed promise is recorded; duplicate goal is excluded from journal");
            yield return .15f;Check(ui.ScreenState==PaperScreen.Playing&&!PaperUI.BlocksGameplay&&!ui.CutsceneView.IsVisible&&story.player.Camera.Mode!=BookTower.CameraSystem.PaperCamera.CameraMode.Cinematic,"Final press hides cutscene and restores gameplay immediately");
            Check(ui.GuidedTarget&&ui.GuidedTarget.GetComponent<ReadableNote>().id==OpeningStoryDirector.WorkNote,"First objective points to the actual work book");
            yield return .3f;Check(ui.ObjectiveGuideVisible&&story.WorkBookEmphasized,"Unread work book has a visible label and warm light");
            Capture("03c-workbook-guide");
            string objective=ui.CurrentObjective;yield return 6f;
            Check(ui.ObjectiveVisible&&ui.CurrentObjective==objective,"Current room objective persists beyond old five-second timeout");
            ui.Pause();yield return .1f;ui.Resume();yield return .15f;
            Check(ui.ObjectiveVisible&&ui.CurrentObjective==objective,"Current objective survives pause and resume");
            Teleport(clearSpawn);yield return .25f;
            pos=story.player.transform.position;Keys(Key.W);yield return .35f;Keys();yield return .1f;
            Check(Vector3.Distance(pos,story.player.transform.position)>.15f,"Player can actually move after last dialogue");
            var control=story.foyer.GetComponentsInChildren<BookTower.Interaction.FoldBridgeControl>().First(c=>c.opticalControl==1);
            Teleport(control.transform.position+new Vector3(0,-.9f,-1.7f));yield return .3f;
            story.storyCamera.enabled=false;Camera.main.transform.position=story.player.transform.position+Vector3.up*1.1f;Camera.main.transform.LookAt(control.transform.position);yield return .3f;
            bool wasOpen=story.bridge.ShutterOpen;
            foreach(var w in Each(Tap()))yield return w;yield return 2f;
            Check(story.bridge.ShutterOpen!=wasOpen,"Actual F input operates light control after dialogue");
            story.storyCamera.enabled=true;
            var note=Object.FindObjectsByType<ReadableNote>(FindObjectsSortMode.None).First(n=>n.id==OpeningStoryDirector.WorkNote);
            Teleport(story.foyer.transform.TransformPoint(new Vector3(-8,3.15f,-9)));yield return .5f;
            story.storyCamera.enabled=false;Camera.main.transform.position=story.player.transform.position+Vector3.up*1.1f;Camera.main.transform.LookAt(note.transform.position);yield return .3f;
            var aim=BookTower.CameraSystem.PaperCamera.AimRay(Camera.main);
            BookTower.Interaction.InteractionRay.Cast(aim,out var aimed,100);
            report.AppendLine("AUDIT: after dialogue target="+BookTower.Interaction.TargetInteractor.Current+" hit="+aimed.collider+" story="+ui.StoryActive+" phase="+GameFlow.Instance.Phase);
            foreach(var w in Each(Tap()))yield return w;yield return .3f;
            Check(ui.ScreenState==PaperScreen.Reading,"Actual F input opens workbench book after final dialogue");
            Check(ui.CurrentObjective.Contains("은박"),"Reading book advances the persistent room objective");
            Check(!ui.GuidedTarget&&!ui.ObjectiveGuideVisible&&!story.WorkBookEmphasized,"Reading clears the book marker and light emphasis");
            ui.Back();yield return .2f;
            foreach(var w in Each(Click()))yield return w;
            Check(ui.ScreenState==PaperScreen.Reading,"Left click also reads workbench through its tabletop proxy");
            story.storyCamera.enabled=true;
            Check(ui.ScreenState==PaperScreen.Reading&&!ui.HasRecord(OpeningStoryDirector.WorkNote),"Reading pauses safely without copying puzzle solutions into the journal");
            story.bridge.Solve();yield return .2f;
            Check(!ui.HasRecord("M01")&&ui.ScreenState==PaperScreen.Reading,"Unread memory is not spoiled in journal; reading is not interrupted");
            ui.Back();foreach(var w in Each(Wait(()=>story.ActiveSequence==OpeningStoryData.Return)))yield return w;
            yield return 1.3f;if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;Capture("04-returning-place");
            foreach(var w in Each(ReadRest()))yield return w;
            Check(ui.HasRecord("M01"),"Return memory enters journal only after its dialogue completes");
            int count=story.SequenceStarts;story.player.TeleportToSafePoint();yield return 1f;
            Check(story.SequenceStarts==count,"Checkpoint does not replay opening or memory");
            Teleport(story.theatreRoom.transform.TransformPoint(new Vector3(-23,.2f,-22)));yield return 1f;
            foreach(var w in Each(Wait(()=>story.ActiveSequence==OpeningStoryData.Theatre)))yield return w;
            if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;Capture("05-entering-picturebook");
            changed.reduceStoryMotion=true;PaperSettingsRuntime.Instance.Apply(changed,false);yield return .4f;
            spriteBefore=ui.CutsceneView.SpiritPosition;yield return .5f;
            Check(ui.CutsceneView.SpiritPosition==spriteBefore,"Reduced motion disables ongoing cutout drift");
            foreach(var w in Each(ReadRest()))yield return w;
            Teleport(story.theatreRoom.transform.TransformPoint(new Vector3(7,.2f,21)));yield return .6f;
            var workshop=story.theatreRoom.GetComponentInChildren<BookTower.Puzzles.StorybookLightPuzzle>();
            if(workshop&&workshop.Duet)
            {
                workshop.SelectLight(1);workshop.DragPiece(0,new Vector3(-1.8f,1,12.5f/1.5f));workshop.DragPiece(1,new Vector3(-.9f,1,12.5f/3));if(!workshop.Flipped)workshop.TurnPiece(1);yield return 2f;
                Check(workshop.Duet.RabbitRecorded&&!workshop.Complete,"One animal does not start the naming story");
                workshop.DragPiece(2,BookTower.Puzzles.ShadowDuet.FoxBodySolution);workshop.DragPiece(3,BookTower.Puzzles.ShadowDuet.FoxTailSolution);if(!workshop.Duet.TailFlipped)workshop.TurnPiece(3);workshop.SelectLight(0);
            }
            else if(workshop&&workshop.realShadows)
            {workshop.DragPaper(false,new Vector3(0,1,12.5f/1.5f));workshop.DragPaper(true,new Vector3(0,1,12.5f/3));workshop.TurnPaper(true);}
            else if(workshop)
            {
                workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.FindBody);workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.FindEars);
                workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.BodyRail);workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.EarRail);
                for(int i=0;i<4;i++)workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.BodyRail);
                workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.EarRail);workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.EarRail);
                workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.Flip);workshop.Use(BookTower.Puzzles.StorybookLightControl.Action.Lamp);
            }
            else
            {
                var theatre=story.theatreRoom.GetComponentInChildren<StorybookShadowTheatre>();
                theatre.practice.CycleForward();theatre.practice.CycleForward();theatre.left.CycleForward();theatre.right.CycleForward();theatre.right.CycleForward();theatre.right.CycleForward();
            }
            foreach(var w in Each(Wait(()=>story.ActiveSequence==OpeningStoryData.Name)))yield return w;
            Check(!ui.HasRecord("M02"),"Actual two-animal puzzle starts memory without prematurely spoiling its name");
            yield return 1.3f;if(!ui.StoryTextComplete)foreach(var w in Each(Tap()))yield return w;Capture("06-moa-name");
            foreach(var w in Each(ReadRest()))yield return w;
            Check(story.NameKnown&&ui.HasRecord("M02")&&!ui.HasRecord("Goal.Moa"),"1A ending records witnessed name once and keeps objectives out of journal");
            count=story.SequenceStarts;Teleport(story.foyer.transform.TransformPoint(new Vector3(0,3.2f,9)));yield return 1f;
            Teleport(story.theatreRoom.transform.TransformPoint(new Vector3(-23,.2f,-22)));yield return 1f;
            Check(story.SequenceStarts==count,"Returning to rooms does not duplicate story scenes");
            // Holding the interaction key across entering a story cannot consume its first line.
            Keys(Key.F);
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/InGame.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return 1.5f;story=Object.FindFirstObjectByType<OpeningStoryDirector>();ui=PaperUI.Instance;
            foreach(var w in Each(Wait(()=>story.ActiveSequence==OpeningStoryData.Promise)))yield return w;
            yield return 1.5f;Check(story.BeatIndex==0,"Key held before cutscene entry cannot skip or advance it");
            Keys();yield return .15f;foreach(var w in Each(Tap()))yield return w;
            Check(story.BeatIndex==1,"Fresh press after release advances normally");
            Keys(Key.F);yield return .1f;ui.Pause();yield return .1f;ui.Resume();yield return 1.4f;
            int heldBeat=story.BeatIndex;Check(heldBeat==1,"Held input across pause/resume cannot skip a line");Keys();yield return .15f;
            foreach(var w in Each(ReadRest()))yield return w;
            Check(!story.companion.Directed&&!ui.StoryActive&&!ui.CutsceneView.IsVisible&&Time.timeScale==1,"Completion releases all story ownership");
            Capture("07-playing-after-cutscene");
            foreach(var w in Each(VerifyMouse()))yield return w;
        }
        static IEnumerator VerifyMouse()
        {
            Pointer(1);yield return .1f;
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/InGame.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return 1.5f;story=Object.FindFirstObjectByType<OpeningStoryDirector>();ui=PaperUI.Instance;
            foreach(var w in Each(Wait(()=>story.ActiveSequence==OpeningStoryData.Promise)))yield return w;
            Check(story.BeatIndex==0,"Mouse held before cutscene entry cannot advance its first line");
            Pointer();yield return .15f;Check(story.BeatIndex==0,"Releasing entry-held mouse does not advance dialogue");
            Pointer(2);yield return .08f;Pointer();yield return .08f;
            Check(story.BeatIndex==0,"Right mouse click does not advance dialogue");
            foreach(var w in Each(Click()))yield return w;
            Check(story.BeatIndex==1,"Clicking outside dialogue advances one completed line");
            Check(!ui.StoryTextComplete,"Next mouse-driven line begins with typewriter text");
            Canvas.ForceUpdateCanvases();
            var button=ui.GetComponentsInChildren<Button>().First(b=>b.gameObject.activeInHierarchy&&b.name=="다음 대사");
            var rect=(RectTransform)button.transform;
            var buttonPoint=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            Pointer(1,buttonPoint);yield return .08f;
            Check(story.BeatIndex==1&&ui.StoryTextComplete,"Left press on dialogue button reveals all text without advancing the line");
            yield return 1f;Check(story.BeatIndex==1,"Holding left mouse does not repeat dialogue actions");
            Pointer(0,buttonPoint);yield return .1f;Check(story.BeatIndex==1,"Dialogue button release does not double-advance revealed text");
            foreach(var w in Each(Click(buttonPoint)))yield return w;
            Check(story.BeatIndex==2,"Actual dialogue button click advances once across press and release");
            Keys(Key.F);Pointer(1);yield return .08f;
            Check(story.BeatIndex==2&&ui.StoryTextComplete,"Simultaneous F and left click reveal only the current line");
            Keys();Pointer();yield return .1f;
            ui.Pause();Pointer(1);yield return .2f;
            Check(story.BeatIndex==2&&ui.ScreenState==PaperScreen.Paused,"Mouse input on pause background does not advance story");
            ui.Resume();yield return .2f;
            Check(story.BeatIndex==2,"Mouse held through pause resume cannot advance story");
            Pointer();yield return .1f;Check(story.BeatIndex==2,"Release after pause resume does not advance story");
            foreach(var w in Each(Click()))yield return w;
            Check(story.BeatIndex==2,"Early click cannot bypass light arriving at the viewer");
            foreach(var w in Each(Wait(()=>story.BeatIndex==3)))yield return w;
            Check(ui.WalkingStory&&ui.BirdRevealed,"Closest light automatically wakes spirit and restores walking");
            int guard=0;
            while(story.ActiveSequence==OpeningStoryData.Promise&&guard++<OpeningStoryData.Beats(OpeningStoryData.Promise).Length*2+2)
                foreach(var w in Each(Click()))yield return w;
            yield return .1f;
            Check(story.Completed(OpeningStoryData.Promise)&&ui.ScreenState==PaperScreen.Playing&&!PaperUI.BlocksGameplay,"Mouse alone finishes remaining dialogue and restores gameplay");
        }
        static IEnumerable Each(IEnumerator e){while(e.MoveNext())yield return e.Current;}
    }
}
