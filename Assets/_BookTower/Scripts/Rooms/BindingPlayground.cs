using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Rooms
{
    public sealed class BindingPlayground : MonoBehaviour
    {
        public Transform[] pulses,signals,mobiles;
        public Light[] signalLights;
        public Vector3[] starts,ends;
        public PuzzleBase puzzle;
        public int PullCount {get;private set;}
        public bool CelebrationPlayed {get;private set;}
        readonly float[] startTime={-100,-100,-100};readonly bool[] latched=new bool[3];
        Quaternion[] rest;Room room;float celebration=-100;
        void Awake(){room=GetComponentInParent<Room>();rest=new Quaternion[mobiles.Length];for(int i=0;i<rest.Length;i++)rest[i]=mobiles[i].localRotation;}
        public void Pull(int index,bool held){if(index<0||index>=3)return;latched[index]=held;startTime[index]=Time.time;PullCount++;}
        void Update()
        {
            if(room&&!room.IsVisible)return;
            if(!CelebrationPlayed&&puzzle&&puzzle.State==PuzzleBase.PuzzleState.Solved){CelebrationPlayed=true;celebration=Time.time;}
            for(int i=0;i<3;i++)
            {
                float t=(Time.time-startTime[i])/1.6f;bool travelling=t>=0&&t<1;
                pulses[i].localScale=Vector3.one*(travelling?.16f:0);
                Vector3 a=Vector3.Lerp(starts[i],ends[i],Mathf.Clamp01(t));a.y+=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*.7f;pulses[i].localPosition=a;
                float arrived=t>=1&&latched[i]?1:0;float solved=CelebrationPlayed?Mathf.Clamp01((Time.time-celebration-i*.3f)*2):0;
                signalLights[i].intensity=Mathf.Lerp(signalLights[i].intensity,2+arrived*7+solved*9,Time.deltaTime*7);
                signals[i].localScale=Vector3.one*(.85f+arrived*.15f+solved*.18f);
            }
            for(int i=0;i<mobiles.Length;i++)mobiles[i].localRotation=rest[i]*Quaternion.Euler(Mathf.Sin(Time.time*.7f+i)*2,0,Mathf.Sin(Time.time*.9f+i*1.8f)*(CelebrationPlayed?5:2));
        }
    }
}
