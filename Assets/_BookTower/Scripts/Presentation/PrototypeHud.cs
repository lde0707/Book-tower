using UnityEngine;
using BookTower.Interaction;
using BookTower.Rooms;
using BookTower.Info;

namespace BookTower.Presentation
{
    /// <summary>
    /// 개발용 화면 표시. F1 로 켜고 끈다.
    ///
    /// 완성된 게임에는 UI 가 없다 — 그건 설계상 확정이다.
    /// 하지만 지금은 조작을 아는 사람이 나뿐이라 **테스트가 불가능하다.**
    /// "어떻게 하는 건지 모르겠다"가 실제로 나왔고, 그 상태로는
    /// 퍼즐이 재미있는지 어떤지 판단할 수가 없다.
    ///
    /// 그래서 조작과 현재 상태만 보여준다. 출시 빌드에서는 빼야 한다.
    /// IMGUI 를 쓰는 이유는 에셋이 하나도 필요 없어서다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private RoomManager rooms;
        private Transform player;
        private GUIStyle box, small, big;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void EnsureStyles()
        {
            if (box != null) return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.98f, 0.97f, 0.94f, 0.92f));
            bg.Apply();

            box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
            box.normal.background = bg;

            small = new GUIStyle(GUI.skin.label)
            { fontSize = 13, normal = { textColor = new Color(0.23f, 0.22f, 0.20f) } };

            big = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.30f, 0.16f) }
            };
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
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerSystem.PlayerController>();
                if (pc != null) player = pc.transform;
            }

            GUILayout.BeginArea(new Rect(12, 8, 320, 260), box);

            GUILayout.Label("조작", big);
            GUILayout.Label("WASD · 이동", small);
            GUILayout.Label("Space · 점프 (공중에서 누르면 활공)", small);
            GUILayout.Label("좌클릭 · 접기 / 줍기 / 주름 깨우기", small);
            GUILayout.Label("Q · E · 카메라 90도 회전", small);
            GUILayout.Label("Tab · 전체 조망     F1 · 이 창 끄기", small);

            GUILayout.Space(10);
            GUILayout.Label("지금", big);

            string roomName = rooms != null && rooms.Current != null
                ? rooms.Current.RoomId : "—";
            GUILayout.Label($"방 · {roomName}", small);

            int kept = Keepsakes.Instance != null ? Keepsakes.Instance.Count : 0;
            GUILayout.Label($"모은 조각 · {kept}개", small);

            if (player != null)
            {
                Interactable near = Interactable.Nearest(player.position);
                GUILayout.Label(near != null
                    ? $"가까이 · {near.Label} — 좌클릭으로 {near.Verb}"
                    : "가까이 · 없음 (빛나는 쪽으로 가보자)", small);
            }

            GUILayout.EndArea();
        }
    }
}
