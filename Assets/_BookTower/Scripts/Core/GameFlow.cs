using UnityEngine;

namespace BookTower.Core
{
    /// <summary>
    /// 게임 전체 페이즈를 관리한다. 씬에 하나만 존재한다.
    ///
    /// 왜 여기는 FSM 클래스를 안 썼나:
    ///   페이즈별로 "진입/유지/퇴장 로직"이 거의 없고, 사실상 입력을 받을지 말지를
    ///   가르는 플래그에 가깝다. 이런 건 enum + switch가 파일 수만 늘리지 않고 더 읽기 쉽다.
    ///   반대로 플레이어는 상태마다 물리·입력 처리가 실제로 달라서 FSM 클래스를 썼다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private GamePhase phase = GamePhase.Boot;

        public GamePhase Phase => phase;

        /// <summary>플레이어가 지금 조작 가능한가</summary>
        public bool AcceptsPlayerInput => phase == GamePhase.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetPhase(GamePhase.Playing);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetPhase(GamePhase next)
        {
            if (phase == next) return;

            phase = next;

            // 일시정지만 시간을 멈춘다. 연출(Cutscene)은 시간이 흘러야 하므로 멈추지 않는다
            Time.timeScale = (next == GamePhase.Paused) ? 0f : 1f;

            GameEvents.RaisePhaseChanged(next);
        }

        public void Pause() => SetPhase(GamePhase.Paused);
        public void Resume() => SetPhase(GamePhase.Playing);

        /// <summary>연출 시작 — 입력을 막지만 시간은 흐른다</summary>
        public void BeginCutscene() => SetPhase(GamePhase.Cutscene);

        public void EndCutscene() => SetPhase(GamePhase.Playing);

        /// <summary>
        /// GameFlow가 씬에 없어도 게임이 돌아가도록 자동 생성.
        /// 프로토타입 단계에서 매번 배치하는 수고를 덜기 위한 안전장치다.
        /// </summary>
        public static GameFlow EnsureExists()
        {
            if (Instance != null) return Instance;

            var found = FindFirstObjectByType<GameFlow>();
            if (found != null) return found;

            var go = new GameObject("[GameFlow]");
            return go.AddComponent<GameFlow>();
        }
    }
}
