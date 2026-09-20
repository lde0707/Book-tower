# -*- coding: utf-8 -*-
"""손으로 그린 느낌을 내는 타일링 텍스처를 절차적으로 만든다.

    python3 Tools/make_textures.py

왜 절차적으로 만드나
  · 손으로 그린 텍스처를 구해 오면 라이선스와 톤을 매번 맞춰야 한다
  · 값이 코드에 있으면 "결을 조금 거칠게" 같은 수정이 숫자 하나로 끝난다
  · 무엇보다 이 게임은 종이가 소재라, 결의 방향과 세기가 아트의 전부다

만드는 것
  T_Paper   종이 결 — 미세한 섬유와 얼룩. 모든 면의 바탕
  T_Crayon  크레파스 칠 — 결을 따라 안 칠해진 자국이 남는다
  T_Pencil  연필 해칭 — 그림자 쪽 면에 겹쳐 쓴다
  T_Paper_Soft  캐릭터용 부드러운 종이결 — 고주파 없이 얼룩과 섬유만
"""
import numpy as np
from PIL import Image
import os

SIZE = 512
OUT = "Assets/_BookTower/Textures"
rng = np.random.default_rng(20260910)   # 매번 같은 그림이 나와야 한다


def tileable_noise(size, scale, octaves=4):
    """이어 붙여도 이음매가 안 보이는 노이즈. 저주파를 겹쳐 만든다."""
    acc = np.zeros((size, size), np.float32)
    amp, total = 1.0, 0.0
    for o in range(octaves):
        f = scale * (2 ** o)
        # 주파수를 정수로 두면 경계에서 정확히 이어진다
        gx, gy = np.meshgrid(np.arange(size), np.arange(size))
        phase = rng.uniform(0, 2 * np.pi, 4)
        w = (np.sin(2 * np.pi * f * gx / size + phase[0]) *
             np.sin(2 * np.pi * f * gy / size + phase[1]) +
             np.sin(2 * np.pi * f * (gx + gy) / size + phase[2]) *
             np.cos(2 * np.pi * f * (gx - gy) / size + phase[3]))
        acc += w * amp
        total += amp
        amp *= 0.5
    return acc / total


def band(size, center, width, stretch=1.0):
    """주파수 대역만 남긴 노이즈.

    sin/cos 곱(tileable_noise)으로 만들면 바둑판 격자가 그대로 보인다.
    작은 물체에 깔면 그 격자가 '자갈' 로 읽힌다 — 캐릭터에서 실제로 그랬다.
    백색잡음을 FFT 로 걸러 내면 격자 없이 유기적이고, FFT 자체가 주기적이라
    타일 이음매도 안 생긴다.

    stretch > 1 이면 가로로 늘어난 결이 된다 — 종이 섬유 방향."""
    w = rng.normal(0, 1, (size, size))
    F = np.fft.fft2(w)
    f = np.fft.fftfreq(size) * size
    FX, FY = np.meshgrid(f, f)
    rad = np.sqrt((FX / stretch) ** 2 + FY ** 2)
    mask = np.exp(-((rad - center) / width) ** 2)
    out = np.real(np.fft.ifft2(F * mask))
    return out / (out.std() + 1e-9)


def soft_paper():
    """캐릭터용. 면 위에서 결이 '잡티' 가 아니라 '바탕' 으로 읽혀야 한다.

    규칙이 벽면용과 다르다.
      · 고주파 금지 — 95px 짜리 물체에서 1px 이하 결은 지글거리는 모래가 된다
      · 대비 낮게 — 형태(음영)가 주인공이고 결은 바탕이다
      · 저주파 얼룩 위주 — 손으로 뜬 종이의 두께 얼룩처럼 보인다
    타일은 1유닛당 2장으로 깐다. 5장을 넘기면 사포처럼 보인다(렌더로 확인).
    """
    blotch = band(SIZE, 3.5, 2.0)
    mottle = band(SIZE, 9.0, 4.0)
    fibre = band(SIZE, 26.0, 10.0, stretch=3.5)
    return 1.0 + blotch * 0.034 + mottle * 0.020 + fibre * 0.012


def paper():
    """종이 결. 가로 섬유가 살짝 우세하고, 아주 옅은 얼룩이 있다."""
    fibre = tileable_noise(SIZE, 24, 3) * 0.5 + tileable_noise(SIZE, 60, 2) * 0.5
    grain = rng.normal(0, 1, (SIZE, SIZE)).astype(np.float32)
    # 가로로 살짝 뭉개서 섬유 방향을 만든다
    grain = (grain + np.roll(grain, 1, 1) + np.roll(grain, 2, 1)) / 3.0
    stain = tileable_noise(SIZE, 5, 2)

    v = 1.0 + fibre * 0.035 + grain * 0.020 + stain * 0.030
    return np.clip(v, 0, 2)


def crayon():
    """크레파스. 결이 있는 종이에 칠하면 오목한 데는 안 칠해진다."""
    tooth = tileable_noise(SIZE, 40, 3)
    stroke = tileable_noise(SIZE, 9, 2)          # 손이 지나간 큰 결
    speck = rng.random((SIZE, SIZE)).astype(np.float32)

    # 결이 깊은 곳은 안료가 안 얹힌다 → 하얗게 남는다
    miss = (tooth > 0.25) & (speck > 0.55)
    v = 1.0 + tooth * 0.06 + stroke * 0.05
    v[miss] += 0.16
    return np.clip(v, 0, 2)


def pencil():
    """연필 해칭. 한 방향으로 그은 선이 겹친다."""
    gx, gy = np.meshgrid(np.arange(SIZE), np.arange(SIZE))
    lines = np.sin((gx + gy) * (2 * np.pi * 26 / SIZE))
    jitter = tileable_noise(SIZE, 30, 2) * 0.6
    v = 1.0 - np.clip(lines + jitter, 0, 1) * 0.18
    v *= (1.0 + tileable_noise(SIZE, 12, 2) * 0.05)
    return np.clip(v, 0, 2)


def save(name, arr, target_mean=0.94):
    # 흰 바탕이 메인이므로 평균을 흰색 가까이로 올린다.
    # 대비는 그대로 두고 밝기만 옮긴다 — 결이 안 보이면 텍스처를 넣은 의미가 없다.
    a = np.asarray(arr, np.float32)
    a = a - a.mean() + target_mean
    img = (np.clip(a, 0, 1) * 255).astype(np.uint8)
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name + ".png")
    Image.fromarray(np.dstack([img, img, img])).save(p)
    print("%-12s %s  평균 %.3f  대비 %.3f" % (name, p, img.mean() / 255, img.std() / 255))


if __name__ == "__main__":
    save("T_Paper", paper())
    save("T_Crayon", crayon())
    save("T_Pencil", pencil())
    save("T_Paper_Soft", soft_paper(), target_mean=0.955)
