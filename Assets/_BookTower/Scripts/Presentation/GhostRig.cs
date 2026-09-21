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

        [Header("팔 없애기")]
        [Tooltip("이 본들을 매 프레임 크기 0 으로 접는다. 비우면 팔이 돌아온다")]
        [SerializeField] private Transform[] foldedBones = new Transform[0];

        [Header("리본 — 공기의 일이라 부드럽다")]
        [SerializeField] private Transform[] ribbons = new Transform[0];
        [Tooltip("리본이 옆으로 흔들리는 각도")]
        [SerializeField] private float ribbonSway = 9f;
        [Tooltip("움직일 때 리본이 뒤로 들리는 각도")]
        [SerializeField] private float ribbonLift = 16f;

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
        private Quaternion[] ribbonRest;

        public void Wire(Animator a, Transform[] fold, Transform[] ribbonTails = null)
        {
            animator = a;
            foldedBones = fold ?? new Transform[0];
            ribbons = ribbonTails ?? new Transform[0];
        }

        private void Awake()
        {
            restPos = transform.localPosition;
            player = GetComponentInParent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            ribbonRest = new Quaternion[ribbons.Length];
            for (int i = 0; i < ribbons.Length; i++)
                if (ribbons[i] != null) ribbonRest[i] = ribbons[i].localRotation;
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

        /// <summary>
        /// 팔을 없앤다 — 메시를 자르지 않고 **위팔 본을 크기 0 으로 접는다.**
        ///
        /// 세 가지 방법을 실제로 렌더해 비교했다.
        ///   · 팔 면을 지우기 — 어깨에 구멍이 남아 속이 들여다보였다
        ///   · 어깨 본을 접기 — 어깨 쪽 몸통 정점까지 끌려 들어가 옆구리가 움푹 팼다
        ///   · **위팔 본을 접기** — 팔 정점이 어깨 관절 한 점으로 빨려 들어가
        ///     몸통 안에 묻힌다. 구멍도 찌그러짐도 없었다. 애니메이션 두 클립
        ///     전 구간을 6프레임씩 찍어 확인했다
        ///
        /// LateUpdate 인 이유: 애니메이터가 Update 뒤에 본을 덮어쓴다.
        /// 그보다 늦게 접어야 이긴다. 0 이 아니라 1e-4 인 이유는 행렬이
        /// 특이해지면 스키닝 경계 계산에서 NaN 이 날 수 있어서다.
        ///
        /// 덤으로 폭이 1.22 → 0.58 로 줄어 CharacterController(지름 0.60)
        /// 안에 들어온다. 팔이 벽을 뚫고 나가 보이던 문제도 같이 사라진다.
        /// </summary>
        private void LateUpdate()
        {
            for (int i = 0; i < foldedBones.Length; i++)
                if (foldedBones[i] != null)
                    foldedBones[i].localScale = new Vector3(1e-4f, 1e-4f, 1e-4f);

            SwayRibbons();
        }

        /// <summary>
        /// 리본은 목 본의 자식이라 고개를 따라간다. 거기에 바람만 얹는다.
        /// 두 가닥의 위상을 어긋나게 해서 같이 까딱이지 않게 한다.
        /// 움직일수록 뒤로 들려 속도가 눈에 보인다.
        /// </summary>
        private void SwayRibbons()
        {
            if (ribbonRest == null) return;
            float speed = player != null ? player.HorizontalVelocity.magnitude : 0f;
            float lift = Mathf.Clamp01(speed / 4f) * ribbonLift;
            float freq = speed > 0.35f ? 5.0f : 1.3f;

            for (int i = 0; i < ribbons.Length; i++)
            {
                if (ribbons[i] == null) continue;
                float ph = i * 1.7f;
                float side = Mathf.Sin(Time.time * freq + ph) * ribbonSway;
                float flap = Mathf.Sin(Time.time * freq * 1.3f + ph) * ribbonSway * 0.4f;
                ribbons[i].localRotation = ribbonRest[i] * Quaternion.Euler(-lift + flap, side, 0f);
            }
        }
    }
}
