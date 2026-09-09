using UnityEngine;
using UnityEngine.InputSystem;
using BookTower.Info;
using BookTower.PlayerSystem;

namespace BookTower.EchoSystem
{
    /// <summary>
    /// 좌클릭 하나로 처리되는 상호작용 허브.
    ///
    /// 접이면(FoldInteractor)과 별개로, 주름 깨우기와 조각 줍기를 담당한다.
    /// 셋 다 좌클릭이라 우선순위를 한 곳에서 정한다:
    ///   조각 줍기 > 주름 깨우기 > (접이면은 FoldInteractor가 처리)
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public class EchoInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask rayMask = ~0;

        private PlayerInputReader input;
        private Camera cam;

        private void Awake() => input = GetComponent<PlayerInputReader>();

        private void Update()
        {
            if (!input.InteractPressed) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, rayMask)) return;

            // ① 조각 줍기
            var piece = hit.collider.GetComponentInParent<CollectiblePiece>();
            if (piece != null && piece.TryPickup(transform.position)) return;

            // ② 주름 깨우기
            var crease = hit.collider.GetComponentInParent<EchoCrease>();
            if (crease != null && crease.TryAwaken(transform.position)) return;
        }
    }
}
