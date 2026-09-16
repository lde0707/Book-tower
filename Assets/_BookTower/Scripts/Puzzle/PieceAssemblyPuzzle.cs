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

        [Header("맞추는 자리")]
        [Tooltip("0보다 크면 이 오브젝트에서 그 거리 안에 있어야 완성된다. " +
                 "이게 없으면 조각을 줍는 순간 서 있던 자리에서 그냥 풀려버려서, " +
                 "'걸쇠로 가져가 맞춘다'(P1) · '복원대에 올린다'(P13) 가 성립하지 않는다")]
        [SerializeField] private float assemblyRange = 0f;

        [Header("피드백")]
        [Tooltip("켜면 '구간(초·중·후반)'까지만 알려준다. " +
                 "정확한 위치를 알려주면 한 자리씩 시행착오로 뚫려 난이도가 무너진다")]
        [SerializeField] private bool partialFeedbackOnly = true;

        private int lastReported = -1;
        private bool assembleRequested;

        /// <summary>
        /// 맞추는 자리에서 실제로 눌렀다.
        ///
        /// 이 신호 없이 자동으로 완성되면 "서 있었더니 풀렸다"가 되어
        /// 플레이어가 자기가 뭘 했는지 모른다.
        /// </summary>
        public void RequestAssemble()
        {
            var box = Keepsakes.Instance;
            int have = 0;
            if (box != null)
                foreach (string id in requiredPieceIds)
                    if (box.Has(id)) have++;

            if (have < requiredPieceIds.Count)
            {
                // 아무 반응이 없으면 자리를 잘못 찾은 줄 안다. 몇 개 남았는지 알려준다.
                Debug.Log($"[맞추기] 아직 조각이 모자란다 — {have} / {requiredPieceIds.Count}", this);
                return;
            }
            assembleRequested = true;
        }

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

            if (have != requiredPieceIds.Count) return false;

            // 맞추는 자리가 정해져 있으면, 거기서 실제로 누를 때까지 기다린다.
            // 자동으로 풀리면 "가만히 있었더니 됐다"가 되어 행동이 사라진다.
            if (assemblyRange > 0f)
            {
                if (!assembleRequested) return false;

                Transform pl = Player;
                if (pl == null) return false;
                if (Vector3.Distance(pl.position, transform.position) > assemblyRange)
                {
                    assembleRequested = false;
                    return false;
                }
            }
            return true;
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
