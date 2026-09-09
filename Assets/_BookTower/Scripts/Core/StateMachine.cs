using System;

namespace BookTower.Core
{
    /// <summary>
    /// 상태 하나. 소유자(TOwner)를 인자로 받아서 상태 객체 자체는 데이터를 거의 갖지 않는다.
    /// </summary>
    public interface IState<TOwner>
    {
        void Enter(TOwner owner);
        void Tick(TOwner owner, float deltaTime);
        void Exit(TOwner owner);
    }

    /// <summary>
    /// 범용 유한 상태 머신.
    ///
    /// 쓰는 기준:
    ///  - 상태마다 "진입/유지/퇴장 시 할 일"이 실제로 다를 때만 쓴다.
    ///  - 단순히 플래그 몇 개로 갈리는 건 enum + switch가 더 읽기 쉽다.
    ///    (그래서 GameFlow의 페이즈는 FSM 클래스로 만들지 않았다)
    /// </summary>
    public class StateMachine<TOwner>
    {
        private readonly TOwner owner;

        public IState<TOwner> Current { get; private set; }

        /// <summary>디버깅·QA용. 인스펙터나 로그로 현재 상태를 확인할 때</summary>
        public string CurrentName => Current?.GetType().Name ?? "(none)";

        /// <summary>현재 상태에 머문 시간. 상태 전이 조건에 자주 쓰인다</summary>
        public float TimeInState { get; private set; }

        /// <summary>(이전 상태, 새 상태)</summary>
        public event Action<IState<TOwner>, IState<TOwner>> StateChanged;

        public StateMachine(TOwner owner)
        {
            this.owner = owner;
        }

        public void Change(IState<TOwner> next)
        {
            if (next == null) return;
            if (ReferenceEquals(next, Current)) return;

            IState<TOwner> prev = Current;

            prev?.Exit(owner);
            Current = next;
            TimeInState = 0f;
            Current.Enter(owner);

            StateChanged?.Invoke(prev, Current);
        }

        public void Tick(float deltaTime)
        {
            TimeInState += deltaTime;
            Current?.Tick(owner, deltaTime);
        }
    }
}
