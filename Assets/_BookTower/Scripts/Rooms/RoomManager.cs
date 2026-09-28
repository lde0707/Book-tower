using UnityEngine;
using BookTower.CameraSystem;
using BookTower.PlayerSystem;
using BookTower.Presentation;

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
        private RampLink standingOn;
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
            UpdateTransit();
            WarnUncovered();
        }

        /// <summary>
        /// 룸 컬링이 못 건드리는 렌더러가 남아 있으면 알려 준다.
        ///
        /// "다른 구조물이 보인다" 는 지적을 두 번 받았고, 두 번 다 원인이
        /// **컬링 대상이 아닌 오브젝트**였다. 한 번은 경사로에 RoomBound 가 없었고,
        /// 한 번은 RoomBound 는 붙었는데 그걸 켜고 끄는 줄이 없었다.
        ///
        /// 사람 눈으로 찾으면 또 놓친다. 씬을 켤 때 기계가 한 번 세게 한다.
        /// 배경과 플레이어는 원래 방에 안 속하므로 빼고 센다.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void WarnUncovered()
        {
            var loose = new System.Text.StringBuilder();
            int n = 0;

            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null) continue;
                if (r.GetComponentInParent<Room>() != null) continue;
                if (r.GetComponentInParent<RoomBound>() != null) continue;
                if (r.GetComponentInParent<BookTower.Story.PaperCompanion>() != null) continue;

                Transform top = r.transform;
                while (top.parent != null) top = top.parent;
                if (top.name == "Background" || top.name == "Player") continue;

                n++;
                if (n <= 8) loose.Append("\n  · ").Append(top.name).Append(" / ").Append(r.name);
            }

            if (n > 0)
                Debug.LogWarning($"[RoomManager] 방에도 RoomBound 에도 안 걸린 렌더러 {n}개 — " +
                                 $"이건 어느 방에 있든 항상 그려집니다.{loose}");
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
                ApplyBounds(wantAll);
            }
            if (showingAll) return;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + checkInterval;
                Room next = Pick();
                if (next != null && next != current) Switch(next);
                UpdateTransit();
            }

            // Q/E 로 카메라를 돌리면 가려야 할 벽도 바뀐다
            if (paperCamera != null && current != null && paperCamera.YawStep != appliedYaw)
            {
                appliedYaw = paperCamera.YawStep;
                foreach (Room room in rooms)
                    if (room != null && room.IsVisible) room.ApplyYaw(appliedYaw);
            ApplyBounds(false);
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

            standingOn = (onRamp != null && bestDist <= rampReach) ? onRamp : null;

            if (standingOn != null)
            {
                Room r = Find(standingOn.RoomIdFor(p));
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

        /// <summary>
        /// 경사로를 건너는 동안에는 카메라가 플레이어를 따라가야 한다.
        ///
        /// 방 구도는 방 하나를 통째로 담는데 경사로는 두 방 **사이**에 있다.
        /// 그래서 경사로를 걷는 동안 플레이어가 화면 밖으로 나갔다가
        /// 다음 방에 도착해야 다시 나타난다. 전 경사로를 재 보니
        /// 14개 중 12개가 그랬고, 2E→2F 는 구간의 77%가 화면 밖이었다.
        ///
        /// 조건을 '경사로 위'로만 잡으면 방 안 출입구 근처에서도 켜져서
        /// 퍼즐을 푸는 도중에 구도가 흔들린다. '경사로 위 + 방 부피 밖'이라야
        /// 딱 경사로 구간만 잡힌다 — 퍼즐은 전부 방 안에 있으므로
        /// 고정 구도는 조금도 건드리지 않는다.
        ///
        /// (검증: 전 경사로를 0.5% 간격으로 훑어 화면 밖으로 나가는 지점 1092개를
        ///  찾았고, 이 조건이 놓치는 것은 0개였다)
        /// </summary>
        private void UpdateTransit()
        {
            if (paperCamera == null || player == null) return;

            bool crossing = standingOn != null
                            && current != null
                            && !current.Contains(player.position);

            paperCamera.SetTransit(crossing);

            // While on the bridge, retain both endpoints so its destination never floats in blank space.
            float progress=0;
            if (standingOn != null) standingOn.DistanceTo(player.position,out progress);
            bool showEndpoints=standingOn != null && progress>.02f && progress<.98f;
            foreach (Room room in rooms)
            {
                if (room==null) continue;
                bool show=room==current || (showEndpoints &&
                    (room.RoomId==standingOn.FromRoomId || room.RoomId==standingOn.ToRoomId));
                // In a perspective interior the next room must exist through the doorway
                // before the player's feet reach the ramp. Keep only nearby linked neighbours.
                if(!show&&current!=null)
                    foreach(var link in ramps)
                        if(link!=null&&((link.FromRoomId==current.RoomId&&link.ToRoomId==room.RoomId)||
                            (link.ToRoomId==current.RoomId&&link.FromRoomId==room.RoomId))&&link.DistanceTo(player.position,out _) < 18f)
                        {show=true;break;}
                if (room.IsVisible!=show) room.SetVisible(show);
                if (show) room.ApplyYaw(paperCamera.YawStep);
            }
        }

        /// <summary>
        /// 경사로·출구 잠금을 지금 방에 맞춰 보였다 감춘다.
        ///
        /// 이것들은 Room 의 자식이 아니라 Map 밑에 그대로 붙어 있어서
        /// 룸 컬링이 못 건드렸다. 경사로 14개 + 잠금 11개 = 25개가
        /// **항상 그려지고 있었고**, 나선형 탑이라 화면에서 전부 겹쳤다.
        /// 발자국이 겹치는 방 쌍이 19개다 — "다른 맵이 보인다"의 정체다.
        /// </summary>
        private void ApplyBounds(bool showAll)
        {
            string id = current != null ? current.RoomId : null;
            foreach (RoomBound b in RoomBound.All)
                if (b != null) b.SetVisible(showAll || b.Belongs(id));
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

            bool first = current == null;
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

            // 경사로와 출구 잠금도 같이 바꿔 준다.
            //
            // 예전엔 이 줄이 없었다. ApplyBounds 는 Tab(전체 조망)을 켜고 끌 때만
            // 불렸고, 그래서 **게임을 켜자마자도, 방을 옮겨도 한 번도 안 불렸다.**
            // 경사로 14개 + 잠금 11개가 처음부터 끝까지 전부 그려지고 있었고,
            // 나선형 탑이라 그것들이 화면에서 전부 겹쳤다 —
            // "다른 구조물이 보인다" 는 지적의 정체다.
            //
            // 컬링을 켜는 지점은 여기 하나여야 한다. 방이 바뀌는 곳이 여기뿐이니
            // 여기에 붙여 두면 다시 빠질 일이 없다.
            ApplyBounds(false);

            // 층이 올라갈수록 밝아진다. 첫 방은 보간 없이 즉시 맞춘다 —
            // 안 그러면 게임 시작 직후 화면이 밝았다가 어두워진다.
            if (LightingDirector.Instance != null)
                LightingDirector.Instance.ApplyRoom(
                    current.SunIntensity, current.Ambient, current.Fog,
                    current.ShadowStrength, snap: first);
        }
    }
}
