using UnityEngine;
using BookTower.Rooms;
using BookTower.Core;
using BookTower.UI;
namespace BookTower.Presentation
{
    /// <summary>Only visual paper tips belong here. Puzzle transforms and lights remain owned by their mechanisms.</summary>
    [DisallowMultipleComponent]
    public sealed class PaperRoomMotion : MonoBehaviour
    {
        public enum MotionState { Dormant, Ambient, Reacting, Settling, Paused }
        public Transform[] tips = System.Array.Empty<Transform>();
        public float amplitude = 1.4f;
        public float period = 7.3f;
        public MotionState State { get; private set; }
        public float Phase { get; private set; }
        Quaternion[] rest;
        Room room;
        Transform viewer;
        ParticleSystem[] dust;
        bool[] dustPaused;
        float reaction, amount, proximity;
        bool wasNear;
        void Awake()
        {
            room=GetComponentInParent<Room>();
            var player=FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();viewer=player?player.transform:null;
            rest=new Quaternion[tips.Length];for(int i=0;i<tips.Length;i++)if(tips[i])rest[i]=tips[i].localRotation;
            dust=GetComponentsInChildren<ParticleSystem>(true);dustPaused=new bool[dust.Length];
            Phase=Mathf.Repeat(transform.position.x*.37f+transform.position.z*.21f,period);
        }
        void OnEnable()=>GameEvents.PuzzleSolved+=React;
        void OnDisable()=>GameEvents.PuzzleSolved-=React;
        void React(MonoBehaviour puzzle){if(puzzle&&puzzle.GetComponentInParent<Room>()==room)reaction=1;}
        void Update()
        {
            bool visible=!room||room.IsVisible;
            bool paused=Time.timeScale<=0;
            bool low=PaperSettingsRuntime.Values.quality==0;
            bool reduced=PaperSettingsRuntime.Values.reduceStoryMotion;
            float range=viewer?Vector3.Distance(viewer.position,transform.position):0;
            bool active=visible&&range<65;
            for(int i=0;i<dust.Length;i++)if(dust[i])
            {
                bool stop=!active||paused||reduced||low;
                if(stop&&dust[i].isPlaying){dust[i].Pause();dustPaused[i]=true;}
                else if(!stop&&dustPaused[i]){dust[i].Play();dustPaused[i]=false;}
            }
            if(!active){State=MotionState.Dormant;return;}
            if(paused){State=MotionState.Paused;return;}
            bool near=range<5;
            if(near&&!wasNear)proximity=.7f;wasNear=near;
            float dt=Time.deltaTime;
            Phase+=dt;
            reaction=Mathf.MoveTowards(reaction,0,dt*.35f);
            proximity=Mathf.MoveTowards(proximity,0,dt*.5f);
            amount=Mathf.MoveTowards(amount,reduced?0:low?.55f:1,dt*2);
            State=reaction>.45f?MotionState.Reacting:reaction>0?MotionState.Settling:MotionState.Ambient;
            for(int i=0;i<tips.Length;i++)if(tips[i])
            {
                float wave=Phase*(Mathf.PI*2/(period+i*.23f))+i*1.731f;
                float gain=amplitude*amount;
                float flutter=(reaction+proximity)*Mathf.Sin(Phase*8+i)*gain;
                tips[i].localRotation=rest[i]*Quaternion.Euler(Mathf.Sin(wave)*gain+flutter,Mathf.Sin(wave*.71f+i)*gain*.35f,Mathf.Sin(wave*.83f)*gain*.6f);
            }
        }
        void OnDestroy(){if(rest==null)return;for(int i=0;i<tips.Length;i++)if(tips[i])tips[i].localRotation=rest[i];}
    }
}
