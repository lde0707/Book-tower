using UnityEngine;
using UnityEngine.InputSystem;
using BookTower.Fold;
using BookTower.PlayerSystem;

namespace BookTower.Interaction
{
    /// <summary>
    /// 커서로 접이면을 조작한다 (플레이어 스펙 2: 캐릭터 + 커서 모델).
    ///
    ///  - 좌클릭         : 다음 단계로 접기
    ///  - Shift + 좌클릭 : 이전 단계로 펴기
    ///  - 플레이어로부터 일정 거리 안의 접이면만 조작 가능
    ///
    /// 같은 일을 F 키로도 할 수 있다 (TargetInteractor). 이쪽은 커서가 고른 것,
    /// 저쪽은 몸이 향한 것에 작용한다. 둘 다 남겨 둔 이유는 그쪽 주석에 있다.
    ///
    /// 입력은 PlayerInputReader를 거친다 — 연출 중 입력 차단이 한 곳에서 처리되도록.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public class FoldInteractor : MonoBehaviour
    {
        [Header("조작 범위")]
        [Tooltip("플레이어에서 이 거리 안의 접이면만 조작할 수 있다")]
        [SerializeField] private float maxDistance = 7f;

        [SerializeField] private LayerMask rayMask = ~0;

        [Header("디버그")]
        [SerializeField] private bool logToConsole = true;

        private PlayerInputReader input;
        private Camera cam;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (!input.PointerPressed) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            // 숨겨진 방의 콜라이더에 가로막히지 않게 보이는 것만 고른다
            if (!InteractionRay.Cast(ray, out RaycastHit hit, 1000f, rayMask)) return;

            FoldPanel panel = hit.collider.GetComponentInParent<FoldPanel>();
            if (panel == null) return;

            if (Vector3.Distance(transform.position, hit.point) > maxDistance)
            {
                if (logToConsole) Debug.Log("[Fold] 너무 멀다 — 가까이 가서 조작할 것");
                return;
            }

            if (input.ModifierHeld) panel.CycleBackward();
            else panel.CycleForward();

            if (logToConsole)
                Debug.Log($"[Fold] {panel.name} → {panel.State} (index {panel.StateIndex})");
        }
    }
}
