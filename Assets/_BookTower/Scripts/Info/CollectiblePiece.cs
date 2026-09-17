using UnityEngine;

namespace BookTower.Info
{
    /// <summary>
    /// 주워서 유품함에 넣는 조각.
    ///
    /// 운반 규칙(플레이어 스펙 5): 한 번에 하나만 물지만,
    /// **유품함에 넣으면 보관되어 다시 물고 다닐 필요가 없다.**
    /// 운반은 노동이 아니라 마지막 한 동작이어야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class CollectiblePiece : MonoBehaviour
    {
        [SerializeField] private string pieceId = "";
        [SerializeField] private string label = "";
        [SerializeField] private string areaId = "";

        [Tooltip("가짜 단서인가. 겉으로는 구분되지 않아야 하고, " +
                 "종이 결·찢긴 방향 같은 질감으로만 판별되게 한다 (색 아님)")]
        [SerializeField] private bool isDecoy = false;

        [Tooltip("이 거리 안에서 클릭하면 주울 수 있다")]
        [SerializeField] private float pickupRange = 3f;

        [Tooltip("주운 뒤 오브젝트를 숨길지")]
        [SerializeField] private bool hideOnPickup = true;

        public string PieceId => pieceId;
        public float PickupRange => pickupRange;

        public bool TryPickup(Vector3 from)
        {
            if (Vector3.Distance(from, transform.position) > pickupRange) return false;

            var box = Keepsakes.Instance;
            if (box == null) box = Keepsakes.EnsureExists();

            bool ok = box.Collect(new Keepsake
            {
                id = string.IsNullOrEmpty(pieceId) ? name : pieceId,
                label = string.IsNullOrEmpty(label) ? name : label,
                areaId = areaId,
                isDecoy = isDecoy
            });

            if (ok)
            {
                Core.GameEvents.RaisePieceCollected(pieceId);
                if (hideOnPickup) gameObject.SetActive(false);
            }
            return ok;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.78f, 0.47f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}
