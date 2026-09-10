using UnityEngine;

namespace BookTower.Rooms
{
    /// <summary>
    /// 경사로가 잇는 두 방을 기억한다.
    ///
    /// 경사로 위에 있는 동안 플레이어는 어느 방에도 속하지 않는다.
    /// 이때 "가장 가까운 방"으로 판정하면 나선형 탑에서는 엉뚱한 방이 잡힌다.
    /// 부피가 큰 옆 층 방이 목적지보다 더 가까울 수 있기 때문이다.
    /// (2A→2B 를 오르는 중에 2E 가 선택되는 실제 사례가 있었다.)
    ///
    /// 어느 방에서 어느 방으로 가는 길인지는 레벨 데이터에 이미 있다.
    /// 추측하지 말고 그대로 쓴다. 중간을 넘으면 목적지 방으로 바꾼다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RampLink : MonoBehaviour
    {
        [SerializeField] private string fromRoomId;
        [SerializeField] private string toRoomId;
        [SerializeField] private Vector3 fromPoint;
        [SerializeField] private Vector3 toPoint;

        public string FromRoomId => fromRoomId;
        public string ToRoomId => toRoomId;

        /// <summary>이 점에서 경사로까지의 거리. t 는 시작(0)~끝(1) 사이의 위치.</summary>
        public float DistanceTo(Vector3 p, out float t)
        {
            Vector3 ab = toPoint - fromPoint;
            float len2 = ab.sqrMagnitude;

            t = len2 < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - fromPoint, ab) / len2);
            return Vector3.Distance(p, fromPoint + ab * t);
        }

        /// <summary>지금 위치라면 어느 방을 보여줘야 하는가.</summary>
        public string RoomIdFor(Vector3 p)
        {
            DistanceTo(p, out float t);
            return t < 0.5f ? fromRoomId : toRoomId;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.66f, 0.44f, 0.37f, 0.9f);
            Gizmos.DrawLine(fromPoint, toPoint);
        }
    }
}
