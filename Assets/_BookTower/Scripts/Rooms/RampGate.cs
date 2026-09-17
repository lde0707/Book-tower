using System.Collections;
using UnityEngine;
using BookTower.Puzzles;

namespace BookTower.Rooms
{
    /// <summary>
    /// 출구를 막고 선 페이지. 그 방의 퍼즐이 풀리면 펴져서 길이 된다.
    ///
    /// 왜 필요한가
    ///   퍼즐을 풀어도 세계가 아무것도 바뀌지 않으면 플레이어는 "이걸 왜 풀지"를
    ///   느낄 수가 없다. 실제로 그렇게 만들어 두고 "재미 요소를 모르겠다"는
    ///   반응이 나왔다. OnSolved 가 로그와 조명 연출뿐이었고, 진행은 처음부터
    ///   끝까지 열려 있어서 퍼즐을 전부 건너뛰고 마지막 방까지 걸어갈 수 있었다.
    ///
    /// 왜 벽이 아니라 접힌 페이지인가
    ///   접기가 이 게임의 언어다. 잠금도 그 언어로 말해야 한다.
    ///   그래서 잠긴 상태는 '세워진 페이지'이고, 풀리면 그게 앞으로 펴져
    ///   경사로 위에 눕는다. 플레이어가 배운 동작이 그대로 보상의 모양이 된다.
    ///
    /// 이게 동시에 "여기서 뭘 해야 하나"의 답이기도 하다.
    ///   길이 막힌 걸 보면 뒤를 돌아 방을 살피게 된다.
    ///   지금 표식(marker)은 빈 오브젝트라 화면에 안 보이는데,
    ///   막힌 길은 무조건 보인다. 안내로서 훨씬 확실하다.
    ///
    /// 소프트락은 안 난다
    ///   · 퍼즐이 없는 방(2A·2D·2F)의 출구에는 아예 안 붙는다
    ///   · 모든 퍼즐은 가역이라 잘못된 상태로 굳지 않는다
    ///   · 그래도 막히면 개발용 표시(F1)의 F2 로 지금 방 퍼즐을 강제로 풀 수 있다
    /// </summary>
    [DisallowMultipleComponent]
    public class RampGate : MonoBehaviour
    {
        [Tooltip("이 퍼즐이 풀리면 열린다. 비어 있으면 처음부터 열린 길이다")]
        [SerializeField] private PuzzleBase puzzle;

        [Tooltip("닫혔을 때 세워지는 각도. 접이면과 같은 축(Vector3.left)으로 돈다")]
        [SerializeField] private float closedAngle = 90f;

        [SerializeField] private float openSeconds = 1.1f;

        [Tooltip("열릴 때 켜지는 빛의 세기. '빛이 닿는 곳만 존재한다' 규칙의 실제 구현이다")]
        [SerializeField] private float openLight = 2.4f;

        private Collider blocker;
        private Light glow;
        private bool opened;

        public bool IsOpen => opened;

        private void Awake()
        {
            blocker = GetComponentInChildren<Collider>();
            glow = GetComponentInChildren<Light>(true);

            if (glow != null) glow.intensity = 0f;

            // 퍼즐이 없으면 잠글 이유가 없다. 이 경우는 데이터 쪽 실수일 가능성이
            // 높지만, 여기서 막아 버리면 그 방은 통째로 못 지나간다. 열어 둔다.
            if (puzzle == null) { OpenNow(); return; }

            transform.localRotation = Quaternion.AngleAxis(closedAngle, Vector3.left);
        }

        private void Update()
        {
            if (opened || puzzle == null) return;
            if (puzzle.State != PuzzleBase.PuzzleState.Solved) return;

            opened = true;
            StartCoroutine(Unfold());
        }

        /// <summary>연출 없이 즉시 연다 (개발용 강제 해결·퍼즐 없는 경우)</summary>
        public void OpenNow()
        {
            opened = true;
            transform.localRotation = Quaternion.identity;
            if (blocker != null) blocker.enabled = false;
            if (glow != null) glow.intensity = openLight;
        }

        private IEnumerator Unfold()
        {
            // 콜라이더는 먼저 끈다. 페이지가 눕는 도중에 플레이어를 밀면
            // 보상이 아니라 사고처럼 보인다.
            if (blocker != null) blocker.enabled = false;
            BookTower.Core.GameEvents.RaiseGateOpened(this);

            Quaternion from = transform.localRotation;
            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                float k = t / openSeconds;
                // 처음엔 천천히, 끝에서 탁 떨어진다 — 종이가 넘어가는 느낌
                k = k * k * (3f - 2f * k);
                transform.localRotation = Quaternion.Slerp(from, Quaternion.identity, k);
                if (glow != null) glow.intensity = Mathf.Lerp(0f, openLight, k);
                yield return null;
            }

            transform.localRotation = Quaternion.identity;
            if (glow != null) glow.intensity = openLight;
        }
    }
}
