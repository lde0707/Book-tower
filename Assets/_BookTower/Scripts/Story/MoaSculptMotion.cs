using UnityEngine;
using BookTower.Core;
namespace BookTower.Story
{
    [DefaultExecutionOrder(160)]
    public sealed class MoaSculptMotion : MonoBehaviour
    {
        public PaperCompanion companion;
        public Animator animator;
        int state;float timer=5,flutterTime;
        void Update()
        {
            if(!companion||!animator)return;
            bool active=!companion.Directed&&GameFlow.Instance?.Phase==GamePhase.Playing;
            animator.speed=active?1:0;if(!active)return;
            float dt=Time.deltaTime;timer-=dt;flutterTime-=dt;
            bool moving=companion.MovementSpeed>.8f;
            if(timer<=0&&!moving){flutterTime=1.5f;timer=Random.Range(6f,11f);}
            int next=moving?1:flutterTime>0?2:0;
            if(next!=state){state=next;animator.CrossFadeInFixedTime(next==1?"Fly":next==2?"Flutter":"Hover",.22f);}
        }
    }
}
