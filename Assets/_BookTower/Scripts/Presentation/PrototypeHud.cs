using UnityEngine;
using BookTower.Interaction;
using BookTower.Rooms;
using BookTower.Info;
using BookTower.Puzzles;
using BookTower.CameraSystem;

namespace BookTower.Presentation
{
    /// <summary>
    /// 개발용 화면 표시. F1 로 켜고 끈다.
    ///
    /// 완성된 게임에는 UI 가 없다 — 그건 설계상 확정이다.
    /// 목표는 빛과 랜드마크로만 전달한다.
    ///
    /// 그런데 그 연출이 아직 없어서 **프로토타입은 아무것도 알려주지 않는다.**
    /// "시작하면 뭘 해야 할지 모르겠다"가 반복해서 나왔고, 그건 버그가 아니라
    /// 안내가 통째로 없어서였다. 조작만 띄우고 목표는 어디에도 없었다.
    ///
    /// 여기 뜨는 내용이 곧 "빛으로 전달해야 할 것"의 목록이다.
    /// 연출이 완성되면 이 창을 꺼도 플레이가 되어야 하고, 그게 검수 기준이 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private RoomManager rooms;
        private PaperCamera paperCam;
        private Transform player;
        private GUIStyle box, small, big, goalStyle;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void EnsureStyles()
        {
            if (box != null) return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.99f, 0.98f, 0.96f, 0.95f));
            bg.Apply();

            box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
            box.normal.background = bg;

            small = new GUIStyle(GUI.skin.label)
            { fontSize = 13, wordWrap = true, normal = { textColor = new Color(0.23f, 0.22f, 0.20f) } };

            big = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.30f, 0.16f) }
            };

            goalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true,
                normal = { textColor = new Color(0.12f, 0.12f, 0.14f) }
            };
        }

        /// <summary>
        /// 카메라 기준 방향을 말로 바꾼다.
        /// 좌표를 보여주면 사람이 못 읽는다. "왼쪽 앞"이면 바로 몸이 움직인다.
        /// </summary>
        private string Bearing(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return "바로 여기";

            float yaw = paperCam != null ? paperCam.YawDegrees : 0f;
            Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * d;

            string ns = local.z > 2f ? "앞" : local.z < -2f ? "뒤" : "";
            string ew = local.x > 2f ? "오른쪽" : local.x < -2f ? "왼쪽" : "";

            if (ns == "" && ew == "") return "바로 앞";
            return (ew + " " + ns).Trim();
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (!visible)
            {
                GUI.Label(new Rect(12, 8, 300, 22), "F1 — 도움말", small);
                return;
            }

            if (rooms == null) rooms = FindFirstObjectByType<RoomManager>();
            if (paperCam == null) paperCam = FindFirstObjectByType<PaperCamera>();
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerSystem.PlayerController>();
                if (pc != null) player = pc.transform;
            }

            Room room = rooms != null ? rooms.Current : null;

            GUILayout.BeginArea(new Rect(12, 8, 340, 352), box);

            // ── 지금 할 일
            GUILayout.Label("지금 할 일", big);
            GUILayout.Label(room != null && !string.IsNullOrEmpty(room.Goal)
                ? room.Goal : "—", goalStyle);

            // ── 어디로
            if (player != null && room != null)
            {
                // 손 닿는 것이 있으면 '지금 F 가 눌릴 것'을 그대로 말한다.
                // 화면에서 그 물체 하나만 세게 반응하고 있으므로 둘이 같은 걸 가리킨다.
                Interactable aimed = TargetInteractor.Current;
                Interactable target = aimed != null
                    ? aimed
                    : Interactable.NearestAny(player.position, room.transform);

                if (aimed != null)
                {
                    GUILayout.Label($"→ {aimed.Label} · F 로 {aimed.Verb}", small);
                }
                else if (target != null)
                {
                    float d = Vector3.Distance(player.position, target.transform.position);
                    string where = Bearing(player.position, target.transform.position);
                    GUILayout.Label($"→ {target.Label} · {where} {d:F0}걸음", small);
                }
                else
                {
                    // 만질 게 없으면 맞추는 자리나 출구로 보낸다
                    var pz = room.GetComponentInChildren<PuzzleBase>();
                    if (pz != null)
                    {
                        float d = Vector3.Distance(player.position, pz.transform.position);
                        GUILayout.Label($"→ 모은 걸 맞추는 자리 · "
                                        + $"{Bearing(player.position, pz.transform.position)} {d:F0}걸음", small);
                    }
                    else GUILayout.Label("→ 다음 방으로", small);
                }
            }

            GUILayout.Space(10);

            // ── 상태
            GUILayout.Label("상태", big);
            GUILayout.Label($"방 · {(room != null ? room.RoomId : "—")}", small);
            GUILayout.Label($"모은 조각 · {(Keepsakes.Instance != null ? Keepsakes.Instance.Count : 0)}개", small);

            GUILayout.Space(10);

            // ── 조작
            GUILayout.Label("조작", big);
            GUILayout.Label("WASD 이동 · Space 점프 · F 상호작용 (좌클릭도 된다)", small);
            GUILayout.Label("Shift+F 되돌려 펴기 · Q E 카메라 회전", small);
            GUILayout.Label("Tab 전체 조망 · F1 이 창", small);

            GUILayout.EndArea();
        }
    }
}
