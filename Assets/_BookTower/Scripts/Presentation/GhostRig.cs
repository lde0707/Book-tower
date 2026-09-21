using UnityEngine;
using BookTower.PlayerSystem;

namespace BookTower.Presentation
{
    /// <summary>
    /// 정령(Quaternius Ghost)의 움직임.
    ///
    /// 책갈피새의 PaperBirdRig 와 역할은 같지만 원리가 반대다.
    /// 새는 뼈대가 없어서 '접힘 단위로 툭툭 건너뛰는' 회전을 코드가 직접 줬다.
    /// 정령은 본 애니메이션이 이미 들어 있으므로 여기서는 **어떤 클립을 언제
    /// 틀지만** 정한다. 클립 안쪽은 건드리지 않는다.
    ///
    /// 이동 규칙은 바꾸지 않는다 — 걷기 + 활공 그대로이고, 발만 안 닿는다.
    /// 점프 거리·간극 폭·경사로 각도가 전부 지금 값에 맞춰 검증돼 있어서,
    /// 진짜로 중력을 약화하면 1B 끊긴길 같은 퍼즐이 통째로 무의미해진다.
    /// 그래서 '떠 있음'은 **시각적으로만** 준다 — 발끝을 바닥에서 띄우고
    /// 위아래로 천천히 흔든다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostRig : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("떠 있는 느낌 — 물리가 아니라 연출이다")]
        [Tooltip("발끝을 바닥에서 이만큼 띄운다")]
        [SerializeField] private float hover = 0.16f;
        [Tooltip("위아래 흔들림 폭")]
        [SerializeField] private float bobHeight = 0.055f;
        [SerializeField] private float bobSpeed = 1.6f;

        [Header("기울기")]
        [Tooltip("움직일 때 앞으로 기우는 각도")]
        [SerializeField] private float leanMax = 8f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private PlayerController player;
        private Vector3 restPos;
        private float lean;

        public void Wire(Animator a) { animator = a; }

        private void Awake()
        {
            restPos = transform.localPosition;
            player = GetComponentInParent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            Vector3 v = player != null ? player.HorizontalVelocity : Vector3.zero;
            float speed = v.magnitude;

            // 애니메이터는 이 값 하나만 본다. 상태 전이는 컨트롤러가 정한다 —
            // 여기서 클립을 직접 재생하면 전이가 끊겨 툭툭 튄다.
            if (animator != null) animator.SetFloat(SpeedId, speed);

            // 방향은 여기서 안 돌린다. PlayerController 가 플레이어 루트를 이미
            // 이동 방향으로 Slerp 한다. 여기서 또 돌리면 두 번 돌아가
            // 45° 로 가면 90° 를 보는 식으로 어긋난다.

            lean = Mathf.Lerp(lean, speed > 0.35f ? leanMax : 0f, Time.deltaTime * 6f);

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = restPos + Vector3.up * (hover + bob);
            transform.localRotation = Quaternion.Euler(lean, 0f, 0f);
        }
    }
}
