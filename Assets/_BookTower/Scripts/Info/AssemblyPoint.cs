using UnityEngine;
using BookTower.Puzzles;

namespace BookTower.Info
{
    /// <summary>
    /// 모은 조각을 맞추는 자리 — 표지의 걸쇠, 발자국 그림, 복원대.
    ///
    /// 왜 필요한가
    ///   원래는 조각을 다 모은 채로 이 자리에 **서 있기만 하면** 저절로 풀렸다.
    ///   그런데 "가만히 서 있으니 풀렸다"는 플레이어의 행동이 아니다.
    ///   자기가 뭘 했는지 모르는 채로 퍼즐이 끝나 버린다.
    ///
    ///   "맞춘다"는 동사가 실제 동작이 되려면 누르는 순간이 있어야 한다.
    ///   그래야 조각을 줍는 것과 맞추는 것이 같은 문법(좌클릭)으로 이어진다.
    ///
    /// 조각이 모자란 채로 눌러도 반응한다 — 몇 개가 남았는지 알려준다.
    /// 아무 반응이 없으면 플레이어는 자리를 잘못 찾은 줄 안다.
    /// </summary>
    [DisallowMultipleComponent]
    public class AssemblyPoint : MonoBehaviour
    {
        [SerializeField] private PieceAssemblyPuzzle puzzle;

        [Tooltip("이 거리 안에서 눌러야 맞출 수 있다")]
        [SerializeField] private float range = 5f;

        public float Range => range;

        public bool TryAssemble(Vector3 from)
        {
            if (puzzle == null) return false;
            if (Vector3.Distance(from, transform.position) > range)
            {
                Debug.Log($"[맞추기] 너무 멀다 — 더 가까이 가서 누를 것", this);
                return true;     // 눌린 건 맞으므로 다른 처리로 넘기지 않는다
            }

            puzzle.RequestAssemble();
            return true;
        }
    }
}
