using UnityEngine;
using BookTower.Core;

namespace BookTower.PlayerSystem
{
    // 상태를 4개로 나눈 기준:
    //   "물리 처리가 실제로 다른가?" — 다르지 않으면 상태를 나누지 않는다.
    //   Idle과 Move는 둘 다 접지 상태에서 같은 물리를 쓰므로 하나(Grounded)로 합쳤다.

    /// <summary>땅 위 — 걷기·정지. 여기서만 안전 지점을 기록한다</summary>
    public class PlayerGroundedState : IState<PlayerController>
    {
        public void Enter(PlayerController p)
        {
            p.VerticalVelocity = -2f;   // 경사면에서 들뜨지 않게 살짝 눌러준다
        }

        public void Tick(PlayerController p, float dt)
        {
            Vector3 dir = p.GetMoveDirection();

            p.MoveWith(dir * p.MoveSpeed);
            p.FaceDirection(dir);
            p.RecordSafePoint();

            if (!p.Controller.isGrounded)
                p.Machine.Change(p.Airborne);
        }

        public void Exit(PlayerController p) { }
    }

    /// <summary>공중 — 그냥 떨어지는 중</summary>
    public class PlayerAirborneState : IState<PlayerController>
    {
        public void Enter(PlayerController p) { }

        public void Tick(PlayerController p, float dt)
        {
            Vector3 dir = p.GetMoveDirection();

            p.VerticalVelocity += p.Gravity * dt;
            p.MoveWith(dir * p.MoveSpeed);
            p.FaceDirection(dir);

            if (p.Controller.isGrounded)
            {
                p.Machine.Change(p.Grounded);
                return;
            }

            // 하강 중에만 활공 진입 — 점프처럼 쓰이는 걸 막는다
            if (p.Input.GlideHeld && p.VerticalVelocity < 0f)
                p.Machine.Change(p.Glide);
        }

        public void Exit(PlayerController p) { }
    }

    /// <summary>
    /// 활공 — 낙하 속도만 늦춘다. 절대 위로 올라가지 않는다.
    /// 자유 비행을 허용하면 P3/P4/P15 퍼즐이 전부 무너지므로 이 제약은 협상 대상이 아니다.
    /// </summary>
    public class PlayerGlideState : IState<PlayerController>
    {
        public void Enter(PlayerController p) { }

        public void Tick(PlayerController p, float dt)
        {
            Vector3 dir = p.GetMoveDirection();

            // 상승 금지: 이미 더 빠르게 떨어지고 있으면 그 속도를 이 값까지만 늦춘다
            p.VerticalVelocity = Mathf.Max(p.VerticalVelocity, p.GlideFallSpeed);

            p.MoveWith(dir * (p.MoveSpeed + p.GlideDrift));
            p.FaceDirection(dir);

            if (p.Controller.isGrounded)
            {
                p.Machine.Change(p.Grounded);
                return;
            }

            if (!p.Input.GlideHeld)
                p.Machine.Change(p.Airborne);
        }

        public void Exit(PlayerController p) { }
    }

    /// <summary>복귀 — 탑 밖으로 떨어졌을 때. 죽음이 아니라 되돌리기다</summary>
    public class PlayerRespawnState : IState<PlayerController>
    {
        private const float Delay = 0.35f;

        public void Enter(PlayerController p)
        {
            p.TeleportToSafePoint();
        }

        public void Tick(PlayerController p, float dt)
        {
            // 짧게 멈췄다가 조작 복귀 — 순간이동이 너무 급작스럽지 않게
            if (p.Machine.TimeInState >= Delay)
                p.Machine.Change(p.Grounded);
        }

        public void Exit(PlayerController p) { }
    }
}
