using UnityEngine;
using BookTower.PlayerSystem;

namespace BookTower.Presentation
{
    /// <summary>
    /// 책갈피새의 움직임. 뼈대가 없다.
    ///
    /// 아트 문서(Docs/02 §5)가 못 박은 규칙을 그대로 코드로 옮긴 것이다.
    ///   · "뼈대 없이 **종이 접힘 단위로 회전**. 부자연스러운 게 아니라 그것이 이 세계의 물리"
    ///   · "눈·표정 없음. **고개 각도, 날개 접힘 정도, 정지 시간**으로만"
    ///
    /// 그래서 고개와 날개는 **정해진 몇 단계 사이를 툭툭 건너뛴다.** 보간하지 않는다.
    /// 반대로 공중에 뜬 몸 전체의 오르내림과 리본의 흔들림은 부드럽다 —
    /// 그건 종이의 접힘이 아니라 공기의 일이라 규칙에 어긋나지 않는다.
    ///
    /// 16프레임을 구워 두지 않고 각도로 돌리는 이유: 3D 모델이라 그럴 필요가 없고,
    /// 프레임을 구우면 카메라 4방향 중 한 각도에서만 맞는 문제가 되살아난다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PaperBirdRig : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform wingL;
        [SerializeField] private Transform wingR;
        [SerializeField] private Transform tail;

        [Header("접힘 단계 — 보간하지 않는다")]
        [Tooltip("고개 각도 단계. 각도 사이를 부드럽게 잇지 않고 건너뛴다")]
        [SerializeField] private float[] headSteps = { -17f, 0f, 14f };

        [Tooltip("날개가 들리는 단계. 0=접힘, 마지막=활짝")]
        [SerializeField] private float[] wingSteps = { 4f, -22f, -46f, -68f };

        [Header("공기의 일 — 여기는 부드러워도 된다")]
        [SerializeField] private float bobHeight = 0.045f;
        [SerializeField] private float bobSpeed = 1.9f;
        [SerializeField] private float tailSway = 7f;

        private PlayerController player;
        private Vector3 restPos;
        private float holdUntil;
        private int headIdx = 1;
        private float lean;

        public void Wire(Transform h, Transform wl, Transform wr, Transform t)
        {
            head = h; wingL = wl; wingR = wr; tail = t;
        }

        private void Awake()
        {
            restPos = transform.localPosition;
            player = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            float speed = player != null ? player.HorizontalVelocity.magnitude : 0f;
            bool moving = speed > 0.35f;
            bool airborne = player != null && !player.Controller.isGrounded;

            // ── 고개: 정지 시간이 곧 표정이다.
            //    움직일 땐 앞으로 숙이고, 서 있을 땐 오래 멈췄다가 한 번 툭 돌린다.
            int want;
            if (moving) want = 2;
            else if (Time.time >= holdUntil)
            {
                want = headIdx == 1 ? (Random.value < 0.5f ? 0 : 2) : 1;
                holdUntil = Time.time + (want == 1 ? Random.Range(1.6f, 3.4f) : Random.Range(0.5f, 1.1f));
            }
            else want = headIdx;

            headIdx = Mathf.Clamp(want, 0, headSteps.Length - 1);
            if (head != null)
                head.localRotation = Quaternion.Euler(headSteps[headIdx], 0f, 0f);

            // ── 날개: 땅에서는 접고, 뜨면 편다. 단계 사이를 건너뛴다
            int w = airborne ? (speed > 3f ? 3 : 2) : (moving ? 1 : 0);
            float wa = wingSteps[Mathf.Clamp(w, 0, wingSteps.Length - 1)];
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, wa);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, -wa);

            // ── 몸통의 부유와 리본 — 공기의 일이라 부드럽다
            float bob = moving ? 0f : Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            lean = Mathf.Lerp(lean, moving ? 9f : 0f, Time.deltaTime * 6f);
            transform.localPosition = restPos + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(lean, 0f, 0f);

            if (tail != null)
            {
                float sway = Mathf.Sin(Time.time * (moving ? 5.2f : 1.4f)) * tailSway;
                tail.localRotation = Quaternion.Euler(-lean * 0.6f + sway * 0.35f, sway, 0f);
            }
        }
    }
}
