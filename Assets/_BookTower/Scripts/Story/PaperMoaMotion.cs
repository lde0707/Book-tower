using UnityEngine;
using BookTower.Core;

namespace BookTower.Story
{
    [DefaultExecutionOrder(150)]
    public sealed class PaperMoaMotion : MonoBehaviour
    {
        public PaperCompanion companion;
        public Animator animator;
        public Vector2 flutterInterval = new(5.5f,10.5f);
        public float AnimationTime { get; private set; }
        public int FlutterCount { get; private set; }
        public float RunBlend => animator ? animator.GetFloat(Speed) : 0;
        static readonly int Speed=Animator.StringToHash("Speed"),Flutter=Animator.StringToHash("Flutter");
        float untilFlutter;
        void Awake(){untilFlutter=3.2f;}
        public void FlutterNow()
        {
            if(!animator||!companion||companion.Directed||GameFlow.Instance?.Phase!=GamePhase.Playing)return;
            animator.SetTrigger(Flutter);FlutterCount++;
            untilFlutter=Random.Range(flutterInterval.x,flutterInterval.y);
        }
        void Update()
        {
            if(!animator||!companion)return;
            bool active=!companion.Directed&&GameFlow.Instance?.Phase==GamePhase.Playing;
            animator.speed=active?1:0;
            if(!active)return;
            float dt=Time.deltaTime;AnimationTime+=dt;
            animator.SetFloat(Speed,Mathf.Clamp01(companion.MovementSpeed/4.5f),.16f,dt);
            untilFlutter-=dt;if(untilFlutter<=0)FlutterNow();
        }
    }
}
