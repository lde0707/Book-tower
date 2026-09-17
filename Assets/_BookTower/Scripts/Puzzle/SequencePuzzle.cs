using System.Collections.Generic;
using UnityEngine;
using BookTower.EchoSystem;
using BookTower.Presentation;

namespace BookTower.Puzzles
{
    /// <summary>
    /// 주름을 정해진 순서로 깨워야 풀리는 퍼즐 (P11 순서의 방).
    ///
    /// 설계 규칙 (QA 점검의 치명 결함 수정 반영):
    ///  - 틀려도 **단서는 절대 소실되지 않는다**. 재생만 초기화된다
    ///  - 대신 초기화에 시간이 걸린다 → 5! = 120순열 × 8초 ≈ 16분이라
    ///    브루트포스가 비합리적이 된다. 소프트락 없이 브루트포스만 억제
    /// </summary>
    public class SequencePuzzle : PuzzleBase
    {
        [Header("정답 순서")]
        [Tooltip("이 순서대로 깨워야 한다. 마모가 오래된 것부터가 기본 설계")]
        [SerializeField] private List<EchoCrease> order = new();

        [Header("실패 처리")]
        [Tooltip("틀렸을 때 초기화에 걸리는 시간. 브루트포스에 비용을 부과하는 유일한 수단")]
        [SerializeField] private float resetSeconds = 8f;

        private readonly List<EchoCrease> input = new();
        private float resetTimer;
        private bool solved;

        public int Progress => input.Count;
        public bool IsResetting => resetTimer > 0f;

        private void OnEnable()
        {
            foreach (EchoCrease c in order)
                if (c != null) c.Awakened += OnAwakened;
        }

        private void OnDisable()
        {
            foreach (EchoCrease c in order)
                if (c != null) c.Awakened -= OnAwakened;
        }

        protected override void Update()
        {
            base.Update();

            if (resetTimer > 0f)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f) input.Clear();
            }
        }

        private void OnAwakened(EchoCrease c)
        {
            if (State != PuzzleState.Active || solved || IsResetting) return;

            input.Add(c);
            int i = input.Count - 1;

            if (i >= order.Count || input[i] != order[i])
            {
                // 틀림 — 단서는 그대로 두고 입력만 되감는다
                resetTimer = resetSeconds;
                Debug.Log($"[Puzzle {PuzzleId}] 순서 틀림 ({input.Count}번째) — {resetSeconds:F0}초 후 초기화. 단서 손실 없음");
                return;
            }

            Debug.Log($"[Puzzle {PuzzleId}] 순서 {input.Count}/{order.Count}");
            if (input.Count == order.Count) solved = true;
        }

        protected override bool CheckSolved() => solved;

        protected override void OnActivated()
        {
            if (order.Count == 0)
                Debug.LogError($"[Puzzle {PuzzleId}] 정답 순서가 비어 있음", this);
        }

        /// <summary>마모 순서대로 자동 정렬 — 기획 의도(오래된 것부터)를 코드로 강제</summary>
        [ContextMenu("마모 순서로 정렬")]
        private void SortByWear()
        {
            order.RemoveAll(c => c == null);
            order.Sort((a, b) => a.Wear.CompareTo(b.Wear));
            Debug.Log($"[Puzzle {PuzzleId}] 마모가 오래된 것부터 정렬됨");
        }

        /// <summary>
        /// 다음에 깨워야 할 주름 하나를 흔들고 밝힌다. 순서를 대신 눌러주지는 않는다 —
        /// 그러면 P11 의 "마모 형태로 순서를 추리한다"는 전제가 사라진다.
        /// </summary>
        protected override void OnHintEscalated(int level)
        {
            if (solved || input.Count >= order.Count) return;

            EchoCrease next = order[input.Count];
            if (next == null) return;

            Light light = next.GetComponentInChildren<Light>();
            if (level == 1) StartCoroutine(HintFx.Tremble(next.transform));
            else if (level == 2) StartCoroutine(HintFx.PulseLight(light));
            else if (level == 3) StartCoroutine(HintFx.StrongNudge(this, next.transform, light));
        }
    }
}
