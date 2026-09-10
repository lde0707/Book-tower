using System.Collections.Generic;
using UnityEngine;

namespace BookTower.Interaction
{
    /// <summary>
    /// 만질 수 있는 것.
    ///
    /// 무대사·무UI 게임이라 "여기 뭔가 할 수 있다"를 말로 알려줄 수 없다.
    /// 그래서 물체 자신이 반응해야 한다 — 가까이 가면 살짝 떠올랐다 내려오고,
    /// 곁에 있는 등이 밝아진다. 플레이어는 설명 없이 "이건 만지는 것"을 안다.
    ///
    /// 프로토타입 단계에서는 이게 없으면 화면에 놓인 게 전부 배경으로 보인다.
    /// 실제로 "어떻게 하는 건지 모르겠다"는 반응이 나왔고, 원인이 이것이었다.
    /// </summary>
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        /// <summary>지금 씬에 있는 것들. 개발용 표시가 가장 가까운 걸 찾을 때 쓴다.</summary>
        public static readonly List<Interactable> All = new();

        [Tooltip("무엇인지 (예: 접이면 · 조각 · 주름)")]
        [SerializeField] private string label = "물체";

        [Tooltip("무엇을 하는지 (예: 접는다 · 줍는다 · 깨운다)")]
        [SerializeField] private string verb = "누른다";

        [SerializeField] private float range = 3.5f;

        [Header("반응")]
        [Tooltip("가까이 갔을 때 떠오르는 높이")]
        [SerializeField] private float lift = 0.18f;
        [SerializeField] private float bobSpeed = 2.6f;
        [Tooltip("곁의 등이 몇 배로 밝아지는가")]
        [SerializeField] private float lightBoost = 1.8f;

        private Transform player;
        private Light nearbyLight;
        private Vector3 restPos;
        private float baseIntensity;
        private float blend;

        public string Label => label;
        public string Verb => verb;
        public float Range => range;
        public bool InRange { get; private set; }

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Start()
        {
            restPos = transform.localPosition;

            // 이 물체를 비추라고 둔 등을 찾는다. level_layout.py 가
            // 조각·주름 위에 같은 이름으로 등을 하나씩 얹어 둔다.
            Transform room = transform.parent;
            if (room != null)
            {
                Transform lights = room.Find("조명");
                if (lights != null)
                {
                    Transform l = lights.Find("빛_" + name);
                    if (l != null) nearbyLight = l.GetComponent<Light>();
                }
            }
            if (nearbyLight != null) baseIntensity = nearbyLight.intensity;
        }

        private void Update()
        {
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerSystem.PlayerController>();
                if (pc == null) return;
                player = pc.transform;
            }

            InRange = Vector3.Distance(player.position, transform.position) <= range;

            // 즉시 켜고 끄면 눈에 거슬린다. 부드럽게 오간다.
            blend = Mathf.MoveTowards(blend, InRange ? 1f : 0f, Time.deltaTime * 4f);

            float bob = InRange ? (Mathf.Sin(Time.time * bobSpeed) * 0.5f + 0.5f) : 0f;
            transform.localPosition = restPos + Vector3.up * (lift * blend * (0.6f + 0.4f * bob));

            if (nearbyLight != null)
                nearbyLight.intensity = Mathf.Lerp(baseIntensity, baseIntensity * lightBoost, blend);
        }

        /// <summary>플레이어에게 가장 가까운, 손 닿는 거리의 것. 없으면 null.</summary>
        public static Interactable Nearest(Vector3 from)
        {
            Interactable best = null;
            float bd = float.MaxValue;
            foreach (Interactable it in All)
            {
                if (it == null || !it.InRange) continue;
                float d = Vector3.Distance(from, it.transform.position);
                if (d < bd) { bd = d; best = it; }
            }
            return best;
        }
    }
}
