using System;
using UnityEngine;
using BookTower.Fold;

namespace BookTower.Core
{
    /// <summary>게임 전체 페이즈</summary>
    public enum GamePhase
    {
        Boot,      // 초기화
        Playing,   // 조작 가능
        Paused,    // 일시정지
        Cutscene,  // 연출 재생 중 (입력 차단)
        Ending     // 엔딩 진행 중
    }

    /// <summary>
    /// 시스템 간 약결합 통신용 이벤트 허브.
    ///
    /// 규칙:
    ///  - 시스템끼리 직접 참조하지 말고 여기를 거친다 (플레이어가 퍼즐을 몰라도 되게)
    ///  - 구독한 쪽은 OnDisable에서 반드시 해제한다
    ///  - 게임 상태를 여기 저장하지 않는다. 알림만 하는 통로다
    /// </summary>
    public static class GameEvents
    {
        // ---------------- 접힘 ----------------

        /// <summary>접이면 하나의 상태가 바뀜</summary>
        public static event Action<FoldPanel> FoldChanged;

        // ---------------- 퍼즐 ----------------

        /// <summary>퍼즐이 활성화됨 (플레이어가 구역에 들어옴)</summary>
        public static event Action<MonoBehaviour> PuzzleActivated;

        /// <summary>퍼즐이 풀림</summary>
        public static event Action<MonoBehaviour> PuzzleSolved;

        /// <summary>
        /// 힌트 단계가 올라감 (1=미세한 떨림, 2=빛 기울기, 3=자동 반접힘).
        /// 기획서의 힌트 에스컬레이션 규칙을 그대로 구현한다.
        /// </summary>
        public static event Action<MonoBehaviour, int> HintEscalated;

        // ---------------- 정보 ----------------

        /// <summary>조각을 유품함에 넣음 (pieceId)</summary>
        public static event Action<string> PieceCollected;

        // ---------------- 이동 ----------------

        /// <summary>막힌 길(RampGate)이 열림</summary>
        public static event Action<MonoBehaviour> GateOpened;

        /// <summary>땅 위에서 일정 거리마다 한 번 — 발소리 타이밍용. 위치만 넘긴다</summary>
        public static event Action<Vector3> Footstep;

        // ---------------- 게임 흐름 ----------------

        /// <summary>게임 페이즈가 바뀜</summary>
        public static event Action<GamePhase> PhaseChanged;

        // ---------------- 발신 ----------------

        public static void RaiseFoldChanged(FoldPanel panel) => FoldChanged?.Invoke(panel);
        public static void RaisePuzzleActivated(MonoBehaviour puzzle) => PuzzleActivated?.Invoke(puzzle);
        public static void RaisePuzzleSolved(MonoBehaviour puzzle) => PuzzleSolved?.Invoke(puzzle);
        public static void RaiseHintEscalated(MonoBehaviour puzzle, int level) => HintEscalated?.Invoke(puzzle, level);
        public static void RaisePieceCollected(string pieceId) => PieceCollected?.Invoke(pieceId);
        public static void RaiseGateOpened(MonoBehaviour gate) => GateOpened?.Invoke(gate);
        public static void RaiseFootstep(Vector3 pos) => Footstep?.Invoke(pos);
        public static void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);

        /// <summary>
        /// 정적 이벤트는 플레이 모드를 나가도 구독이 남을 수 있다.
        /// (특히 Enter Play Mode Options에서 Domain Reload를 끈 경우)
        /// 그래서 플레이 시작마다 싹 비운다 — 안 하면 "이전 실행의 유령 구독자"가 남아
        /// 재현 안 되는 버그가 생긴다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            FoldChanged = null;
            PuzzleActivated = null;
            PuzzleSolved = null;
            HintEscalated = null;
            PieceCollected = null;
            GateOpened = null;
            Footstep = null;
            PhaseChanged = null;
        }
    }
}
