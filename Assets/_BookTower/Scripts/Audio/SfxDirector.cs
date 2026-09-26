using UnityEngine;
using BookTower.Core;
using BookTower.Fold;

namespace BookTower.Audio
{
    /// <summary>
    /// 이벤트 허브(GameEvents)를 구독해 종이 소리를 재생한다.
    ///
    /// 사운드 방향(Docs/02 §10): 종이 소리만(넘김·구김·찢김·스침), 음악은 최소한,
    /// 퍼즐 성공은 팡파레가 아니라 "접힘이 맞물리는 소리" 하나로.
    ///
    /// 클립은 Resources/Sfx 에서 이름으로 불러온다 — LevelBuilder 가 JSON에서
    /// 씬을 굽는 것과 같은 "데이터가 원본" 방식이라, 씬에 오디오소스를 수동으로
    /// 연결할 필요가 없다. 클립이 없으면 경고만 남기고 조용히 건너뛴다 —
    /// 사운드 파일이 아직 없다고 게임이 멈추면 안 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class SfxDirector : MonoBehaviour
    {
        public static SfxDirector Instance { get; private set; }

        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.32f;

        private AudioClip foldStep, puzzleSolve, pieceUp, gateOpen, rustleStep, hintTremble, hintNudge;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foldStep    = Load("fold_step");
            puzzleSolve = Load("puzzle_solve");
            pieceUp     = Load("piece_pickup");
            gateOpen    = Load("gate_open");
            rustleStep  = Load("rustle_step");
            hintTremble = Load("hint_tremble");
            hintNudge   = Load("hint_nudge");
        }

        private static AudioClip Load(string name)
        {
            var clip = Resources.Load<AudioClip>("Sfx/" + name);
            if (clip == null)
                Debug.LogWarning($"[SfxDirector] Resources/Sfx/{name}.wav 이 없다 — 해당 효과음은 생략된다. " +
                                  "Tools/make_sounds.py 로 생성했는지 확인할 것");
            return clip;
        }

        private void OnEnable()
        {
            GameEvents.FoldChanged += OnFoldChanged;
            GameEvents.PuzzleSolved += OnPuzzleSolved;
            GameEvents.PieceCollected += OnPieceCollected;
            GameEvents.GateOpened += OnGateOpened;
            GameEvents.Footstep += OnFootstep;
            GameEvents.HintEscalated += OnHintEscalated;
        }

        private void OnDisable()
        {
            GameEvents.FoldChanged -= OnFoldChanged;
            GameEvents.PuzzleSolved -= OnPuzzleSolved;
            GameEvents.PieceCollected -= OnPieceCollected;
            GameEvents.GateOpened -= OnGateOpened;
            GameEvents.Footstep -= OnFootstep;
            GameEvents.HintEscalated -= OnHintEscalated;
        }

        private void OnFoldChanged(FoldPanel panel)
        {
            if (panel != null) PlayAt(foldStep, panel.transform.position, 0.65f);
        }

        private void OnPuzzleSolved(MonoBehaviour puzzle)
        {
            PlayAt(puzzleSolve, puzzle != null ? puzzle.transform.position : PlayerPos(), 1f);
        }

        private void OnPieceCollected(string pieceId) => PlayAt(pieceUp, PlayerPos(), 0.8f);

        private void OnGateOpened(MonoBehaviour gate)
        {
            PlayAt(gateOpen, gate != null ? gate.transform.position : PlayerPos(), 0.9f);
        }

        private void OnFootstep(Vector3 pos) => PlayAt(rustleStep, pos, footstepVolume);

        /// <summary>
        /// 힌트 1단계(떨림)·3단계(반접힘)에만 소리를 붙인다. 2단계(빛)는 시각만 —
        /// 소리가 매번 늘어나면 힌트가 "점점 시끄러워지는 경고"로 읽혀서
        /// 무대사 게임의 정적을 깬다.
        /// </summary>
        private void OnHintEscalated(MonoBehaviour puzzle, int level)
        {
            Vector3 pos = puzzle != null ? puzzle.transform.position : PlayerPos();
            if (level == 1) PlayAt(hintTremble, pos, 0.3f);
            else if (level == 3) PlayAt(hintNudge, pos, 0.55f);
        }

        private Vector3 PlayerPos()
        {
            var pc = FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();
            return pc != null ? pc.transform.position : transform.position;
        }

        private void PlayAt(AudioClip clip, Vector3 pos, float volume)
        {
            if (clip == null) return;
            var go=new GameObject("Paper one-shot");go.transform.position=pos;var source=go.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=1;source.volume=Mathf.Clamp01(volume*masterVolume);BookTower.UI.PaperAudioLevel.Bind(source);source.Play();Destroy(go,clip.length+1);
        }

        /// <summary>씬에 없어도 자동 생성 — GameFlow.EnsureExists() 와 같은 패턴</summary>
        public static SfxDirector EnsureExists()
        {
            if (Instance != null) return Instance;

            var found = FindFirstObjectByType<SfxDirector>();
            if (found != null) return found;

            var go = new GameObject("[SfxDirector]");
            return go.AddComponent<SfxDirector>();
        }
    }
}
