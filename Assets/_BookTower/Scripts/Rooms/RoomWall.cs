using UnityEngine;
using UnityEngine.Rendering;

namespace BookTower.Rooms
{
    /// <summary>어느 방향의 외벽인가.</summary>
    public enum WallFace { None, North, South, East, West, Ceiling }

    /// <summary>
    /// 카메라를 향한 외벽을 숨긴다.
    ///
    /// 2E·3A 같은 방은 사방 벽과 천장으로 완전히 닫혀 있다.
    /// 카메라를 어디에 두든 카메라 쪽 벽이 방 안을 가리고, 천장은 항상 가린다.
    /// 심즈나 모뉴먼트 밸리가 쓰는 방식대로 카메라를 향한 면만 숨긴다.
    ///
    /// 중요: 렌더러를 끄는 게 아니라 ShadowsOnly 로 바꾼다.
    /// 이 게임은 그림자가 곧 정답이라, 벽을 렌더링에서 완전히 빼면
    /// 그 벽이 막고 있던 빛까지 통과해 퍼즐의 정답 자체가 바뀐다.
    /// 눈에는 안 보이되 빛은 그대로 막아야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomWall : MonoBehaviour
    {
        [SerializeField] private WallFace face = WallFace.None;

        private Renderer rend;
        private ShadowCastingMode original = ShadowCastingMode.On;

        public WallFace Face => face;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) original = rend.shadowCastingMode;
        }

        /// <summary>카메라 스냅 단계(0~3)에 맞춰 이 면을 숨길지 결정한다.</summary>
        public void ApplyYaw(int yawStep)
        {
            if (rend == null) return;

            bool hide = face == WallFace.Ceiling || face == HiddenFor(yawStep);

            if (!hide)
            {
                rend.shadowCastingMode = original;
                return;
            }

            // 원래 그림자를 안 만들던 물체라면 ShadowsOnly 로 바꿔도 안 보이는 게 아니라
            // 없던 그림자가 생긴다. 그런 경우만 렌더러를 끈다.
            if (original == ShadowCastingMode.Off) rend.enabled = false;
            else rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        /// <summary>
        /// yaw 0 이면 카메라는 -z 쪽에서 +z 를 바라본다. 그러면 남쪽 벽이 앞을 막는다.
        /// PaperCamera 의 Quaternion.Euler(pitch, yaw*90, 0) 규칙과 맞물려 있다.
        /// </summary>
        public static WallFace HiddenFor(int yawStep) => (((yawStep % 4) + 4) % 4) switch
        {
            0 => WallFace.South,
            1 => WallFace.West,
            2 => WallFace.North,
            _ => WallFace.East,
        };
    }
}
