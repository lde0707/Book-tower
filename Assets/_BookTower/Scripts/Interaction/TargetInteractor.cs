using UnityEngine;
using BookTower.Fold;
using BookTower.Info;
using BookTower.EchoSystem;
using BookTower.PlayerSystem;

namespace BookTower.Interaction
{
    /// <summary>
    /// F 키 하나로 하는 상호작용.
    ///
    /// 왜 커서를 두고 키를 더하는가
    ///   플레이어 스펙 문서(04)는 "캐릭터 + 커서"를 고르면서 완전 캐릭터 조작을
    ///   이렇게 물렸다 — "접기 같은 정밀 조작이 답답해짐".
    ///   실제 배치를 세어 보니 그 걱정이 맞는 방은 16개 중 3개뿐이다.
    ///   (접이면끼리 최소 거리: 1A 14.5 · 1C 37.5 · 1E 8.6 · 3B 13.6 · HD 12.8,
    ///    접기 사거리는 7 — 8.6~13.6 인 1E·3B·HD 만 둘이 동시에 범위에 든다)
    ///
    ///   답답해지는 진짜 원인은 '키로 고른다'가 아니라 '무엇이 골렸는지 안 보인다'
    ///   였다. 그래서 고르는 규칙을 몸의 방향으로 두고, 골린 것 하나만 반응하게 했다.
    ///   플레이어는 몸을 조금 틀어서 대상을 바꾼다 — 그건 조준이 아니라 동작이다.
    ///
    /// 커서는 없애지 않았다. 둘 다 된다.
    ///   마우스는 멀리 있는 걸 즉시 집을 수 있고, F 는 화면에서 손을 뗄 필요가 없다.
    ///   하나만 남겨서 얻을 게 없다.
    ///
    /// 우선순위는 EchoInteractor 와 같게 맞춘다 —
    ///   조각 줍기 > 주름 깨우기 > 맞추기 > 접기.
    ///   같은 물체에 둘이 붙는 경우는 지금 데이터에는 없지만(맞추는 자리 3곳은
    ///   전부 box 다), 나중에 겹쳐도 두 입력이 서로 다르게 동작하면 안 된다.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public class TargetInteractor : MonoBehaviour
    {
        [Tooltip("지금 고른 것을 계속 고르려는 성향. 1.1 이면 다른 후보가 "
               + "10% 더 좋아야 바뀐다. 없으면 두 물체 사이에서 표시가 떨린다")]
        [SerializeField] private float stickiness = 1.12f;

        [Tooltip("등 뒤의 것에 붙는 거리 가중치. 1.8 이면 뒤쪽 물체는 "
               + "1.8배 멀리 있는 것처럼 친다 — 바라보는 쪽이 먼저 골린다")]
        [SerializeField] private float backPenalty = 1.8f;

        [SerializeField] private bool logToConsole = true;

        /// <summary>지금 F 를 누르면 반응할 것. 없으면 null</summary>
        public static Interactable Current { get; private set; }

        private PlayerInputReader input;

        private void Awake() => input = GetComponent<PlayerInputReader>();

        private void OnDisable()
        {
            if (Current != null) Current.SetTarget(false);
            Current = null;
        }

        private void Update()
        {
            Retarget();
            if (input.InteractPressed) Act();
        }

        /// <summary>
        /// 손 닿는 것 중 하나를 고른다.
        ///
        /// 점수는 거리지만, 등 뒤에 있으면 더 멀리 있는 것으로 친다.
        /// 그래서 두 접이면 사이에 서 있어도 '몸이 향한 쪽'이 고른 것이 된다.
        /// </summary>
        private void Retarget()
        {
            Vector3 p = transform.position;
            Vector3 fwd = transform.forward;

            Interactable best = null;
            float bestScore = float.MaxValue;

            foreach (Interactable it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.InRange) continue;
                if (!Visible(it)) continue;

                Vector3 d = it.transform.position - p;
                d.y = 0f;
                float dist = d.magnitude;

                float dot = dist > 0.01f ? Vector3.Dot(fwd, d / dist) : 1f;
                float score = dist * Mathf.Lerp(1f, backPenalty, (1f - dot) * 0.5f);

                // 지금 고른 것은 조금 유리하게 — 안 그러면 걸을 때마다 깜빡인다
                if (it == Current) score /= stickiness;

                if (score < bestScore) { bestScore = score; best = it; }
            }

            if (best == Current) return;

            if (Current != null) Current.SetTarget(false);
            Current = best;
            if (Current != null) Current.SetTarget(true);
        }

        /// <summary>
        /// 지금 화면에 그려지고 있는가.
        ///
        /// 룸 컬링은 다른 방의 렌더러만 끄고 Interactable 은 살려 둔다.
        /// 지금 레벨에 실제로 한 곳이 걸린다 — F0(표지, 첫 방)의 가장자리에서
        /// 1B 의 P3_대체용_접이면 이 6.4 에 있다 (접기 사거리 7).
        /// 안 막으면 첫 방에서 보이지도 않는 퍼즐을 접게 된다.
        /// 커서 쪽에서 InteractionRay 가 막는 것과 같은 규칙을 여기서도 건다.
        /// (Tools/level_layout.py 검사 12번이 이런 자리를 계속 찾아낸다)
        /// </summary>
        private static bool Visible(Interactable it)
        {
            var r = it.GetComponent<Renderer>();
            if (r == null) r = it.GetComponentInChildren<Renderer>();
            if (r == null) return true;
            if (!r.enabled) return false;
            return r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        private void Act()
        {
            if (Current == null)
            {
                if (logToConsole) Debug.Log("[F] 손 닿는 곳에 만질 게 없다");
                return;
            }

            GameObject go = Current.gameObject;

            var piece = go.GetComponentInParent<CollectiblePiece>();
            if (piece != null)
            {
                if (!piece.TryPickup(transform.position) && logToConsole)
                    Debug.Log($"[조각] 이미 가지고 있다 ({piece.PieceId})", piece);
                return;
            }

            var crease = go.GetComponentInParent<EchoCrease>();
            if (crease != null)
            {
                if (!crease.TryAwaken(transform.position) && logToConsole)
                    Debug.Log("[주름] 깨울 수 없다 — 죽은 주름이다", crease);
                return;
            }

            var spot = go.GetComponentInParent<AssemblyPoint>();
            if (spot != null) { spot.TryAssemble(transform.position); return; }

            var panel = go.GetComponentInParent<FoldPanel>();
            if (panel != null)
            {
                if (input.ModifierHeld) panel.CycleBackward();
                else panel.CycleForward();

                if (logToConsole)
                    Debug.Log($"[Fold] {panel.name} -> {panel.State} (index {panel.StateIndex})");
                return;
            }

            if (logToConsole)
                Debug.Log($"[F] '{go.name}' 에 붙은 처리기가 없다", go);
        }
    }
}
