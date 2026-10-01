using UnityEngine;
using BookTower.UI;
namespace BookTower.Rooms
{
    public sealed class StorybookPaperMotion : MonoBehaviour
    {
        public Transform[] papers;
        Quaternion[] rest;
        Room room;float clock,amount;
        void Awake(){room=GetComponentInParent<Room>();rest=new Quaternion[papers.Length];for(int i=0;i<papers.Length;i++)if(papers[i])rest[i]=papers[i].localRotation;}
        void Update(){if(room&&!room.IsVisible||Time.timeScale<=0)return;clock+=Time.deltaTime;amount=Mathf.MoveTowards(amount,PaperSettingsRuntime.Values.reduceStoryMotion?0:PaperSettingsRuntime.Values.quality==0?.45f:1,Time.deltaTime*2);for(int i=0;i<papers.Length;i++)if(papers[i])papers[i].localRotation=rest[i]*Quaternion.Euler(0,Mathf.Sin(clock*.65f+i)*1.5f*amount,Mathf.Sin(clock*.8f+i*1.3f)*amount);}
    }
}
