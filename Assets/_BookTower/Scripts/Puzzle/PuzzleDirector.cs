using System.Collections.Generic;
using UnityEngine;
using BookTower.Core;

namespace BookTower.Puzzles
{
    /// <summary>
    /// 지금 어떤 퍼즐이 진행 중인지 추적하고, 플레이테스트용 기록을 남긴다.
    ///
    /// 기획서의 "플레이테스트에서 측정할 것"을 그대로 구현한다:
    ///   퍼즐별 소요 시간 / 힌트 단계별 발동 비율 / 이탈 지점
    /// </summary>
    [DisallowMultipleComponent]
    public class PuzzleDirector : MonoBehaviour
    {
        public static PuzzleDirector Instance { get; private set; }

        /// <summary>퍼즐 한 판의 기록</summary>
        public struct Record
        {
            public string puzzleId;
            public float seconds;
            public float targetSeconds;
            public int hintLevel;
        }

        [SerializeField] private bool logReportOnQuit = true;

        private readonly List<Record> records = new();

        public PuzzleBase Active { get; private set; }
        public IReadOnlyList<Record> Records => records;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.PuzzleActivated += HandleActivated;
            GameEvents.PuzzleSolved += HandleSolved;
        }

        private void OnDisable()
        {
            GameEvents.PuzzleActivated -= HandleActivated;
            GameEvents.PuzzleSolved -= HandleSolved;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleActivated(MonoBehaviour mb)
        {
            if (mb is PuzzleBase p) Active = p;
        }

        private void HandleSolved(MonoBehaviour mb)
        {
            if (mb is not PuzzleBase p) return;

            records.Add(new Record
            {
                puzzleId = p.PuzzleId,
                seconds = p.ActiveTime,
                targetSeconds = p.TargetSeconds,
                hintLevel = p.HintLevel
            });

            if (Active == p) Active = null;
        }

        /// <summary>플레이테스트 리포트를 콘솔에 출력</summary>
        [ContextMenu("플레이테스트 리포트 출력")]
        public void LogReport()
        {
            if (records.Count == 0)
            {
                Debug.Log("[PuzzleDirector] 기록 없음");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 플레이테스트 리포트 ===");
            sb.AppendLine("퍼즐 | 소요 | 목표 | 비율 | 힌트");

            float total = 0f;
            int hinted = 0;

            foreach (Record r in records)
            {
                float ratio = r.targetSeconds > 0f ? r.seconds / r.targetSeconds : 0f;
                sb.AppendLine($"{r.puzzleId} | {r.seconds:F1}s | {r.targetSeconds:F0}s | {ratio:F2}x | {r.hintLevel}단계");

                total += r.seconds;
                if (r.hintLevel > 0) hinted++;
            }

            sb.AppendLine($"합계 {total / 60f:F1}분 · 힌트 발동 {hinted}/{records.Count}개 퍼즐");
            Debug.Log(sb.ToString());
        }

        private void OnApplicationQuit()
        {
            if (logReportOnQuit) LogReport();
        }
    }
}
