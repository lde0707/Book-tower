using UnityEngine;
using BookTower.PlayerSystem;
using BookTower.Core;

namespace BookTower.Presentation
{
    /// <summary>Visual FSM only: never changes the controller, collider or jump trajectory.</summary>
    [DefaultExecutionOrder(120), DisallowMultipleComponent]
    public sealed class HoodSpiritAnimator : MonoBehaviour
    {
        public enum Pose { Idle, Move, JumpRise, Fall, Glide, Land, LookAround, TidyBookmark, Stretch }
        [SerializeField] Animator animator;
        [SerializeField] Transform sway;
        PlayerController player;
        Pose pose;
        float idleTime, nextAction, actionTime, lastYaw, roll, lean;
        int previousAction = -1;
        bool wasAirborne;
        public Pose CurrentPose => pose;
        public Animator ModelAnimator => animator;

        void Awake()
        {
            player=GetComponentInParent<PlayerController>();
            if(!animator) animator=GetComponentInChildren<Animator>();
            if(animator) animator.applyRootMotion=false;
            lastYaw=transform.eulerAngles.y;
            Schedule();
        }
        void OnEnable() { idleTime=0; actionTime=0; wasAirborne=false; }
        void Schedule() { nextAction=Random.Range(8f,15f); idleTime=0; }
        void Change(Pose next,float blend=.2f)
        {
            if(pose==next) return;
            pose=next;actionTime=0;
            animator.CrossFadeInFixedTime(next.ToString(),blend,0,0);
        }
        public void Step(float dt,float speed,bool airborne,bool rising,bool gliding,bool blocked,bool activeInput)
        {
            if(!animator || dt<=0) return;
            actionTime+=dt;
            if(blocked) { Change(Pose.Idle);Schedule();wasAirborne=false;return; }
            bool moving=speed>(pose==Pose.Move?.16f:.3f);
            if(airborne)
            {
                Change(gliding?Pose.Glide:rising?Pose.JumpRise:Pose.Fall,.14f);
                Schedule();wasAirborne=true;
            }
            else if(moving)
            {
                Change(Pose.Move,.16f);Schedule();wasAirborne=false;
            }
            else if(wasAirborne) { Change(Pose.Land,.09f);Schedule();wasAirborne=false; }
            else if(pose==Pose.Land && actionTime<.42f) { }
            else if(pose>=Pose.LookAround)
            {
                float duration=pose==Pose.LookAround?4.8f:pose==Pose.TidyBookmark?3.8f:4.5f;
                if(activeInput || actionTime>=duration-.12f) { Change(Pose.Idle,.2f);Schedule(); }
            }
            else
            {
                Change(Pose.Idle);
                if(activeInput) Schedule();
                else if((idleTime+=dt)>=nextAction)
                {
                    int choice=(previousAction+Random.Range(1,3)+3)%3;
                    previousAction=choice;Change((Pose)((int)Pose.LookAround+choice),.35f);
                    Schedule();
                }
            }
            float target=pose==Pose.Move?Mathf.Clamp(speed/4.5f,.65f,1.35f):1;
            animator.speed=Mathf.Lerp(animator.speed,target,1-Mathf.Exp(-10*dt));
        }
        void Update()
        {
            if(!player || !animator) return;
            if(GameFlow.Instance && GameFlow.Instance.Phase==GamePhase.Paused) return;
            bool blocked=BookTower.UI.PaperUI.BlocksGameplay || (GameFlow.Instance && !GameFlow.Instance.AcceptsPlayerInput)
                || player.Machine?.Current==player.Respawn;
            bool air=player.Machine?.Current==player.Airborne || player.Machine?.Current==player.Glide;
            bool input=player.Input && (player.Input.Move.sqrMagnitude>.01f || player.Input.InteractPressed || player.Input.PointerPressed);
            Step(Time.deltaTime,player.HorizontalVelocity.magnitude,air,player.VerticalVelocity>.3f,player.Machine?.Current==player.Glide,blocked,input);
            if(!sway || Time.deltaTime<=0) return;
            float yaw=transform.eulerAngles.y;
            float turn=Mathf.DeltaAngle(lastYaw,yaw)/Mathf.Max(Time.deltaTime,.001f);lastYaw=yaw;
            float amount=blocked?0:Mathf.Clamp01(player.HorizontalVelocity.magnitude/2);
            roll=Mathf.Lerp(roll,-Mathf.Clamp(turn*.022f,-6,6)*amount,1-Mathf.Exp(-6*Time.deltaTime));
            lean=Mathf.Lerp(lean,air?0:amount*2,1-Mathf.Exp(-5*Time.deltaTime));
            sway.localRotation=Quaternion.Euler(lean,0,roll);
        }
    }
}
