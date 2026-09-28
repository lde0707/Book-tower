using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class WorkshopVerifier
    {
        static IEnumerator VerifyReal()
        {
            yield return 1.5f;player=Object.FindFirstObjectByType<PlayerController>();room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(x=>x.RoomId=="1A");puzzle=room.GetComponentInChildren<StorybookLightPuzzle>();
            original=InputSystem.settings;var settings=Object.Instantiate(original);settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
            Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).First(p=>p.PuzzleId=="P1").Solve();yield return 1.5f;
            Teleport(new Vector3(30,.25f,-208));
            foreach(var at in new[]{new Vector3(48,2.3f,-200.8f),new Vector3(68,4.25f,-192.8f),W(-25,-12.8f),W(-13,-12.8f),W(-5,0),W(3,12)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}
            Check(player.transform.position.y>3.7f,"Real controller traverses F0 corridor and compact curved room entrance");
            var basePuzzle=puzzle.GetComponent<FoldCombinationPuzzle>();basePuzzle.Activate();
            Check(puzzle.realShadows&&!puzzle.bodyShadow&&!puzzle.earShadow,"Synthetic shadow copies removed");
            Check(puzzle.lantern.type==LightType.Spot&&puzzle.lantern.shadows!=LightShadows.None,"Real spot light casts shadows");
            yield return 2f;Check(!puzzle.Complete,"Initial separated puppets cannot solve");
            var camera=Object.FindFirstObjectByType<PaperCamera>();camera.enabled=false;
            var target=puzzle.body.position+Vector3.up*.9f;Teleport(target+new Vector3(0,-.9f,-2.5f));yield return .3f;
            Camera.main.transform.position=target+new Vector3(0,1.2f,-2.5f);Camera.main.transform.LookAt(target);yield return .2f;
            var before=puzzle.BodyPosition;
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return .15f;
            Check(PaperShadowDrag.Holding,"Actual left mouse press grabs puppet collider");
            for(int i=0;i<8;i++){InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1,delta=new Vector2(18,0)});yield return .04f;}
            Check(Vector3.Distance(before,puzzle.body.localPosition)>.1f,"Held mouse motion continuously moves real paper");
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return .2f;Check(!PaperShadowDrag.Holding,"Mouse release drops paper and restores camera input");
            target=puzzle.ears.position+Vector3.up*1.1f;Teleport(target+new Vector3(0,-1.1f,-2.5f));yield return .3f;
            Camera.main.transform.position=target+new Vector3(0,1.2f,-2.5f);Camera.main.transform.LookAt(target);yield return .2f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
            Check(PaperShadowDrag.Holding,"F grabs ears in toggle mode");
            InputSystem.QueueStateEvent(mouse,new MouseState{scroll=new Vector2(0,120)});yield return .1f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return .1f;
            Check(puzzle.Flipped,"Mouse wheel flips held asymmetric ears");
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2});yield return .1f;InputSystem.QueueStateEvent(mouse,new MouseState());yield return .1f;
            Check(!puzzle.Flipped,"Right click reverses ear orientation");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return .1f;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .1f;
            Check(!PaperShadowDrag.Holding,"Second F releases without activating another object");
            puzzle.DragPaper(false,new Vector3(0,1,12.5f/1.5f));puzzle.DragPaper(true,new Vector3(0,1,12.5f/3));yield return 2f;
            Check(!puzzle.Complete,"Correct size still requires ear orientation");
            puzzle.Use(StorybookLightControl.Action.Lamp);puzzle.TurnPaper(true);yield return 2f;Check(!puzzle.Complete,"Aligned objects without light cannot solve");
            puzzle.Use(StorybookLightControl.Action.Lamp);yield return 3f;
            report.AppendLine("AUDIT: "+puzzle.Audit+" P2="+basePuzzle.State+" holding="+PaperShadowDrag.Holding+" body="+puzzle.body.localPosition+" ears="+puzzle.ears.localPosition);
            Check(puzzle.Complete&&basePuzzle.State==PuzzleBase.PuzzleState.Solved,"Continuous position, scale, orientation and light solve original P2");
            // Capture the actual wall twice: shadow-casting paper on/off must change visible pixels.
            Teleport(W(3,12));Camera.main.transform.position=puzzle.stage.TransformPoint(new Vector3(4,5.2f,7));Camera.main.transform.LookAt(puzzle.stage.TransformPoint(new Vector3(0,3,12.5f)));yield return .3f;
            var cast=puzzle.body.GetComponentsInChildren<Renderer>().Concat(puzzle.ears.GetComponentsInChildren<Renderer>()).Where(r=>r.shadowCastingMode!=ShadowCastingMode.Off).ToArray();
            Check(cast.Length>=2,"Both cutouts cast geometric shadows");
            var on=ShadowPixels();InteriorF0Builder.Capture(Camera.main,Out+"/real-shadow-on.png");foreach(var r in cast)r.shadowCastingMode=ShadowCastingMode.Off;yield return .3f;
            var off=ShadowPixels();InteriorF0Builder.Capture(Camera.main,Out+"/real-shadow-off.png");int changed=0;for(int i=0;i<on.Length;i++)if(Mathf.Abs(on[i].grayscale-off[i].grayscale)>.03f)changed++;
            foreach(var r in cast)r.shadowCastingMode=ShadowCastingMode.TwoSided;
            Check(changed>150,"Rendered pixels respond to real shadow casters: "+changed);
            camera.enabled=true;camera.SetAim(-20,-3);yield return 1f;InteriorF0Builder.Capture(Camera.main,Out+"/1A-play-solved.png");
            foreach(var at in new[]{W(12,20),W(18,23),W(24.5f,23),W(26.5f,30)}){var e=Walk(at);while(e.MoveNext())yield return e.Current;}
            Check(player.transform.position.y>3.7f,"Existing 1B exit remains traversable");
            Check(Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="1B").IsVisible,"Next room is visible on connecting passage");
        }
        static Color[] ShadowPixels()
        {
            var camera=Camera.main;var old=camera.targetTexture;var rt=RenderTexture.GetTemporary(640,360,24);camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(640,360,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,640,360),0,0);t.Apply();var pixels=t.GetPixels();Object.Destroy(t);RenderTexture.active=prior;camera.targetTexture=old;RenderTexture.ReleaseTemporary(rt);return pixels;
        }
    }
}
