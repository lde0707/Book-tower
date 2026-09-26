using UnityEngine;
using UnityEngine.InputSystem;
using BookTower.Info;
using BookTower.PlayerSystem;
using BookTower.Interaction;

namespace BookTower.EchoSystem
{
    /// <summary>
    /// 좌클릭 하나로 처리되는 상호작용 허브 (키보드 쪽은 TargetInteractor).
    ///
    /// 접이면(FoldInteractor)과 별개로, 주름 깨우기와 조각 줍기를 담당한다.
    /// 셋 다 좌클릭이라 우선순위를 한 곳에서 정한다:
    ///   조각 줍기 > 주름 깨우기 > 맞추기 > (접이면은 FoldInteractor가 처리)
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public class EchoInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask rayMask = ~0;

        [Tooltip("만질 수 없는 걸 클릭했을 때도 로그를 남긴다. " +
                 "왜 반응이 없는지 추적할 때만 켠다")]
        [SerializeField] private bool logMisses = false;

        private PlayerInputReader input;
        private Camera cam;

        private void Awake() => input = GetComponent<PlayerInputReader>();

        private void Update()
        {
            if (!input.PointerPressed) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = BookTower.CameraSystem.PaperCamera.AimRay(cam);
            // 숨겨진 방의 콜라이더에 가로막히지 않게 보이는 것만 고른다
            if (!InteractionRay.Cast(ray, out RaycastHit hit, 1000f, rayMask)) return;

            // ① 조각 줍기
            var piece = hit.collider.GetComponentInParent<CollectiblePiece>();
            if (piece != null)
            {
                if (piece.TryPickup(transform.position)) return;

                // 클릭은 닿았는데 안 주워졌다. 이유를 남기지 않으면
                // "줍기가 안 된다"로만 보이고 원인을 찾을 수가 없다.
                float d = Vector3.Distance(transform.position, piece.transform.position);
                Debug.Log(d > piece.PickupRange
                    ? $"[조각] 너무 멀다 — {d:F1} / {piece.PickupRange:F1}. 더 가까이 가서 클릭"
                    : $"[조각] 이미 가지고 있다 ({piece.PieceId})", piece);
                return;
            }

            // ② 주름 깨우기
            var crease = hit.collider.GetComponentInParent<EchoCrease>();
            if (crease != null)
            {
                if (crease.TryAwaken(transform.position)) return;
                Debug.Log("[주름] 깨울 수 없다 — 너무 멀거나 죽은 주름이다", crease);
                return;
            }

            // ③ 맞추는 자리 — 모은 조각을 여기서 맞춘다
            var spot = hit.collider.GetComponentInParent<AssemblyPoint>();
            if (spot != null && spot.TryAssemble(transform.position)) return;
            var beacon = hit.collider.GetComponentInParent<BookTower.Puzzles.MemoryBeacon>();
            if (beacon != null) { beacon.TryIgnite(transform.position); return; }

            if (logMisses) Debug.Log($"[상호작용] '{hit.collider.name}' 은 만질 수 있는 게 아니다");
        }
    }
}
