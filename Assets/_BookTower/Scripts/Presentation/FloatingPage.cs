using UnityEngine;

namespace BookTower.Presentation
{
    /// <summary>
    /// 공중을 천천히 흘러가는 낱장.
    ///
    /// 목적은 예쁘게 만드는 게 아니라 **스케일 감**이다.
    /// 배경에 움직이는 작은 것이 있어야 탑이 얼마나 큰지 읽힌다.
    /// 끝까지 흘러가면 반대편으로 되돌려 계속 순환시킨다.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingPage : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private Vector3 drift = new(0.6f, 0.08f, 0f);
        [Tooltip("이 거리를 넘어가면 반대편으로 되돌린다")]
        [SerializeField] private float wrapDistance = 60f;

        [Header("회전")]
        [SerializeField] private Vector3 spin = new(8f, 14f, 5f);

        [Header("흔들림")]
        [SerializeField] private float swayAmount = 0.4f;
        [SerializeField] private float swaySpeed = 0.7f;

        private Vector3 origin;
        private float phase;

        private void Start()
        {
            origin = transform.position;
            phase = Random.value * Mathf.PI * 2f;

            // 개체마다 조금씩 달라야 반복처럼 안 보인다
            float variance = Random.Range(0.7f, 1.3f);
            drift *= variance;
            spin *= Random.Range(0.6f, 1.4f);
        }

        private void Update()
        {
            transform.position += drift * Time.deltaTime;

            // 좌우로 살랑이는 느낌
            float sway = Mathf.Sin(Time.time * swaySpeed + phase) * swayAmount * Time.deltaTime;
            transform.position += new Vector3(0f, 0f, sway);

            transform.Rotate(spin * Time.deltaTime, Space.Self);

            if (Vector3.Distance(transform.position, origin) > wrapDistance)
                transform.position = origin - drift.normalized * wrapDistance * 0.5f;
        }
    }
}
