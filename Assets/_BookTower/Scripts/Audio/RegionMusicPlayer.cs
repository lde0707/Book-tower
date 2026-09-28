using UnityEngine;
using BookTower.Rooms;
using BookTower.UI;
namespace BookTower.Audio
{
    [DisallowMultipleComponent]
    public sealed class RegionMusicPlayer : MonoBehaviour
    {
        RegionMusicLibrary library;
        RoomManager rooms;
        readonly AudioSource[] sources=new AudioSource[3];
        readonly PaperAudioLevel[] gains=new PaperAudioLevel[3];
        readonly float[] weights=new float[3];
        int selected=-1;
        public int CurrentRegion=>selected+1;
        public AudioSource[] Sources=>sources;
        public static bool Attach(GameObject owner)
        {
            if(owner.GetComponent<RegionMusicPlayer>())return true;
            var data=Resources.Load<RegionMusicLibrary>("RegionMusic");
            if(!data||data.clips==null||data.clips.Length!=3||!data.clips[0]||!data.clips[1]||!data.clips[2])return false;
            var music=owner.AddComponent<RegionMusicPlayer>();music.library=data;music.Initialize();return true;
        }
        void Initialize()
        {
            rooms=FindFirstObjectByType<RoomManager>();
            for(int i=0;i<3;i++)
            {
                var child=new GameObject("Region music "+(i+1));child.transform.SetParent(transform,false);
                var source=child.AddComponent<AudioSource>();source.clip=library.clips[i];source.loop=true;source.playOnAwake=false;source.spatialBlend=0;source.volume=0;
                source.ignoreListenerPause=false;source.ignoreListenerVolume=false;
                PaperAudioLevel.Bind(source,PaperAudioBus.Music);sources[i]=source;gains[i]=child.GetComponent<PaperAudioLevel>();
            }
            SelectRoom(rooms&&rooms.Current?rooms.Current.RoomId:"F0");
        }
        public static int RegionFor(string id)
        {
            if(string.IsNullOrEmpty(id))return 1;
            if(id[0]=='2')return 2;
            if(id[0]=='3')return 3;
            return 1; // F0 and every room of the first chapter share the opening score.
        }
        public void SelectRoom(string id)
        {
            int next=RegionFor(id)-1;if(next==selected)return;
            selected=next;
            if(!sources[next].isPlaying)sources[next].Play();
        }
        void Update()
        {
            if(!library)return;
            if(rooms&&rooms.Current)SelectRoom(rooms.Current.RoomId);
            // Existing UI owns AudioListener.pause. Keep the blend stationary during pauses.
            if(AudioListener.pause)return;
            float step=Time.unscaledDeltaTime/Mathf.Max(.1f,library.fadeSeconds);
            for(int i=0;i<3;i++)
            {
                weights[i]=Mathf.MoveTowards(weights[i],i==selected?1:0,step);
                gains[i].baseline=library.volume*weights[i];
                if(i!=selected&&weights[i]==0&&sources[i].isPlaying)sources[i].Stop();
            }
        }
    }
}
