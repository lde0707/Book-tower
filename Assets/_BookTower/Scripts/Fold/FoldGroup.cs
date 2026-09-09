using System.Collections.Generic;
using UnityEngine;

namespace BookTower.Fold
{
    /// <summary>
    /// 하나의 퍼즐이 쓰는 접이면 묶음.
    /// 퍼즐은 개별 FoldPanel을 직접 알 필요 없이 이 그룹의 "조합"만 본다.
    ///
    /// 조합은 int 배열로 표현된다. 예) [0, 2, 3] = 첫 면 닫힘, 둘째 수직, 셋째 넘어감
    /// </summary>
    [DisallowMultipleComponent]
    public class FoldGroup : MonoBehaviour
    {
        [Tooltip("비워두면 자식에서 자동으로 찾는다")]
        [SerializeField] private List<FoldPanel> panels = new();

        /// <summary>묶음 안 아무 면이나 바뀌었을 때</summary>
        public event System.Action<FoldGroup> CombinationChanged;

        public int Count => panels.Count;
        public IReadOnlyList<FoldPanel> Panels => panels;

        private void Awake()
        {
            if (panels.Count == 0)
                panels.AddRange(GetComponentsInChildren<FoldPanel>(true));
        }

        private void OnEnable()
        {
            foreach (FoldPanel p in panels)
                if (p != null) p.StateChanged += HandlePanelChanged;
        }

        private void OnDisable()
        {
            foreach (FoldPanel p in panels)
                if (p != null) p.StateChanged -= HandlePanelChanged;
        }

        private void HandlePanelChanged(FoldPanel _) => CombinationChanged?.Invoke(this);

        /// <summary>현재 조합을 정수 배열로</summary>
        public int[] GetCombination()
        {
            var result = new int[panels.Count];
            for (int i = 0; i < panels.Count; i++)
                result[i] = panels[i] != null ? panels[i].StateIndex : -1;
            return result;
        }

        /// <summary>정답 배열과 일치하는지</summary>
        public bool Matches(int[] answer)
        {
            if (answer == null || answer.Length != panels.Count) return false;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null) return false;
                if (panels[i].StateIndex != answer[i]) return false;
            }
            return true;
        }

        /// <summary>모든 면의 시각적 회전이 멈췄는지 (연출 타이밍용)</summary>
        public bool AllSettled()
        {
            foreach (FoldPanel p in panels)
                if (p != null && !p.IsSettled) return false;
            return true;
        }

        /// <summary>디버그 출력용 — "0-2-3" 형태</summary>
        public string CombinationString() => string.Join("-", GetCombination());
    }
}
