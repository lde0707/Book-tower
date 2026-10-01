using UnityEngine;

namespace BookTower.Presentation
{
    /// <summary>
    /// 공중을 천천히 흘러가는 낱장.
    ///
    /// 목적은 예쁘게 만드는 게 아니라 **스케일 감**이다.
    /// 배경에 움직이는 작은 것이 있어야 탑이 얼마나 큰지 읽힌다.
    /// 제한된 공간 안에서 끊김 없는 곡선으로 흐른다.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingPage : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private Vector3 drift = new(0.6f, 0.08f, 0f);
        [Tooltip("기존 직렬화 호환 값. 순환 반경을 계산하는 데 사용한다")]
        [SerializeField] private float wrapDistance = 60f;

        [Header("회전")]
        [SerializeField] private Vector3 spin = new(8f, 14f, 5f);

        [Header("흔들림")]
        [SerializeField] private float swayAmount = 0.4f;
        [SerializeField] private float swaySpeed = 0.7f;

        private Vector3 origin;
        private float phase;
        private Quaternion rest;
        private BookTower.Rooms.Room room;
        private float clock,amount,radius;

        private void Start()
        {
            origin = transform.position;
            rest=transform.rotation;room=GetComponentInParent<BookTower.Rooms.Room>();
            phase=Mathf.Repeat(origin.x*.37f+origin.z*.21f,Mathf.PI*2);
            radius=Mathf.Min(8,wrapDistance*.12f);
            if(room){var b=room.WorldBounds;radius=Mathf.Clamp(Mathf.Min(origin.x-b.min.x,b.max.x-origin.x,origin.z-b.min.z,b.max.z-origin.z)-1,.15f,radius);}
        }

        private void Update()
        {
            if(room&&!room.IsVisible||Time.timeScale<=0)return;
            clock+=Time.deltaTime;
            amount=Mathf.MoveTowards(amount,BookTower.UI.PaperSettingsRuntime.Values.reduceStoryMotion?0:1,Time.deltaTime);
            float a=clock*Mathf.Max(.035f,drift.magnitude/Mathf.Max(1,radius))*.3f+phase;
            // Closed drift, including matching velocity at the loop boundary; no visible teleport.
            var offset=new Vector3(Mathf.Sin(a)-Mathf.Sin(phase),Mathf.Sin(clock*swaySpeed+phase)*swayAmount*.4f,(Mathf.Cos(a)-Mathf.Cos(phase))*.35f)*radius*.5f;
            transform.position=origin+offset*amount;
            transform.rotation=rest*Quaternion.Euler(Mathf.Sin(a)*spin.x*amount,Mathf.Sin(a*.7f)*spin.y*amount,Mathf.Sin(a*.9f)*spin.z*amount);
        }
    }
}
