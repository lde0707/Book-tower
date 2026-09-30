using UnityEngine;
using BookTower.Core;
using BookTower.CameraSystem;

namespace BookTower.PlayerSystem
{
    /// <summary>
    /// 플레이어(책갈피새)의 몸통. 상태 머신을 소유하고, 상태들이 공통으로 쓰는
    /// 이동·중력·복귀 기능을 제공한다.
    ///
    /// 상태 자체의 판단은 각 State 클래스가 한다 (PlayerStates.cs).
    ///
    /// 설계 규칙 (플레이어 스펙 문서):
    ///  - 표면 구속형 이동. 자유 비행 금지
    ///  - 활공은 하강만, 상승 불가
    ///  - 죽음 없음. 떨어지면 안전 지점으로 복귀
    ///  - 입력은 카메라 방위각 기준으로 재매핑
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float walkSpeed = 3.8f;
        [SerializeField] private float runSpeed = 5.9f;
        [SerializeField] private float turnSpeed = 14f;

        [Tooltip("목표 속도까지 가속되는 데 걸리는 시간(초). 0이면 예전처럼 즉시 최고속도 — " +
                 "책갈피새는 리본이라 급출발이 아니라 관성이 붙어 미끄러지듯 움직여야 " +
                 "'정령 같다'는 느낌이 난다")]
        [SerializeField] private float accelTime = 0.14f;

        [Tooltip("입력을 놓았을 때 멈추는 데 걸리는 시간(초). 가속보다 살짝 길게 두면 " +
                 "'미끄러지다 멈춘다'는 인상이 남는다")]
        [SerializeField] private float decelTime = 0.24f;

        [Header("점프 · 활공")]
        [SerializeField] private float gravity = -18f;

        [Tooltip("점프로 오르는 높이. 이 값은 마음대로 못 올린다 — " +
                 "Tools/level_layout.py 의 점프 검증이 P3(1B의 끊긴 길)를 " +
                 "뛰어넘지 않는지 확인하고 있고, C# 과 파이썬 값이 어긋나면 " +
                 "그 검증이 아무것도 보장하지 않게 된다")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("활공 중 최대 하강 속도(음수). 0에 가까울수록 오래 뜬다")]
        [SerializeField] private float glideFallSpeed = -1.6f;

        [Tooltip("활공 중 추가되는 전진력")]
        [SerializeField] private float glideDrift = 1.4f;

        [Header("복귀 (죽음 없음)")]
        [Tooltip("마지막 안전 지점보다 이만큼 아래로 떨어지면 되돌린다. " +
                 "절대 높이로 잡으면 탑 위층에서 떨어질 때 낙하가 한참 이어진다")]
        [SerializeField] private float fallBelowSafePoint = 6f;
        [SerializeField] private float safePointInterval = 0.5f;

        [Header("디버그")]
        [SerializeField] private bool showStateInLog = false;

        // ---- 참조 ----
        public CharacterController Controller { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public PaperCamera Camera { get; private set; }

        // ---- 상태 머신 ----
        public StateMachine<PlayerController> Machine { get; private set; }

        public PlayerGroundedState Grounded { get; private set; }
        public PlayerAirborneState Airborne { get; private set; }
        public PlayerGlideState Glide { get; private set; }
        public PlayerRespawnState Respawn { get; private set; }

        // ---- 공유 값 ----
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public bool WantsToRun => Input && Input.RunHeld && Input.Move.sqrMagnitude > .01f;
        public float MoveSpeed => WantsToRun ? runSpeed : walkSpeed;
        public float Gravity => gravity;
        public float JumpHeight => jumpHeight;
        public float GlideFallSpeed => glideFallSpeed;
        public float GlideDrift => glideDrift;

        /// <summary>수직 속도. 상태들이 공유한다</summary>
        public float VerticalVelocity { get; set; }

        /// <summary>
        /// 현재 수평 속도. 상태가 바뀌어도(땅→공중 등) 이어져서 관성이 끊기지 않는다.
        /// </summary>
        public Vector3 HorizontalVelocity { get; set; }

        /// <summary>
        /// 점프로 떠 있는가.
        ///
        /// 점프 뒤에 활공을 이어 붙이면 체공이 크게 늘어나 1B 의 틈(6.4)을
        /// 그냥 넘어가고, P3 의 "그림자로 길을 잇는다"가 통째로 무의미해진다.
        /// (점프만: 4.0 · 점프+활공: 7.2 — 계산은 level_layout.py 에 있다)
        /// 그래서 점프로 뜬 동안에는 활공을 막는다.
        /// 발을 헛디뎌 떨어지는 경우는 그대로 활공할 수 있다.
        /// </summary>
        public bool JumpedThisAir { get; set; }

        /// <summary>현재 상태 이름 (QA·디버그용)</summary>
        public string StateName => Machine?.CurrentName ?? "(none)";

        [Header("발소리")]
        [Tooltip("이 거리를 걸을 때마다 발소리 이벤트를 한 번 낸다")]
        [SerializeField] private float footstepInterval = 1.6f;

        private Vector3 safePoint;
        private float safeTimer;
        private float footstepDistance;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Input = GetComponent<PlayerInputReader>();
            Camera = FindFirstObjectByType<PaperCamera>();

            // F 키 상호작용은 씬 구성기가 붙여 준다. 그런데 이미 만들어 둔 씬에는
            // 없어서, 씬을 다시 굽지 않으면 F 가 아무 반응도 안 한다.
            // "안 되는데요" 가 또 나올 자리라 여기서 한 번 더 확인한다.
            if (GetComponent<Interaction.TargetInteractor>() == null)
                gameObject.AddComponent<Interaction.TargetInteractor>();

            GameFlow.EnsureExists();
            BookTower.Audio.SfxDirector.EnsureExists();

            Grounded = new PlayerGroundedState();
            Airborne = new PlayerAirborneState();
            Glide = new PlayerGlideState();
            Respawn = new PlayerRespawnState();

            Machine = new StateMachine<PlayerController>(this);
            safePoint = transform.position;
        }

        private void Start()
        {
            Machine.Change(Grounded);

            if (showStateInLog)
                Machine.StateChanged += (prev, next) =>
                    Debug.Log($"[Player] {prev?.GetType().Name ?? "-"} → {next.GetType().Name}");
        }

        private void Update()
        {
            if (GameFlow.Instance != null && GameFlow.Instance.Phase == GamePhase.Cutscene) return;
            Machine.Tick(Time.deltaTime);

            if (transform.position.y < safePoint.y - fallBelowSafePoint && Machine.Current != Respawn)
                Machine.Change(Respawn);
        }

        // ------------------------------------------------------------ 공용 기능

        /// <summary>
        /// 입력을 카메라 방위각 기준으로 변환한 이동 방향.
        /// 이걸 빼먹으면 카메라를 90도 돌린 직후 조작 방향이 뒤집힌다.
        /// </summary>
        public Vector3 GetMoveDirection()
        {
            Vector2 raw = Input.Move;
            float yaw = Camera != null && raw.sqrMagnitude>.01f
                ? Camera.ConsumeMovementYaw()
                : Camera != null ? Camera.YawDegrees : 0f;
            return Quaternion.Euler(0f, yaw, 0f) * new Vector3(raw.x, 0f, raw.y);
        }

        /// <summary>수평 이동 + 수직 속도를 한 번에 적용</summary>
        public void MoveWith(Vector3 horizontal)
        {
            Controller.Move((horizontal + Vector3.up * VerticalVelocity) * Time.deltaTime);
        }

        /// <summary>
        /// 목표 수평 속도로 부드럽게 다가간다. 최고 속도는 절대 넘지 않는다 —
        /// Tools/level_layout.py 의 점프 도달거리 계산이 "즉시 최고속도"를 가정하고
        /// 그 값으로 1B 의 틈(6.4)을 못 넘는다는 걸 검증하고 있다. 가속 구간을 넣으면
        /// 실제 도달거리는 그 계산값보다 같거나 작아지기만 하므로(오버슈트가 없는 한)
        /// 검증은 계속 유효하다. targetVelocity 의 크기를 moveSpeed 계열 값 이하로만
        /// 넘겨야 이 전제가 지켜진다.
        /// </summary>
        public void ApproachHorizontalVelocity(Vector3 targetVelocity, float dt)
        {
            bool accelerating = targetVelocity.sqrMagnitude > HorizontalVelocity.sqrMagnitude;
            float time = accelerating ? accelTime : decelTime;
            float maxDelta = time <= 0f
                ? float.PositiveInfinity
                : (runSpeed + glideDrift) / time * dt;

            HorizontalVelocity = Vector3.MoveTowards(HorizontalVelocity, targetVelocity, maxDelta);
        }

        /// <summary>이동 방향을 바라보게 회전</summary>
        public void FaceDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        /// <summary>땅에 있을 때 주기적으로 안전 지점을 기록</summary>
        public void RecordSafePoint()
        {
            safeTimer -= Time.deltaTime;
            if (safeTimer > 0f) return;

            safePoint = transform.position;
            safeTimer = safePointInterval;
        }

        /// <summary>
        /// 걸은 거리를 누적해 일정 간격마다 발소리 이벤트를 낸다.
        /// 타이머가 아니라 거리 기준인 이유 — 제자리에 멈춰 있을 때는
        /// 발소리가 나면 안 되고, 빨리 걸으면 더 자주 나야 자연스럽다.
        /// </summary>
        public void TickFootsteps()
        {
            footstepDistance += HorizontalVelocity.magnitude * Time.deltaTime;
            if (footstepDistance < footstepInterval) return;

            footstepDistance = 0f;
            GameEvents.RaiseFootstep(transform.position);
        }

        /// <summary>마지막 안전 지점으로 되돌린다 (죽음 아님)</summary>
        public void TeleportToSafePoint()
        {
            Controller.enabled = false;   // CharacterController를 끄고 옮겨야 위치가 먹는다
            transform.position = safePoint + Vector3.up * 0.5f;
            Controller.enabled = true;
            VerticalVelocity = 0f;
            HorizontalVelocity = Vector3.zero;
            JumpedThisAir = false;
        }
    }
}
