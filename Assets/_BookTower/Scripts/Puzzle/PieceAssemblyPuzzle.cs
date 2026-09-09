using System.Collections.Generic;
using UnityEngine;
using BookTower.Info;

namespace BookTower.Puzzles
{
    /// <summary>
    /// 흩어진 조각을 모아야 풀리는 퍼즐 (P5 다급한 발자국, P13 메타퍼즐).
    ///
    /// "정보 = 열쇠" 기믹의 판정부.
    /// 레드 헤링이 섞여 있으므로 **전부 모으는 게 아니라 골라내야** 한다.
    /// </summary>
    public class PieceAssemblyPuzzle : PuzzleBase
    {
        [Header("필요한 조각 id")]
        [Tooltip("이 조각들을 전부 모으면 풀린다. 가짜(decoy)는 여기 넣지 않는다")]
        [SerializeField] private List<string> requiredPieceIds = new();

        [Header("피드백")]
        [Tooltip("켜면 '구간(초·중·후반)'까지만 알려준다. " +
                 "정확한 위치를 알려주면 한 자리씩 시행착오로 뚫려 난이도가 무너진다")]
        [SerializeField] private bool partialFeedbackOnly = true;

        private int lastReported = -1;

        protected override bool CheckSolved()
        {
            var box = Keepsakes.Instance;
            if (box == null || requiredPieceIds.Count == 0) return false;

            int have = 0;
            foreach (string id in requiredPieceIds)
                if (box.Has(id)) have++;

            if (partialFeedbackOnly && have != lastReported)
            {
                lastReported = have;
                ReportBand(have, requiredPieceIds.Count);
            }

            return have == requiredPieceIds.Count;
        }

        /// <summary>정확한 개수 대신 구간만 알려준다</summary>
        private void ReportBand(int have, int total)
        {
            if (have == 0) return;

            float r = (float)have / total;
            string band = r < 0.34f ? "초반" : r < 0.67f ? "중반" : "후반";
            Debug.Log($"[Puzzle {PuzzleId}] 복원 {band}부까지 왔다");
        }

        protected override void OnActivated()
        {
            Keepsakes.EnsureExists();
            if (requiredPieceIds.Count == 0)
                Debug.LogError($"[Puzzle {PuzzleId}] 필요한 조각 목록이 비어 있음", this);
        }
    }
}
