using UnityEngine;
using BookTower.Core;

namespace BookTower.Puzzles
{
    /// <summary>
    /// 모든 퍼즐의 공통 뼈대.
    ///
    /// 상태: Locked(아직 열리지 않음) → Active(푸는 중) → Solved(해결)
    ///
    /// 설계 규칙:
    ///  - 판정은 논리값으로만 한다 (그림자 렌더 결과를 읽지 않는다)
    ///  - 되돌릴 수 없는 상태를 만들지 않는다 (소프트락 금지)
    ///  - 목표 시간(targetSeconds)은 힌트 에스컬레이션의 기준이 된다
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        public enum PuzzleState { Locked, Active, Solved }

        [Header("식별")]
        [Tooltip("기획서의 퍼즐 번호. 예: P2, P14")]
        [SerializeField] private string puzzleId = "P0";

        [Header("난이도 기준")]
        [Tooltip("기획서의 목표 시간(초). 힌트 단계는 이 값의 1.0 / 1.5 / 2.0배에서 올라간다")]
        [SerializeField] private float targetSeconds = 180f;

        [Header("활성화")]
        [Tooltip("플레이어가 이 거리 안에 들어오면 자동으로 Active가 된다. " +
                 "0이면 코드로 Activate()를 직접 불러야 한다")]
        [SerializeField] private float activateRadius = 14f;

        [Header("디버그")]
        [SerializeField] private bool logToConsole = true;

        private Transform player;

        /// <summary>플레이어. 없으면 null — 하위 퍼즐이 거리 판정에 쓴다</summary>
        protected Transform Player
        {
            get
            {
                if (player == null)
                {
                    var pc = FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();
                    if (pc != null) player = pc.transform;
                }
                return player;
            }
        }

        public string PuzzleId => puzzleId;
        public float TargetSeconds => targetSeconds;
        public PuzzleState State { get; private set; } = PuzzleState.Locked;

        /// <summary>Active가 된 뒤 흐른 시간. 힌트 판단과 QA 계측에 쓴다</summary>
        public float ActiveTime { get; private set; }

        /// <summary>현재 힌트 단계 (0 = 아직 없음, 1~3)</summary>
        public int HintLevel { get; private set; }

        protected virtual void Update()
        {
            if (State == PuzzleState.Locked)
            {
                TryActivateByProximity();
                return;
            }

            if (State != PuzzleState.Active) return;

            ActiveTime += Time.deltaTime;

            if (CheckSolved())
            {
                Solve();
                return;
            }

            UpdateHintEscalation();
        }

        /// <summary>퍼즐이 풀렸는지 판정한다. 반드시 논리값만 사용할 것</summary>
        protected abstract bool CheckSolved();

        /// <summary>
        /// 퍼즐을 켜주는 장치가 따로 없으면 영원히 Locked로 남는다.
        /// 트리거 볼륨을 일일이 놓는 대신 거리로 처리한다 — 배치 실수가 원천 차단된다.
        /// </summary>
        private void TryActivateByProximity()
        {
            if (activateRadius <= 0f) return;

            if (player == null)
            {
                var pc = FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();
                if (pc == null) return;
                player = pc.transform;
            }

            if (Vector3.Distance(player.position, transform.position) <= activateRadius)
                Activate();
        }

        public void Activate()
        {
            if (State != PuzzleState.Locked) return;

            State = PuzzleState.Active;
            ActiveTime = 0f;
            HintLevel = 0;

            OnActivated();
            GameEvents.RaisePuzzleActivated(this);

            if (logToConsole) Debug.Log($"[Puzzle {puzzleId}] 활성화");
        }

        public void Solve()
        {
            if (State == PuzzleState.Solved) return;

            State = PuzzleState.Solved;

            OnSolved();
            GameEvents.RaisePuzzleSolved(this);

            if (logToConsole)
                Debug.Log($"[Puzzle {puzzleId}] 해결 — 소요 {ActiveTime:F1}초 " +
                          $"(목표 {targetSeconds:F0}초, 힌트 {HintLevel}단계)");
        }

        /// <summary>
        /// 기획서의 3단계 힌트 규칙.
        ///   1단계(×1.0): 관련 오브젝트가 미세하게 떨림
        ///   2단계(×1.5): 빛줄기가 해당 페이지 쪽으로 기욺
        ///   3단계(×2.0): 접혀야 할 면 하나가 저절로 반쯤 접힘
        /// 실제 연출은 이 이벤트를 구독하는 쪽이 담당한다.
        /// </summary>
        private void UpdateHintEscalation()
        {
            int next = HintLevel;

            if (HintLevel < 1 && ActiveTime >= targetSeconds * 1.0f) next = 1;
            else if (HintLevel < 2 && ActiveTime >= targetSeconds * 1.5f) next = 2;
            else if (HintLevel < 3 && ActiveTime >= targetSeconds * 2.0f) next = 3;

            if (next == HintLevel) return;

            HintLevel = next;
            OnHintEscalated(HintLevel);
            GameEvents.RaiseHintEscalated(this, HintLevel);

            if (logToConsole) Debug.Log($"[Puzzle {puzzleId}] 힌트 {HintLevel}단계");
        }

        protected virtual void OnActivated() { }
        protected virtual void OnSolved() { }
        protected virtual void OnHintEscalated(int level) { }
    }
}
