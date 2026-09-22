# -*- coding: utf-8 -*-
"""방 하나를 **게임 카메라 그대로** 렌더한다.

    python3 room_preview.py level_layout.json 1A out.png

게임과 맞춘 것
  · 카메라: 방의 camYaw 스텝(90°) × 피치 30°, 직교 투영, 방 전체가 들어오게
  · 카메라 쪽 외벽(HIDDEN_FACE)과 천장은 숨긴다 — RoomWall 이 하는 일
  · 팔레트: light #FBFAF6 / mid #F2EFE7 / shade #DED8CC / accent #F2C879
    + 꾸미기 전용 색(dressing 의 spine* 등)
  · 연필 외곽선: 물체 경계에 잉크 1~2px (PencilOutline 이 하는 일의 근사)
  · 회전: rot = 유니티 Euler(x, y, z) — z → x → y 순서로 돈다

게임과 다른 것
  · 그림자와 천창 빛은 안 그린다. 이 도구의 목적은 "방이 찼는가, 길이 열려 있는가"
    를 보는 것이고, 빛은 level_layout.py 의 검사기가 따로 본다.
"""
import json
import math
import sys

import numpy as np
from PIL import Image, ImageDraw

PITCH = 30.0
HIDDEN_FACE = {0: "south", 1: "west", 2: "north", 3: "east"}

HEX = lambda h: np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], float)
PAL = {
    "light": HEX("#FBFAF6"), "mid": HEX("#F2EFE7"), "shade": HEX("#DED8CC"),
    "accent": HEX("#F2C879"), "floor": HEX("#EFEBE2"),
}
INK = np.array([58, 55, 51], float)
BG = np.array([253, 252, 250], float)
SUN = np.array([-0.287, -0.819, 0.497])


def set_palette(extra):
    for k, v in extra.items():
        PAL[k] = HEX(v)


def R(rot):
    """유니티 Quaternion.Euler(x, y, z) 의 회전 행렬 (z → x → y)."""
    x, y, z = (math.radians(rot.get(k, 0.0)) for k in ("x", "y", "z"))
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    Rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    Rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    return Ry @ Rx @ Rz


CORNERS = np.array([[sx, sy, sz] for sx in (-.5, .5) for sy in (-.5, .5) for sz in (-.5, .5)])
# 면: (꼭짓점 인덱스 4개, 바깥 법선) — CORNERS 순서 기준
FACES = [
    ((0, 1, 3, 2), (-1, 0, 0)), ((4, 6, 7, 5), (1, 0, 0)),
    ((0, 4, 5, 1), (0, -1, 0)), ((2, 3, 7, 6), (0, 1, 0)),
    ((0, 2, 6, 4), (0, 0, -1)), ((1, 5, 7, 3), (0, 0, 1)),
]


def box_quads(o):
    p = np.array([o["pos"]["x"], o["pos"]["y"], o["pos"]["z"]])
    s = np.array([o["scale"]["x"], o["scale"]["y"], o["scale"]["z"]])
    rm = R(o.get("rot") or {})
    v = (CORNERS * s) @ rm.T + p
    out = []
    for idx, n in FACES:
        out.append(([v[i] for i in idx], rm @ np.array(n, float)))
    return out


def camera(step):
    th, ph = math.radians(step * 90), math.radians(PITCH)
    f = np.array([math.sin(th) * math.cos(ph), -math.sin(ph), math.cos(th) * math.cos(ph)])
    r = np.array([math.cos(th), 0.0, -math.sin(th)])
    u = np.cross(f, r)
    return f, r, u


def render_area(a, W=1100, H=720, pad=1.06, show_hidden=False):
    step = a.get("camYaw", 0)
    f, r, u = camera(step)
    hid = HIDDEN_FACE[step % 4]

    boxes = []
    # 바닥 (LevelBuilder 가 따로 만든다)
    fl = dict(pos=dict(x=0, y=-0.2, z=0), scale=dict(x=a["size"]["x"], y=0.4, z=a["size"]["z"]),
              mat="floor", name="바닥")
    if a.get("splitGap", 0) > 0.01:
        g = a["splitGap"]
        half = (a["size"]["z"] - g) / 2
        off = (g + half) / 2
        boxes.append(dict(fl, pos=dict(x=0, y=-0.2, z=-off), scale=dict(x=a["size"]["x"], y=0.4, z=half)))
        boxes.append(dict(fl, pos=dict(x=0, y=-0.2, z=off), scale=dict(x=a["size"]["x"], y=0.4, z=half)))
    else:
        boxes.append(fl)

    # LevelBuilder 가 따로 세우는 뒷벽 (JSON 에 없다)
    if a.get("backWall", 0) > 0.01 and hid != "north":
        bw = a["backWall"]
        boxes.append(dict(pos=dict(x=0, y=bw / 2, z=a["size"]["z"] / 2), scale=dict(x=a["size"]["x"], y=bw, z=0.3),
                          mat="mid", name="뒷벽", face="north"))

    for o in a["objects"]:
        if o.get("kind") == "marker":
            continue
        face = o.get("face", "")
        if not show_hidden and (face == hid or face == "ceiling"):
            continue
        boxes.append(o)

    # 화면 좌표로
    quads = []
    for bi, o in enumerate(boxes):
        col = PAL.get(o.get("mat", "mid"), PAL["mid"])
        for pts, n in box_quads(o):
            if np.dot(n, f) >= 0:        # 뒷면
                continue
            lam = max(0.0, float(np.dot(n, -SUN)))
            k = 0.72 + 0.34 * lam
            if n[1] > 0.7:
                k = max(k, 0.98)          # 위를 보는 면은 거의 종이색 그대로
            q = [(float(np.dot(p, r)), float(np.dot(p, u)), float(-np.dot(p, f))) for p in pts]
            quads.append((q, np.clip(col * k, 0, 255), -2 if o.get("noOutline") else bi))

    xs = [v[0] for q, _, _ in quads for v in q]
    ys = [v[1] for q, _, _ in quads for v in q]
    # 틀은 바닥+벽 기준으로 잡는다 (꾸미기가 삐져나가도 틀이 흔들리지 않게)
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    sc = min(W / (x1 - x0), H / (y1 - y0)) / pad
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2

    img = np.tile(BG, (H, W, 1))
    zb = np.full((H, W), -1e18)
    idb = np.full((H, W), -1, int)

    def P(v):
        return (W / 2 + (v[0] - cx) * sc, H / 2 - (v[1] - cy) * sc)

    for q, col, bi in quads:
        for tri in ((q[0], q[1], q[2]), (q[0], q[2], q[3])):
            s0, s1, s2 = (np.array(P(v)) for v in tri)
            xa = max(int(min(s0[0], s1[0], s2[0])), 0); xb = min(int(max(s0[0], s1[0], s2[0])) + 2, W)
            ya = max(int(min(s0[1], s1[1], s2[1])), 0); yb = min(int(max(s0[1], s1[1], s2[1])) + 2, H)
            if xb <= xa or yb <= ya:
                continue
            area = (s1[0] - s0[0]) * (s2[1] - s0[1]) - (s2[0] - s0[0]) * (s1[1] - s0[1])
            if abs(area) < 1e-9:
                continue
            X, Y = np.meshgrid(np.arange(xa, xb) + .5, np.arange(ya, yb) + .5)
            w0 = ((s1[0] - X) * (s2[1] - Y) - (s2[0] - X) * (s1[1] - Y)) / area
            w1 = ((s2[0] - X) * (s0[1] - Y) - (s0[0] - X) * (s2[1] - Y)) / area
            w2 = 1 - w0 - w1
            m = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
            z = w0 * tri[0][2] + w1 * tri[1][2] + w2 * tri[2][2]
            sub = zb[ya:yb, xa:xb]
            m &= z > sub
            if not m.any():
                continue
            sub[m] = z[m]
            img[ya:yb, xa:xb][m] = col
            idb[ya:yb, xa:xb][m] = bi

    # 연필 외곽선 — 서로 다른 물체의 경계
    edge = np.zeros((H, W), bool)
    ok = idb != -2
    edge[:, 1:] |= (idb[:, 1:] != idb[:, :-1]) & ok[:, 1:] & ok[:, :-1]
    edge[1:, :] |= (idb[1:, :] != idb[:-1, :]) & ok[1:, :] & ok[:-1, :]
    img[edge] = img[edge] * 0.35 + INK * 0.65
    return Image.fromarray(img.astype(np.uint8)), sc


def main():
    path, aid, out = sys.argv[1], sys.argv[2], sys.argv[3]
    d = json.load(open(path, encoding="utf-8"))
    set_palette(d.get("meta", {}).get("dressingPalette", {}))
    a = next(x for x in d["areas"] if x["id"] == aid)
    img, _ = render_area(a)
    img.save(out)


if __name__ == "__main__":
    main()
