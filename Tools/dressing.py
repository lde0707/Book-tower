# -*- coding: utf-8 -*-
"""방 꾸미기 — "너무 허전하다" 에 대한 답.

왜 필요했나
  방을 1.6배로 키운 뒤(Docs/09) 바닥이 300~2,300㎡ 인데 꾸밈 오브젝트는
  방마다 0~8개, 그것도 1m 남짓한 것들이었다. 게임 카메라로 렌더해 보면
  넓은 베이지 바닥에 점 몇 개가 떠 있다. "허전하다" 는 정확한 진단이었다.

무엇으로 채우나 — 모티프는 셋으로만 (통일감이 먼저다)
  ① 서재     책장·책등·기댄 거인 책·쌓인 거인 책·독서대. 1장이 가장 짙다
  ② 책갈피   붉은 끈·벽에 꽂힌 페이지. 표지와 2장. 플레이어(정령의 리본)와 같은 색
  ③ 파손     무너진 책장·찢긴 큰 페이지·그을음. 층이 오를수록 짙다
  3장은 "찢김이 많고 여백이 넓다" 가 설계된 정서라 **덩어리는 크게, 개수는 적게** 둔다.

지키는 것 (어기면 게임이 망가지는 순서대로)
  1. **천창 빛 경로를 막지 않는다.** 그림자가 곧 정답이다.
     빛 대상 점에서 태양 반대 방향으로 천장까지 올린 선분과 부딪히면 버린다
  2. **길을 막지 않는다.** 문(경사로 입구)에서 방 가운데, 퍼즐 자리까지의 통로를
     비워 둔다. 발목 높이(0.3 미만) 흩뿌림만 통로에 허용한다 — 걸어서 넘어간다
  3. **퍼즐과 헷갈리지 않는다.** 퍼즐 조각은 밝은 종이(light) 판이다.
     꾸미기는 light·accent 를 절대 안 쓰고, 조각 주변 2.5m 안에는 낱장을 안 둔다.
     accent 를 안 쓰는 이유가 하나 더 있다 — lit_targets 가 accent 박스를
     '빛을 받아야 할 것' 으로 세어서 천창이 엉뚱한 데 뚫린다
  4. **시야를 막지 않는다.** 1.2m 넘는 건 벽에 붙이고 그 벽의 face 를 단다.
     카메라가 그쪽으로 돌면 벽과 같이 그림자만 남기고 사라진다

전부 결정적이다 — 방 id 로 시드를 고정한다. 다시 돌려도 같은 방이 나온다.
"""
import math
import random
import zlib

# ─────────────────────────────────────────── 색 — 전부 채도 낮게
#
# 아트 규칙: 흰 종이가 바탕, 채도는 낮게. 책등에 색을 주는 건 "흰 방에 회색
# 상자" 에서 벗어나는 가장 싼 방법이다. 대신 밝기를 종이보다 한두 단계 낮춰서
# 퍼즐(밝은 종이)이 여전히 제일 먼저 눈에 들어오게 한다.
PALETTE = {
    "frame": "#CFC5B6",      # 책장 틀 — 바랜 종이 상자
    "frameDk": "#B6AB9B",
    "spine1": "#C9B8A3",     # 세피아
    "spine2": "#A9B4B8",     # 먼지 낀 파랑
    "spine3": "#BDAA98",     # 따뜻한 회갈
    "spine4": "#B3B79E",     # 세이지
    "spine5": "#C6A69C",     # 바랜 장미
    "spine6": "#9A958D",     # 먹 회색 — 가끔 끼워 리듬을 준다
    "page": "#EEE8DC",       # 낱장 — 퍼즐 조각(light #FBFAF6)보다 확실히 누렇게
    "pageOld": "#E0D6C3",
    "cover1": "#B39C88",     # 거인 책 표지
    "cover2": "#9DA8AE",
    "cover3": "#A6AC93",
    "ribbon": "#C8705A",     # 책갈피 끈 — 정령의 리본과 같은 계열
    "rug": "#E2D8C8",
    "rugEdge": "#C9BBA6",
    "soot": "#C2BBB0",
    "ink": "#5A534B",
    "candle": "#EDE3CF",
    "flame": "#F0CE8A",
    "wainscot": "#D9D0C1",   # 벽 아랫단 — 윗단(벽 본색)보다 한 단계 가라앉게
    "trim": "#C8BCAA",       # 몰딩·기둥
    "seam": "#E3DCCF",       # 바닥 이음선 — 바닥이 '깔아 놓은 거인 페이지' 로 읽히게
}
SPINES = ["spine1", "spine2", "spine3", "spine4", "spine5", "spine1", "spine3", "spine6"]
COVERS = ["cover1", "cover2", "cover3"]

# 방 층(floor)별 성격. 숫자는 "얼마나" 이지 "무엇" 이 아니다 — 무엇은 모티프가 정한다
STYLE = {
    0: dict(shelf=0.55, lean=0.20, pinned=0.25, corner=0.6, desk=1, openbook=0, rug=1,
            scatter=0.030, ribbon=0.45, fallen=0.20, scrap=0.05, ruin=0.0),
    1: dict(shelf=0.80, lean=0.12, pinned=0.08, corner=0.9, desk=2, openbook=1, rug=1,
            scatter=0.034, ribbon=0.15, fallen=0.40, scrap=0.10, ruin=0.0),
    2: dict(shelf=0.45, lean=0.18, pinned=0.37, corner=0.7, desk=2, openbook=1, rug=1,
            scatter=0.036, ribbon=0.35, fallen=0.25, scrap=0.25, ruin=0.10),
    3: dict(shelf=0.25, lean=0.20, pinned=0.15, corner=0.5, desk=0, openbook=1, rug=0,
            scatter=0.016, ribbon=0.10, fallen=0.20, scrap=0.50, ruin=0.40),
    4: dict(shelf=0.85, lean=0.10, pinned=0.05, corner=0.9, desk=1, openbook=1, rug=1,
            scatter=0.030, ribbon=0.20, fallen=0.40, scrap=0.10, ruin=0.0),
}

CELL = 0.25
TALL = 1.2                       # 이보다 높으면 벽에 붙여야 한다
PREFIX = "장식_"                 # 검사가 이 접두어로 꾸미기를 알아본다

# 이 이름이 들어간 오브젝트는 '만지는 것' 이다. 주변을 넉넉히 비운다
INTERACTIVE_KEYS = ("P", "랜드마크", "조각", "에코", "주름", "기록", "가림막", "간극",
                    "걸쇠", "복원대", "왜상", "열쇠구멍", "채광구", "문틀", "고정표식",
                    "숨겨진틈", "그림자판독벽", "물리적증거", "빛", "엔딩", "되돌아가기",
                    "위화감", "HD")


def _o(name, pos, scale, mat, rot=None, face="", outline=True, collide=True, note=""):
    d = dict(kind="box", name=PREFIX + name,
             pos=dict(x=round(pos[0], 3), y=round(pos[1], 3), z=round(pos[2], 3)),
             scale=dict(x=round(scale[0], 3), y=round(scale[1], 3), z=round(scale[2], 3)),
             mat=mat, note=note, bridgeFor="", bridgeY=0.0, face=face)
    if rot:
        rx, ry, rz = rot
        ry = ((ry + 180.0) % 360.0) - 180.0
        # Y축 90° 배수 회전만 있으면 회전 대신 크기를 바꿔 축 정렬 상자로 둔다.
        # level_layout 의 기존 검사기(시야·겹침)는 회전을 모르고 scale 을 그대로
        # 발자국으로 읽는다. 여기서 풀어 두면 그 검사들이 그대로 정확하다.
        if abs(rx) < 0.01 and abs(rz) < 0.01 and abs(ry - round(ry / 90.0) * 90.0) < 0.01:
            if int(round(ry / 90.0)) % 2:
                sc = d["scale"]
                sc["x"], sc["z"] = sc["z"], sc["x"]
        else:
            d["rot"] = dict(x=round(rx, 2), y=round(ry, 2), z=round(rz, 2))
    if not outline:
        d["noOutline"] = True
    if not collide:
        d["noCollider"] = True
    return d


# ─────────────────────────────────────────── 좌표 — 벽 기준 로컬 틀
#
# 벽마다 (along, inward) 틀을 만든다. 모듈은 이 틀 안에서 한 번만 짜고,
# 네 벽에 똑같이 돌려 붙인다. 벽마다 따로 짜면 한 벽만 틀리는 버그가 생긴다.

# 벽마다 yaw 하나. 로컬 +z 가 방 안쪽, 로컬 +x 가 벽을 따라가게 돈다.
# (예전엔 크기를 벽 방향으로 바꿔 놓고 거기에 90° 를 또 돌려서, 동·서쪽 벽의
#  책장이 벽을 따라 서지 않고 방 안으로 튀어나왔다. 둘 중 하나만 해야 한다.)
WALL_YAW = {"south": 0.0, "west": 90.0, "north": 180.0, "east": -90.0}


class Frame:
    def __init__(self, face, hx, hz, inner):
        self.face = face
        self.yaw = WALL_YAW[face]
        th = math.radians(self.yaw)
        self.a = (math.cos(th), -math.sin(th))      # 로컬 +x
        self.i = (math.sin(th), math.cos(th))       # 로컬 +z = 방 안쪽
        half = hz if face in ("north", "south") else hx
        self.base = (-self.i[0] * (half - inner), -self.i[1] * (half - inner))
        self.length = (hx if face in ("north", "south") else hz) * 2

    def p(self, s, d, y):
        """벽을 따라 s, 방 안쪽으로 d, 높이 y → 방 로컬 (x, y, z)."""
        return (self.base[0] + self.a[0] * s + self.i[0] * d, y,
                self.base[1] + self.a[1] * s + self.i[1] * d)

    def size(self, w, h, dep):
        """벽 기준 (폭, 높이, 깊이). 회전(yaw)이 방향을 맞춘다."""
        return (w, h, dep)


# ─────────────────────────────────────────── 점유 격자

class Grid:
    def __init__(self, hx, hz):
        self.hx, self.hz = hx, hz
        self.nx = int(math.ceil(hx * 2 / CELL))
        self.nz = int(math.ceil(hz * 2 / CELL))
        # 0 빈칸 / 1 통로(낮은 것만 허용) / 2 막힘
        self.g = [[0] * self.nz for _ in range(self.nx)]

    def _idx(self, x, z):
        return int((x + self.hx) / CELL), int((z + self.hz) / CELL)

    def mark_rect(self, x0, x1, z0, z1, v):
        i0, k0 = self._idx(max(x0, -self.hx), max(z0, -self.hz))
        i1, k1 = self._idx(min(x1, self.hx - 1e-6), min(z1, self.hz - 1e-6))
        for i in range(max(i0, 0), min(i1, self.nx - 1) + 1):
            col = self.g[i]
            for k in range(max(k0, 0), min(k1, self.nz - 1) + 1):
                if col[k] < v:
                    col[k] = v

    def mark_disc(self, x, z, r, v):
        i0, k0 = self._idx(x - r, z - r)
        i1, k1 = self._idx(x + r, z + r)
        for i in range(max(i0, 0), min(i1, self.nx - 1) + 1):
            cx = -self.hx + (i + .5) * CELL
            for k in range(max(k0, 0), min(k1, self.nz - 1) + 1):
                cz = -self.hz + (k + .5) * CELL
                if (cx - x) ** 2 + (cz - z) ** 2 <= r * r and self.g[i][k] < v:
                    self.g[i][k] = v

    def mark_seg(self, x0, z0, x1, z1, w, v):
        n = max(2, int(math.hypot(x1 - x0, z1 - z0) / (CELL * 2)))
        for t in range(n + 1):
            u = t / n
            self.mark_disc(x0 + (x1 - x0) * u, z0 + (z1 - z0) * u, w / 2, v)

    def free(self, x0, x1, z0, z1, allow=0):
        """사각형 안이 전부 allow 이하인가. 방 밖이면 False."""
        if x0 < -self.hx or x1 > self.hx or z0 < -self.hz or z1 > self.hz:
            return False
        i0, k0 = self._idx(x0, z0)
        i1, k1 = self._idx(x1 - 1e-6, z1 - 1e-6)
        for i in range(max(i0, 0), min(i1, self.nx - 1) + 1):
            col = self.g[i]
            for k in range(max(k0, 0), min(k1, self.nz - 1) + 1):
                if col[k] > allow:
                    return False
        return True

    def free_cells(self, allow=0):
        out = []
        for i in range(self.nx):
            for k in range(self.nz):
                if self.g[i][k] <= allow:
                    out.append((-self.hx + (i + .5) * CELL, -self.hz + (k + .5) * CELL))
        return out


def _foot(o, margin=0.0):
    """오브젝트의 바닥 발자국(회전 포함 보수적 AABB)."""
    sx, sz = o["scale"]["x"], o["scale"]["z"]
    ry = math.radians((o.get("rot") or {}).get("y", 0.0))
    c, s = abs(math.cos(ry)), abs(math.sin(ry))
    ex = (sx * c + sz * s) / 2 + margin
    ez = (sx * s + sz * c) / 2 + margin
    x, z = o["pos"]["x"], o["pos"]["z"]
    return x - ex, x + ex, z - ez, z + ez


def _is_interactive(o):
    if o["kind"] in ("fold", "piece", "crease", "marker"):
        return True
    if o.get("bridgeFor"):
        return True
    if o.get("mat") == "accent":
        return True
    n = o["name"]
    return any(k in n for k in INTERACTIVE_KEYS)


# ─────────────────────────────────────────── 빛 경로

def _light_segments(a, sun, room_h, lit_targets):
    """빛 대상 → 천장. 이 선분들과 부딪히는 꾸미기는 버린다."""
    segs = []
    dx, dy, dz = -sun[0], -sun[1], -sun[2]           # 대상에서 태양 쪽
    t = room_h / dy
    for x, z in lit_targets(a):
        segs.append(((x, 0.2, z), (x + dx * t, room_h, z + dz * t)))
    return segs


def _seg_hits_aabb(p0, p1, lo, hi):
    tmin, tmax = 0.0, 1.0
    for i in range(3):
        d = p1[i] - p0[i]
        if abs(d) < 1e-9:
            if p0[i] < lo[i] or p0[i] > hi[i]:
                return False
            continue
        t0, t1 = (lo[i] - p0[i]) / d, (hi[i] - p0[i]) / d
        if t0 > t1:
            t0, t1 = t1, t0
        tmin, tmax = max(tmin, t0), min(tmax, t1)
        if tmin > tmax:
            return False
    return True


def _aabb3(o, pad=0.15):
    x0, x1, z0, z1 = _foot(o, pad)
    sy = o["scale"]["y"]
    rot = o.get("rot") or {}
    if abs(rot.get("x", 0)) > 1 or abs(rot.get("z", 0)) > 1:
        ext = max(o["scale"]["x"], o["scale"]["y"], o["scale"]["z"]) / 2
        return ((o["pos"]["x"] - ext - pad, o["pos"]["y"] - ext - pad, o["pos"]["z"] - ext - pad),
                (o["pos"]["x"] + ext + pad, o["pos"]["y"] + ext + pad, o["pos"]["z"] + ext + pad))
    return ((x0, o["pos"]["y"] - sy / 2 - pad, z0), (x1, o["pos"]["y"] + sy / 2 + pad, z1))


# ─────────────────────────────────────────── 그림자 스크린
#
# 접기 퍼즐 방에서 뒷벽과 바닥은 **그림자를 받는 화면** 이다(LevelBuilder:
# "그림자를 받아낼 뒷벽 — 없으면 퍼즐 검증이 불가능하다"). 접이면이 서면
# 그림자가 태양 방향으로 바닥을 지나 벽까지 올라간다. 그 길 위에 책장이나
# 몰딩이 있으면 그림자 모양이 깨져서 정답이 안 읽힌다.
#
# 접이면은 경첩(pos)에서 +z 로 scale.z 만큼 뻗고, -x 축으로 0/45/90/135° 선다
# (FoldPanel.foldAngles, hingeAxis = left). 각 상태의 네 모서리에서 태양 방향으로
# 광선을 쏴 바닥·벽에 닿는 점을 모은다. 그 점들과 패널을 감싼 상자가
# "손대지 말 곳" 이다. 보수적이지만 틀릴 수 없다.

FOLD_ANGLES = (0.0, 45.0, 90.0, 135.0)


def _shadow_zones(a, sun, margin=0.8):
    hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
    wall_n = hz - 0.25 if a.get("backWall", 0) > 0.01 else hz
    zones = []
    for o in a["objects"]:
        if o.get("kind") != "fold":
            continue
        px, py, pz = o["pos"]["x"], o["pos"]["y"], o["pos"]["z"]
        sx, L = o["scale"]["x"], o["scale"]["z"]
        pts = []
        for ang in FOLD_ANGLES:
            th = math.radians(ang)
            for dx in (-sx / 2, sx / 2):
                for l in (0.0, L):
                    pts.append((px + dx, py + l * math.sin(th), pz + l * math.cos(th)))
        hits = []
        for x, y, z in pts:
            ts = [y / -sun[1]]
            if sun[0] < 0:
                ts.append((x - (-hx + 0.25)) / -sun[0])
            elif sun[0] > 0:
                ts.append(((hx - 0.25) - x) / sun[0])
            if sun[2] > 0:
                ts.append((wall_n - z) / sun[2])
            t = max(0.0, min(ts))
            hits.append((x + sun[0] * t, y + sun[1] * t, z + sun[2] * t))
        allp = pts + hits
        lo = tuple(min(p[i] for p in allp) - margin for i in range(3))
        hi = tuple(max(p[i] for p in allp) + margin for i in range(3))
        zones.append((o["name"], (lo[0], -1.0, lo[2]), (hi[0], hi[1], hi[2])))
    return zones


def _aabb_overlap(lo1, hi1, lo2, hi2):
    return all(lo1[i] < hi2[i] and lo2[i] < hi1[i] for i in range(3))


# ─────────────────────────────────────────── 모듈 — 벽

def bookshelf(fr, s, w, h, rng, tag, ruin=False):
    """책장 한 칸. 책등 하나하나가 따로 논다 — 연필 외곽선이 책 사이를 그어 준다."""
    dep = 0.75
    out = []
    face = fr.face
    t = 0.10
    rows = max(3, int(h / 1.05))
    gap = (h - t) / rows

    def put(n, s_, d_, y_, w_, h_, dp_, mat, rot=None, outline=True, collide=True):
        # 벽 기준 회전(yaw) 위에 로컬 기울기를 얹는다
        r = rot or (0.0, 0.0, 0.0)
        out.append(_o(f"{tag}_{n}", fr.p(s_, d_, y_), fr.size(w_, h_, dp_), mat,
                      rot=(r[0], fr.yaw + r[1], r[2]),
                      face=face, outline=outline, collide=collide))

    # 틀
    put("옆L", s - w / 2 + t / 2, dep / 2, h / 2, t, h, dep, "frame")
    put("옆R", s + w / 2 - t / 2, dep / 2, h / 2, t, h, dep, "frame")
    put("위", s, dep / 2, h - t / 2, w, t, dep, "frame")
    put("뒤", s, 0.04, h / 2, w - 2 * t, h, 0.06, "frameDk", outline=False)
    for r_ in range(rows):
        y0 = r_ * gap
        put(f"칸{r_}", s, dep / 2, y0 + t / 2, w - 2 * t, t, dep, "frame")

        # 책 — 폭을 채우되 가끔 비우고, 가끔 눕히고, 가끔 기댄다
        if ruin and rng.random() < 0.45:
            continue                        # 무너진 서가 — 칸이 통째로 빈다
        x = s - w / 2 + t + 0.02
        end = s + w / 2 - t - 0.02
        lean_used = False
        k = 0
        while x < end - 0.1:
            k += 1
            roll = rng.random()
            if roll < 0.07:                 # 빈틈
                x += rng.uniform(0.15, 0.4)
                continue
            if roll < 0.15 and end - x > 0.8:   # 눕혀 쌓은 책 3권
                bw = rng.uniform(0.55, 0.75)
                yy = y0 + t
                for m in range(rng.randint(2, 3)):
                    bh = rng.uniform(0.08, 0.13)
                    put(f"칸{r_}_눕{k}_{m}", x + bw / 2 + rng.uniform(-.03, .03), dep * 0.5,
                        yy + bh / 2, bw, bh, dep * 0.8, rng.choice(SPINES), outline=False, collide=False)
                    yy += bh
                x += bw + 0.03
                continue
            bw = rng.uniform(0.09, 0.2)
            bh = gap * rng.uniform(0.55, 0.88)
            if x + bw > end:
                break
            if not lean_used and roll > 0.93 and end - x > 0.5:
                # 기댄 책 한 권 — 줄 맞춘 선반을 깨는 한 획
                ang = rng.choice((-1, 1)) * rng.uniform(10, 18)
                put(f"칸{r_}_기댐{k}", x + bw / 2 + 0.05, dep * 0.5, y0 + t + bh / 2,
                    bw, bh, dep * 0.8, rng.choice(SPINES), rot=(0, 0, ang), outline=False, collide=False)
                lean_used = True
                x += bw + 0.18
                continue
            put(f"칸{r_}_책{k}", x + bw / 2, dep * 0.5 + rng.uniform(-.04, .04), y0 + t + bh / 2,
                bw, bh, dep * rng.uniform(0.7, 0.85), rng.choice(SPINES), outline=False, collide=False)
            x += bw + rng.uniform(0.0, 0.02)
    return out, dep


def giant_lean_book(fr, s, h, rng, tag):
    """벽에 기댄 거인 책 — 이 탑의 이름 그대로. 벽 한 칸을 크게 먹는다."""
    w = rng.uniform(2.0, 2.6)
    th = rng.uniform(0.45, 0.65)
    ang = -rng.uniform(8, 13)          # 벽에서 방 쪽으로 기운다
    cov = rng.choice(COVERS)
    d0 = th / 2 + math.sin(math.radians(-ang)) * h / 2 + 0.08
    out = []
    face = fr.face
    for n, (dd, dw, dh, mat) in enumerate(((-th / 2 + 0.05, w, h, cov),
                                           (0, w - 0.18, h - 0.14, "page"),
                                           (th / 2 - 0.05, w, h, cov))):
        out.append(_o(f"{tag}_{n}", fr.p(s, d0 + dd, h / 2 * math.cos(math.radians(ang))),
                      fr.size(dw, dh, 0.1 if mat != "page" else th - 0.12), mat,
                      rot=(ang, fr.yaw, 0.0), face=face, outline=(mat != "page"), collide=True))
    # 책등 띠
    out.append(_o(f"{tag}_띠", fr.p(s - w / 2 + 0.05, d0, h / 2 * math.cos(math.radians(ang))),
                  fr.size(0.12, h, th), cov, rot=(ang, fr.yaw, 0.0), face=face, outline=False, collide=False))
    return out, d0 + th


def pinned_pages(fr, s, w, y_lo, y_hi, rng, tag, ribbon=True):
    """벽에 핀으로 꽂은 페이지 — 필사실. 바닥 발자국이 거의 없다."""
    out = []
    n = rng.randint(3, 6)
    for k in range(n):
        pw, ph = rng.uniform(0.5, 0.8), rng.uniform(0.65, 1.0)
        ss = s + rng.uniform(-w / 2 + pw / 2, w / 2 - pw / 2)
        yy = rng.uniform(y_lo + ph / 2, y_hi - ph / 2)
        out.append(_o(f"{tag}_{k}", fr.p(ss, 0.03, yy), fr.size(pw, ph, 0.02),
                      rng.choice(("page", "page", "pageOld")),
                      rot=(0, fr.yaw, rng.uniform(-9, 9)), face=fr.face, collide=False))
        if ribbon and rng.random() < 0.35:
            out.append(_o(f"{tag}_{k}_끈", fr.p(ss + pw * 0.25, 0.05, yy - ph / 2 - 0.25),
                          fr.size(0.07, 0.6, 0.015), "ribbon",
                          rot=(0, fr.yaw, rng.uniform(-6, 6)), face=fr.face, outline=False, collide=False))
    return out, 0.1


def hanging_ribbons(fr, s, w, h, rng, tag):
    """천장 쪽에서 늘어뜨린 책갈피 끈 — 표지 방의 모티프. 발자국 없음."""
    out = []
    for k in range(rng.randint(2, 4)):
        ln = rng.uniform(1.6, 3.2)
        ss = s + rng.uniform(-w / 2, w / 2)
        out.append(_o(f"{tag}_{k}", fr.p(ss, 0.12, h - ln / 2 - 0.2), fr.size(0.14, ln, 0.02), "ribbon",
                      rot=(0, fr.yaw, rng.uniform(-3, 3)), face=fr.face, outline=True, collide=False))
    return out, 0.15


# ─────────────────────────────────────────── 모듈 — 바닥

def giant_book_stack(x, z, rng, tag, face):
    """쌓인 거인 책. 모서리를 채우는 덩어리."""
    out = []
    y = 0.0
    n = rng.randint(2, 4)
    base_yaw = rng.uniform(-10, 10)
    for k in range(n):
        w, d = rng.uniform(2.2, 2.9), rng.uniform(1.6, 2.1)
        th = rng.uniform(0.38, 0.58)
        yaw = base_yaw + rng.uniform(-9, 9)
        cov = rng.choice(COVERS)
        cx, cz = x + rng.uniform(-.15, .15), z + rng.uniform(-.15, .15)
        out.append(_o(f"{tag}_{k}_표지", (cx, y + th / 2, cz), (w, th, d), cov, rot=(0, yaw, 0), face=face))
        out.append(_o(f"{tag}_{k}_면", (cx + 0.06, y + th / 2, cz), (w - 0.1, th - 0.12, d + 0.02), "page",
                      rot=(0, yaw, 0), face=face, outline=False, collide=False))
        y += th
    if rng.random() < 0.6:                 # 맨 위에 작은 책 몇 권 — 스케일 대비
        for m in range(rng.randint(1, 3)):
            bw, bd, bh = rng.uniform(0.7, 1.0), rng.uniform(0.5, 0.7), rng.uniform(0.12, 0.2)
            out.append(_o(f"{tag}_작은{m}", (x + rng.uniform(-.4, .4), y + bh / 2, z + rng.uniform(-.3, .3)),
                          (bw, bh, bd), rng.choice(SPINES), rot=(0, rng.uniform(-40, 40), 0), face=face,
                          collide=False))
            y += bh
    return out, y


def reading_desk(x, z, yaw, rng, tag):
    """독서대 — 펼친 책, 잉크, 깃펜, 초. 필사실의 중심."""
    out = []
    W, D, H = 2.2, 1.2, 1.0
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def L(dx, dz):                          # 책상 기준 → 로컬
        return x + dx * c + dz * s, z - dx * s + dz * c

    px, pz = L(0, 0)
    out.append(_o(f"{tag}_상판", (px, H, pz), (W, 0.1, D), "frame", rot=(0, yaw, 0)))
    for n, (dx, dz) in enumerate(((-W / 2 + .1, -D / 2 + .1), (W / 2 - .1, -D / 2 + .1),
                                  (-W / 2 + .1, D / 2 - .1), (W / 2 - .1, D / 2 - .1))):
        lx, lz = L(dx, dz)
        out.append(_o(f"{tag}_다리{n}", (lx, H / 2, lz), (0.1, H, 0.1), "frameDk", rot=(0, yaw, 0),
                      outline=False))
    # 펼친 책 — 두 면이 살짝 V 로
    for n, sgn in enumerate((-1, 1)):
        bx, bz = L(sgn * 0.32 - 0.2, 0)
        out.append(_o(f"{tag}_책{n}", (bx, H + 0.1, bz), (0.62, 0.06, 0.85), "page",
                      rot=(0, yaw, sgn * 7), collide=False))
    cx, cz = L(-0.2, 0)
    out.append(_o(f"{tag}_책등", (cx, H + 0.07, cz), (0.08, 0.05, 0.86), rng.choice(COVERS),
                  rot=(0, yaw, 0), outline=False, collide=False))
    ix, iz = L(0.75, -0.25)
    out.append(_o(f"{tag}_잉크", (ix, H + 0.14, iz), (0.2, 0.18, 0.2), "ink", rot=(0, yaw, 0), collide=False))
    qx, qz = L(0.78, -0.2)
    out.append(_o(f"{tag}_깃", (qx, H + 0.3, qz), (0.04, 0.42, 0.16), "page", rot=(0, yaw, 14),
                  collide=False))
    kx, kz = L(0.75, 0.32)
    out.append(_o(f"{tag}_초", (kx, H + 0.2, kz), (0.12, 0.3, 0.12), "candle", rot=(0, yaw, 0), collide=False))
    out.append(_o(f"{tag}_불", (kx, H + 0.4, kz), (0.06, 0.1, 0.06), "flame", rot=(0, yaw, 0),
                  outline=False, collide=False))
    # 책상 아래 쌓인 책
    for m in range(rng.randint(1, 3)):
        bx, bz = L(rng.uniform(-0.6, 0.6), rng.uniform(-0.2, 0.2))
        out.append(_o(f"{tag}_아래{m}", (bx, 0.09 + m * 0.16, bz), (0.8, 0.15, 0.55), rng.choice(SPINES),
                      rot=(0, yaw + rng.uniform(-25, 25), 0), collide=False))
    return out


def open_giant_book(x, z, yaw, rng, tag):
    """바닥에 펼쳐진 거인 책 — 낮아서 시야를 안 막고, 방 한가운데 '섬' 이 된다."""
    out = []
    W, D = rng.uniform(3.2, 4.2), rng.uniform(2.4, 3.0)
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    cov = rng.choice(COVERS)
    out.append(_o(f"{tag}_표지", (x, 0.06, z), (W + 0.2, 0.12, D + 0.2), cov, rot=(0, yaw, 0)))
    for n, sgn in enumerate((-1, 1)):
        dx = sgn * W / 4
        px, pz = x + dx * c, z - dx * s
        out.append(_o(f"{tag}_면{n}", (px, 0.24, pz), (W / 2 - 0.08, 0.22, D), "page",
                      rot=(0, yaw, -sgn * 4)))
    # 글줄 대신 가는 선 몇 개 — 멀리서 '글이 있는 면' 으로 읽히게
    for n in range(6):
        sgn = -1 if n < 3 else 1
        dx = sgn * W / 4
        dz = -D / 2 + 0.5 + (n % 3) * (D - 1.0) / 2
        px, pz = x + dx * c + dz * s, z - dx * s + dz * c
        out.append(_o(f"{tag}_줄{n}", (px, 0.36, pz), (W / 2 - 0.6, 0.01, 0.05), "pageOld",
                      rot=(0, yaw, -sgn * 4), outline=False, collide=False))
    if rng.random() < 0.7:
        dx = rng.uniform(-0.2, 0.2)
        out.append(_o(f"{tag}_끈", (x + dx * c, 0.38, z - dx * s), (0.12, 0.02, D + 1.2), "ribbon",
                      rot=(0, yaw + rng.uniform(-8, 8), 0), outline=True, collide=False))
    return out, (W + 0.4, D + 0.4)


def toppled_shelf(x, z, yaw, rng, tag):
    """쓰러진 책장 — 3장의 파손. 누운 틀과 쏟아진 책."""
    out = []
    L_, H_ = rng.uniform(3.0, 4.0), 0.75
    out.append(_o(f"{tag}_틀", (x, H_ / 2, z), (L_, H_, 0.9), "frameDk", rot=(0, yaw, 0)))
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for m in range(rng.randint(5, 9)):
        dx, dz = rng.uniform(-L_ / 2, L_ / 2), rng.uniform(0.6, 1.8) * rng.choice((-1, 1))
        px, pz = x + dx * c + dz * s, z - dx * s + dz * c
        out.append(_o(f"{tag}_책{m}", (px, 0.08, pz), (rng.uniform(0.5, 0.8), 0.14, rng.uniform(0.35, 0.5)),
                      rng.choice(SPINES), rot=(0, rng.uniform(0, 180), rng.uniform(-6, 6)), collide=False))
    return out, (L_ + 0.5, 4.2)


def torn_giant_page(x, z, yaw, rng, tag):
    """찢겨 바닥에 떨어진 큰 페이지 — 3장의 여백 안에 놓는 큰 획 하나."""
    w, d = rng.uniform(2.4, 3.4), rng.uniform(3.0, 4.2)
    out = [_o(f"{tag}", (x, 0.02, z), (w, 0.03, d), "pageOld", rot=(0, yaw, 0))]
    # 찢긴 가장자리 — 작은 삼각 조각 대신 어긋난 띠 두 개
    for n in range(2):
        out.append(_o(f"{tag}_찢김{n}", (x + rng.uniform(-w / 3, w / 3), 0.035, z + (d / 2 + 0.2) * (1 if n else -1)),
                      (rng.uniform(0.6, 1.2), 0.02, 0.35), "pageOld", rot=(0, yaw + rng.uniform(-20, 20), 0),
                      collide=False))
    return out, (w + 0.4, d + 0.8)


def rug(x, z, w, d, yaw, tag):
    return [
        _o(f"{tag}_가장자리", (x, 0.012, z), (w, 0.02, d), "rugEdge", rot=(0, yaw, 0), collide=False),
        _o(f"{tag}", (x, 0.02, z), (w - 0.5, 0.02, d - 0.5), "rug", rot=(0, yaw, 0), outline=False,
           collide=False),
    ]


def book_pile(x, z, rng, tag):
    """바닥의 낮은 책 더미 — 방 가운데 '섬'. 0.9m 를 안 넘어 시야를 안 막는다."""
    out = []
    for st_ in range(rng.randint(1, 2)):
        ox, oz = (rng.uniform(-.45, .45), rng.uniform(-.45, .45)) if st_ else (0.0, 0.0)
        y = 0.0
        base = rng.uniform(0, 180)
        for k in range(rng.randint(3, 6)):
            bh = rng.uniform(0.12, 0.2)
            if y + bh > 0.9:
                break
            out.append(_o(f"{tag}_{st_}_{k}", (x + ox + rng.uniform(-.06, .06), y + bh / 2, z + oz + rng.uniform(-.06, .06)),
                          (rng.uniform(0.7, 1.0), bh, rng.uniform(0.5, 0.7)), rng.choice(SPINES),
                          rot=(0, base + rng.uniform(-18, 18), 0), collide=k < 2))
            y += bh
    if rng.random() < 0.7:
        out.append(_o(f"{tag}_펼침", (x + rng.uniform(-.9, .9), 0.03, z + rng.uniform(-.9, .9)),
                      (0.95, 0.05, 0.7), "page", rot=(0, rng.uniform(0, 180), 0), collide=False))
    return out


def floor_seams(a, step=3.2):
    """바닥 이음선. 납작해서(0.006) 길·빛·그림자 어느 것도 안 건드린다."""
    hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
    out = []
    k = int(hx * 2 / step)
    for i in range(1, k):
        x = -hx + i * (hx * 2 / k)
        out.append(_o(f"바닥이음_x{i}", (x, 0.003, 0), (0.05, 0.006, hz * 2 - 0.6), "seam",
                      outline=False, collide=False))
    k = int(hz * 2 / step)
    for i in range(1, k):
        z = -hz + i * (hz * 2 / k)
        out.append(_o(f"바닥이음_z{i}", (0, 0.003, z), (hx * 2 - 0.6, 0.006, 0.05), "seam",
                      outline=False, collide=False))
    return out


def _is_flat_seam(o):
    return "바닥이음" in o["name"]


# ─────────────────────────────────────────── 모듈 — 흩뿌림 (발목 아래)


def loose_pages(x, z, rng, tag):
    out = []
    for k in range(rng.randint(2, 5)):
        out.append(_o(f"{tag}_{k}", (x + rng.uniform(-.8, .8), 0.01 + k * 0.004, z + rng.uniform(-.8, .8)),
                      (rng.uniform(0.4, 0.55), 0.008, rng.uniform(0.55, 0.72)),
                      rng.choice(("page", "page", "pageOld")),
                      rot=(0, rng.uniform(0, 180), 0), outline=True, collide=False))
    return out


def fallen_books(x, z, rng, tag):
    out = []
    for k in range(rng.randint(1, 3)):
        out.append(_o(f"{tag}_{k}", (x + rng.uniform(-.5, .5), 0.09 + k * 0.02, z + rng.uniform(-.5, .5)),
                      (rng.uniform(0.6, 0.9), rng.uniform(0.12, 0.18), rng.uniform(0.42, 0.6)),
                      rng.choice(SPINES), rot=(0, rng.uniform(0, 180), rng.uniform(-4, 4)), collide=False))
    return out


def ribbon_trail(x, z, rng, tag):
    """바닥에 흘러간 책갈피 끈 — 몇 토막이 이어진다. 붉은 점이 방에 리듬을 준다."""
    out = []
    ang = rng.uniform(0, 180)
    px, pz = x, z
    for k in range(rng.randint(3, 5)):
        ln = rng.uniform(0.5, 0.9)
        out.append(_o(f"{tag}_{k}", (px, 0.015, pz), (0.12, 0.012, ln), "ribbon",
                      rot=(0, ang, 0), outline=False, collide=False))
        rad = math.radians(ang)
        px += math.sin(rad) * ln * 0.95
        pz += math.cos(rad) * ln * 0.95
        ang += rng.uniform(-35, 35)
    return out


def scraps(x, z, rng, tag):
    out = []
    for k in range(rng.randint(3, 6)):
        out.append(_o(f"{tag}_{k}", (x + rng.uniform(-1, 1), 0.008, z + rng.uniform(-1, 1)),
                      (rng.uniform(0.15, 0.4), 0.006, rng.uniform(0.12, 0.35)), "pageOld",
                      rot=(0, rng.uniform(0, 180), 0), outline=False, collide=False))
    if rng.random() < 0.5:
        out.append(_o(f"{tag}_그을음", (x, 0.004, z), (rng.uniform(1.2, 2.2), 0.004, rng.uniform(0.8, 1.5)),
                      "soot", rot=(0, rng.uniform(0, 180), 0), outline=False, collide=False))
    return out


# ─────────────────────────────────────────── 벽 마감
#
# 벽은 화면 위쪽 절반을 차지하는데 통째로 한 장짜리 회색 판이었다.
# 실제 서재 벽처럼 층을 준다 — 걸레받이 · 아랫단 판 · 허리 몰딩 · 코니스 · 기둥.
# 하나하나는 얇지만 벽이 '만든 것' 으로 읽히게 된다. 전부 그 벽의 face 를 달아
# 벽이 숨을 때 같이 숨는다. 콜라이더는 없다 — 벽이 이미 막는다.

INWARD = {"north": (0, -1), "south": (0, 1), "east": (-1, 0), "west": (1, 0)}


def wall_pieces(a, room_h):
    """이 방의 벽 조각들: (face, 벽을 따라 lo, hi, 안쪽 면 좌표, 높이)."""
    out = []
    for o in a["objects"]:
        f = o.get("face")
        if f not in INWARD:
            continue
        along = "x" if f in ("north", "south") else "z"
        perp = "z" if along == "x" else "x"
        c, w = o["pos"][along], o["scale"][along]
        t = o["scale"][perp]
        ix, iz = INWARD[f]
        inner = o["pos"][perp] + (iz if perp == "z" else ix) * t / 2
        out.append((f, c - w / 2, c + w / 2, inner, o["pos"]["y"] + o["scale"]["y"] / 2))
    if a.get("backWall", 0) > 0.01 and not any(p[0] == "north" for p in out):
        hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
        # LevelBuilder 가 따로 세우는 뒷벽 — 방 폭 전체, 두께 0.5
        out.append(("north", -hx, hx, hz - 0.25, a["backWall"]))
    return out


def wall_trim(a, grid, room_h, rng, commit):
    for n, (f, lo, hi, inner, top) in enumerate(wall_pieces(a, room_h)):
        ix, iz = INWARD[f]
        along_x = f in ("north", "south")
        yaw = {"north": 0, "south": 180, "east": 90, "west": -90}[f]

        def P(s_, d_, y_):
            if along_x:
                return (s_, y_, inner + iz * d_)
            return (inner + ix * d_, y_, s_)

        def S(w_, h_, d_):
            return (w_, h_, d_) if along_x else (d_, h_, w_)

        # 퍼즐이 걸린 벽 구간(격자 2)은 건너뛴다 — 연속 구간으로 나눈다
        runs, cur = [], None
        step = 0.25
        s_ = lo + 0.05
        while s_ < hi - 0.05:
            x, _, z = P(s_, 0.3, 0)
            ok = grid.free(x - 0.12, x + 0.12, z - 0.12, z + 0.12, allow=1)
            if ok and cur is None:
                cur = [s_, s_]
            elif ok:
                cur[1] = s_
            elif cur is not None:
                runs.append(cur); cur = None
            s_ += step
        if cur is not None:
            runs.append(cur)

        h_wall = min(top, room_h)
        for r_i, (r0, r1) in enumerate(runs):
            L = r1 - r0 + step
            if L < 0.8:
                continue
            mid = (r0 + r1) / 2
            tag = f"벽마감{n}_{r_i}"
            parts = [
                _o(f"{tag}_아랫단", P(mid, 0.03, 0.85), S(L, 1.2, 0.05), "wainscot",
                   face=f, outline=False, collide=False),
                _o(f"{tag}_걸레받이", P(mid, 0.07, 0.17), S(L, 0.34, 0.13), "trim",
                   face=f, outline=True, collide=False),
                _o(f"{tag}_허리", P(mid, 0.06, 1.47), S(L, 0.1, 0.11), "trim",
                   face=f, outline=True, collide=False),
            ]
            if h_wall > 3.5:
                parts.append(_o(f"{tag}_코니스", P(mid, 0.1, h_wall - 0.35), S(L, 0.28, 0.2), "trim",
                                face=f, outline=True, collide=False))
            # 기둥 — 약 4.8m 마다, 구간 끝에서 0.6 안쪽
            k = int((L - 1.2) / 4.8)
            for m in range(k + 1):
                sp = r0 + 0.6 + (L - 1.2) * (m / k if k else 0.5)
                parts.append(_o(f"{tag}_기둥{m}", P(sp, 0.11, h_wall / 2), S(0.46, h_wall, 0.22), "trim",
                                face=f, outline=True, collide=False))
            commit(parts)


# ─────────────────────────────────────────── 한 방 꾸미기

def dress_area(a, ramps, sun, room_h_fn, lit_targets):
    rng = random.Random(zlib.crc32(a["id"].encode("utf-8")))
    st = STYLE.get(a["floor"], STYLE[1])
    hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
    H = room_h_fn(a)
    grid = Grid(hx, hz)
    inner = 0.27                               # 벽 두께 절반 + 여유

    # ── 1) 비워 둘 곳
    hot = [(0.0, 0.0)]
    for o in a["objects"]:
        # 방 껍데기(벽·천장)는 비워 둘 곳이 아니다. 이름으로 거르면 안 된다 —
        # 2E·3A 는 천장 이름이 '천장' 이라('천창테' 가 아니라) 방 전체가
        # 막힌 것으로 잡혀서 꾸미기가 0개였다. face 가 곧 껍데기라는 뜻이다.
        if o.get("face") and not _is_interactive(o):
            continue
        if _is_interactive(o):
            x0, x1, z0, z1 = _foot(o, 2.2)
            grid.mark_rect(x0, x1, z0, z1, 2)
            hot.append((o["pos"]["x"], o["pos"]["z"]))
        else:
            x0, x1, z0, z1 = _foot(o, 0.6)
            grid.mark_rect(x0, x1, z0, z1, 2)
    q = a.get("puzzle")
    if q and q.get("hasAt"):
        grid.mark_disc(q["at"]["x"], q["at"]["z"], 3.0, 2)
        hot.append((q["at"]["x"], q["at"]["z"]))

    # 1B 처럼 바닥이 갈라진 방
    if a.get("splitGap", 0) > 0.01:
        g = a["splitGap"] / 2 + 0.6
        grid.mark_rect(-hx, hx, -g, g, 2)

    # 문(경사로 입구) — 입구 앞을 넓게 비우고, 가운데와 퍼즐까지 통로를 낸다
    doors = []
    for r in ramps:
        for key, rid in (("from", r.get("fromId")), ("to", r.get("toId"))):
            if rid != a["id"]:
                continue
            p = r[key]
            lx, lz = p["x"] - a["pos"]["x"], p["z"] - a["pos"]["z"]
            lx, lz = max(-hx, min(hx, lx)), max(-hz, min(hz, lz))
            doors.append((lx, lz, r.get("width", 4.0)))
    for lx, lz, w in doors:
        grid.mark_disc(lx, lz, w / 2 + 2.5, 2)
        for hx_, hz_ in hot:
            grid.mark_seg(lx, lz, hx_, hz_, 3.2, 1)
    for i in range(len(hot)):
        for j in range(i + 1, len(hot)):
            grid.mark_seg(hot[i][0], hot[i][1], hot[j][0], hot[j][1], 2.6, 1)

    light = _light_segments(a, sun, H, lit_targets)
    shadow = _shadow_zones(a, sun)
    for _, lo, hi in shadow:
        grid.mark_rect(lo[0], hi[0], lo[2], hi[2], 2)

    # 카메라에서 보여야 하는 것 — level_layout 검사 7번과 같은 대상
    see = [o for o in a["objects"] if o["kind"] in ("fold", "piece", "crease", "marker")]
    tan_p = math.tan(math.radians(30.0))
    hidden = {0: "south", 1: "west", 2: "north", 3: "east"}

    def ok_sight(objs):
        """높은 꾸미기가 퍼즐 요소를 카메라 사각지대에 넣지 않는가.

        검사 7번은 방의 기본 각도만 본다. 꾸미기는 Q/E 로 돌렸을 때도
        가리면 안 되므로 네 방향을 다 본다. 그 방향에서 숨는 벽에 붙은 건 뺀다."""
        for o in objs:
            _, hi = _aabb3(o, pad=0.0)
            top = hi[1]
            if top < 1.6:
                continue
            x0, x1, z0, z1 = _foot(o)
            ox, oz = (x0 + x1) / 2, (z0 + z1) / 2
            ex, ez = (x1 - x0) / 2, (z1 - z0) / 2
            for yaw in range(4):
                if o.get("face") == hidden[yaw]:
                    continue
                for t in see:
                    dx, dz = t["pos"]["x"] - ox, t["pos"]["z"] - oz
                    if yaw == 0:   along, lat, half, ext = dz, dx, ex, ez
                    elif yaw == 1: along, lat, half, ext = dx, dz, ez, ex
                    elif yaw == 2: along, lat, half, ext = -dz, dx, ex, ez
                    else:          along, lat, half, ext = -dx, dz, ez, ex
                    depth = (top - t["pos"]["y"]) / tan_p
                    if depth > 0 and -ext < along < depth + ext and abs(lat) < half + 0.8:
                        return False
        return True

    placed = []

    def ok_light(objs):
        for o in objs:
            lo, hi = _aabb3(o)
            for p0, p1 in light:
                if _seg_hits_aabb(p0, p1, lo, hi):
                    return False
        return True

    def ok_shadow(objs):
        for o in objs:
            lo, hi = _aabb3(o, pad=0.0)
            for _, zlo, zhi in shadow:
                if _aabb_overlap(lo, hi, zlo, zhi):
                    return False
        return True

    def commit(objs, rect=None, level=2):
        if not ok_light(objs) or not ok_sight(objs) or not ok_shadow(objs):
            return False
        placed.extend(objs)
        if rect:
            grid.mark_rect(*rect, level)
        return True

    # ── 2) 벽 마감 — 모듈보다 먼저. 책장이 그 앞에 선다
    wall_trim(a, grid, H, rng, commit)

    # ── 2') 벽 모듈
    faces = ["north", "east", "west", "south"]
    shelf_h = min(4.4, H * 0.68)
    n_mod = 0
    for face in faces:
        fr = Frame(face, hx, hz, inner)
        half_len = fr.length / 2 - 0.4
        s = -half_len
        # 카메라 기본 방향 쪽 벽은 덜 채운다 — 대부분의 시간 숨어 있다
        density = 0.55 if face == "south" else 1.0
        while s < half_len - 0.6:
            roll = rng.random()
            if roll > density:
                s += rng.uniform(1.0, 2.0)
                continue
            kind = rng.random()
            if kind < st["shelf"]:
                w = rng.choice((2.0, 2.4, 2.4, 2.8))
                gen = lambda s_c: bookshelf(fr, s_c, w, shelf_h, rng, f"책장{n_mod}",
                                            ruin=rng.random() < st["ruin"])
                need = w
            elif kind < st["shelf"] + st["lean"]:
                w = 2.8
                gen = lambda s_c: giant_lean_book(fr, s_c, rng.uniform(3.0, 4.2), rng, f"기댄책{n_mod}")
                need = w
            elif kind < st["shelf"] + st["lean"] + st["pinned"]:
                w = rng.uniform(2.0, 3.2)
                gen = lambda s_c: pinned_pages(fr, s_c, w, 1.4, min(H - 0.8, 4.6), rng, f"핀{n_mod}",
                                               ribbon=st["ribbon"] > 0.2)
                need = w
            else:
                if st["ribbon"] > 0.3:
                    w = 1.6
                    gen = lambda s_c: hanging_ribbons(fr, s_c, w, H, rng, f"매단끈{n_mod}")
                    need = w
                else:
                    s += rng.uniform(0.8, 1.6)
                    continue
            s_c = s + need / 2
            if s_c + need / 2 > half_len:
                break
            objs, depth = gen(s_c)
            # 발자국 — 벽을 따라 need, 안쪽으로 depth
            x0, _, z0 = fr.p(s_c - need / 2, 0, 0)
            x1, _, z1 = fr.p(s_c + need / 2, depth + 0.1, 0)
            rect = (min(x0, x1), max(x0, x1), min(z0, z1), max(z0, z1))
            if grid.free(*rect, allow=0) and commit(objs, rect):
                n_mod += 1
                s += need + rng.uniform(0.05, 0.9)
            else:
                s += 0.5

    # ── 3) 모서리 — 쌓인 거인 책
    for cx_, cz_ in ((hx, hz), (-hx, hz), (hx, -hz), (-hx, -hz)):
        if rng.random() > st["corner"]:
            continue
        x = cx_ - math.copysign(inner + 1.8, cx_)
        z = cz_ - math.copysign(inner + 1.5, cz_)
        rect = (x - 1.7, x + 1.7, z - 1.4, z + 1.4)
        face = "north" if cz_ > 0 else "south"
        if grid.free(*rect, allow=0):
            objs, top = giant_book_stack(x, z, rng, f"거인책더미{len(placed)}", face)
            commit(objs, rect)

    # ── 4) 방 안의 섬 — 독서대(+러그), 펼친 거인 책, 쓰러진 책장, 찢긴 큰 페이지
    def try_place(fp, gen, tries=60, margin_wall=1.2, level=2):
        for _ in range(tries):
            x = rng.uniform(-hx + margin_wall + fp[0] / 2, hx - margin_wall - fp[0] / 2)
            z = rng.uniform(-hz + margin_wall + fp[1] / 2, hz - margin_wall - fp[1] / 2)
            rect = (x - fp[0] / 2, x + fp[0] / 2, z - fp[1] / 2, z + fp[1] / 2)
            if grid.free(*rect, allow=0):
                objs = gen(x, z)
                if commit(objs, rect, level):
                    return True
        return False

    area_m2 = hx * hz * 4
    n_desk = st["desk"] if area_m2 > 450 else min(st["desk"], 1)
    for k in range(n_desk):
        yaw = rng.choice((0, 90, 0, 90)) + rng.uniform(-8, 8)
        def gen(x, z, k=k, yaw=yaw):
            objs = reading_desk(x, z, yaw, rng, f"독서대{k}")
            if st["rug"]:
                objs = rug(x, z, 4.6, 3.4, yaw, f"러그{k}") + objs
            return objs
        try_place((5.0, 4.0), gen)

    for k in range(st["openbook"] + (1 if area_m2 > 1100 else 0)):
        yaw = rng.uniform(-25, 25)
        try_place((4.8, 3.6), lambda x, z, k=k, yaw=yaw: open_giant_book(x, z, yaw, rng, f"펼친책{k}")[0])

    if st["ruin"] > 0:
        for k in range(1 + int(area_m2 / 700)):
            if rng.random() < st["ruin"] * 1.6:
                yaw = rng.uniform(0, 180)
                try_place((4.4, 4.4), lambda x, z, k=k, yaw=yaw: toppled_shelf(x, z, yaw, rng, f"쓰러진책장{k}")[0])
        for k in range(1 + int(area_m2 / 600)):
            yaw = rng.uniform(0, 180)
            try_place((3.6, 4.6), lambda x, z, k=k, yaw=yaw: torn_giant_page(x, z, yaw, rng, f"큰페이지{k}")[0],
                      level=1)

    # ── 4-a) 통로 러너 — 문에서 방 가운데로 깔린 긴 카펫.
    #   큰 바닥에 '사람이 다니던 길' 이 생긴다. 납작하고 콜라이더가 없다.
    inter = [_foot(o, 0.8) for o in a["objects"] if _is_interactive(o) and not o.get("face")]

    def free_of_inter(x0, x1, z0, z1):
        return not any(x0 < b[1] and b[0] < x1 and z0 < b[3] and b[2] < z1 for b in inter)

    if st["rug"]:
        for n_r, (lx, lz, w) in enumerate(doors):
            # 문이 붙은 벽에서 곧장 안쪽으로 — 대각선 러너는 판자처럼 보였다
            if hx - abs(lx) < hz - abs(lz):
                ux, uz, dist = -math.copysign(1, lx), 0.0, abs(lx)
            else:
                ux, uz, dist = 0.0, -math.copysign(1, lz), abs(lz)
            lat = abs(lz) if ux else abs(lx)
            lat_half = hz if ux else hx
            if dist < 6 or lat > lat_half - 4.0:
                continue                     # 구석 문에서 벽을 따라 뻗으면 긴 의자처럼 보였다
            yaw = math.degrees(math.atan2(ux, uz))
            runs, cur, t = [], None, 1.0
            while t < dist + 0.1:
                cx, cz = lx + ux * t, lz + uz * t
                piece = [_o("x", (cx, 0.02, cz), (2.4, 0.03, 1.0), "rug", rot=(0, yaw, 0))]
                x0, x1, z0, z1 = _foot(piece[0])
                ok = (abs(cx) < hx - 1.2 and abs(cz) < hz - 1.2 and free_of_inter(x0, x1, z0, z1)
                      and ok_shadow(piece) and ok_light(piece))
                if ok:
                    cur = [t, t] if cur is None else [cur[0], t]
                elif cur is not None:
                    runs.append(cur); cur = None
                t += 0.5
            if cur is not None:
                runs.append(cur)
            for m, (t0, t1) in enumerate(runs):
                L = t1 - t0 + 1.0
                if L < 4.0:
                    continue
                tm = (t0 + t1) / 2
                cx, cz = lx + ux * tm, lz + uz * tm
                placed.extend([
                    _o(f"러너{n_r}_{m}_가장자리", (cx, 0.012, cz), (2.4, 0.02, L), "rugEdge", rot=(0, yaw, 0),
                       collide=False),
                    _o(f"러너{n_r}_{m}", (cx, 0.02, cz), (1.9, 0.02, L - 0.3), "rug", rot=(0, yaw, 0),
                       outline=False, collide=False),
                ])

    # ── 4-b) 낮은 책 더미 — 넓은 바닥 가운데의 섬
    n_pile = int(area_m2 / 160 * (0.5 if a["floor"] == 3 else 1.0))
    for k in range(n_pile):
        try_place((1.8, 1.8), lambda x, z, k=k: book_pile(x, z, rng, f"책더미{k}"), tries=40, margin_wall=2.0)

    # ── 4-c) 바닥 이음선
    placed.extend(floor_seams(a))

    # ── 5) 흩뿌림 — 통로(1)에도 둔다. 발목 아래라 걸어서 넘는다

    pieces = [(o["pos"]["x"], o["pos"]["z"]) for o in a["objects"] if o["kind"] == "piece"]
    cells = grid.free_cells(allow=1)
    rng.shuffle(cells)
    # 고르게 뿌리면 허전함 대신 지저분함이 된다 — 실제로 그렇게 보였다.
    # 누가 여기서 책을 읽다 떠난 것처럼, 가구·책장 곁에 모이게 한다.
    anchors = [(o["pos"]["x"], o["pos"]["z"]) for o in placed
               if o["name"].endswith("_상판") or o["name"].endswith("_표지") or "_옆L" in o["name"]]
    if anchors:
        def near(c):
            return min((c[0] - ax) ** 2 + (c[1] - az) ** 2 for ax, az in anchors)
        cells.sort(key=lambda c: near(c) + rng.uniform(0, 9.0))
    n_scatter = int(area_m2 * st["scatter"] * 0.55)
    used = []
    for x, z in cells:
        if n_scatter <= 0:
            break
        if any((x - px) ** 2 + (z - pz) ** 2 < 2.5 ** 2 for px, pz in pieces):
            continue                         # 조각과 헷갈리지 않게
        if any((x - ux) ** 2 + (z - uz) ** 2 < 2.2 ** 2 for ux, uz in used):
            continue                         # 뭉치지 않게
        roll = rng.random()
        tag = f"흩{len(used)}"
        if roll < st["ribbon"] * 0.5:
            objs = ribbon_trail(x, z, rng, "끈" + tag)
        elif roll < st["ribbon"] * 0.5 + st["fallen"]:
            objs = fallen_books(x, z, rng, "책" + tag)
        elif roll < st["ribbon"] * 0.5 + st["fallen"] + st["scrap"]:
            objs = scraps(x, z, rng, "조각" + tag)
        else:
            objs = loose_pages(x, z, rng, "낱장" + tag)
        if commit(objs):
            used.append((x, z))
            n_scatter -= 1

    a["objects"].extend(placed)
    return len(placed)


def apply_dressing(areas, ramps, sun, room_h_fn, lit_targets):
    total = 0
    for a in areas:
        total += dress_area(a, ramps, sun, room_h_fn, lit_targets)
    return total


# ─────────────────────────────────────────── 검사 — level_layout.validate 가 부른다

def check(areas, ramps, sun, room_h_fn, lit_targets, err, warn):
    """꾸미기가 지켜야 할 것들을 다시 확인한다.

    배치할 때 이미 피했지만, 나중에 누가 퍼즐을 옮기거나 방 크기를 바꾸면
    꾸미기는 그대로 남는다. 그때 조용히 길이 막히지 않게 여기서 잡는다.
    """
    for a in areas:
        deco = [o for o in a["objects"] if o["name"].startswith(PREFIX)]
        if not deco:
            continue
        H = room_h_fn(a)
        light = _light_segments(a, sun, H, lit_targets)

        # 빛 경로
        for o in deco:
            lo, hi = _aabb3(o, pad=0.0)
            for p0, p1 in light:
                if _seg_hits_aabb(p0, p1, lo, hi):
                    err(f"{a['id']}: 꾸미기 '{o['name']}' 가 천창 빛 경로를 막는다")
                    break

        # 그림자 스크린 — 접이면 그림자가 지나가는 길
        for name, zlo, zhi in _shadow_zones(a, sun, margin=0.3):
            for o in deco:
                if _is_flat_seam(o):
                    continue
                lo, hi = _aabb3(o, pad=0.0)
                if _aabb_overlap(lo, hi, zlo, zhi):
                    err(f"{a['id']}: 꾸미기 '{o['name']}' 가 '{name}' 의 그림자 길 위에 있다")
                    break

        # 금지 색
        for o in deco:
            if o["mat"] in ("light", "accent"):
                err(f"{a['id']}: 꾸미기 '{o['name']}' 가 {o['mat']} 를 쓴다 — 퍼즐 색/빛 대상과 겹친다")

        # 높은 건 벽에 붙어 있어야 한다
        hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
        for o in deco:
            top = o["pos"]["y"] + o["scale"]["y"] / 2
            if top > TALL + 0.4 and not o.get("face"):
                err(f"{a['id']}: 꾸미기 '{o['name']}' 가 {top:.1f}m 인데 벽 표시가 없다 — 카메라를 가린다")

        # 문 앞 · 퍼즐 자리
        blockers = [o for o in deco if not o.get("noCollider") and o["pos"]["y"] + o["scale"]["y"] / 2 > 0.45]
        spots = []
        for r in ramps:
            for key, rid in (("from", r.get("fromId")), ("to", r.get("toId"))):
                if rid == a["id"]:
                    p = r[key]
                    spots.append(("문", p["x"] - a["pos"]["x"], p["z"] - a["pos"]["z"], r.get("width", 4) / 2 + 1.5))
        q = a.get("puzzle")
        if q and q.get("hasAt"):
            spots.append(("퍼즐 자리", q["at"]["x"], q["at"]["z"], 2.0))
        for label, sx, sz, rad in spots:
            for o in blockers:
                x0, x1, z0, z1 = _foot(o)
                nx = min(max(sx, x0), x1)
                nz = min(max(sz, z0), z1)
                if (nx - sx) ** 2 + (nz - sz) ** 2 < rad * rad:
                    err(f"{a['id']}: 꾸미기 '{o['name']}' 가 {label} 근처({rad:.1f}m)를 막는다")
                    break

        # 밀도 보고 — 경고가 아니라 정보. 너무 적으면 경고
        area_m2 = hx * hz * 4
        if len(deco) / area_m2 < 0.05 and a["floor"] != 3:
            warn(f"{a['id']}: 꾸미기가 {len(deco)}개 / {area_m2:.0f}㎡ — 여전히 허전할 수 있다")
