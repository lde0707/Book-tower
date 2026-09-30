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
        public static bool ConsumesInput=>Holding||Aimed||consumedFrame==Time.frameCount;
        static PaperShadowDrag active;static int consumedFrame=-1;
        PaperShadowHandle held;PlayerInputReader input;
        public const float GrabDistance=12f;
        public static PaperShadowHandle Aimed { get; private set; }
        int began;
        Vector3 previousPlayerPosition;
        public PaperShadowHandle Held=>held;
        void Awake(){input=GetComponent<PlayerInputReader>();}
        void OnDisable(){Release();Aimed=null;}
        void OnApplicationFocus(bool focus){if(!focus)Release();}
        void Release(){if(held)consumedFrame=Time.frameCount;held=null;if(active==this)active=null;}
        void Update()
        {
            var camera=Camera.main;var mouse=Mouse.current;
            Aimed=null;
            if(!camera||mouse==null||!PaperCamera.AimActive||PaperUI.BlocksGameplay||(BookTower.Core.GameFlow.Instance&&!BookTower.Core.GameFlow.Instance.AcceptsPlayerInput)||(PaperUI.Instance&&PaperUI.Instance.StoryActive)){Release();return;}
            if(!held)
            {
                if(!InteractionRay.Cast(PaperCamera.AimRay(camera),out var hit,GrabDistance+Vector3.Distance(camera.transform.position,transform.position)+1))return;
                var handle=hit.collider.GetComponentInParent<PaperShadowHandle>();
                if(!handle||!handle.puzzle||!handle.CanGrab)return;
                var room=handle.GetComponentInParent<BookTower.Rooms.Room>();
                if(room&&!room.IsVisible)return;
                if(Vector3.Distance(transform.position,hit.point)>GrabDistance)return;
                Aimed=handle;
                if(!input.PointerPressed)return;
                held=handle;active=this;began=Time.frameCount;consumedFrame=Time.frameCount;
                previousPlayerPosition=transform.position;

                var orbit=camera.GetComponent<PaperCamera>();if(orbit)orbit.ConsumeMovementYaw();
                PaperUI.Notify("클릭 유지: 마우스 상하·좌우 또는 이동 키로 옮기기 · 우클릭: 뒤집기 · 놓으면 고정");return;
            }
            if(!held.puzzle||held.puzzle.Complete||Vector3.Distance(transform.position,held.transform.position)>18f){Release();return;}
            var heldRoom=held.GetComponentInParent<BookTower.Rooms.Room>();
            if(heldRoom&&!heldRoom.IsVisible){Release();return;}
            if(Time.frameCount>began&&!mouse.leftButton.isPressed){Release();return;}
            if(mouse.rightButton.wasPressedThisFrame)held.puzzle.TurnPiece(held.PieceIndex);
        }
        // One planar drag gesture handles both axes without unstable ray/plane intersections.
        public static float DistanceGain(float distance)
        {
            float near=Mathf.SmoothStep(.2f,1,Mathf.InverseLerp(1.2f,5.5f,distance));
            float far=Mathf.Lerp(1,.22f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,15,distance)));
            return near*far;
        }
        void LateUpdate()
        {
            if(!held||!Camera.main||Mouse.current==null||Time.timeScale==0||Time.frameCount==began)return;
            var mouse=Mouse.current;
            float gain=DistanceGain(Vector3.Distance(transform.position,held.transform.position));
            if(input.ModifierHeld)gain*=.3f;
            var delta=Vector2.ClampMagnitude(mouse.delta.ReadValue(),120)*.015f*gain;
            float lateral=delta.x,depth=delta.y;
            Vector3 position=held.puzzle.PiecePosition(held.PieceIndex);
            var stage=held.puzzle.stage;var camera=Camera.main.transform;
            var right=stage.InverseTransformDirection(Vector3.ProjectOnPlane(camera.right,stage.up)).normalized;
            var forward=Vector3.Cross(right,Vector3.up).normalized;
            Vector3 carry=stage.InverseTransformVector(Vector3.ProjectOnPlane(transform.position-previousPlayerPosition,stage.up));
            previousPlayerPosition=transform.position;
            held.puzzle.DragPiece(held.PieceIndex,position+right*lateral+forward*depth+carry);
        }
    }
}
