using UnityEngine;
using BookTower.Fold;

namespace BookTower.Puzzles
{
    /// <summary>
    /// 접이면 조합이 정답과 일치하면 풀리는 퍼즐.
    /// P6(첫 번째 봉인), P14(삼중 조건의 문) 같은 퍼즐이 이 형태다.
    ///
    /// 판정은 FoldGroup의 논리값(StateIndex 배열)만 본다 — 그림자 픽셀을 읽지 않는다.
    /// 각도를 나중에 0/30/60/90으로 바꿔도 정답 조합은 그대로 유효하다.
    /// </summary>
    public class FoldCombinationPuzzle : PuzzleBase
    {
        [Header("대상")]
        [SerializeField] private FoldGroup group;

        [Header("정답 조합")]
        [Tooltip("접이면 개수와 길이가 같아야 한다. 각 값은 0~3 (닫힘/살짝/수직/넘어감)")]
        [SerializeField] private int[] answer;

        [Header("연출")]
        [Tooltip("켜면 모든 면의 회전이 멈춘 뒤에야 정답으로 인정한다 (연출이 겹치는 걸 방지)")]
        [SerializeField] private bool requireSettled = true;

        private void Reset()
        {
            group = GetComponentInChildren<FoldGroup>();
        }

        protected override bool CheckSolved()
        {
            if (group == null) return false;
            if (requireSettled && !group.AllSettled()) return false;

            return group.Matches(answer);
        }

        protected override void OnActivated()
        {
            if (group == null)
                Debug.LogError($"[Puzzle {PuzzleId}] FoldGroup이 비어 있음", this);
            else if (answer == null || answer.Length != group.Count)
                Debug.LogError(
                    $"[Puzzle {PuzzleId}] 정답 배열 길이({answer?.Length ?? 0})가 " +
                    $"접이면 개수({group.Count})와 다름", this);
        }

        /// <summary>디버그용 — 지금 조합과 정답을 함께 출력</summary>
        [ContextMenu("현재 조합 확인")]
        private void LogCombination()
        {
            if (group == null) return;
            Debug.Log($"[Puzzle {PuzzleId}] 현재 {group.CombinationString()} / 정답 {string.Join("-", answer)}");
        }
    }
}
