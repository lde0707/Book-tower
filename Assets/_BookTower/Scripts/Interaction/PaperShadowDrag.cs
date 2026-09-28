using UnityEngine;
using UnityEngine.InputSystem;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.UI;
namespace BookTower.Interaction
{
    [DefaultExecutionOrder(-50),RequireComponent(typeof(PlayerInputReader))]
    public sealed class PaperShadowDrag:MonoBehaviour
    {
        public static bool Holding=>active&&active.held;
        public static bool ConsumesInput=>Holding||consumedFrame==Time.frameCount;
        static PaperShadowDrag active;static int consumedFrame=-1;
        PaperShadowHandle held;PlayerInputReader input;Plane plane;Vector3 offset;
        Vector2 pointer;bool mouseHeld;int began;
        public PaperShadowHandle Held=>held;
        void Awake(){input=GetComponent<PlayerInputReader>();}
        void OnDisable(){Release();}
        void OnApplicationFocus(bool focus){if(!focus)Release();}
        void Release(){if(held)consumedFrame=Time.frameCount;held=null;if(active==this)active=null;}
        void Update()
        {
            var camera=Camera.main;var mouse=Mouse.current;
            if(!camera||mouse==null||PaperUI.BlocksGameplay||(PaperUI.Instance&&PaperUI.Instance.StoryActive)){Release();return;}
            if(!held)
            {
                if(!input.PointerPressed&&!input.InteractPressed)return;
                if(!InteractionRay.Cast(PaperCamera.AimRay(camera),out var hit,9))return;
                var handle=hit.collider.GetComponentInParent<PaperShadowHandle>();
                if(!handle||!handle.puzzle||handle.puzzle.Complete)return;
                var room=handle.GetComponentInParent<BookTower.Rooms.Room>();
                if(room&&!room.IsVisible)return;
                if(Vector3.Distance(transform.position,hit.point)>6)return;
                held=handle;active=this;mouseHeld=input.PointerPressed;began=Time.frameCount;consumedFrame=Time.frameCount;
                plane=new Plane(handle.puzzle.stage.up,hit.point);offset=handle.transform.position-hit.point;
                pointer=new Vector2(camera.pixelWidth*.5f,camera.pixelHeight*.5f);
                PaperUI.Notify("누른 채 끌기 · 놓으면 고정 · 귀는 휠/우클릭으로 뒤집기");return;
            }
            if(Time.frameCount>began&&((mouseHeld&&!mouse.leftButton.isPressed)||(!mouseHeld&&input.InteractPressed))){Release();return;}
            if(mouse.rightButton.wasPressedThisFrame||Mathf.Abs(mouse.scroll.ReadValue().y)>.1f)held.puzzle.TurnPaper(held.isEar);
            pointer+=mouse.delta.ReadValue();pointer.x=Mathf.Clamp(pointer.x,12,camera.pixelWidth-12);pointer.y=Mathf.Clamp(pointer.y,12,camera.pixelHeight-12);
            var ray=camera.ScreenPointToRay(pointer);
            Vector3 wanted=held.transform.position;
            if(plane.Raycast(ray,out float distance)&&distance<40)wanted=ray.GetPoint(distance)+offset;
            else
            {
                var right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
                var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
                var delta=mouse.delta.ReadValue();wanted+=right*delta.x*.012f+forward*delta.y*.012f;
            }
            if(Vector3.Distance(transform.position,wanted)>7)wanted=transform.position+(wanted-transform.position).normalized*7;
            wanted=Vector3.MoveTowards(held.transform.position,wanted,Time.deltaTime*7);
            held.puzzle.DragPaper(held.isEar,held.puzzle.stage.InverseTransformPoint(wanted));
        }
        void OnGUI(){if(!held)return;var before=GUI.color;GUI.color=new Color(1,.87f,.58f);GUI.DrawTexture(new Rect(pointer.x-4,Screen.height-pointer.y-4,8,8),Texture2D.whiteTexture);GUI.color=before;}
    }
}
