using System.Collections.Generic;
using UnityEngine;

namespace BookTower.Rooms
{
    /// <summary>
    /// 방에 속하지 않지만 방과 함께 보였다 사라져야 하는 것 — 경사로와 출구 잠금.
    ///
    /// 왜 필요한가
    ///   룸 컬링은 Room 컴포넌트가 붙은 것만 끈다. 그런데 경사로는 두 방 **사이**에
    ///   있어서 어느 방의 자식도 아니고, 그래서 Map 밑에 그대로 붙어 있었다.
    ///   경사로 14개 + 잠금 11개 = **25개가 항상 그려지고 있었다.**
    ///
    ///   나선형 탑이라 그 25개가 화면에서 전부 겹친다. 실제로 방 발자국이
    ///   겹치는 쌍이 19개다. "다른 맵이 보여서 시야를 가린다", "공중 정원 같다"는
    ///   반응의 정체가 이것이었다. 방을 아무리 닫아도 이걸 안 끄면 그대로다.
    ///
    ///   콜라이더는 끄지 않는다. 경사로를 오르는 도중에 발밑이 사라지면 안 된다 —
    ///   Room.SetVisible 과 같은 원칙이다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomBound : MonoBehaviour
    {
        /// <summary>씬에 있는 것들. RoomManager 가 매번 찾지 않도록 모아 둔다.</summary>
        public static readonly List<RoomBound> All = new();

        [Tooltip("이 방들 중 하나가 '지금 방'일 때만 보인다")]
        [SerializeField] private string[] roomIds = new string[0];

        private Renderer[] renderers;
        private Light[] lights;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
        }

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public bool Belongs(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || roomIds == null) return false;
            foreach (string id in roomIds)
                if (id == roomId) return true;
            return false;
        }

        public void SetVisible(bool on)
        {
            if (renderers != null)
                foreach (Renderer r in renderers)
                    if (r != null) r.enabled = on;

            if (lights != null)
                foreach (Light l in lights)
                    if (l != null) l.enabled = on;
        }
    }
}
