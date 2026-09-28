using UnityEngine;
using BookTower.Core;
using BookTower.PlayerSystem;

namespace BookTower.Story
{
    public sealed class PaperCompanion : MonoBehaviour
    {
        public Transform leftWing, rightWing, head;
        public Light glow;
        public Transform lightSeed;
        public PlayerController player;
        [Tooltip("새 모델은 플레이어 곁에서 낮게 비행한다. 이동 경로는 벽 충돌을 확인한다.")]
        public bool airborneFollow;
        public bool Directed { get; set; }
        public float MovementSpeed { get; private set; }
        public bool Grounded { get; private set; }
        readonly RaycastHit[] hits = new RaycastHit[24];
        Vector3 velocity, previousPlayer;
        bool following;
        float heightVelocity;
        public void Pose(Vector3 position, Vector3 lookAt, float wings, float light)
        {
            transform.position = position;
            var direction = lookAt - position; direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction);
            if(leftWing)leftWing.localRotation = Quaternion.Euler(0, 0, -wings);
            if(rightWing)rightWing.localRotation = Quaternion.Euler(0, 0, wings);
            if(glow)glow.intensity = light;
        }
        void LateUpdate()
        {
            if (!player || Directed || GameFlow.Instance?.Phase != GamePhase.Playing) return;
            float dt=Time.deltaTime;if(dt<=0)return;
            if(airborneFollow){FollowAir(dt);return;}
            Vector3 destination=player.transform.TransformPoint(new Vector3(.72f,0,-.48f));
            // Follow on the same walkable surface. At a narrow bridge edge use the player's
            // centre line instead of putting the bird in the pit. No solid bird collider:
            // the companion cannot block the player or intercept the light puzzle.
            bool support=Floor(destination,out var floor);
            if(!support)
            {destination=player.transform.position-player.transform.forward*.45f;support=Floor(destination,out floor);}
            if(!support){MovementSpeed=Mathf.MoveTowards(MovementSpeed,0,10*dt);Grounded=false;return;}
            destination.y=floor+.022f;
            bool teleported=following&&Vector3.Distance(player.transform.position,previousPlayer)>5;
            previousPlayer=player.transform.position;following=true;
            Vector3 before=transform.position;
            if(teleported||Vector3.Distance(before,destination)>12)
            {transform.position=destination;velocity=Vector3.zero;heightVelocity=0;MovementSpeed=0;Grounded=true;return;}
            Vector3 planar=destination-before;planar.y=0;
            Vector3 move=Vector3.SmoothDamp(before,new Vector3(destination.x,before.y,destination.z),ref velocity,.17f,7.5f,dt)-before;
            if(planar.sqrMagnitude<.015f*.015f){move=Vector3.zero;velocity=Vector3.zero;}
            if(Blocked(before,move,out var obstruction))
            {move=Vector3.ProjectOnPlane(move,obstruction.normal);move.y=0;if(Blocked(before,move,out _))move=Vector3.zero;}
            Vector3 next=before+move;
            Grounded=Floor(next,out float nextFloor);
            if(Grounded&&Mathf.Abs(nextFloor-before.y)<.75f)
            {next.y=Mathf.SmoothDamp(before.y,nextFloor+.022f,ref heightVelocity,.055f,12,dt);transform.position=next;}
            else velocity=Vector3.zero; // Wait at gaps rather than walking through empty space.
            Vector3 travelled=transform.position-before;travelled.y=0;MovementSpeed=travelled.magnitude/dt;
            Vector3 look=MovementSpeed>.08f?travelled:player.transform.forward;
            if(look.sqrMagnitude>.0001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look),1-Mathf.Exp(-10*dt));
            if(glow)glow.intensity=.18f;
        }
        void FollowAir(float dt)
        {
            float t=Time.time;
            Vector3 destination=player.transform.TransformPoint(new Vector3(.70f+.09f*Mathf.Sin(t*.8f),.55f+.045f*Mathf.Sin(t*2.1f),-.40f+.07f*Mathf.Cos(t*.8f)));
            bool teleported=following&&Vector3.Distance(player.transform.position,previousPlayer)>5;
            previousPlayer=player.transform.position;following=true;Grounded=false;
            Vector3 before=transform.position;
            if(teleported){transform.position=destination;velocity=Vector3.zero;MovementSpeed=0;return;}
            Vector3 candidate=Vector3.SmoothDamp(before,destination,ref velocity,.24f,8,dt);
            Vector3 step=candidate-before;
            if(Blocked(before-Vector3.up*.16f,step,out var hit))
            {
                step=Vector3.ProjectOnPlane(step,hit.normal);
                if(Blocked(before-Vector3.up*.16f,step,out _))step=Vector3.zero;
                velocity=Vector3.zero;
            }
            transform.position=before+step;MovementSpeed=step.magnitude/dt;
            Vector3 look=player.HorizontalVelocity.sqrMagnitude>.1f?player.HorizontalVelocity:player.transform.position-transform.position;look.y=0;
            if(look.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look),1-Mathf.Exp(-5*dt));
            if(glow)glow.intensity=.12f;
        }
        bool Ignore(Collider c)=>!c||c.transform.IsChildOf(transform)||(player&&c.transform.IsChildOf(player.transform));
        bool Floor(Vector3 position,out float height)
        {
            height=position.y;float closest=float.MaxValue;bool found=false;
            int count=Physics.RaycastNonAlloc(position+Vector3.up*.65f,Vector3.down,hits,3.2f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(!Ignore(hits[i].collider)&&hits[i].normal.y>.60f&&hits[i].distance<closest)
                {closest=hits[i].distance;height=hits[i].point.y;found=true;}
            return found;
        }
        bool Blocked(Vector3 origin,Vector3 delta,out RaycastHit hit)
        {
            hit=default;if(delta.sqrMagnitude<.000001f)return false;
            int count=Physics.SphereCastNonAlloc(origin+Vector3.up*.40f,.18f,delta.normalized,hits,delta.magnitude+.025f,~0,QueryTriggerInteraction.Ignore);
            float closest=float.MaxValue;bool found=false;
            for(int i=0;i<count;i++)if(!Ignore(hits[i].collider)&&hits[i].normal.y<.6f&&hits[i].distance<closest)
            {closest=hits[i].distance;hit=hits[i];found=true;}
            return found;
        }
    }
}
