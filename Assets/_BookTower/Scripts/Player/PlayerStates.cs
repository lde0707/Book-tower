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

            p.ApproachHorizontalVelocity(dir * p.MoveSpeed, dt);
            p.MoveWith(p.HorizontalVelocity);
            p.FaceDirection(dir);
            p.RecordSafePoint();
            p.TickFootsteps();

            // 스페이스: 땅에서는 점프, 공중에서는 활공.
            // v = sqrt(2 * g * h) — 원하는 높이에서 필요한 초기 속도를 역산한다.
            if (p.Input.JumpPressed)
            {
                p.VerticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(p.Gravity) * p.JumpHeight);
                p.JumpedThisAir = true;
                p.Machine.Change(p.Airborne);
                return;
            }

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
            p.ApproachHorizontalVelocity(dir * p.MoveSpeed, dt);
            p.MoveWith(p.HorizontalVelocity);
            p.FaceDirection(dir);

            if (p.Controller.isGrounded)
            {
                p.Machine.Change(p.Grounded);
                return;
            }

            // 하강 중에만 활공 진입 — 점프처럼 쓰이는 걸 막는다
            // 점프로 뜬 동안에는 활공을 막는다.
            // 이어 붙이면 체공이 4.0 → 7.2 로 늘어나 1B 의 틈(6.4)을 그냥 넘고,
            // P3 의 "다른 페이지를 접어 그림자로 길을 잇는다"가 통째로 무의미해진다.
            // 발을 헛디뎌 떨어지는 경우는 그대로 활공할 수 있다.
            if (p.Input.GlideHeld && p.VerticalVelocity < 0f && !p.JumpedThisAir)
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

            p.ApproachHorizontalVelocity(dir * (p.MoveSpeed + p.GlideDrift), dt);
            p.MoveWith(p.HorizontalVelocity);
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
