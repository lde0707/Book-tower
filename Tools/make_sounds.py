# -*- coding: utf-8 -*-
"""종이 소리만으로 이루어진 효과음을 절차적으로 만든다.

    python make_sounds.py          모든 소리를 생성
    python make_sounds.py --plot   생성 후 파형 PNG도 함께 남긴다 (눈으로 확인용)

왜 절차적으로 만드나 (Tools/make_textures.py 와 같은 이유)
  · 사운드 라이브러리를 구해 오면 라이선스와 톤(=팡파레·음악 느낌)을 매번 걷어내야 한다
  · 이 게임의 효과음은 전부 "종이"라는 한 재료에서 나와야 한다(Docs/02 §10) —
    화이트노이즈를 대역 제한(band-limit)하는 것만으로 종이 질감을 낼 수 있어서
    실제로 합성이 녹음보다 더 이 게임의 톤에 맞는다
  · 엔벨로프 숫자 하나로 "좀 더 짧게" 같은 수정이 끝난다

만드는 것 — SfxDirector.cs 가 이름으로 그대로 불러온다 (Resources/Sfx/<name>.wav)
  fold_step     접이면 상태가 바뀔 때 — 짧은 클릭 + 낮은 톡(몸통)
  puzzle_solve  퍼즐 성공 — "팡파레 금지, 접힘이 맞물리는 소리 하나로"라는 규칙대로
                fold_step 을 세 번 겹쳐 올림조로 쌓고 마지막에 낮은 울림으로 가라앉힌다
  piece_pickup  조각을 주움 — fold_step 의 클릭 부분만, 더 가볍고 밝게
  gate_open     막힌 길이 열림 — 먹먹하다가 점점 트이는 노이즈 스웰(큰 페이지가 펴지는 느낌)
  rustle_step   발소리 대체 — 아주 짧고 조용한 스침
  hint_tremble  힌트 1단계 — 아주 여린 떨림(연속 미세 틱)
  hint_nudge    힌트 3단계 — fold_step 과 같은 언어지만 더 부드럽고 낮게(미완의 도움이므로)

전부 44.1kHz mono 16bit WAV. 시드를 고정해 매번 같은 소리가 나오게 한다.
"""
import numpy as np
import wave
import os
import sys

SR = 44100
OUT = "Assets/_BookTower/Resources/Sfx"
rng = np.random.default_rng(20260918)


# ─────────────────────────────────────────── 기본 도구

def white(n):
    return rng.normal(0.0, 1.0, n).astype(np.float64)


def band(sig, lo, hi, sr=SR):
    """rfft 로 lo~hi Hz 만 남긴다. scipy 없이 numpy 만으로 대역 제한."""
    spec = np.fft.rfft(sig)
    freqs = np.fft.rfftfreq(len(sig), 1.0 / sr)
    mask = (freqs >= lo) & (freqs <= hi)
    spec = spec * mask
    return np.fft.irfft(spec, n=len(sig))


def expdecay(n, tau, sr=SR):
    """지수 감쇠 엔벨로프. tau 가 클수록 오래 남는다."""
    t = np.arange(n) / sr
    return np.exp(-t / max(tau, 1e-6))


def attack(n, seconds, sr=SR):
    """0→1 로 부드럽게 올라가는 어택(스무스스텝)."""
    k = min(max(int(seconds * sr), 1), n)
    ramp = np.linspace(0.0, 1.0, k)
    ramp = ramp * ramp * (3 - 2 * ramp)
    out = np.ones(n)
    out[:k] = ramp
    return out


def pad(sig, seconds, sr=SR):
    tail = np.zeros(int(seconds * sr))
    return np.concatenate([sig, tail])


def mix_at(base, sig, at_seconds, sr=SR):
    start = int(at_seconds * sr)
    end = start + len(sig)
    if end > len(base):
        base = np.concatenate([base, np.zeros(end - len(base))])
    base[start:end] += sig
    return base


def normalize(sig, peak=0.9):
    m = np.max(np.abs(sig))
    if m < 1e-9:
        return sig
    return sig / m * peak


def fadeout(sig, seconds=0.12, sr=SR):
    """끝을 0 으로 내린다.

    안 하면 파형이 진폭을 가진 채로 뚝 끊기고, 그건 '딸깍' 으로 들린다.
    실제로 gate_open 이 그랬다 — 엔벨로프가 expdecay(tau=0.75) 인데 길이가
    1.3초라 끝에서도 exp(-1.3/0.75) = 0.18 이 남아 있었다.
    개별 소리마다 tau 를 맞추는 것보다 출력 직전에 한 번 거는 게 확실하다.
    """
    k = min(int(seconds * sr), len(sig))
    if k < 2:
        return sig
    ramp = np.linspace(1.0, 0.0, k)
    ramp = ramp * ramp * (3 - 2 * ramp)     # 스무스스텝으로 내려야 꺾인 자국이 안 남는다
    out = sig.copy()
    out[-k:] *= ramp
    return out


def save_wav(name, sig, sr=SR):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"{name}.wav")
    sig = fadeout(sig)
    sig = np.clip(sig, -1.0, 1.0)
    data = (sig * 32767).astype(np.int16)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())
    return path


# ─────────────────────────────────────────── 재료 — 종이 "클릭"과 "톡"
#
# 이 게임의 모든 효과음은 이 두 재료의 배합비만 바꿔서 나온다.
#   클릭 — 짧고 밝은 대역(종이 모서리끼리 부딪는 소리)
#   톡   — 낮은 대역의 짧은 울림(접힘의 무게감)

def click(n, lo=2000, hi=8000, tau=0.006):
    sig = band(white(n), lo, hi) * expdecay(n, tau)
    return sig


def thock(n, lo=150, hi=500, tau=0.05):
    sig = band(white(n), lo, hi) * expdecay(n, tau)
    return sig


# ─────────────────────────────────────────── 효과음 7종

def make_fold_step():
    """접이면 상태 변경 — 짧은 클릭 + 낮은 톡. 이 게임 사운드의 기본 단어."""
    n = int(0.18 * SR)
    sig = np.zeros(n)
    sig = mix_at(sig, click(int(0.03 * SR)), 0.0)
    sig = mix_at(sig, thock(int(0.10 * SR)) * 0.55, 0.004)
    return normalize(sig, 0.85)


def make_puzzle_solve():
    """
    퍼즐 성공 — "팡파레 금지, 접힘이 맞물리는 소리 하나로" (Docs/02 §10).
    fold_step 의 클릭을 세 번, 점점 높은 대역으로 겹쳐 쌓아 "맞물려 들어간다"는
    인상을 주고, 마지막에 낮은 울림(thock 을 길게 늘인 것)으로 가라앉힌다.
    화음이나 멜로디는 안 쓴다 — 음악이 아니라 종이 소리여야 한다.
    """
    total = pad(np.zeros(1), 0.9)
    bands = [(2000, 7000), (2500, 8500), (3200, 10000)]
    offsets = [0.0, 0.085, 0.165]
    for (lo, hi), t in zip(bands, offsets):
        c = click(int(0.05 * SR), lo, hi, tau=0.02)
        total = mix_at(total, c * 0.8, t)

    settle = band(white(int(0.55 * SR)), 250, 900) * expdecay(int(0.55 * SR), 0.28) * 0.5
    total = mix_at(total, settle, 0.19)
    return normalize(total, 0.95)


def make_piece_pickup():
    """조각을 주움 — fold_step 의 클릭 부분만, 더 짧고 밝게. 가벼운 손동작 느낌."""
    n = int(0.14 * SR)
    sig = click(n, lo=3200, hi=9500, tau=0.02) * attack(n, 0.0015)
    return normalize(sig, 0.75)


def make_gate_open():
    """
    막힌 길이 열림 — 큰 페이지가 천천히 펴지는 소리.
    처음엔 먹먹하다가(저역만) 점점 트인다(전 대역) — 저역/전역 두 버전을
 만들어 열림 정도(open)로 크로스페이드한다. 종이가 팽팽히 펴지는 느낌을 위해
    스웰(서서히 커짐) 엔벨로프를 얹는다.
    """
    n = int(1.3 * SR)
    raw = white(n)
    muffled = band(raw, 150, 900)
    bright = band(raw, 150, 5200)

    t = np.arange(n) / SR
    open_amt = np.clip(t / 0.6, 0.0, 1.0)
    open_amt = open_amt * open_amt * (3 - 2 * open_amt)
    blended = muffled * (1 - open_amt) + bright * open_amt

    swell = attack(n, 0.35) * expdecay(n, 0.75)
    sig = blended * swell
    return normalize(sig, 0.8)


def make_rustle_step():
    """발소리 대체 — 아주 짧고 조용한 스침. 반복 재생되므로 짧고 간결해야 한다."""
    n = int(0.11 * SR)
    sig = band(white(n), 600, 5000) * expdecay(n, 0.028) * attack(n, 0.003)
    return normalize(sig, 0.55)


def make_hint_tremble():
    """
    힌트 1단계 — "관련 오브젝트가 미세하게 떨림"의 소리판.
    아주 여린 틱을 몇 번 겹쳐, 무언가 신경질적으로 살짝 떠는 느낌을 낸다.
    성가시면 안 되므로 전체 볼륨을 낮게 잡는다(SfxDirector 쪽에서도 0.3배 추가로 줄임).
    """
    n = int(0.5 * SR)
    sig = np.zeros(n)
    ticks = [0.0, 0.09, 0.19, 0.27, 0.38]
    for i, t in enumerate(ticks):
        amp = 0.5 * (1.0 - i / len(ticks))
        tick = click(int(0.02 * SR), lo=1500, hi=4200, tau=0.01) * amp
        sig = mix_at(sig, tick, t)
    return normalize(sig, 0.45)


def make_hint_nudge():
    """
    힌트 3단계 — fold_step 과 같은 언어(클릭+톡)지만 더 부드럽고 낮게.
    "저절로 반쯤 접힘"이라 완결감이 없어야 하므로 fold_step 보다 짧고 여운을 줄인다.
    """
    n = int(0.16 * SR)
    sig = np.zeros(n)
    sig = mix_at(sig, click(int(0.025 * SR), lo=1000, hi=3200, tau=0.012) * 0.7, 0.0)
    sig = mix_at(sig, thock(int(0.08 * SR), lo=120, hi=380, tau=0.04) * 0.45, 0.003)
    return normalize(sig, 0.65)


RECIPES = {
    "fold_step": make_fold_step,
    "puzzle_solve": make_puzzle_solve,
    "piece_pickup": make_piece_pickup,
    "gate_open": make_gate_open,
    "rustle_step": make_rustle_step,
    "hint_tremble": make_hint_tremble,
    "hint_nudge": make_hint_nudge,
}


def plot_all():
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    fig, axes = plt.subplots(len(RECIPES), 1, figsize=(9, 1.6 * len(RECIPES)))
    for ax, name in zip(axes, RECIPES):
        sig = RECIPES[name]()
        t = np.arange(len(sig)) / SR
        ax.plot(t, sig, linewidth=0.6, color="#3A3733")
        ax.set_title(name, fontsize=9, loc="left")
        ax.set_xlim(0, max(t[-1], 0.05))
        ax.set_ylim(-1.05, 1.05)
        ax.tick_params(labelsize=7)
    fig.tight_layout()
    qa_dir = "Docs/_qa"
    os.makedirs(qa_dir, exist_ok=True)
    out_path = os.path.join(qa_dir, "sfx_waveforms.png")
    fig.savefig(out_path, dpi=130)
    print(f"파형 미리보기: {out_path}")


def verify():
    """만든 소리를 다시 읽어 귀로 확인할 수 없는 결함을 잡는다.

    소리는 들어봐야 아는 게 대부분이지만, 이 셋은 파형만 봐도 확실히 결함이다.
      · 끝이 0 이 아님 → '딸깍'
      · 클리핑        → 지직거림
      · 완전 무음      → 레시피가 망가진 것
    """
    import wave as _w, struct
    bad = 0
    print(f"\n{'파일':<16}{'초':>6}{'피크':>7}{'클립':>6}{'끝':>7}  판정")
    for name in RECIPES:
        path = os.path.join(OUT, f"{name}.wav")
        with _w.open(path) as f:
            n, sr = f.getnframes(), f.getframerate()
            raw = f.readframes(n)
        v = np.array(struct.unpack("<%dh" % (len(raw) // 2), raw), dtype=np.float64) / 32768.0
        peak = float(np.max(np.abs(v)))
        clip = int(np.sum(np.abs(v) >= 32700 / 32768))
        edge = max(int(sr * 0.003), 1)
        tail = float(np.mean(np.abs(v[-edge:])))

        why = []
        if tail > 0.02: why.append("끝에서 딸깍")
        if clip > 0: why.append(f"클리핑 {clip}")
        if peak < 0.05: why.append("거의 무음")
        if why: bad += 1
        print(f"{name:<16}{n/sr:>6.2f}{peak:>7.2f}{clip:>6}{tail:>7.3f}  {', '.join(why) or 'OK'}")

    print(f"\n문제 {bad}건")
    return bad


def main():
    for name, fn in RECIPES.items():
        sig = fn()
        path = save_wav(name, sig)
        print(f"{name:16s} {len(sig)/SR:5.2f}s  -> {path}")

    if "--plot" in sys.argv:
        plot_all()

    if verify() > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
