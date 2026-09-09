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
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 14f;

        [Header("활공 — 상승 불가, 하강만")]
        [SerializeField] private float gravity = -18f;
        [Tooltip("활공 중 최대 하강 속도(음수). 0에 가까울수록 오래 뜬다")]
        [SerializeField] private float glideFallSpeed = -1.6f;
        [Tooltip("활공 중 추가되는 전진력")]
        [SerializeField] private float glideDrift = 1.4f;

        [Header("복귀 (죽음 없음)")]
        [Tooltip("마지막 안전 지점보다 이만큼 아래로 떨어지면 되돌린다. " +
                 "절대 높이로 잡으면 탑 위층에서 떨어질 때 낙하가 한참 이어진다")]
        [SerializeField] private float fallBelowSafePoint = 15f;
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
        public float MoveSpeed => moveSpeed;
        public float Gravity => gravity;
        public float GlideFallSpeed => glideFallSpeed;
        public float GlideDrift => glideDrift;

        /// <summary>수직 속도. 상태들이 공유한다</summary>
        public float VerticalVelocity { get; set; }

        /// <summary>현재 상태 이름 (QA·디버그용)</summary>
        public string StateName => Machine?.CurrentName ?? "(none)";

        private Vector3 safePoint;
        private float safeTimer;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Input = GetComponent<PlayerInputReader>();
            Camera = FindFirstObjectByType<PaperCamera>();

            GameFlow.EnsureExists();

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
            float yaw = Camera != null ? Camera.YawDegrees : 0f;
            return Quaternion.Euler(0f, yaw, 0f) * new Vector3(raw.x, 0f, raw.y);
        }

        /// <summary>수평 이동 + 수직 속도를 한 번에 적용</summary>
        public void MoveWith(Vector3 horizontal)
        {
            Controller.Move((horizontal + Vector3.up * VerticalVelocity) * Time.deltaTime);
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

        /// <summary>마지막 안전 지점으로 되돌린다 (죽음 아님)</summary>
        public void TeleportToSafePoint()
        {
            Controller.enabled = false;   // CharacterController를 끄고 옮겨야 위치가 먹는다
            transform.position = safePoint + Vector3.up * 0.5f;
            Controller.enabled = true;
            VerticalVelocity = 0f;
        }
    }
}
