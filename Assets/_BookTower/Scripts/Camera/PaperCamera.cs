using UnityEngine;
using UnityEngine.InputSystem;
using BookTower.Core;

namespace BookTower.CameraSystem
{
    /// <summary>
    /// 디오라마형 3인칭 카메라.
    ///
    /// 규칙 (플레이어 스펙 문서 5-1):
    ///  - 직교(Orthographic) 투영 — 평면감 유지 + 정렬 판정 정확도
    ///  - 회전은 90도 스냅 4방향만 (Q / E). 자유 회전 금지
    ///  - 캐릭터를 "느슨하게" 따라감 — 밀착 추적 금지, 데드존 사용
    ///
    /// 모드
    ///  - Room      : 지금 있는 방 하나를 화면에 담는다 (기본 플레이 모드)
    ///  - Follow    : 방 개념 없이 플레이어만 따라감
    ///  - Overview  : 맵 전체가 화면에 들어오게 자동으로 맞춤 (레벨 확인·퍼즐 배치용)
    ///  - Fixed     : 한 지점 고정 (특정 퍼즐 구도)
    ///  - Cinematic : 연출이 카메라를 직접 제어
    ///
    /// Tab 키로 Follow ↔ Overview 전환.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class PaperCamera : MonoBehaviour
    {
        public enum CameraMode { Follow, Overview, Fixed, Cinematic, Room }

        [Header("추적 대상")]
        public Transform target;

        [Header("모드")]
        [SerializeField] private CameraMode mode = CameraMode.Follow;

        [Header("전체 조망 (Overview)")]
        [Tooltip("이 오브젝트 아래의 모든 렌더러가 화면에 들어오도록 맞춘다. 보통 Map 루트")]
        [SerializeField] private Transform frameTarget;

        [Tooltip("가장자리 여백. 1.0이면 딱 맞고, 1.15면 15% 여유")]
        [SerializeField] private float framePadding = 1.12f;

        [Header("룸 모드")]
        [Tooltip("방이 이보다 커서 한 화면에 다 안 들어오면, 방 전체를 담는 대신 " +
                 "플레이어를 따라간다. 방 밖으로는 나가지 않는다")]
        [SerializeField] private float maxRoomOrthoSize = 16f;

        [Tooltip("큰 방에서 플레이어를 따라갈 때의 부드러움")]
        [SerializeField] private float roomFollowSpeed = 3.5f;

        [Tooltip("경사로를 건너는 동안의 최대 화면 크기. 방보다 크게는 안 잡는다 — " +
                 "Min 으로 거는 이유는 들어갈 때만 좁혀지고 나올 때 넓어지지 않게 하려는 것. " +
                 "안 그러면 경사로마다 화면이 줌인·줌아웃을 반복한다")]
        [SerializeField] private float transitOrthoSize = 11f;

        [Header("Follow 모드 뷰")]
        [SerializeField] private float followOrthoSize = 6f;

        [Tooltip("내려다보는 각도. 0에 가까울수록 완전한 2D처럼, 클수록 디오라마처럼 보인다. " +
                 "그림자가 바닥에 지는 걸 봐야 하므로 25~35도 권장")]
        [Range(0f, 60f)][SerializeField] private float pitch = 30f;

        [Header("스냅 회전")]
        [SerializeField] private float rotateSpeed = 8f;

        [Header("느슨한 추적")]
        [SerializeField] private float followSpeed = 4f;
        [Tooltip("이 거리 안에서는 따라가지 않는다 (미세 흔들림 방지)")]
        [SerializeField] private float deadZone = 0.8f;

        private Bounds roomBounds;
        private float roomPad = 1.18f;
        private bool hasRoom;
        private bool transiting;
        private CameraMode modeBeforeOverview = CameraMode.Room;

        private int yawStep;
        private Vector3 focus;
        private float orthoSize;
        private float distance = 40f;
        private Camera cam;

        /// <summary>현재 스냅 단계 (0~3)</summary>
        public int YawStep => yawStep;

        /// <summary>현재 방위각. 플레이어 입력 재매핑에 쓴다</summary>
        public float YawDegrees => yawStep * 90f;

        public CameraMode Mode => mode;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;

            orthoSize = followOrthoSize;
            if (target != null) focus = target.position;
        }

        private void Start()
        {
            if (mode == CameraMode.Overview) FitToFrameTarget();
        }

        private void LateUpdate()
        {
            if (mode == CameraMode.Cinematic) return;

            HandleInput();

            if (mode == CameraMode.Follow) FollowTarget();
            else if (mode == CameraMode.Room) FitRoom();
            else if (mode == CameraMode.Overview) FitToFrameTarget();

            ApplyTransform();
        }

        private void HandleInput()
        {
            if (GameFlow.Instance != null && !GameFlow.Instance.AcceptsPlayerInput) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame) yawStep = (yawStep + 3) % 4;  // 반시계
            if (kb.eKey.wasPressedThisFrame) yawStep = (yawStep + 1) % 4;  // 시계

            if (kb.tabKey.wasPressedThisFrame)
            {
                if (mode == CameraMode.Overview) SetMode(modeBeforeOverview);
                else { modeBeforeOverview = mode; SetMode(CameraMode.Overview); }
            }
        }

        private void FollowTarget()
        {
            orthoSize = Mathf.Lerp(orthoSize, followOrthoSize, 1f - Mathf.Exp(-rotateSpeed * Time.deltaTime));

            if (target == null) return;

            if (Vector3.Distance(focus, target.position) > deadZone)
            {
                focus = Vector3.Lerp(
                    focus,
                    target.position,
                    1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            }
        }

        /// <summary>
        /// frameTarget 아래의 모든 렌더러가 화면에 들어오도록 focus와 orthoSize를 계산한다.
        /// 카메라가 회전하면 필요한 크기도 달라지므로 매 프레임 다시 맞춘다.
        /// </summary>
        private void FitToFrameTarget()
        {
            if (frameTarget == null) return;

            var renderers = frameTarget.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float needed = RequiredOrthoSize(bounds) * framePadding;
            focus = bounds.center;
            orthoSize = Mathf.Lerp(orthoSize, needed, 1f - Mathf.Exp(-rotateSpeed * Time.deltaTime));

            distance = Mathf.Max(40f, bounds.size.magnitude * 0.75f + 15f);
        }

        /// <summary>
        /// 이 범위가 화면에 다 들어오려면 orthographicSize 가 얼마여야 하는가.
        /// 카메라가 기울어 있으므로 8개 모서리를 카메라 기준 좌표로 옮겨서 잰다.
        /// 각도가 바뀌면 필요한 크기도 바뀌기 때문에 매 프레임 다시 계산한다.
        /// </summary>
        private float RequiredOrthoSize(Bounds bounds)
        {
            Quaternion inv = Quaternion.Inverse(Quaternion.Euler(pitch, YawDegrees, 0f));
            Vector3 e = bounds.extents;
            float maxX = 0f, maxY = 0f;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);

                Vector3 local = inv * corner;
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            }

            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            return Mathf.Max(maxY, maxX / aspect);
        }

        /// <summary>
        /// 룸 모드. 방 하나를 화면에 담는다.
        /// 방이 너무 커서 한 화면에 안 들어오면 플레이어를 따라가되,
        /// 시선이 방 밖으로 나가지 않게 방 범위 안에 묶는다.
        /// </summary>
        private void FitRoom()
        {
            if (!hasRoom) { FollowTarget(); return; }

            float needed = RequiredOrthoSize(roomBounds) * roomPad;
            float wantSize = Mathf.Min(needed, maxRoomOrthoSize);

            Vector3 wantFocus = roomBounds.center;

            // 경사로를 건너는 중 — 방 구도 안에 경사로가 없으므로 플레이어를 따라간다.
            // 방 안에서는 절대 이쪽으로 오지 않는다 (RoomManager 가 방 부피 밖일 때만 켠다).
            if (transiting && target != null)
            {
                wantFocus = target.position;
                wantSize = Mathf.Min(wantSize, transitOrthoSize);
            }
            else if (needed > maxRoomOrthoSize && target != null)
            {
                Vector3 p = target.position;
                Vector3 min = roomBounds.min, max = roomBounds.max;
                wantFocus = new Vector3(
                    Mathf.Clamp(p.x, min.x, max.x),
                    Mathf.Clamp(p.y, min.y, max.y),
                    Mathf.Clamp(p.z, min.z, max.z));
            }

            float k = 1f - Mathf.Exp(-roomFollowSpeed * Time.deltaTime);
            orthoSize = Mathf.Lerp(orthoSize, wantSize, k);
            focus = Vector3.Lerp(focus, wantFocus, k);

            // 직교 카메라에서 거리는 원근에 영향을 주지 않는다.
            // 방이 카메라 뒤로 넘어가지만 않으면 되므로 딱 그만큼만 물린다.
            // 예전엔 대각선의 2배(60~137)를 썼는데, 거리 기반 효과를 쓸 때
            // 그게 그대로 화면 전체에 걸린다.
            distance = Mathf.Max(40f, roomBounds.size.magnitude * 0.75f + 15f);
        }

        /// <summary>
        /// 경사로를 건너는 동안인가. RoomManager 가 켜고 끈다.
        ///
        /// 방 구도는 방 하나를 통째로 담는데, 경사로는 두 방 **사이**에 있어서
        /// 그 구도 안에 없다. 그래서 경사로를 걷는 동안 플레이어가 화면 밖으로
        /// 나가 버린다 — 14개 경사로 중 12개가 그랬고, 2E→2F 는 구간의 77%가
        /// 화면 밖이었다. 방 안에서는 구도를 고정한 채 경사로에서만 따라간다.
        /// </summary>
        public void SetTransit(bool on) => transiting = on;

        /// <summary>RoomManager 가 방이 바뀔 때 호출한다.</summary>
        public void FrameRoom(BookTower.Rooms.Room room)
        {
            if (room == null) return;

            bool first = !hasRoom;

            roomBounds = room.WorldBounds;
            roomPad = room.CamPad;
            if (first) transiting = false;
            hasRoom = true;
            yawStep = ((room.CamYaw % 4) + 4) % 4;   // 방마다 정해둔 각도로 되돌린다
            mode = CameraMode.Room;

            // 첫 방은 즉시 맞춘다. 보간을 두면 게임 시작 직후 카메라가
            // 엉뚱한 데서 날아오는 것처럼 보인다.
            if (first)
            {
                focus = roomBounds.center;
                orthoSize = Mathf.Min(RequiredOrthoSize(roomBounds) * roomPad, maxRoomOrthoSize);
                distance = Mathf.Max(60f, roomBounds.size.magnitude * 2f);
                ApplyTransform();
            }
        }

        private void ApplyTransform()
        {
            Quaternion targetRot = Quaternion.Euler(pitch, YawDegrees, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                1f - Mathf.Exp(-rotateSpeed * Time.deltaTime));

            transform.position = focus - transform.rotation * Vector3.forward * distance;

            cam.orthographicSize = orthoSize;
            cam.farClipPlane = Mathf.Max(200f, distance * 3f);
        }

        // ---------------- 외부 제어 ----------------

        public void SetMode(CameraMode next)
        {
            mode = next;
            if (next == CameraMode.Overview) FitToFrameTarget();
        }

        /// <summary>특정 지점을 보게 고정 (퍼즐 구도용)</summary>
        public void FocusOn(Vector3 worldPoint)
        {
            focus = worldPoint;
            mode = CameraMode.Fixed;
        }

        public void ReturnToFollow() => SetMode(CameraMode.Follow);

        /// <summary>스냅 각도를 코드로 지정 (연출에서 특정 각도를 보여줄 때)</summary>
        public void SetYawStep(int step) => yawStep = ((step % 4) + 4) % 4;
    }
}
