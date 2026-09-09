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
    ///  - Follow    : 평소 플레이. 플레이어를 느슨하게 따라감
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
        public enum CameraMode { Follow, Overview, Fixed, Cinematic }

        [Header("추적 대상")]
        public Transform target;

        [Header("모드")]
        [SerializeField] private CameraMode mode = CameraMode.Follow;

        [Header("전체 조망 (Overview)")]
        [Tooltip("이 오브젝트 아래의 모든 렌더러가 화면에 들어오도록 맞춘다. 보통 Map 루트")]
        [SerializeField] private Transform frameTarget;

        [Tooltip("가장자리 여백. 1.0이면 딱 맞고, 1.15면 15% 여유")]
        [SerializeField] private float framePadding = 1.12f;

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
                SetMode(mode == CameraMode.Overview ? CameraMode.Follow : CameraMode.Overview);
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

            // 카메라 기준 좌표로 8개 모서리를 옮겨서 필요한 화면 크기를 구한다
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
            float needed = Mathf.Max(maxY, maxX / aspect) * framePadding;

            focus = bounds.center;
            orthoSize = Mathf.Lerp(orthoSize, needed, 1f - Mathf.Exp(-rotateSpeed * Time.deltaTime));

            // 뒤쪽 오브젝트가 카메라 뒤로 넘어가지 않게 거리를 넉넉히
            distance = Mathf.Max(40f, bounds.size.magnitude * 2f);
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
