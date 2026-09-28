using UnityEngine;
using BookTower.UI;
namespace BookTower.Rooms
{
    public sealed class StorybookPaperMotion : MonoBehaviour
    {
        public Transform[] papers;
        Quaternion[] rest;
        void Awake(){rest=new Quaternion[papers.Length];for(int i=0;i<papers.Length;i++)if(papers[i])rest[i]=papers[i].localRotation;}
        void Update(){float amount=PaperSettingsRuntime.Values.reduceStoryMotion?0:1;for(int i=0;i<papers.Length;i++)if(papers[i])papers[i].localRotation=rest[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*.65f+i)*3*amount,Mathf.Sin(Time.time*.8f+i*1.3f)*2*amount);}
    }
}
