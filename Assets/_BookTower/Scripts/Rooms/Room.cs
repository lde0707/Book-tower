using UnityEngine;

namespace BookTower.Rooms
{
    /// <summary>
    /// 방 하나. 탑의 구역 하나가 곧 방 하나다.
    ///
    /// 이 게임의 맵은 나선형으로 쌓아 올린 탑이라, 한 방 바로 위에
    /// 다른 방이 덮개처럼 얹혀 있다. 직교 카메라를 위에서 30도로 내려다보게
    /// 두면 위층이 카메라와 플레이어 사이를 가로막는다.
    /// (실제로 시작 지점 F0 위에는 3A 가 51x45 크기로 얹혀 있었다.)
    ///
    /// 그래서 "지금 있는 방만 그린다". 나머지는 렌더러를 끈다.
    /// 콜라이더는 그대로 두므로 물리는 계속 동작하고,
    /// 경사로를 오르는 도중에 발밑이 사라지는 일은 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public class Room : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [Tooltip("이 방의 부피. 방 트랜스폼 기준 로컬 좌표")]
        [SerializeField] private Vector3 boundsCenter;
        [SerializeField] private Vector3 boundsSize = Vector3.one;
        [Tooltip("이 방 바닥의 월드 높이. 부피가 겹치는 방을 구분하는 기준")]
        [SerializeField] private float floorY;

        [Header("카메라")]
        [Tooltip("이 방을 보여줄 기본 스냅 각도 (0~3)")]
        [SerializeField] private int camYaw;
        [Tooltip("가장자리 여백. 1.18이면 18% 여유")]
        [SerializeField] private float camPad = 1.18f;

        private Renderer[] renderers;
        private RoomWall[] walls;
        private bool visible = true;

        public string RoomId => roomId;
        public int CamYaw => camYaw;
        public float CamPad => camPad;
        public bool IsVisible => visible;
        public float FloorY => floorY;

        public Bounds WorldBounds => new(transform.position + boundsCenter, boundsSize);

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            walls = GetComponentsInChildren<RoomWall>(true);
        }

        /// <summary>
        /// 이 점이 방 안인가.
        ///
        /// 위아래로 겹치는 방이 있으므로 XZ 만으로는 부족하다.
        /// 바닥보다 살짝 아래(경사로 진입)부터 천장 위 여유까지를 이 방의 높이로 본다.
        /// 접이면 위에 올라서거나 활공 중이어도 방을 벗어난 것으로 보면 화면이 깜빡인다.
        /// </summary>
        public bool Contains(Vector3 p)
        {
            Bounds b = WorldBounds;
            if (p.x < b.min.x || p.x > b.max.x) return false;
            if (p.z < b.min.z || p.z > b.max.z) return false;
            return p.y > floorY - 2.5f && p.y < b.max.y + 5f;
        }

        /// <summary>부피가 겹치는 방이 여럿일 때, 바닥 높이가 가장 가까운 쪽이 지금 서 있는 방이다.</summary>
        public float FloorGap(Vector3 p) => Mathf.Abs(p.y - floorY);

        public void SetVisible(bool on)
        {
            if (renderers == null) return;
            visible = on;
            foreach (Renderer r in renderers)
                if (r != null) r.enabled = on;
        }

        /// <summary>카메라 각도가 바뀌면 가리는 벽도 바뀐다.</summary>
        public void ApplyYaw(int yawStep)
        {
            if (walls == null) return;
            foreach (RoomWall w in walls)
                if (w != null) w.ApplyYaw(yawStep);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.78f, 0.47f, 0.35f);
            Gizmos.DrawWireCube(transform.position + boundsCenter, boundsSize);
        }
    }
}
