# -*- coding: utf-8 -*-
"""
거인 책의 탑 — 레벨 레이아웃 단일 원본.

이 파일이 레벨의 유일한 출처다.
  python3 Tools/level_layout.py build     → Assets/_BookTower/Level/level_layout.json 생성
  python3 Tools/level_layout.py validate  → 경사·도달성·겹침 검증
"""
import json, math, os, sys

R1 = 18.0           # 1층 나선 반경
R2 = 26.0           # 2층 나선 반경 (방이 커서 더 넓게)
R = R1              # 하위 호환
SLOPE_LIMIT = 50.0  # CharacterController slopeLimit
GLIDE_DROP  = 6.0   # 활공으로 안전하게 내려올 수 있는 낙차
FOLD_RISE   = 3.5   # 접이면 하나로 오를 수 있는 높이

def spiral(step, y, radius=None):
    rad = radius if radius is not None else R1
    r = math.radians(step * 90)
    return (round(math.sin(r) * rad, 2), y, round(math.cos(r) * rad, 2))

def area(id, name, floor, pos, size, wall=0.0, note="", objects=None, puzzle=None):
    return dict(id=id, name=name, floor=floor,
                pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                size=dict(x=size[0], z=size[1]),
                backWall=wall, note=note,
                objects=objects or [], puzzle=puzzle)

def box(name, pos, scale, mat="mid", note=""):
    return dict(kind="box", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=scale[0], y=scale[1], z=scale[2]), mat=mat, note=note)

def fold(name, pos, size, mat="light", state=0, note=""):
    return dict(kind="fold", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=size[0], y=0.08, z=size[1]), mat=mat, state=state, note=note)

def piece(name, pos, pid, label, area_id, decoy=False, mat="light", note=""):
    return dict(kind="piece", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=1.0, y=0.12, z=1.2), mat=mat, note=note,
                pieceId=pid, label=label, areaId=area_id, decoy=decoy)

def crease(name, pos, cid, wear, active=True, mat="accent", note=""):
    return dict(kind="crease", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=3.5, y=0.06, z=0.25), mat=mat, note=note,
                creaseId=cid, wear=wear, active=active)

def marker(name, pos, note=""):
    return dict(kind="marker", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=1, y=1, z=1), mat="light", note=note)

def puzzle(pid, target, answer, panels, note="", ptype="fold", pieces=None, creases=None):
    return dict(id=pid, type=ptype, targetSeconds=target, answer=answer or [],
                panels=panels or [], pieces=pieces or [], creases=creases or [], note=note)

# ─────────────────────────────────────────────────────────── 구역 정의
AREAS = []
RAMPS = []

def ramp(name, a, b, width=4.0, mat="mid"):
    RAMPS.append(dict(name=name,
                      **{"from": dict(x=a[0], y=a[1], z=a[2])},
                      to=dict(x=b[0], y=b[1], z=b[2]), width=width, mat=mat))

# ═══════════════════════════════════════════════ Floor 0 · 표지
P_F0 = (0.0, 0.0, 0.0)
AREAS.append(area("F0", "F0_표지", 0, P_F0, (14, 12), wall=0,
    note="가장 좁고 어둡다. 유일하게 빛을 받는 건 걸쇠 하나",
    objects=[
        box("랜드마크_걸쇠", (0, 0.5, 2), (1.6, 0.8, 0.5), "accent",
            "P1 · 조각 2개를 맞추면 표지가 열린다"),
        box("조각_A", (-4.5, 0.25, 3.5), (0.9, 0.1, 0.9), "light"),
        box("조각_B", (4.5, 0.25, -0.5), (0.9, 0.1, 0.9), "light"),
        marker("▲시작지점", (0, 0.2, -4)),
    ]))

# ═══════════════════════════════════════════════ Floor 1 · 갈라진 페이지들
P_1A = spiral(0, -2); P_1B = spiral(1, 1); P_1C = spiral(2, 4)
P_1D = spiral(3, 7);  P_1E = spiral(0, 10)

AREAS.append(area("1A", "1A_접힘방향", 1, P_1A, (16, 14), wall=5,
    note="P2 · 접는 자리에서 목표가 안 보이게 시야를 끊는다",
    objects=[
        box("랜드마크_세워진페이지", (0, 2.5, 0), (0.3, 5, 9), "shade",
            "접는 자리에서 목표가 안 보이게 시선을 끊는 벽"),
        fold("P2_접이면", (-5, 0.1, -3), (4, 4), "light"),
        marker("◎목표지점", (5, 0.2, 4)),
        fold("위화감_반쯤접힌페이지", (4, 0.1, -4), (2.4, 2.4), "light", 1,
             "위화감 ① 누가 접다가 중간에 그만뒀다"),
        box("위화감_깃털", (2.6, 0.15, -5), (0.25, 0.05, 0.9), "light",
            "위화감 ② 선대가 여기 있었다"),
    ],
    puzzle=puzzle("P2", 120, [2], ["P2_접이면"],
                  "목표 지점에서 확인한 방향대로 접어야 한다. 접는 자리에선 목표가 안 보인다")))

AREAS.append(area("1B", "1B_끊긴길", 1, P_1B, (20, 14), wall=5,
    note="P3 · 바닥에 간극. 다른 페이지를 접어 그림자로 건너야 한다",
    objects=[
        box("간극_가림막", (0, -0.6, 0.5), (20, 0.05, 4), "shade",
            "P3 · 여기가 찢겨나간 자리 (바닥보다 아래에 표식만)"),
        fold("P3_대체용_접이면", (-7, 0.1, -6), (5, 5), "light"),
        marker("랜드마크_찢겨나간_간극", (0, 0.6, 0.5)),
    ],
    puzzle=puzzle("P3", 240, [1], ["P3_대체용_접이면"],
                  "빠진 조각은 찾을 수 없다. 다른 페이지를 접어 그 그림자로 간극을 덮는다")))

AREAS.append(area("1C", "1C_교차그림자", 1, P_1C, (16, 30), wall=0,
    note="P4 · 두 방 + 가운데 반사 페이지. P14 반사 해법의 학습 장치",
    objects=[
        box("방1_뒷벽", (0, 3, -14), (16, 6, 0.3), "shade"),
        fold("P4_접이면_A", (-4, 0.1, -11), (4, 4), "light"),
        box("방2_뒷벽", (0, 3, 14), (16, 6, 0.3), "shade"),
        fold("P4_접이면_B", (4, 0.1, 11), (4, 4), "light"),
        box("분리벽_좌", (-5.5, 3, 0), (5, 6, 0.3), "shade"),
        box("분리벽_우", (5.5, 3, 0), (5, 6, 0.3), "shade"),
        box("랜드마크_반사페이지", (0, 2, 0), (3.5, 4, 0.15), "accent",
            "P4 · 여기 서면 양쪽 방의 그림자가 반사되어 함께 보인다 (→ P14 반사 해법 학습)"),
    ],
    puzzle=puzzle("P4", 300, [2, 1], ["P4_접이면_A", "P4_접이면_B"],
                  "두 방의 그림자가 교차하는 지점에 통로가 드러난다. 반사 페이지로 왕복 없이 확인")))

AREAS.append(area("1D", "1D_다급한발자국", 1, P_1D, (16, 14), wall=5,
    note="P5 · 조각 3개 중 하나는 레드 헤링. 한 화면에 모으지 않는다",
    objects=[
        piece("조각_1", (-5, 0.25, -4), "P5_a", "발자국 조각 (좌)", "1D"),
        piece("조각_2", (1, 0.25, 2), "P5_b", "발자국 조각 (우)", "1D"),
        piece("조각_3_레드헤링", (6, 0.25, 5), "P5_x", "낯선 조각", "1D", decoy=True, mat="shade",
              note="P5 · 레드 헤링. 종이 결·찢긴 방향이 다른 시대의 것 (색 아닌 질감으로 구분)"),
        box("위화감_진한얼룩", (-2, 0.12, -1), (1.8, 0.02, 1.4), "shade",
            "위화감 ③ 다른 얼룩보다 진하다 = 최근이다"),
        marker("랜드마크_발자국그림", (0, 0.2, 5),
               "조각을 맞추면 '전임이 다급하게 향하는' 그림 → 놀람 포인트 #1"),
    ],
    puzzle=puzzle("P5", 240, None, None, ptype="assembly", pieces=["P5_a", "P5_b"],
                  note="진짜 조각 2개만 모아야 한다. 가짜를 골라내는 게 퍼즐")))

AREAS.append(area("1E", "1E_첫번째봉인", 1, P_1E, (18, 16), wall=8,
    note="P6 · 접이면 3개의 그림자로 P1의 문양을 만든다. 열릴 때 네 번째 조각이 비친다(→P13)",
    objects=[
        fold("P6_접이면_1", (-5, 0.1, -2), (3.5, 3.5), "light"),
        fold("P6_접이면_2", (0, 0.1, -4), (3.5, 3.5), "light"),
        fold("P6_접이면_3", (5, 0.1, -2), (3.5, 3.5), "light"),
        marker("랜드마크_봉인된페이지", (0, 4, 7.5)),
        marker("→2장_입구", (0, 0.2, 6)),
    ],
    puzzle=puzzle("P6", 120, [2, 1, 2],
                  ["P6_접이면_1", "P6_접이면_2", "P6_접이면_3"],
                  "세 그림자가 합쳐져 문양이 되면 봉인 해제")))

# ═══════════════════════════════════════════════ Floor 2 · 접힌 기억
P_2A = spiral(1, 14.0, R2); P_2B = spiral(2, 16.5, R2); P_2C = spiral(3, 19.0, R2)
P_2D = spiral(0, 21.5, R2); P_2E = spiral(1, 24.0, R2); P_2F = spiral(2, 26.5, R2)

AREAS.append(area("2A", "2A_주름깨우기", 2, P_2A, (14, 12), wall=5,
    note="P7 · 에코 튜토리얼. 활성 주름은 미세하게 빛을 머금는다",
    objects=[
        crease("주름_활성", (0, 0.13, 1), "2A_main", 0.5,
               note="P7 · 문지르면 최근 며칠의 행동이 재생된다. 활성 주름은 빛을 머금는 시각 규칙"),
        crease("주름_비활성_1", (-4, 0.12, -2), "2A_off1", 0.2, active=False, mat="shade"),
        crease("주름_비활성_2", (4, 0.12, -3), "2A_off2", 0.8, active=False, mat="shade"),
        marker("랜드마크_빛나는주름", (0, 0.5, 1)),
    ]))

AREAS.append(area("2B", "2B_따라접기", 2, P_2B, (18, 14), wall=6,
    note="P8 · 재생에서 손동작은 몸에 가려 안 보이고 결과 그림자만 보인다 → 각도 역산",
    objects=[
        marker("에코_재생지점", (-5, 0.2, -3), "여기서 재생. 손동작은 가려져 있다"),
        box("가림막_전임의몸", (-5, 1.5, -1), (2.5, 3, 0.2), "shade",
            "P8 · 접는 손동작을 가리는 실루엣. 그림자만 보고 역산해야 함"),
        fold("P8_접이면", (4, 0.1, -3), (4, 4), "light"),
        box("그림자판독벽", (0, 3, 7), (18, 6, 0.3), "shade",
            "비대칭 요소를 넣어 해가 유일하게 (모서리 하나가 잘린 형태)"),
    ],
    puzzle=puzzle("P8", 240, [3], ["P8_접이면"],
                  "재생 화면에서 손동작은 가려져 있다. 결과 그림자만 보고 각도를 역산")))

AREAS.append(area("2C", "2C_숨겨진틈", 2, P_2C, (20, 18), wall=6,
    note="P9 · 과거의 접힘 상태 ↔ 현재 위치 공간 변환. P15의 선행 학습",
    objects=[
        fold("P9_공간접이면", (0, 0.1, -6), (8, 8), "mid",
             "이 면이 접히면 방의 기하가 바뀐다 — 같은 지점이 다른 곳에 온다"),
        box("고정표식_못자국", (-6, 0.14, 3), (0.4, 0.05, 0.4), "accent",
            "양쪽 상태에 공통으로 존재하는 참조점. 이게 없으면 불공정해진다"),
        marker("숨겨진틈_위치", (5, 0.2, 4)),
        marker("랜드마크_두겹페이지", (0, 1.5, -6)),
    ],
    puzzle=puzzle("P9", 300, [2], ["P9_공간접이면"],
                  "기억은 지금과 다르게 접혀 있던 시절의 장면. 못자국을 기준으로 현재 위치를 계산")))

AREAS.append(area("2D", "2D_엇갈린기록", 2, P_2D, (16, 22), wall=0,
    note="P10 · 긴 방. 기록 4장을 마주보게 걸어 한 화면에 비교 가능하게",
    objects=[
        box("기록_1", (-7.5, 2.5, -6), (0.2, 3, 3), "light"),
        box("기록_2", (-7.5, 2.5, 2), (0.2, 3, 3), "light"),
        box("기록_3_어긋남", (7.5, 2.5, -6), (0.2, 3, 3), "accent",
            "P10 · 넷 중 이것만 주름의 기억과 어긋난다"),
        box("기록_4", (7.5, 2.5, 2), (0.2, 3, 3), "light"),
        box("물리적증거_찢긴결", (0, 0.14, 8), (2, 0.05, 1.2), "accent",
            "P10 · 주름 기억 쪽의 물리 증거. 찢긴 결이 맞물려 진위 판단 근거가 된다"),
        marker("랜드마크_나란한기록", (0, 3, -2)),
    ]))

AREAS.append(area("2E", "2E_순서의방", 2, P_2E, (22, 22), wall=0,
    note="P11 · 주름 5개가 사방+천장에. 한 각도로는 2개만 보여 회전이 강제된다",
    objects=[
        box("벽_북", (0, 3.5, 11), (22, 7, 0.3), "shade"),
        box("벽_남", (0, 3.5, -11), (22, 7, 0.3), "shade"),
        box("벽_동", (11, 3.5, 0), (0.3, 7, 22), "shade"),
        box("벽_서", (-11, 3.5, 0), (0.3, 7, 22), "shade"),
        box("천장", (0, 7.2, 0), (22, 0.3, 22), "shade"),
        crease("주름_1_가장오래됨", (0, 1.5, 10.4), "2E_1", 0.05),
        crease("주름_2", (10.4, 1.5, 0), "2E_2", 0.28),
        crease("주름_3", (0, 1.5, -10.4), "2E_3", 0.50),
        crease("주름_4", (-10.4, 1.5, 0), "2E_4", 0.72),
        crease("주름_5_가장최근", (0, 6.8, 0), "2E_5", 0.95,
               note="P11 · 천장. 이것 때문에 위를 올려다보게 된다"),
        marker("랜드마크_사방의주름", (0, 3, 0),
               "마모 형태(①) + 장면 인과(②) + 소리(③ 보조)의 3중 근거로 순서 판정"),
    ],
    puzzle=puzzle("P11", 360, None, None, ptype="sequence",
                  creases=["2E_1", "2E_2", "2E_3", "2E_4", "2E_5"],
                  note="마모가 오래된 것부터. 틀려도 단서 소실 없이 8초 후 초기화")))

AREAS.append(area("2F", "2F_작은그림자", 2, P_2F, (12, 10), wall=4,
    note="P12 · 좁고 어둡게. 퍼즐이 아니라 서사 비트 — 단일 인터랙션으로 명확하게",
    objects=[
        box("좁은통로_좌", (-5, 2, 0), (0.3, 4, 10), "shade"),
        box("좁은통로_우", (5, 2, 0), (0.3, 4, 10), "shade"),
        marker("에코_어린책갈피새", (0, 0.3, 3),
               "어린 자신이 위험에 처하고, 전임이 황급히 그쪽으로 향하는 장면"),
        marker("→3장_입구", (0, 0.2, 4.5)),
    ]))

# ═══════════════════════════════════════════════ Floor 3 · 마지막 장
# 3-A → 3-B → 3-C 는 반드시 일직선. 3-C의 빛이 뒤쪽 3-A까지 닿아야 하기 때문.
P_3A = (0.0, 31.0, 0.0)
P_3B = (0.0, 33.0, 30.0)
P_3C = (0.0, 36.0, 54.0)

AREAS.append(area("3A", "3A_복원의방", 3, P_3A, (32, 28), wall=0,
    note="P13(메타퍼즐) → P15(왜상) → 엔딩. 이 게임에서 가장 중요한 방. "
         "조각이 바닥·사방벽·천장에 흩어져 있고, 그게 곧 전임이다",
    objects=[
        box("벽_북", (0, 5, 14), (32, 10, 0.3), "shade"),
        box("벽_남", (0, 5, -14), (32, 10, 0.3), "shade"),
        box("벽_동", (16, 5, 0), (0.3, 10, 28), "shade"),
        box("벽_서", (-16, 5, 0), (0.3, 10, 28), "shade"),
        box("천장_위층페이지뒷면", (0, 10.2, 0), (32, 0.3, 28), "shade",
            "천장 = 위층 페이지의 뒷면. 책이라는 설정과 자연스럽게 맞는다"),

        box("복원대", (0, 0.6, -6), (5, 1.2, 3), "accent",
            "P13 · 조각 7개를 그을음 각도 순서로 복원. 기준점은 P6의 네 번째 조각"),

        # 왜상 조각 — 바닥·벽·천장에 흩어짐. P13을 푸는 8분 내내 화면에 있어야 한다
        box("왜상조각_바닥_1", (-9, 0.12, 4), (2.2, 0.04, 0.7), "shade"),
        box("왜상조각_바닥_2", (6, 0.12, 8), (1.6, 0.04, 0.9), "shade"),
        box("왜상조각_바닥_3", (-3, 0.12, 10), (2.8, 0.04, 0.5), "shade"),
        box("왜상조각_벽북_1", (-7, 3.5, 13.7), (1.8, 0.8, 0.05), "shade"),
        box("왜상조각_벽서_1", (-15.7, 5.5, 3), (0.05, 1.2, 2.4), "shade"),
        box("왜상조각_벽동_1", (15.7, 4.2, -5), (0.05, 0.9, 2.0), "shade"),
        box("왜상조각_천장_1", (2, 10.0, 2), (2.6, 0.05, 1.1), "shade"),
        box("왜상조각_천장_2", (-5, 10.0, -3), (1.4, 0.05, 1.8), "shade"),

        marker("◈정렬시점_허공", (0, 5.5, 2),
               "P15 · 여기서만 조각이 새의 실루엣으로 정렬된다. 허공이라 걸어갈 수 없다"),
        fold("P15_공간접이면", (-8, 0.1, -10), (7, 7), "mid",
             "P15 · 이걸 접으면 방의 기하가 포개져 ◈가 플레이어 위치와 겹친다"),

        marker("히든_책속의책", (12, 0.3, -10), "엔딩 후 열림"),
        piece("조각_p1", (-11, 0.25, -9), "P13_1", "그을음 조각 1", "3A"),
        piece("조각_p2", (-4, 0.25, -11), "P13_2", "그을음 조각 2", "3A"),
        piece("조각_p3", (3, 0.25, -10), "P13_3", "그을음 조각 3", "3A"),
        piece("조각_p4", (10, 0.25, -7), "P13_4", "그을음 조각 4", "3A"),
        piece("조각_p5", (13, 0.25, 1), "P13_5", "그을음 조각 5", "3A"),
        piece("조각_p6", (9, 0.25, 9), "P13_6", "그을음 조각 6", "3A"),
        piece("조각_p7", (-12, 0.25, 8), "P13_7", "그을음 조각 7", "3A"),
    ],
    puzzle=puzzle("P13", 480, None, None, ptype="assembly",
                  pieces=["P13_1","P13_2","P13_3","P13_4","P13_5","P13_6","P13_7"],
                  note="그을음 각도 순서로 복원. 기준점은 1E에서 얻은 네 번째 조각. "
                       "피드백은 구간(초·중·후반)까지만")))

AREAS.append(area("3B", "3B_삼중조건의문", 3, P_3B, (20, 18), wall=0,
    note="P14 · A·B·C 세 접이면이 서로를 방해한다. 셋이 한 화면에 보여야 하므로 회전은 안 쓴다",
    objects=[
        box("문틀_좌", (-4, 3, 8.8), (2, 6, 0.4), "shade"),
        box("문틀_우", (4, 3, 8.8), (2, 6, 0.4), "shade"),
        box("열쇠구멍", (0, 2.2, 8.8), (0.6, 0.6, 0.4), "accent",
            "P14 ① 채광구 빛이 여기 정확히 닿아야 한다"),
        box("채광구", (-6, 8, -6), (2, 0.2, 2), "accent"),
        fold("P14_A면", (-6, 0.1, -4), (5, 5), "light",
             "P14 ① A면이 빛을 열쇠구멍으로 보낸다"),
        fold("P14_B면", (6, 0.1, -4), (4, 4), "light",
             "P14 ② B면이 빛을 가리면 옆 주름이 깨어나지 못한다"),
        fold("P14_C면", (0, 0.1, 2), (5, 5), "light",
             "P14 ③ C면을 접어야 아래 홈이 드러난다. 그런데 접으면 A의 빛을 가로막는다 "
             "— 유일해는 C면을 '반사면'으로 쓰는 각도"),
        box("주름_B조건", (8, 0.13, 0), (3, 0.06, 0.25), "accent"),
        marker("조각홈", (0, 0.2, 4)),
    ],
    puzzle=puzzle("P14", 480, [2, 0, 1], ["P14_A면", "P14_B면", "P14_C면"],
                  "세 조건이 서로를 무너뜨린다. 유일해는 C면을 '가로막는 벽'이 아니라 "
                  "'빛을 꺾는 반사면'으로 쓰는 각도")))

AREAS.append(area("3C", "3C_마지막장", 3, P_3C, (18, 16), wall=0,
    note="문을 나선 곳. 여기서 빛을 켜면 그 빛이 뒤쪽(3A)을 비추며 새 모양 그림자가 생긴다",
    objects=[
        box("빛_점화대", (0, 0.6, 4), (2.5, 1.2, 2.5), "accent",
            "여기서 빛을 켜면 -Z 방향(3B의 열린 문 너머 3A)을 비춘다"),
        marker("→되돌아가기", (0, 0.2, -7), "3A로 돌아가면 P15 왜상"),
        marker("엔딩_지점", (0, 0.2, 0)),
    ]))

# ═══════════════════════════════════════════════ 히든 · 책 속의 책
AREAS.append(area("HD", "HD_책속의책", 4, (80.0, 31.0, 0.0), (16, 16), wall=6,
    note="엔딩 후 열리는 중첩공간. 어느 엔딩을 골랐든 열린다",
    objects=[
        fold("HD_접이면_1", (-4, 0.1, -3), (3, 3), "light"),
        fold("HD_접이면_2", (4, 0.1, -3), (3, 3), "light"),
        marker("랜드마크_축소된탑", (0, 1, 3)),
    ]))

# ═══════════════════════════════════════════════ 구역 연결 (경사로 자동 계산)
BY_ID = {a["id"]: a for a in AREAS}

def edge_point(a, toward, lift=0.1):
    """구역 a의 중심에서 toward 방향으로 가장자리까지 나간 지점"""
    cx, cy, cz = a["pos"]["x"], a["pos"]["y"], a["pos"]["z"]
    hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2
    dx, dz = toward[0] - cx, toward[2] - cz
    n = math.hypot(dx, dz)
    if n < 1e-6: return (cx, cy + lift, cz)
    dx, dz = dx / n, dz / n
    # 사각형 경계와의 교점
    t = min(hx / abs(dx) if abs(dx) > 1e-6 else 1e9,
            hz / abs(dz) if abs(dz) > 1e-6 else 1e9)
    return (round(cx + dx * t, 2), round(cy + lift, 2), round(cz + dz * t, 2))

def connect(a_id, b_id, width=4.0):
    a, b = BY_ID[a_id], BY_ID[b_id]
    pa = (a["pos"]["x"], a["pos"]["y"], a["pos"]["z"])
    pb = (b["pos"]["x"], b["pos"]["y"], b["pos"]["z"])
    ramp(f"연결_{a_id}→{b_id}", edge_point(a, pb), edge_point(b, pa), width)

for pair in [("F0","1A"),("1A","1B"),("1B","1C"),("1C","1D"),("1D","1E"),
             ("1E","2A"),("2A","2B"),("2B","2C"),("2C","2D"),("2D","2E"),("2E","2F"),
             ("2F","3A"),("3A","3B"),("3B","3C")]:
    connect(*pair)

# ═══════════════════════════════════════════════ 출력 / 검증
def build():
    out = dict(areas=AREAS, ramps=RAMPS,
               meta=dict(spiralRadius1=R1, spiralRadius2=R2, slopeLimit=SLOPE_LIMIT,
                         glideDrop=GLIDE_DROP, foldRise=FOLD_RISE))
    d = "Assets/_BookTower/Level"
    os.makedirs(d, exist_ok=True)
    p = os.path.join(d, "level_layout.json")
    with open(p, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print(f"생성: {p}  (구역 {len(AREAS)}개 / 경사로 {len(RAMPS)}개)")

def validate():
    errs, warns = [], []

    # 1) 경사로 기울기
    for r in RAMPS:
        a, b = r["from"], r["to"]
        run = math.hypot(b["x"] - a["x"], b["z"] - a["z"])
        rise = abs(b["y"] - a["y"])
        if run < 0.01:
            errs.append(f"{r['name']}: 수평 거리가 0 — 오를 수 없음")
            continue
        ang = math.degrees(math.atan2(rise, run))
        if ang > SLOPE_LIMIT:
            errs.append(f"{r['name']}: 경사 {ang:.1f}° > 한계 {SLOPE_LIMIT}° — 못 올라감")
        elif ang > SLOPE_LIMIT - 10:
            warns.append(f"{r['name']}: 경사 {ang:.1f}° — 한계에 근접")
        if run > 30:
            warns.append(f"{r['name']}: 길이 {run:.1f} — 걷기만 하는 구간이 길다")

    # 2) 구역 겹침 (XZ 겹치고 높이차 4 미만이면 충돌)
    for i in range(len(AREAS)):
        for j in range(i + 1, len(AREAS)):
            a, b = AREAS[i], AREAS[j]
            dy = abs(a["pos"]["y"] - b["pos"]["y"])
            if dy >= 4: continue
            ox = abs(a["pos"]["x"] - b["pos"]["x"]) < (a["size"]["x"] + b["size"]["x"]) / 2
            oz = abs(a["pos"]["z"] - b["pos"]["z"]) < (a["size"]["z"] + b["size"]["z"]) / 2
            if ox and oz:
                errs.append(f"구역 겹침: {a['id']} ↔ {b['id']} (높이차 {dy:.1f})")

    # 3) 도달성 (F0에서 시작해 경사로를 따라 전부 닿는가)
    adj = {}
    for r in RAMPS:
        n = r["name"].replace("연결_", "")
        s, t = n.split("→")
        adj.setdefault(s, []).append(t)
        adj.setdefault(t, []).append(s)   # 접힘은 가역이므로 양방향
    seen, stack = {"F0"}, ["F0"]
    while stack:
        cur = stack.pop()
        for nxt in adj.get(cur, []):
            if nxt not in seen:
                seen.add(nxt); stack.append(nxt)
    unreachable = [a["id"] for a in AREAS if a["id"] not in seen and a["id"] != "HD"]
    for u in unreachable:
        errs.append(f"도달 불가 구역: {u}")

    # 4) 퍼즐 정합성 (타입별)
    all_piece_ids = {}
    for a in AREAS:
        for o in a["objects"]:
            if o["kind"] == "piece":
                pid = o["pieceId"]
                if pid in all_piece_ids:
                    errs.append(f"조각 id 중복: '{pid}' ({all_piece_ids[pid]} ↔ {a['id']})")
                all_piece_ids[pid] = a["id"]

    all_crease_ids = {}
    for a in AREAS:
        for o in a["objects"]:
            if o["kind"] == "crease":
                cid = o["creaseId"]
                if cid in all_crease_ids:
                    errs.append(f"주름 id 중복: '{cid}' ({all_crease_ids[cid]} ↔ {a['id']})")
                all_crease_ids[cid] = a["id"]

    for a in AREAS:
        p = a.get("puzzle")
        if not p: continue
        t = p.get("type", "fold")

        if t == "fold":
            names = [o["name"] for o in a["objects"] if o["kind"] == "fold"]
            for pn in p["panels"]:
                if pn not in names:
                    errs.append(f"{a['id']} {p['id']}: 접이면 '{pn}' 이 구역에 없음")
            if len(p["answer"]) != len(p["panels"]):
                errs.append(f"{a['id']} {p['id']}: 정답 길이({len(p['answer'])}) ≠ 접이면 수({len(p['panels'])})")
            for v in p["answer"]:
                if not 0 <= v <= 3:
                    errs.append(f"{a['id']} {p['id']}: 정답 값 {v} 범위 밖(0~3)")

        elif t == "assembly":
            if not p["pieces"]:
                errs.append(f"{a['id']} {p['id']}: 필요한 조각 목록이 비어 있음")
            for pid in p["pieces"]:
                if pid not in all_piece_ids:
                    errs.append(f"{a['id']} {p['id']}: 조각 '{pid}' 이 어디에도 배치되지 않음")
            # 레드 헤링을 정답에 넣으면 안 된다
            for o in a["objects"]:
                if o["kind"] == "piece" and o.get("decoy") and o["pieceId"] in p["pieces"]:
                    errs.append(f"{a['id']} {p['id']}: 가짜 조각 '{o['pieceId']}' 이 정답에 포함됨")

        elif t == "sequence":
            if len(p["creases"]) < 2:
                errs.append(f"{a['id']} {p['id']}: 순서 퍼즐인데 주름이 {len(p['creases'])}개")
            wears = {}
            for o in a["objects"]:
                if o["kind"] == "crease": wears[o["creaseId"]] = o["wear"]
            for cid in p["creases"]:
                if cid not in wears:
                    errs.append(f"{a['id']} {p['id']}: 주름 '{cid}' 이 구역에 없음")
            # 정답 순서가 마모 순서와 일치해야 단서가 근거로 작동한다
            seq = [wears.get(c, -1) for c in p["creases"]]
            if seq != sorted(seq):
                errs.append(f"{a['id']} {p['id']}: 정답 순서가 마모 순서와 다름 — 마모 단서가 무의미해짐")

    # 5) 3층 일직선 (P15 연출의 물리적 전제)
    a3, b3, c3 = BY_ID["3A"], BY_ID["3B"], BY_ID["3C"]
    if not (abs(a3["pos"]["x"] - b3["pos"]["x"]) < 0.5 and abs(b3["pos"]["x"] - c3["pos"]["x"]) < 0.5):
        errs.append("3A·3B·3C가 일직선이 아님 — 3C의 빛이 3A에 닿지 못해 P15 연출이 불가능")

    print(f"구역 {len(AREAS)} / 경사로 {len(RAMPS)}")
    print(f"\n오류 {len(errs)}건")
    for e in errs: print("  ✗", e)
    print(f"\n경고 {len(warns)}건")
    for w in warns: print("  !", w)
    return len(errs)



# ═══════════════════════════════════════════════ 이동 시뮬레이션
MOVE_SPEED = 4.0    # PlayerController.moveSpeed

# 기획서(Docs/06)의 구역별 퍼즐 목표 시간(초)
PUZZLE_BUDGET = {
    "F0": 6*60, "1A": 2*60, "1B": 4*60, "1C": 5*60, "1D": 4*60, "1E": 2*60,
    "2A": 2*60, "2B": 4*60, "2C": 5*60, "2D": 4*60, "2E": 6*60, "2F": 2*60,
    "3A": 8*60 + 4*60 + 3*60,   # P13 + P15 + 엔딩
    "3B": 8*60, "3C": 1*60, "HD": 12*60,
}

ROUTE = ["F0","1A","1B","1C","1D","1E","2A","2B","2C","2D","2E","2F","3A","3B","3C"]

def simulate():
    print("=== 이동 시뮬레이션 (걷기 4 유닛/초) ===\n")
    print(f"{'구간':<12}{'경사':>7}{'이동거리':>10}{'이동시간':>10}{'퍼즐시간':>10}{'누적':>9}")
    print("-" * 60)

    ramp_by = {r["name"]: r for r in RAMPS}
    total = 0.0
    walk_total = 0.0

    for i, aid in enumerate(ROUTE):
        a = BY_ID[aid]
        # 구역 내 이동 — 대각선의 1.5배를 왕복한다고 가정
        diag = math.hypot(a["size"]["x"], a["size"]["z"])
        inner = diag * 1.5 / MOVE_SPEED

        # 다음 구역으로 가는 경사로
        seg, ang = 0.0, 0.0
        if i + 1 < len(ROUTE):
            r = ramp_by.get(f"연결_{aid}→{ROUTE[i+1]}")
            if r:
                f, t = r["from"], r["to"]
                run = math.hypot(t["x"]-f["x"], t["z"]-f["z"])
                rise = abs(t["y"]-f["y"])
                ang = math.degrees(math.atan2(rise, run))
                length = math.hypot(run, rise)
                # 오르막은 느려진다 (경사에 비례해 최대 40% 감속)
                seg = length / (MOVE_SPEED * (1 - 0.4 * ang / 90))

        walk = inner + seg
        pz = PUZZLE_BUDGET.get(aid, 0)
        total += walk + pz
        walk_total += walk
        print(f"{aid:<12}{ang:>6.1f}°{diag*1.5+ (seg*MOVE_SPEED):>9.0f}u{walk:>9.0f}s{pz:>9.0f}s{total/60:>8.1f}m")

    print("-" * 60)
    print(f"\n총 플레이타임 추정 : {total/60:.1f}분")
    print(f"  · 퍼즐          : {(total-walk_total)/60:.1f}분")
    print(f"  · 이동          : {walk_total/60:.1f}분  ({walk_total/total*100:.0f}%)")
    print(f"\n목표(본편)        : 70분")

    ratio = walk_total / total
    if total/60 > 78:
        print("\n판정: ✗ 너무 김 — 구역을 줄이거나 간격을 좁혀야 함")
    elif total/60 < 62:
        print("\n판정: ✗ 너무 짧음")
    elif ratio > 0.25:
        print(f"\n판정: ! 이동 비중 {ratio*100:.0f}% — 걷기만 하는 시간이 많다")
    else:
        print(f"\n판정: ✓ 총 {total/60:.1f}분, 이동 비중 {ratio*100:.0f}% — 적정")
    return total

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "validate"
    if cmd == "build": build()
    elif cmd == "sim": simulate()
    else: sys.exit(1 if validate() else 0)
