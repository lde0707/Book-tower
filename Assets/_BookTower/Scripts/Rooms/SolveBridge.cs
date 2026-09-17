using System.Collections;
using UnityEngine;
using BookTower.Puzzles;

namespace BookTower.Rooms
{
    /// <summary>
    /// 퍼즐을 풀면 올라와 길이 되는 판 (P3 · 1B 의 찢겨나간 간극).
    ///
    /// "접은 페이지의 그림자가 간극을 덮는다" 의 실제 구현부다.
    /// 그림자 픽셀을 밟게 만드는 건 아직 못 하므로, 논리가 성립한 순간
    /// 그 자리에 실제 바닥이 올라오게 한다. 판정은 논리값으로만 한다는
    /// 규칙(PuzzleBase)과도 어긋나지 않는다.
    ///
    /// 잠겨 있는 동안 콜라이더를 꺼 두는 게 중요하다.
    ///   이 판은 바닥보다 0.7 아래에 있다. 콜라이더를 켜 두면 간극이
    ///   '구멍'이 아니라 '얕은 구덩이'가 되고, 빠지면 stepOffset(0.45)으로는
    ///   못 올라온다. 낙하 복귀는 안전 지점보다 15 아래로 떨어져야 걸리므로
    ///   그 사이에서 영영 갇힌다 — 죽음이 없는 게임에서 유일하게 만들 수 있는
    ///   진짜 소프트락이다. 그래서 잠겨 있을 땐 그냥 통과해 떨어지게 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public class SolveBridge : MonoBehaviour
    {
        [SerializeField] private PuzzleBase puzzle;

        [Tooltip("풀렸을 때 올라올 높이(로컬 y). 바닥 윗면과 같아야 한다")]
        [SerializeField] private float openY = 0.05f;

        [SerializeField] private float riseSeconds = 1.2f;

        private Collider col;
        private float closedY;
        private bool opened;

        public bool IsOpen => opened;

        private void Awake()
        {
            col = GetComponent<Collider>();
            closedY = transform.localPosition.y;

            if (puzzle == null) { OpenNow(); return; }
            if (col != null) col.enabled = false;     // 잠긴 동안은 통과한다
        }

        private void Update()
        {
            if (opened || puzzle == null) return;
            if (puzzle.State != PuzzleBase.PuzzleState.Solved) return;

            opened = true;
            StartCoroutine(Rise());
        }

        /// <summary>연출 없이 즉시 잇는다 (개발용 강제 해결)</summary>
        public void OpenNow()
        {
            opened = true;
            Vector3 p = transform.localPosition;
            transform.localPosition = new Vector3(p.x, openY, p.z);
            if (col != null) col.enabled = true;
        }

        private IEnumerator Rise()
        {
            Vector3 p = transform.localPosition;

            for (float t = 0f; t < riseSeconds; t += Time.deltaTime)
            {
                float k = t / riseSeconds;
                k = k * k * (3f - 2f * k);
                transform.localPosition = new Vector3(p.x, Mathf.Lerp(closedY, openY, k), p.z);
                yield return null;
            }

            transform.localPosition = new Vector3(p.x, openY, p.z);

            // 다 올라온 뒤에 켠다. 올라오는 도중에 켜면 플레이어를 밀어 올린다.
            if (col != null) col.enabled = true;
        }
    }
}
