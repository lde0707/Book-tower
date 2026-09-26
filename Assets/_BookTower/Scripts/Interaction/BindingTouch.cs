using UnityEngine;
using BookTower.Rooms;
namespace BookTower.Interaction
{
    [RequireComponent(typeof(Interactable))]
    public sealed class BindingTouch : MonoBehaviour
    {
        public enum TouchKind { PullKnot, TapBook, BrushDust, FlutterPage }
        public TouchKind kind;
        public Transform moving,dust,stamp;
        public BindingPlayground playground;
        public int index;
        public AudioClip sound;
        public bool IsLatched {get;private set;}
        public bool IsRevealed {get;private set;}
        public int UseCount {get;private set;}
        public float LastUsed {get;private set;}=-100;
        float revealedAt;Vector3 home;Quaternion rotation;AudioSource source;Room room;
        void Awake()
        {
            room=GetComponentInParent<Room>();if(moving){home=moving.localPosition;rotation=moving.localRotation;}
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.minDistance=1.5f;source.maxDistance=16;source.rolloffMode=AudioRolloffMode.Linear;source.volume=.38f;BookTower.UI.PaperAudioLevel.Bind(source);
        }
        public bool TryUse(Vector3 actor)
        {
            if(!isActiveAndEnabled||(room&&!room.IsVisible)||Vector3.Distance(actor,transform.position)>GetComponent<Interactable>().Range||Time.time-LastUsed<.28f)return false;
            LastUsed=Time.time;UseCount++;
            if(kind==TouchKind.PullKnot){IsLatched=!IsLatched;if(playground)playground.Pull(index,IsLatched);}
            if(kind==TouchKind.BrushDust&&!IsRevealed){IsRevealed=true;revealedAt=Time.time;}
            if(sound){source.pitch=kind==TouchKind.TapBook?Mathf.Pow(2,index*3/12f):1;source.PlayOneShot(sound);}
            return true;
        }
        void Update()
        {
            if(room&&!room.IsVisible){if(source.isPlaying)source.Stop();return;}
            float t=Mathf.Max(0,Time.time-LastUsed),wave=Mathf.Sin(t*17)*Mathf.Exp(-t*3.4f);
            if(moving)
            {
                moving.localPosition=home;
                moving.localRotation=rotation;
                if(kind==TouchKind.PullKnot)moving.localPosition=home+Vector3.down*(IsLatched?.30f:0)+Vector3.right*(wave*.07f);
                if(kind==TouchKind.TapBook)moving.localRotation=rotation*Quaternion.Euler(0,0,wave*7);
                if(kind==TouchKind.FlutterPage)moving.localRotation=rotation*Quaternion.Euler(wave*32,Mathf.Sin(t*11)*Mathf.Exp(-t*2)*10,0);
                if(kind==TouchKind.BrushDust&&IsRevealed)moving.localScale=Vector3.Lerp(moving.localScale,Vector3.zero,1-Mathf.Exp(-5*Time.deltaTime));
            }
            if(dust)dust.localScale=IsRevealed?Vector3.one*Mathf.Clamp01(1-(Time.time-revealedAt)/1.2f):Vector3.one;
            if(stamp)stamp.localScale=Vector3.one*(IsRevealed?Mathf.Clamp01((Time.time-revealedAt)/1.0f):0);
        }
    }
}
