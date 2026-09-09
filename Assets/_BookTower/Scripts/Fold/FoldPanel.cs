using UnityEngine;

namespace BookTower.Fold
{
    /// <summary>
    /// 접이면(페이지) 하나. 이산 4단계로만 접힌다.
    ///
    /// 기본 각도: 0 / 45 / 90 / 135
    ///   0   — 페이지에 납작하게 누움 (닫힘). 이 상태에선 밟고 지나갈 수 있는 바닥이 된다
    ///   45  — 살짝 일어섬
    ///   90  — 수직으로 섬
    ///   135 — 수직을 넘어 반대로 기욺
    ///
    /// [중요] 논리 상태(StateIndex)와 시각적 각도(foldAngles)는 분리되어 있다.
    /// 퍼즐 판정은 반드시 StateIndex(0~3)로 하고, 각도 값이나 렌더링 결과를 읽지 말 것.
    /// 각도는 연출·가독성을 위해 언제든 바뀔 수 있으므로 판정 기준이 되면 안 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class FoldPanel : MonoBehaviour
    {
        public enum FoldState
        {
            Closed = 0,   // 납작하게 누움
            Low = 1,      // 살짝 일어섬
            Upright = 2,  // 수직
            Over = 3      // 수직을 넘어 기욺
        }

        [Header("접힘 상태 (논리값 — 퍼즐 판정은 이것으로)")]
        [SerializeField] private FoldState state = FoldState.Closed;

        [Header("단계별 각도 (연출값 — 판정과 무관, 자유롭게 튜닝)")]
        [Tooltip("그림자가 단계마다 확실히 구분되어야 한다. " +
                 "45와 135는 높이가 같아(sin45 = sin135) 그림자 길이가 비슷할 수 있으니, " +
                 "구분이 어려우면 0/30/60/90 같은 단조 증가 배열로 바꿀 것")]
        [SerializeField] private float[] foldAngles = { 0f, 45f, 90f, 135f };

        [Header("힌지 설정")]
        [Tooltip("이 축을 중심으로 접힌다. 접히는 방향이 반대면 부호를 뒤집을 것")]
        [SerializeField] private Vector3 hingeAxis = Vector3.left;

        [Tooltip("시각적 전환 속도. 판정에는 영향 없음")]
        [SerializeField] private float tweenSpeed = 9f;

        /// <summary>현재 접힘 상태(논리)</summary>
        public FoldState State => state;

        /// <summary>0~3 정수. 퍼즐 정답 조합 비교에 쓴다</summary>
        public int StateIndex => (int)state;

        /// <summary>단계 총 개수</summary>
        public int StateCount => Mathf.Max(1, foldAngles.Length);

        /// <summary>시각적 회전이 목표에 도달했는지 (연출 대기용, 판정과 무관)</summary>
        public bool IsSettled { get; private set; } = true;

        /// <summary>이 접이면만의 변경 알림. 전역 알림은 GameEvents.FoldChanged</summary>
        public event System.Action<FoldPanel> StateChanged;

        private Quaternion baseRot;

        private void Awake()
        {
            baseRot = transform.localRotation;
            transform.localRotation = TargetRotation();   // 시작 상태 즉시 반영
        }

        public void SetState(FoldState next)
        {
            if (state == next) return;

            state = next;
            IsSettled = false;

            StateChanged?.Invoke(this);
            Core.GameEvents.RaiseFoldChanged(this);
        }

        /// <summary>다음 단계로 (마지막 다음은 0으로 — 모든 접힘은 가역)</summary>
        public void CycleForward() => SetState((FoldState)(((int)state + 1) % StateCount));

        /// <summary>이전 단계로</summary>
        public void CycleBackward() => SetState((FoldState)(((int)state + StateCount - 1) % StateCount));

        private Quaternion TargetRotation()
        {
            int i = Mathf.Clamp((int)state, 0, foldAngles.Length - 1);
            return baseRot * Quaternion.AngleAxis(foldAngles[i], hingeAxis);
        }

        private void Update()
        {
            Quaternion target = TargetRotation();

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                target,
                1f - Mathf.Exp(-tweenSpeed * Time.deltaTime));

            IsSettled = Quaternion.Angle(transform.localRotation, target) < 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 axis = transform.TransformDirection(hingeAxis.normalized) * 0.6f;
            Gizmos.DrawLine(transform.position - axis, transform.position + axis);
        }
    }
}
