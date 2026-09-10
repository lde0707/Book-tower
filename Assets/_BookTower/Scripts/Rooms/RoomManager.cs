using UnityEngine;
using BookTower.CameraSystem;
using BookTower.PlayerSystem;

namespace BookTower.Rooms
{
    /// <summary>
    /// 플레이어가 지금 어느 방에 있는지 판단하고, 그 방만 보이게 한다.
    ///
    /// 방 판정
    ///   1) 부피 안에 들어와 있는 방이 있으면 그 방
    ///   2) 없으면(= 경사로 위) 가장 가까운 방
    ///   경사로 중간쯤에서 자연스럽게 다음 방으로 넘어간다.
    ///   별도의 트리거 볼륨을 두지 않는 이유는, 볼륨을 하나라도 잘못 놓으면
    ///   그 방에서 화면이 안 바뀌는 버그가 조용히 생기기 때문이다.
    ///
    /// 히스테리시스를 두는 이유
    ///   경계선 위에 서 있으면 두 방의 거리가 거의 같아져서 매 프레임 번갈아
    ///   선택될 수 있다. 그러면 화면이 떨린다. 확실히 더 가까울 때만 바꾼다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PaperCamera paperCamera;

        [Header("전환")]
        [Tooltip("판정 주기(초). 매 프레임 다 돌 필요는 없다")]
        [SerializeField] private float checkInterval = 0.1f;
        [Tooltip("경사로 중심선에서 이 거리 안이면 경사로를 밟고 있는 것으로 본다. " +
                 "경사로 폭의 절반(3.2)에 키를 더한 값 정도")]
        [SerializeField] private float rampReach = 4.5f;

        private Room[] rooms;
        private RampLink[] ramps;
        private Room current;
        private int appliedYaw = -1;
        private float nextCheck;
        private bool showingAll;

        public Room Current => current;

        private void Start()
        {
            rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            ramps = FindObjectsByType<RampLink>(FindObjectsSortMode.None);

            if (player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }
            if (paperCamera == null) paperCamera = FindFirstObjectByType<PaperCamera>();

            if (rooms.Length == 0)
            {
                Debug.LogWarning("[RoomManager] 방을 하나도 못 찾았습니다. 맵이 생성되지 않았을 수 있습니다.");
                return;
            }

            foreach (Room r in rooms) r.SetVisible(false);
            Switch(Pick());
        }

        private void Update()
        {
            if (rooms == null || rooms.Length == 0) return;

            // Tab 으로 전체 조망을 켜면 방 하나만 보여선 의미가 없다.
            // 레벨 전체를 확인하려고 켜는 모드이므로 전부 보여준다.
            bool wantAll = paperCamera != null && paperCamera.Mode == PaperCamera.CameraMode.Overview;
            if (wantAll != showingAll)
            {
                showingAll = wantAll;
                foreach (Room r in rooms)
                {
                    if (r == null) continue;
                    r.SetVisible(wantAll || r == current);
                    // 천장과 앞쪽 벽을 안 걷으면 조망이 상자 더미로만 보인다
                    if (wantAll) r.ApplyYaw(paperCamera.YawStep);
                }
            }
            if (showingAll) return;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + checkInterval;
                Room next = Pick();
                if (next != null && next != current) Switch(next);
            }

            // Q/E 로 카메라를 돌리면 가려야 할 벽도 바뀐다
            if (paperCamera != null && current != null && paperCamera.YawStep != appliedYaw)
            {
                appliedYaw = paperCamera.YawStep;
                current.ApplyYaw(appliedYaw);
            }
        }

        private Room Pick()
        {
            if (player == null) return current;
            Vector3 p = player.position;

            // 1) 경사로 위에 서 있는가를 먼저 본다.
            //
            //    방의 부피는 위층까지 닿을 만큼 높다. 그래서 순서를 반대로 하면
            //    2C→2D 경사로를 오르는 도중에 아래층 1E 의 부피에 들어가 버린다.
            //    (1E 바닥 10, 경사로 높이 22 — 12 나 차이 나는데도 잡혔다.)
            //    경사로를 밟고 있다는 건 훨씬 확실한 신호이므로 이쪽을 먼저 본다.
            RampLink onRamp = null;
            float bestDist = float.MaxValue;
            foreach (RampLink l in ramps)
            {
                if (l == null) continue;
                float d = l.DistanceTo(p, out _);
                if (d < bestDist) { bestDist = d; onRamp = l; }
            }

            if (onRamp != null && bestDist <= rampReach)
            {
                Room r = Find(onRamp.RoomIdFor(p));
                if (r != null) return r;
            }

            // 2) 방 안에 있는가.
            //    나선형 탑이라 방끼리 위아래로 겹친다. 후보가 여럿이면
            //    바닥 높이가 가장 가까운 쪽이 지금 서 있는 방이다.
            Room inside = null;
            float bestGap = float.MaxValue;
            foreach (Room r in rooms)
            {
                if (r == null || !r.Contains(p)) continue;
                float gap = r.FloorGap(p);
                if (gap < bestGap) { bestGap = gap; inside = r; }
            }
            if (inside != null) return inside;

            // 3) 낙하 중이거나 경사로에서 벗어났다 — 방을 바꾸지 않는다.
            //    여기서 억지로 다른 방을 고르면 떨어지는 동안 화면이 마구 바뀐다.
            return current;
        }

        private Room Find(string id)
        {
            foreach (Room r in rooms)
                if (r != null && r.RoomId == id) return r;
            return null;
        }

        private void Switch(Room next)
        {
            if (next == null) return;

            if (current != null) current.SetVisible(false);
            current = next;
            current.SetVisible(true);

            if (paperCamera != null)
            {
                paperCamera.FrameRoom(current);
                appliedYaw = paperCamera.YawStep;
            }
            else
            {
                appliedYaw = current.CamYaw;
            }

            current.ApplyYaw(appliedYaw);
        }
    }
}
