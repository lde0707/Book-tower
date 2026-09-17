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

def area(id, name, floor, pos, size, wall=0.0, note="", objects=None, puzzle=None,
         split=0.0):
    # split > 0 이면 바닥 가운데가 그만큼(원본 단위) 비어 있는 방이다.
    # 예전엔 LevelBuilder 가 `a.id == "1B"` 로 하드코딩하고 있었는데,
    # 그러면 여기 검증기가 "어느 방이 갈라져 있는지"를 알 수 없다.
    # 실제로 그 탓에 P3 의 접이면이 간극 건너편에 놓인 게 오래 안 잡혔다.
    return dict(id=id, name=name, floor=floor,
                pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                size=dict(x=size[0], z=size[1]),
                backWall=wall, note=note, splitGap=split,
                objects=objects or [], puzzle=puzzle)

def box(name, pos, scale, mat="mid", note="", bridge_for="", bridge_y=0.0):
    # bridge_for 를 주면 "그 퍼즐이 풀리면 여기까지 올라와 길이 되는 판" 이 된다.
    # 잠겨 있는 동안은 콜라이더가 꺼진다 — 안 그러면 바닥보다 조금 낮은
    # 이 판이 얕은 구덩이가 되어, 빠지면 stepOffset(0.45)으로는 못 올라온다.
    # 떨어지면 아래로 통과해 안전 지점으로 되돌아가는 쪽이 낫다.
    return dict(kind="box", name=name, pos=dict(x=pos[0], y=pos[1], z=pos[2]),
                scale=dict(x=scale[0], y=scale[1], z=scale[2]), mat=mat, note=note,
                bridgeFor=bridge_for, bridgeY=bridge_y)

def fold(name, pos, size, mat="light", state=0, note=""):
    # state 자리에 실수로 설명을 넣으면 JSON 의 int 필드에 문자열이 들어가고,
    # 유니티의 JsonUtility 가 레벨 전체를 못 읽는다. 조용히 넘어가면 안 된다.
    if not isinstance(state, int):
        raise TypeError(
            f"fold('{name}'): state 는 0~3 정수여야 한다. "
            f"설명을 넣으려면 note= 를 쓸 것. 받은 값: {state!r}")
    if not 0 <= state <= 3:
        raise ValueError(f"fold('{name}'): state {state} 는 0~3 범위 밖")
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

def puzzle(pid, target, answer, panels, note="", ptype="fold", pieces=None, creases=None,
           at=None, assembly_range=0.0):
    # at = 이 퍼즐을 '푸는 자리'. 조립 퍼즐은 조각을 모으는 것으로 끝나지 않고
    # 걸쇠나 복원대까지 가져가야 완성된다. 자리를 안 정해 두면 조각을 줍는 순간
    # 서 있던 데서 그냥 풀려버린다.
    return dict(id=pid, type=ptype, targetSeconds=target, answer=answer or [],
                panels=panels or [], pieces=pieces or [], creases=creases or [], note=note,
                at=dict(x=at[0], y=at[1], z=at[2]) if at else dict(x=0.0, y=0.0, z=0.0),
                hasAt=1 if at else 0, assemblyRange=assembly_range)

# ─────────────────────────────────────────────────────────── 구역 정의
AREAS = []
RAMPS = []

def ramp(name, a, b, width=4.0, mat="mid", from_id="", to_id=""):
    # fromId/toId 를 명시하는 이유
    #   경사로 위에서는 어느 방에도 속하지 않는다. 이때 "가장 가까운 방"으로
    #   판정하면 나선 구조에서는 엉뚱한 방(예: 2A→2B 도중에 2E)이 잡힌다.
    #   부피가 큰 방이 옆 층에서 더 가까울 수 있기 때문이다.
    #   어느 방과 어느 방을 잇는지는 설계 시점에 이미 알고 있으므로 그대로 적어 둔다.
    RAMPS.append(dict(name=name,
                      **{"from": dict(x=a[0], y=a[1], z=a[2])},
                      to=dict(x=b[0], y=b[1], z=b[2]), width=width, mat=mat,
                      fromId=from_id, toId=to_id))

# ═══════════════════════════════════════════════ Floor 0 · 표지
P_F0 = (0.0, 0.0, 0.0)
AREAS.append(area("F0", "F0_표지", 0, P_F0, (14, 12), wall=0,
    note="가장 좁고 어둡다. 유일하게 빛을 받는 건 걸쇠 하나",
    objects=[
        box("랜드마크_걸쇠", (0, 0.5, 2), (1.6, 0.8, 0.5), "accent",
            "P1 · 조각 2개를 맞추면 표지가 열린다"),
        # 조각을 걸쇠 기준 좌우 반대편에 둔다.
        # 하나를 줍고 나머지를 가지러 반대편으로 걸어야 하고, 그 왕복이
        # 곧 이동 튜토리얼이 된다. 가르치지 않고 걷게 만드는 자리다.
        piece("조각_A", (-4.5, 0.25, 3.5), "P1_a", "표지 조각 1", "F0"),
        piece("조각_B", (4.5, 0.25, -0.5), "P1_b", "표지 조각 2", "F0"),
        box("위화감_긁힌자국", (0.9, 0.55, 2), (0.35, 0.25, 0.06), "shade",
            "위화감 ① 걸쇠 옆면에 최근에 긁힌 자국. 강조 연출 없이 지나가게 둔다"),
        marker("▲시작지점", (0, 0.2, -4)),
    ],
    puzzle=puzzle("P1", 90, [], [], ptype="assembly", pieces=["P1_a", "P1_b"],
                  at=(0, 0.5, 2), assembly_range=3.5,
                  note="첫 퍼즐은 절대 막히면 안 된다. 실패 조건도 가짜 조각도 없다. "
                       "여기서 배우는 건 '줍는다 → 맞춘다' 하나뿐이다")))

# ═══════════════════════════════════════════════ Floor 1 · 갈라진 페이지들
# 1C 는 앞뒤로 30 이나 되는 긴 방이라 기본 반경으로는 안쪽 끝이 F0 위에 걸친다.
# 방이 깊은 만큼 바깥으로 밀어낸다.
P_1A = spiral(0, -2); P_1B = spiral(1, 1); P_1C = spiral(2, 4, R1 + 8)
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

AREAS.append(area("1B", "1B_끊긴길", 1, P_1B, (20, 14), wall=5, split=4.0,
    note="P3 · 바닥에 간극. 다른 페이지를 접어 그림자로 건너야 한다",
    objects=[
        # P3 를 풀면 이 판이 바닥 높이까지 올라와 간극을 잇는다.
        # "접은 페이지의 그림자가 간극을 덮는다" 의 실제 구현부다.
        # 폭은 간극(4)보다 넓게 잡아 양쪽 바닥에 걸치게 한다.
        box("간극_가림막", (0, -0.6, 0), (20, 0.05, 4.5), "shade",
            "P3 · 찢겨나간 자리. 풀면 여기까지 올라와 길이 된다",
            bridge_for="P3", bridge_y=0.05),
        # 접이면은 반드시 **입구 쪽**에 있어야 한다.
        # 예전엔 간극 건너편(z -6)에 있었는데, 입구가 z +7 이라
        # 건너지 않으면 닿지 않았다. 즉 P3 는 풀 수도 건널 수도 없었다.
        fold("P3_대체용_접이면", (-7, 0.1, 5), (5, 5), "light"),
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
        # 표식이 아니라 실제 물건이어야 한다 — 여기를 눌러서 조각을 맞추기 때문이다.
        # 표식은 렌더러도 콜라이더도 없는 빈 오브젝트라 클릭이 닿지 않는다.
        box("랜드마크_발자국그림", (0, 0.3, 5), (5, 0.6, 3.5), "accent",
            "P5 · 여기에 조각을 맞춘다. 완성되면 '전임이 다급하게 향하는' 그림 → 놀람 포인트 #1"),
    ],
    puzzle=puzzle("P5", 240, None, None, ptype="assembly", pieces=["P5_a", "P5_b"],
                  at=(0, 0.2, 5), assembly_range=4.0,   # 랜드마크_발자국그림 자리
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
# 2E 는 22x22 정사각형 큰 방이고 바로 위 3A 는 32x28 이다.
# 높이차가 7밖에 안 되는데 XZ 가 겹치면 어느 방인지 판정이 흔들린다.
P_2D = spiral(0, 21.5, R2); P_2E = spiral(1, 24.0, R2 + 6); P_2F = spiral(2, 26.5, R2)

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
        fold("P9_공간접이면", (0, 0.1, -6), (8, 8), "mid", 0,
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
        fold("P15_공간접이면", (-8, 0.1, -10), (7, 7), "mid", 0,
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
                  at=(0, 0.6, -6), assembly_range=5.0,
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
        fold("P14_A면", (-6, 0.1, -4), (5, 5), "light", 0,
             "P14 ① A면이 빛을 열쇠구멍으로 보낸다"),
        fold("P14_B면", (6, 0.1, -4), (4, 4), "light", 0,
             "P14 ② B면이 빛을 가리면 옆 주름이 깨어나지 못한다"),
        fold("P14_C면", (0, 0.1, 2), (5, 5), "light", 0,
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
    ramp(f"연결_{a_id}→{b_id}", edge_point(a, pb), edge_point(b, pa), width,
         from_id=a_id, to_id=b_id)

for pair in [("F0","1A"),("1A","1B"),("1B","1C"),("1C","1D"),("1D","1E"),
             ("1E","2A"),("2A","2B"),("2B","2C"),("2C","2D"),("2D","2E"),("2E","2F"),
             ("2F","3A"),("3A","3B"),("3B","3C")]:
    connect(*pair)

# ═══════════════════════════════════════════════ 방 확대 + 룸 카메라
#
# 왜 확대하나
#   원래 방은 14x12 ~ 32x28 이었다. 이동 속도 4유닛/초 기준으로
#   가장 작은 방을 가로지르는 데 3.5초밖에 안 걸린다. "방"이 아니라 "발판"이다.
#   XZ 평면만 균일하게 키운다. y(높이)는 건드리지 않으므로
#   경사로는 같은 높이를 더 긴 거리로 오르게 되어 오히려 완만해진다.
#
# 왜 XZ만 균일하게 키우나
#   구역 위치·크기·오브젝트 위치·오브젝트 크기를 같은 배율로 곱하면
#   "겹치지 않는다 / 도달 가능하다" 같은 성질이 그대로 보존된다.
#   손으로 적은 300줄짜리 좌표를 다시 검토할 필요가 없다.

XZ_SCALE = 1.6

# ─────────────────────────────────────────── 플레이어 이동 제원
#
# PlayerController 의 값을 그대로 옮겨 적은 것이다. 둘이 어긋나면
# 아래 점프 검증이 아무것도 보장하지 못하므로 C# 을 고치면 여기도 고쳐야 한다.
MOVE_SPEED_P   = 5.5
GRAVITY        = 18.0
JUMP_HEIGHT    = 1.2
GLIDE_FALL     = 1.6
GLIDE_DRIFT    = 1.4

# 1B 의 바닥 틈. 여기가 P3 의 전부다 —
# "빠진 조각은 찾을 수 없다. 다른 페이지를 접어 그림자로 덮어라"
FLOOR_GAP_BASE = 4.0
CAM_PITCH = 30.0            # PaperCamera.pitch 와 같아야 한다
CAM_PAD   = 0.95            # 방 가장자리 여백. 1.18 은 너무 멀어서 20% 당겼다
CAM_DIST  = 90.0            # 직교 카메라를 뒤로 물리는 거리

# 기본은 yaw 0 (카메라가 -z 쪽에서 +z 를 바라봄 → 뒷벽에 지는 그림자가 보인다).
# 카메라 쪽 벽은 아래 면 컬링으로 숨기므로, 방마다 각도를 따로 줄 일이 거의 없다.
CAM_YAW_OVERRIDE = {}


def apply_xz_scale(s):
    if abs(s - 1.0) < 1e-9:
        return
    for a in AREAS:
        a["pos"]["x"] = round(a["pos"]["x"] * s, 2)
        a["pos"]["z"] = round(a["pos"]["z"] * s, 2)
        a["size"]["x"] = round(a["size"]["x"] * s, 2)
        a["size"]["z"] = round(a["size"]["z"] * s, 2)
        for o in a["objects"]:
            o["pos"]["x"] = round(o["pos"]["x"] * s, 2)
            o["pos"]["z"] = round(o["pos"]["z"] * s, 2)
            o["scale"]["x"] = round(o["scale"]["x"] * s, 2)
            o["scale"]["z"] = round(o["scale"]["z"] * s, 2)
        q = a.get("puzzle")
        if q and q.get("hasAt"):
            q["at"]["x"] = round(q["at"]["x"] * s, 2)
            q["at"]["z"] = round(q["at"]["z"] * s, 2)
            q["assemblyRange"] = round(q["assemblyRange"] * s, 2)
    for r in RAMPS:
        for k in ("from", "to"):
            r[k]["x"] = round(r[k]["x"] * s, 2)
            r[k]["z"] = round(r[k]["z"] * s, 2)
        r["width"] = round(r["width"] * s, 2)

    global FLOOR_GAP
    FLOOR_GAP = round(FLOOR_GAP_BASE * s, 2)


FLOOR_GAP = FLOOR_GAP_BASE


def apply_room_cameras():
    for a in AREAS:
        a["camYaw"] = CAM_YAW_OVERRIDE.get(a["id"], 0)
        a["camPad"] = CAM_PAD


apply_xz_scale(XZ_SCALE)
apply_room_cameras()


# ─────────────────────────────────────────── 벽 면 판정
#
# 2E, 3A 같은 방은 사방 벽 + 천장으로 완전히 닫혀 있다.
# 카메라를 어느 각도에 두든 카메라 쪽 벽이 방 안을 가린다. 천장은 항상 가린다.
# 심즈나 모뉴먼트 밸리가 쓰는 방식대로, 카메라를 향한 벽만 숨긴다.
#
# 숨긴다 = 렌더러를 끄는 게 아니라 ShadowsOnly 로 바꾼다.
# 렌더러를 꺼버리면 그 벽이 빛도 안 막게 되어 그림자 퍼즐의 정답이 바뀐다.
# 이 게임은 그림자가 곧 정답이므로 조명 차폐는 그대로 두어야 한다.
#
# 어느 면인지는 이름이 아니라 형태로 판정한다. 이름 규칙에 기대면
# 나중에 이름 하나 바꾸는 순간 조용히 깨진다.

def infer_face(a, o):
    if o["kind"] != "box":
        return ""
    sx, sy, sz = o["scale"]["x"], o["scale"]["y"], o["scale"]["z"]
    px, py, pz = o["pos"]["x"], o["pos"]["y"], o["pos"]["z"]

    # 천장 — 얇고 높이 떠 있다
    if sy <= 0.5 and py > 3.0 and sx >= a["size"]["x"] * 0.6 and sz >= a["size"]["z"] * 0.6:
        return "ceiling"

    if sy < 3.0:
        return ""                      # 낮은 건 시야를 막지 못한다

    # 외곽 벽만 대상. 방 한가운데 있는 칸막이는 퍼즐 장치이므로 건드리면 안 된다.
    edge_x = a["size"]["x"] * 0.25
    edge_z = a["size"]["z"] * 0.25

    # 외곽 벽이라면 그 방향으로 방을 거의 가로질러야 한다.
    # 이 조건이 없으면 2D 의 '기록' 4장(0.2 x 3 x 3)처럼
    # 벽에 걸린 퍼즐 오브젝트까지 벽으로 오인해서 숨겨버린다.
    SPAN = 0.6
    if sz <= 1.2 and sx > sz and abs(pz) > edge_z and sx >= a["size"]["x"] * SPAN:
        return "north" if pz > 0 else "south"
    if sx <= 1.2 and sz > sx and abs(px) > edge_x and sz >= a["size"]["z"] * SPAN:
        return "east" if px > 0 else "west"
    return ""


def apply_faces():
    for a in AREAS:
        for o in a["objects"]:
            o["face"] = infer_face(a, o)


apply_faces()

# 카메라를 향해서 숨겨지는 면. yaw 0 이면 카메라가 -z 쪽에 있으므로 남쪽 벽이 막는다.
HIDDEN_FACE = {0: "south", 1: "west", 2: "north", 3: "east"}

# ─────────────────────────────────────────── 시야 계산 보조
def yaw_dir(step, pitch=CAM_PITCH):
    """Unity 의 Quaternion.Euler(pitch, yaw, 0) * Vector3.forward 와 같은 값."""
    y = math.radians(step * 90)
    p = math.radians(pitch)
    return (math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p))


def area_box(a):
    """구역이 차지하는 부피. 바닥판 + 벽 높이."""
    h = max(8.0, a["backWall"] + 2.0)
    c = (a["pos"]["x"], a["pos"]["y"] + h * 0.5, a["pos"]["z"])
    e = (a["size"]["x"] * 0.5, h * 0.5, a["size"]["z"] * 0.5)
    return c, e


def seg_hits_box(p0, p1, c, e):
    """선분 p0→p1 이 AABB(center c, extents e)를 지나가는가 (slab 판정)."""
    d = tuple(p1[i] - p0[i] for i in range(3))
    tmin, tmax = 0.0, 1.0
    for i in range(3):
        lo, hi = c[i] - e[i], c[i] + e[i]
        if abs(d[i]) < 1e-9:
            if p0[i] < lo or p0[i] > hi:
                return False
            continue
        t1 = (lo - p0[i]) / d[i]
        t2 = (hi - p0[i]) / d[i]
        if t1 > t2:
            t1, t2 = t2, t1
        tmin = max(tmin, t1)
        tmax = min(tmax, t2)
        if tmin > tmax:
            return False
    return True


# ═══════════════════════════════════════════════ 조명
#
# 아트 문서(Docs/02) 7절이 규칙을 못 박아 뒀다. 그대로 지킨다.
#   · 주광원은 1개(탑 꼭대기). 방향이 항상 명확해야 퍼즐이 성립한다
#   · 광량은 층이 올라갈수록 증가한다
#   · 그림자 엣지는 선명하게 — 그림자가 곧 정답이라 흐릿하면 버그다
#   · 보조광은 약한 앰비언트만. 두 번째 그림자를 만드는 광원은 금지
#   · 색온도는 등불빛 #F2C879
#
# 그래서 강조광(accent)은 전부 그림자를 만들지 않는다.
# 그림자를 안 만드는 광원은 "두 번째 그림자"를 만들지 않으므로 규칙에 어긋나지 않고,
# "빛이 닿는 곳만 존재한다"는 컬러 규칙을 실제 조명으로 구현해 준다.
#
# 앰비언트를 0으로 두지 않는 이유
#   팔레트에 '그림자 = #5C5648, 완전 검정 금지'라고 적혀 있다.
#   앰비언트가 0이면 그림자가 새까매져서 그 규칙이 깨지고,
#   무엇보다 그림자 안의 형태가 안 읽혀 퍼즐이 불가능해진다.

# 층별 광량 — 위로 갈수록 밝아진다
# 아트를 흰 종이 기반으로 바꾸면서 전부 낮췄다.
# 흰 면은 반사율이 높아서 예전 값을 그대로 쓰면 하얗게 날아가고,
# 날아가면 그림자만 보이는 게 아니라 그림자'도' 안 보이게 된다.
# 대신 앰비언트는 올렸다 — 연필 그림자는 회색이지 검정이 아니다.
FLOOR_LIGHT = {
    # fog 는 전부 0 이다. 직교 카메라를 뒤로 물려 쓰는 구조에서는
    # 거리 안개가 원근이 아니라 화면 전체 균일 백화로 나타난다.
    # 배선은 남겨 둔다 — 나중에 원근 카메라를 쓸 일이 생기면 그대로 쓸 수 있다.
    #
    # 앰비언트를 한 번 더 낮췄다.
    # 전체가 고르게 밝으면 강조광이 켜져 있어도 "빛나 보이지" 않는다.
    # 빛은 절대 밝기가 아니라 주변과의 차이로 읽힌다.
    0: dict(sun=0.22, ambient=0.10, fog=0.0),   # 표지 · 게임에서 가장 어둡다
    1: dict(sun=0.72, ambient=0.24, fog=0.0),   # 정돈 · 빛이 잘 든다
    2: dict(sun=0.84, ambient=0.20, fog=0.0),   # 혼란 · 밝지만 대비가 세다 = 조각난 빛
    3: dict(sun=1.05, ambient=0.30, fog=0.0),   # 개방
}
HIDDEN_LIGHT = dict(sun=0.66, ambient=0.18, fog=0.0)

SHADOW_STRENGTH = 0.62   # 흰 종이 위에서는 0.8만 돼도 먹처럼 진해진다


# 랜드마크가 아니지만 반드시 눈에 띄어야 하는 설비.
# 통째로 규칙을 만들지 않고 하나씩 고른 이유가 있다.
#   · mat=="accent" 인 것 전부에 빛을 주면 2D 의 '기록_3_어긋남'까지 빛난다.
#     넷 중 어긋난 하나를 찾는 게 P10 인데, 빛으로 찍어주면 퍼즐이 사라진다.
#   · 위화감 ①②③(반쯤 접힌 페이지·깃털·진한 얼룩)도 빼야 한다.
#     스토리보드가 "강조 연출 없이 지나가면서 마주치게"라고 못 박아 뒀다.
FOCAL = {
    "2C": ["고정표식_못자국"],   # 과거·현재의 공통 참조점. 못 찾으면 P9 가 불공정해진다
    "3A": ["복원대"],           # 조각을 올리는 자리
    "3B": ["열쇠구멍"],          # P14 의 목표
    "3C": ["빛_점화대"],         # 이 방의 전부
}

# 그림자를 받아내는 면. 여기에 빛을 더하면 그림자 대비가 죽어 정답이 안 읽힌다.
# 강조광을 이 근처에 두지 않는다.
SHADOW_CANVAS_HINT = ("뒷벽", "판독벽", "벽_", "천장", "분리벽")

# 이름으로만 거르면 방을 닫는 단순 외벽까지 '퍼즐을 읽는 면'으로 오인된다.
# 그림자는 물리적으로 다 받지만, 검사가 지키려는 건 **퍼즐을 읽는 면**이다.
# 그걸 안 나누면 방 하나 닫을 때마다 없는 오류가 스물몇 개씩 나온다.
NOT_CANVAS_PREFIX = ("외벽_", "천창테_")

def is_canvas(name):
    if any(name.startswith(p) for p in NOT_CANVAS_PREFIX):
        return False
    return any(h in name for h in SHADOW_CANVAS_HINT)


def accent(name, pos, intensity, rng):
    return dict(name=name,
                pos=dict(x=round(pos[0], 2), y=round(pos[1], 2), z=round(pos[2], 2)),
                intensity=intensity, range=rng)


def apply_lighting():
    exit_of = {}
    for r in RAMPS:
        exit_of.setdefault(r["fromId"], r["from"])

    for a in AREAS:
        base = FLOOR_LIGHT.get(a["floor"], HIDDEN_LIGHT) if a["id"] != "HD" else HIDDEN_LIGHT
        a["light"] = dict(sun=base["sun"], ambient=base["ambient"],
                          fog=base["fog"], shadowStrength=SHADOW_STRENGTH)

        acc = []
        for o in a["objects"]:
            px, py, pz = o["pos"]["x"], o["pos"]["y"], o["pos"]["z"]

            # 랜드마크 — 멀리서도 보이는 그 방의 목표물
            if o["name"].startswith("랜드마크"):
                # 뒷벽에 붙어 있는 랜드마크(1E 의 봉인된 페이지 같은 것)에는
                # 등을 달지 않는다. 그 벽이 곧 그림자 캔버스라서, 램프를 달면
                # 정답인 그림자가 씻겨 나간다. 이런 랜드마크는 빛이 아니라
                # 그림자가 드러내야 한다.
                on_canvas = (a["backWall"] > 0.01
                             and abs(pz - a["size"]["z"] * 0.5) < 4.0)
                if on_canvas:
                    continue
                strong = a["id"] == "F0"      # 표지에선 이것 하나만 빛난다
                acc.append(accent("빛_" + o["name"], (px, py + 2.2, pz),
                                  5.0 if strong else 2.6, 15 if strong else 7.5))

            # 활성 주름은 미세하게 빛을 머금는다 (2A에서 세운 시각 규칙)
            elif o["kind"] == "crease" and o.get("active"):
                acc.append(accent("빛_" + o["name"], (px, py + 1.2, pz), 1.5, 5.5))

            # 주울 수 있는 조각. 가짜도 똑같이 빛나야 한다 —
            # 진짜만 빛나면 결을 볼 필요 없이 답이 나온다
            elif o["kind"] == "piece":
                # 표지는 "빛을 받는 건 걸쇠 하나뿐"인 방이다.
                # 그래도 조각이 아예 안 보이면 찾을 방법이 없으므로,
                # 걸쇠를 보러 간 뒤에야 겨우 알아볼 만큼만 남긴다.
                dim = a["id"] == "F0"
                acc.append(accent("빛_" + o["name"], (px, py + 1.4, pz),
                                  0.7 if dim else 1.9, 3.5 if dim else 5.5))

            elif o["name"] in FOCAL.get(a["id"], ()):
                acc.append(accent("빛_" + o["name"], (px, py + 2.0, pz), 2.8, 9.5))

        # 다음 목적지 쪽이 항상 미세하게 더 밝다 (Docs/07 길찾기)
        # 표지만 예외 — 걸쇠를 풀기 전에는 출구가 없다
        # 낮고 좁게 깐다. 문간 바닥에 고이는 정도여야지,
        # 크게 잡으면 출구 쪽 벽까지 밝혀서 그 벽에 지는 그림자를 씻어낸다.
        # "미세하게 더 밝다"가 원래 문서의 표현이기도 하다.
        # 문간에 딱 붙이지 않고 방 안쪽으로 6할 지점에 둔다.
        # 출구는 대개 +z 쪽 가장자리이고 뒷벽도 거기 있어서, 문간에 두면
        # 그 벽에 지는 그림자를 씻어낸다. 안쪽에 두면 "저쪽이 밝다"는
        # 방향 신호는 그대로면서 벽은 건드리지 않는다.
        e = exit_of.get(a["id"])
        if e is not None and a["id"] != "F0":
            ex = (e["x"] - a["pos"]["x"]) * 0.6
            ez = (e["z"] - a["pos"]["z"]) * 0.6
            acc.append(accent("빛_출구", (ex, 1.0, ez), 1.6, 7))

        a["accents"] = acc


apply_lighting()


# ═══════════════════════════════════════════════ 지금 할 일
#
# 완성된 게임에는 UI 가 없다. 목표는 빛과 랜드마크로만 전달한다.
# 그런데 그 연출이 아직 없는 상태라 **프로토타입은 아무것도 알려주지 않는다.**
# "시작하면 뭘 해야 할지 모르겠다"가 세 번 나왔고, 그건 버그가 아니라
# 안내가 통째로 없어서였다.
#
# 그래서 방마다 한 줄짜리 목표를 데이터에 적어 둔다. 개발용 표시(F1)가 이걸 읽는다.
# 출시 빌드에서는 안 쓴다 — 여기 적힌 내용이 곧 "빛으로 전달해야 할 것"의 목록이고,
# 연출이 완성되면 이 줄들이 필요 없어져야 정상이다.

GOALS = {
    "F0": "흩어진 조각 2개를 주워 걸쇠에 맞춘다",
    "1A": "목표 지점에서 필요한 그림자 모양을 확인하고, 돌아와 접이면을 맞춘다",
    "1B": "끊긴 길. 접이면을 접어 그 그림자를 밟고 건넌다",
    "1C": "남북 두 방의 접이면을 각각 맞춰 그림자를 교차시킨다. 가운데 반사 페이지에서 둘 다 보인다",
    "1D": "바닥의 조각을 주워 발자국 그림에 맞춘다. 셋 중 하나는 가짜다",
    "1E": "접이면 3개의 그림자를 합쳐 표지에서 본 문양을 만든다",
    "2A": "빛을 머금은 주름을 클릭해 과거를 재생한다",
    "2B": "잔상이 남긴 그림자 모양을 보고 접이면 각도를 역산한다",
    "2C": "큰 접이면을 접어 방을 과거의 모양으로 포갠다. 못 자국이 기준점이다",
    "2D": "기록 4장을 주름의 기억과 대조한다. 하나만 어긋나 있다",
    "2E": "주름 5개를 오래된 순서대로 깨운다. 마모 정도가 근거다",
    "2F": "통로 끝의 잔상을 재생한다",
    "3A": "흩어진 조각 7개를 모아 복원대에서 그을음 각도 순으로 맞춘다",
    "3B": "세 접이면을 맞춰 빛을 열쇠구멍으로 보낸다. 가로막지 말고 꺾어야 한다",
    "3C": "점화대에서 빛을 켠다",
    "HD": "중첩된 공간에서 접이면을 조작해 바깥 층위의 길을 만든다",
}


def apply_goals():
    for a in AREAS:
        a["goal"] = GOALS.get(a["id"], "")


apply_goals()


# ═══════════════════════════════════════════════ 닫힌 방
#
# "오픈된 공중 정원 같다 / 다른 맵이 보여서 시야를 가린다."
#
# 원인이 둘이었다.
#   ① 경사로 14개 + 잠금 11개가 룸 컬링 밖에 있어 항상 그려졌다 (RoomBound 로 해결)
#   ② 방 16개 중 **닫혀 있는 게 2개뿐**이었다. 나머지는 뒷벽 하나짜리 열린 상자다.
#
# 여기서 ②를 고친다 — 사방 벽과 천장을 덮는다.
#
# 천장을 덮으면 햇빛이 막혀서 그림자 퍼즐이 통째로 죽는다.
# 그래서 천장에 **천창(빛구멍)**을 뚫는다. 광원 각도(Euler 55,-30,0)를 역산해
# 퍼즐에 필요한 것들 위로 정확히 빛이 떨어지는 자리에 낸다.
#
# 이게 오히려 컨셉에 더 맞는다. 아트 문서의 "빛이 닿는 곳만 존재한다" 가
# 비유가 아니라 실제 조명이 되고, 광원 방향이 한 줄기로 명확해져서
# "접힘 → 그림자" 인과를 플레이어가 예측할 수 있게 된다.
LIGHT_DIR = (-0.287, -0.819, 0.497)     # PrototypeSceneBuilder 의 Euler(55,-30,0)
ROOM_H = 7.0                            # 벽·천장 높이
WALL_T = 0.5
CEIL_T = 0.3
SKY_MARGIN = 3.5                        # 천창을 대상보다 이만큼 넉넉히
CEIL_LIP = 2.0                          # 천장 테두리로 최소한 남길 폭

def sky_offset(h=ROOM_H):
    """바닥의 한 점을 비추려면 천창이 그 점에서 얼마나 떨어져 있어야 하는가."""
    t = h / (-LIGHT_DIR[1])
    return (-LIGHT_DIR[0] * t, -LIGHT_DIR[2] * t)

def room_height(a):
    """이 방의 벽·천장 높이. 뒷벽(최대 8)이 천장을 뚫으면 안 된다."""
    return max(a["backWall"] + 1.0, 6.0)

def lit_targets(a):
    """천창 빛이 **반드시** 닿아야 하는 바닥 점들.

    조각과 주름은 여기 안 넣는다. 그것들은 자기 등(`빛_<이름>`)을 하나씩
    달고 있고(20개 중 18개, 나머지 2개는 의도적으로 꺼 둔 죽은 주름이다),
    그 등이 "만질 수 있다"는 신호다.

    전부 천창으로 비추려 하면 천창이 천장의 60~80% 가 되어 버린다.
    그러면 방을 닫은 의미가 없다. 역할을 나눈다 —
      · 천창 = 그림자 퍼즐을 위한 **방향이 있는** 빛
      · 등   = 주울 것·만질 것을 가리키는 **점** 빛
    이게 아트 문서의 "빛이 닿는 곳만 존재한다" 를 더 정확히 옮긴 것이기도 하다.
    """
    pts = []
    for o in a["objects"]:
        if o["kind"] == "fold":
            pts.append((o["pos"]["x"], o["pos"]["z"]))
        elif o["kind"] == "box" and o.get("mat") == "accent":
            pts.append((o["pos"]["x"], o["pos"]["z"]))
    q = a.get("puzzle")
    if q and q.get("hasAt"):
        pts.append((q["at"]["x"], q["at"]["z"]))
    return pts

def cluster(pts, radius=9.0):
    """가까운 대상끼리 묶는다. 무리마다 천창을 따로 낸다.

    하나로 덮으면 1C(접이면 39 떨어짐)·3B 는 천창이 천장의 60~67% 가 된다.
    그러면 방을 닫은 의미가 없다. 무리별로 내면 천장은 거의 막히고
    빛줄기가 여러 개 떨어지는 그림이 된다 — 컨셉에도 이쪽이 맞는다.
    """
    groups = []
    for x, z in sorted(pts):
        for g in groups:
            if (min(abs(x - g["x0"]), abs(x - g["x1"])) <= radius and
                    min(abs(z - g["z0"]), abs(z - g["z1"])) <= radius):
                g["x0"] = min(g["x0"], x); g["x1"] = max(g["x1"], x)
                g["z0"] = min(g["z0"], z); g["z1"] = max(g["z1"], z)
                break
        else:
            groups.append(dict(x0=x, x1=x, z0=z, z1=z))
    return groups


def ceiling_pieces(holes, hx, hz):
    """구멍 뚫린 천장을 최소 개수의 상자로 쪼갠다.

    z 를 띠로 나누고, 띠마다 x 방향으로 구멍 사이 조각만 남긴다.
    격자 타일로 깔면 방 하나에 수백 개가 생겨서 못 쓴다.
    """
    zs = sorted({-hz, hz} | {v for h in holes for v in (h["z0"], h["z1"])})
    out = []
    for i in range(len(zs) - 1):
        z0, z1 = zs[i], zs[i + 1]
        if z1 - z0 < 0.05:
            continue
        zc = (z0 + z1) / 2
        cuts = sorted((h["x0"], h["x1"]) for h in holes if h["z0"] <= zc <= h["z1"])
        x = -hx
        for cx0, cx1 in cuts:
            if cx0 - x > 0.05:
                out.append((x, cx0, z0, z1))
            x = max(x, cx1)
        if hx - x > 0.05:
            out.append((x, hx, z0, z1))
    return out


def apply_rooms_closed():
    for a in AREAS:
        H = room_height(a)
        ox, oz = sky_offset(H)
        hx, hz = a["size"]["x"] / 2, a["size"]["z"] / 2

        # ── 사방 벽. 이미 그 면을 맡은 벽이 있으면 안 세운다
        have = {o.get("face") for o in a["objects"] if o.get("face")}
        if a["backWall"] > 0.01:
            have.add("north")          # 뒷벽은 LevelBuilder 가 north 로 만든다

        edges = dict(
            north=((0.0, hz), (a["size"]["x"], WALL_T)),
            south=((0.0, -hz), (a["size"]["x"], WALL_T)),
            east=((hx, 0.0), (WALL_T, a["size"]["z"])),
            west=((-hx, 0.0), (WALL_T, a["size"]["z"])),
        )
        for face, ((px, pz), (sx, sz)) in edges.items():
            if face in have:
                continue
            w = box(f"외벽_{face}", (px, H / 2, pz), (sx, H, sz), "shade",
                    "방을 닫는 외벽. 카메라 쪽 면은 RoomWall 이 숨긴다. "
                    "이름을 '벽_' 과 분리한 이유는 이게 퍼즐을 읽는 면이 아니기 때문이다")
            w["face"] = face
            a["objects"].append(w)

        # ── 천창: 비춰야 할 것들을 빛 방향으로 천장까지 역투영하고, 무리별로 낸다
        pts = lit_targets(a)
        groups = cluster([(x + ox, z + oz) for x, z in pts]) if pts else \
                 [dict(x0=0.0, x1=0.0, z0=0.0, z1=0.0)]

        holes = []
        for g in groups:
            h = dict(x0=g["x0"] - SKY_MARGIN, x1=g["x1"] + SKY_MARGIN,
                     z0=g["z0"] - SKY_MARGIN, z1=g["z1"] + SKY_MARGIN)
            # 방 밖으로는 못 나간다. 다만 대상이 벽에 붙어 있으면 테두리를 포기한다 —
            # 빛이 안 닿는 접이면을 만드는 것보다 테두리가 얇은 게 낫다.
            h["x0"] = max(h["x0"], -hx + 0.5); h["x1"] = min(h["x1"], hx - 0.5)
            h["z0"] = max(h["z0"], -hz + 0.5); h["z1"] = min(h["z1"], hz - 0.5)
            if h["x1"] - h["x0"] > 0.5 and h["z1"] - h["z0"] > 0.5:
                holes.append({k: round(v, 2) for k, v in h.items()})

        a["skylights"] = holes
        a["roomHeight"] = round(H, 2)

        # ── 천장
        if "ceiling" in have:
            continue
        for n, (x0, x1, z0, z1) in enumerate(ceiling_pieces(holes, hx, hz), 1):
            c = box(f"천창테_{n}", ((x0 + x1) / 2, H, (z0 + z1) / 2),
                    (x1 - x0, CEIL_T, z1 - z0), "shade",
                    "천장. 빛은 천창으로만 들어온다")
            c["face"] = "ceiling"
            a["objects"].append(c)

apply_rooms_closed()


# ═══════════════════════════════════════════════ 출구 잠금
#
# 퍼즐을 풀어도 세계가 아무것도 바뀌지 않으면 플레이어는 "이걸 왜 풀지"를
# 느낄 수가 없다. 실제로 그렇게 만들어 놓고 플레이테스트에서 "재미 요소를
# 모르겠다"는 반응이 나왔다. 로그와 조명 연출만 있었고 진행은 처음부터 열려 있었다.
#
# 그래서 퍼즐이 있는 방은 **출구를 닫아 둔다.**
# 형태는 벽이 아니라 **접혀 선 페이지**다 — 접기가 이 게임의 언어이므로
# 잠금도 그 언어로 말해야 한다. 풀면 그 페이지가 펴져 바닥이 되고 빛이 든다.
#
# 이게 동시에 "여기서 뭘 해야 하나"의 답도 된다. 길이 막혀 있는 걸 보면
# 뒤를 돌아 방을 살피게 된다. 안 보이는 표식보다 이쪽이 훨씬 확실하다.
#
# 퍼즐이 없는 방(2A·2D·2F)의 출구는 열어 둔다. 풀 것이 없는데 막으면
# 그냥 소프트락이다.
GATE_HEIGHT = 3.0          # 점프(1.2)로는 절대 못 넘는 높이여야 한다

def apply_gates():
    by_id = {a["id"]: a for a in AREAS}
    for r in RAMPS:
        a = by_id.get(r.get("fromId"))
        q = (a or {}).get("puzzle")
        r["gatePuzzle"] = q["id"] if q else ""
        r["gateHeight"] = GATE_HEIGHT if q else 0.0

apply_gates()


# ═══════════════════════════════════════════════ 출입구
#
# 방의 외벽·통로벽이 경사로가 붙는 자리를 그대로 막고 있었다.
# 지금까지는 아무 데서나 되돌아갈 수 있어서 티가 안 났는데, 출구를 퍼즐로
# 잠그는 순간 "지나갈 수 있다"가 진행의 전제가 된다. 한 방이라도 막히면
# 그 뒤를 통째로 못 본다.
#
# 그래서 벽을 손으로 하나씩 고치는 대신, 길을 막는 벽에 문을 뚫는다.
# 대상은 두 가지다.
#   · 경사로 끝점이 벽 안에 박혀 있는 경우 (2B·2E·3A·3B)
#   · 입구에서 출구로 가는 직선을 벽이 가로지르는 경우 (2F 의 좁은 통로)
#
# 벽 하나를 문 양옆 두 조각으로 쪼갠다. 이름 앞머리를 지키는 게 중요한데,
# 그림자 면 판정(SHADOW_CANVAS_HINT)이 이름으로 걸러내고 있기 때문이다.
# 주의: 이 단계는 apply_xz_scale 뒤에 돈다. 즉 값이 **이미 1.6배 된 뒤**다.
# 경사로 폭은 그 시점에 6.4 이므로 문은 그보다 조금 넓게 잡는다.
DOOR_W = 8.0

# 문을 그림자 면에 내야 했던 자리. 퍼즐의 픽션이 깨지는 곳이라 따로 보고한다.
DOOR_ON_CANVAS = []

def _seg_hits_box(p, q, o, pad=0.3):
    """선분 p→q 가 상자 o 의 XZ 발자국을 지나는가 (거친 표본)."""
    x0 = o["pos"]["x"] - o["scale"]["x"] * 0.5 - pad
    x1 = o["pos"]["x"] + o["scale"]["x"] * 0.5 + pad
    z0 = o["pos"]["z"] - o["scale"]["z"] * 0.5 - pad
    z1 = o["pos"]["z"] + o["scale"]["z"] * 0.5 + pad
    for i in range(61):
        t = i / 60
        x = p[0] + (q[0] - p[0]) * t
        z = p[1] + (q[1] - p[1]) * t
        if x0 <= x <= x1 and z0 <= z <= z1:
            return (x, z)
    return None

def apply_doorways():
    for a in AREAS:
        ins = [r for r in RAMPS if r.get("toId") == a["id"]]
        outs = [r for r in RAMPS if r.get("fromId") == a["id"]]
        if not ins or not outs:
            continue

        entry = (ins[0]["to"]["x"] - a["pos"]["x"], ins[0]["to"]["z"] - a["pos"]["z"])
        exit_ = (outs[0]["from"]["x"] - a["pos"]["x"], outs[0]["from"]["z"] - a["pos"]["z"])

        out = []
        for o in a["objects"]:
            if o["kind"] != "box" or o.get("bridgeFor"):
                out.append(o); continue

            top = o["pos"]["y"] + o["scale"]["y"] * 0.5
            bottom = o["pos"]["y"] - o["scale"]["y"] * 0.5
            if top <= 1.65 or bottom >= 1.1:
                out.append(o); continue

            # 이 벽을 뚫어야 하는 자리를 **전부** 모은다.
            # 한 곳만 뚫고 끝내면 안 된다 — 2B 의 판독벽과 2E 의 서쪽 벽은
            # 입구와 출구가 같은 벽에 붙어 있어서 문이 두 개 필요하다.
            hits = []
            for pt in (entry, exit_):
                h = _seg_hits_box(pt, pt, o)
                if h: hits.append(h)
            h = _seg_hits_box(entry, exit_, o)
            if h: hits.append(h)

            if not hits:
                out.append(o); continue

            along = "x" if o["scale"]["x"] >= o["scale"]["z"] else "z"
            span = o["scale"][along]
            lo = o["pos"][along] - span / 2
            hi = o["pos"][along] + span / 2

            cuts = []
            for h in hits:
                at = h[0] if along == "x" else h[1]
                at = min(max(at, lo), hi)
                cuts.append((at - DOOR_W / 2, at + DOOR_W / 2))
            cuts.sort()

            # 겹치는 문은 하나로 합친다
            merged = [list(cuts[0])]
            for c0, c1 in cuts[1:]:
                if c0 <= merged[-1][1]:
                    merged[-1][1] = max(merged[-1][1], c1)
                else:
                    merged.append([c0, c1])

            # 문 사이에 남는 조각만 벽으로 남긴다
            parts, cursor, n = [], lo, 0
            for c0, c1 in merged + [[hi, hi]]:
                if c0 - cursor > 0.4:
                    parts.append((cursor, min(c0, hi)))
                cursor = max(cursor, c1)
            if is_canvas(o["name"]):
                DOOR_ON_CANVAS.append((a["id"], o["name"], len(merged)))

            for a0, a1 in parts:
                n += 1
                part = json.loads(json.dumps(o))
                part["name"] = f"{o['name']}_{n}"
                part["scale"][along] = round(a1 - a0, 2)
                part["pos"][along] = round((a0 + a1) / 2, 2)
                out.append(part)

        a["objects"] = out

apply_doorways()




# ═══════════════════════════════════════════════ 출력 / 검증
def build():
    out = dict(areas=AREAS, ramps=RAMPS,
               meta=dict(spiralRadius1=R1, spiralRadius2=R2, slopeLimit=SLOPE_LIMIT,
                         glideDrop=GLIDE_DROP, foldRise=FOLD_RISE,
                         xzScale=XZ_SCALE, camPitch=CAM_PITCH, camPad=CAM_PAD,
                         floorGap=FLOOR_GAP, jumpHeight=JUMP_HEIGHT))
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

    # 2) 구역 겹침
    #
    #    높이차 기준을 4 에서 9 로 올렸다.
    #    4 로는 "물리적으로 안 부딪힘"만 보장될 뿐이고,
    #    XZ 가 겹치는 두 방이 8 정도 차이로 포개져 있으면
    #    플레이어가 어느 방에 있는지 판정이 흔들린다.
    #    (F0 바닥 0 과 1C 바닥 4 가 z 로 5만큼 겹쳐서 실제로 오판정이 났다.)
    OVERLAP_DY = 9.0
    for i in range(len(AREAS)):
        for j in range(i + 1, len(AREAS)):
            a, b = AREAS[i], AREAS[j]
            dy = abs(a["pos"]["y"] - b["pos"]["y"])
            if dy >= OVERLAP_DY: continue
            ox = abs(a["pos"]["x"] - b["pos"]["x"]) < (a["size"]["x"] + b["size"]["x"]) / 2
            oz = abs(a["pos"]["z"] - b["pos"]["z"]) < (a["size"]["z"] + b["size"]["z"]) / 2
            if ox and oz:
                errs.append(f"구역 겹침: {a['id']} ↔ {b['id']} (높이차 {dy:.1f}) — 어느 방인지 판정이 흔들린다")

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

    # 6) 구역 간 시야 — 카메라와 방 사이에 다른 방이 끼어 있는가
    #
    #    이 검사가 없어서 "시작하자마자 플레이어가 안 보이는" 버그가 났다.
    #    나선형으로 쌓아 올린 탑은 F0 바로 위 y=31 에 3A(32x28)가 덮개처럼 얹힌다.
    #    지금은 룸 컬링(현재 방만 렌더링)으로 실제 화면에서는 가려지지 않지만,
    #    컬링을 끄거나 탑 전체를 보여주는 연출에서는 그대로 문제가 되므로 남겨 둔다.
    blocked = []
    for a in AREAS:
        d = yaw_dir(a["camYaw"])
        focus = (a["pos"]["x"], a["pos"]["y"] + 2.0, a["pos"]["z"])
        campos = tuple(focus[i] - d[i] * CAM_DIST for i in range(3))
        for b in AREAS:
            if b["id"] == a["id"]:
                continue
            c, e = area_box(b)
            if seg_hits_box(campos, focus, c, e):
                blocked.append((a["id"], b["id"]))
    for aid, bid in blocked:
        warns.append(f"시야 가림: {aid} 를 보는데 {bid} 가 카메라 앞을 막는다 (룸 컬링으로 실사용에선 해소)")

    # 7) 구역 안 시야 — 방 안의 벽이 퍼즐 요소를 가리는가
    #
    #    이건 컬링으로도 못 막는다. 같은 방 안이라 항상 같이 그려지기 때문이다.
    #    높이 h 인 벽 뒤로는 h / tan(피치) 만큼 사각지대가 생긴다.
    tan_p = math.tan(math.radians(CAM_PITCH))
    SEE = ("fold", "piece", "crease", "marker")
    for a in AREAS:
        yaw = a["camYaw"]
        culled = {HIDDEN_FACE[yaw], "ceiling"}
        for o in a["objects"]:
            if o["kind"] != "box":
                continue
            if o.get("face") in culled:
                continue          # 카메라를 향한 면이라 런타임에 숨겨진다
            top = o["pos"]["y"] + o["scale"]["y"] * 0.5
            if o["scale"]["y"] < 2.0:
                continue
            for t in a["objects"]:
                if t is o or t["kind"] not in SEE:
                    continue
                dx = t["pos"]["x"] - o["pos"]["x"]
                dz = t["pos"]["z"] - o["pos"]["z"]
                if yaw == 0:   along, lateral, half = dz, dx, o["scale"]["x"] * 0.5
                elif yaw == 1: along, lateral, half = dx, dz, o["scale"]["z"] * 0.5
                elif yaw == 2: along, lateral, half = -dz, dx, o["scale"]["x"] * 0.5
                else:          along, lateral, half = -dx, dz, o["scale"]["z"] * 0.5
                depth = (top - t["pos"]["y"]) / tan_p
                if depth <= 0:
                    continue
                if 0 < along < depth and abs(lateral) < half + 0.5:
                    errs.append(
                        f"{a['id']}: '{o['name']}'(높이 {o['scale']['y']:.1f}) 뒤 사각지대에 "
                        f"'{t['name']}' 가 들어감 — 카메라에서 안 보인다")

    # 8) 강조광이 그림자를 씻어내지 않는가
    #
    #    이 게임은 그림자가 정답이다. 그림자를 받아내는 면 가까이에 빛을 하나 더 두면
    #    대비가 죽어서 정답이 안 읽힌다. 아트 문서의 "보조광 금지"와 같은 취지다.
    #
    #    단, 벽이라고 다 그림자 면은 아니다.
    #    접이면이 없는 방(2E 는 주름 순서 퍼즐이다)의 벽은 그냥 벽이고,
    #    거기 그어진 주름은 오히려 빛을 머금어야 한다 — 2A 에서 세운 규칙이다.
    #    그래서 접기 퍼즐이 있는 방의 수직면만 그림자 면으로 본다. 천장은 제외한다.
    for a in AREAS:
        pz = a.get("puzzle")
        if not pz or pz.get("type") != "fold":
            continue

        canvases = [o for o in a["objects"]
                    if o["kind"] == "box" and o["scale"]["y"] >= 2.0
                    and is_canvas(o["name"])
                    and "천장" not in o["name"]]
        if a["backWall"] > 0.01:
            canvases.append(dict(name="뒷벽(자동)",
                                 pos=dict(x=0.0, y=a["backWall"] * 0.5, z=a["size"]["z"] * 0.5),
                                 scale=dict(x=a["size"]["x"], y=a["backWall"], z=0.3)))

        for ac in a.get("accents", []):
            for c in canvases:
                dx = abs(ac["pos"]["x"] - c["pos"]["x"]) - c["scale"]["x"] * 0.5
                dz = abs(ac["pos"]["z"] - c["pos"]["z"]) - c["scale"]["z"] * 0.5
                d = math.hypot(max(dx, 0.0), max(dz, 0.0))
                if d < ac["range"] * 0.55:
                    errs.append(f"{a['id']}: 강조광 '{ac['name']}' 이 그림자 면 "
                                f"'{c['name']}' 에서 {d:.1f} — 그림자 대비가 죽는다 "
                                f"(도달 {ac['range']:.0f})")

    # 9) 점프가 퍼즐을 건너뛰지 않는가
    #
    #    플레이어 스펙(Docs/04)이 "자유 비행 금지"를 명시하고 어떤 퍼즐이 깨지는지까지
    #    적어 뒀다. 점프는 자유 비행의 축소판이라 같은 검사를 받아야 한다.
    #
    #    특히 점프 직후 활공을 이어 붙이면 체공이 길어져 1B 의 틈을 그냥 넘는다.
    #    그래서 코드에서 '점프로 뜬 동안은 활공 금지'를 걸었고, 여기서 그 전제 아래
    #    실제로 못 넘는지 확인한다. 전제가 깨지면 이 검사도 같이 깨져야 한다.
    t_up = math.sqrt(2 * JUMP_HEIGHT / GRAVITY)
    jump_dist = MOVE_SPEED_P * 2 * t_up
    chain_dist = MOVE_SPEED_P * t_up + (MOVE_SPEED_P + GLIDE_DRIFT) * (JUMP_HEIGHT / GLIDE_FALL)

    if jump_dist >= FLOOR_GAP * 0.85:
        errs.append(f"점프 도달 거리 {jump_dist:.1f} 가 1B 의 틈 {FLOOR_GAP:.1f} 에 너무 가깝다 "
                    f"— P3(그림자로 길 잇기)를 그냥 뛰어넘게 된다")
    if chain_dist < FLOOR_GAP:
        warns.append(f"점프+활공 도달 {chain_dist:.1f} < 틈 {FLOOR_GAP:.1f} — "
                     f"활공 금지 없이도 안전하지만, 값이 바뀌면 다시 위험해진다")

    # 접이면을 세웠을 때의 높이를 점프로 넘어버리면 접을 이유가 없어진다
    for a in AREAS:
        for o in a["objects"]:
            if o["kind"] != "fold":
                continue
            upright = o["scale"]["z"]          # 90도로 세우면 z 길이만큼 선다
            if JUMP_HEIGHT >= upright * 0.8:
                errs.append(f"{a['id']}: 점프 {JUMP_HEIGHT} 로 접이면 '{o['name']}'"
                            f"(세우면 {upright:.1f}) 를 넘는다 — 접을 이유가 사라진다")

    # 10) 조립 퍼즐의 '맞추는 자리'가 실제 설비 위에 있는가
    #
    #    조각을 다 모아도 이 자리까지 가야 완성된다. 자리가 허공이면
    #    플레이어는 조각을 들고 방을 헤매게 되고, 아무 단서도 없다.
    for a in AREAS:
        q = a.get("puzzle")
        if not q or not q.get("hasAt"):
            continue
        # 표식(marker)은 렌더러도 콜라이더도 없는 빈 오브젝트라 클릭이 닿지 않는다.
        # 맞추는 자리에는 눈에 보이고 누를 수 있는 물건이 있어야 한다.
        CLICKABLE = ("box", "fold", "piece", "crease")
        near = None
        for o in a["objects"]:
            if o["kind"] not in CLICKABLE:
                continue
            d = math.hypot(o["pos"]["x"] - q["at"]["x"], o["pos"]["z"] - q["at"]["z"])
            if near is None or d < near[0]:
                near = (d, o["name"])
        if near is None or near[0] > 2.5:
            errs.append(f"{a['id']} {q['id']}: 맞추는 자리에 누를 물건이 없다 "
                        f"(가장 가까운 것 {near[1] if near else '없음'} "
                        f"{near[0]:.1f} 떨어짐) — 표식만 있으면 클릭이 닿지 않는다")

    # 11) 모든 방에 "지금 할 일"이 적혀 있는가
    #
    #    빈 방이 하나라도 있으면 플레이어는 거기서 멈춘다.
    #    이 줄은 동시에 "빛으로 전달해야 할 것"의 목록이기도 하다.
    for a in AREAS:
        if not a.get("goal"):
            errs.append(f"{a['id']}: 지금 할 일이 비어 있다 — 들어가면 뭘 해야 할지 알 방법이 없다")

    # 12) 다른 방에 서서 손이 닿는 물체가 있는가
    #
    #    룸 컬링은 다른 방의 **렌더러만** 끈다 (콜라이더는 살려 둬야 경사로가 유지된다).
    #    그런데 상호작용 사거리는 거리로만 판정하므로, 방 A 의 구석에 서면
    #    보이지도 않는 방 B 의 물체가 사거리 안에 들어올 수 있다.
    #
    #    실제로 F0(표지, 첫 방)의 가장자리에서 1B 의 P3 접이면이 6.4 에 있다 (사거리 7).
    #    첫 방에서 보이지 않는 퍼즐을 건드리게 되는 최악의 경우다.
    #    지금은 TargetInteractor·InteractionRay 가 "안 보이면 후보에서 뺀다" 로 막고 있다.
    #    이 검사는 그 방어가 필요한 곳이 어디인지를 계속 드러내 준다 —
    #    방어를 지우면 그날로 버그가 된다.
    REACH = {"fold": 7.0, "piece": 3.0, "crease": 3.5}
    for a in AREAS:
        x0, x1 = a["pos"]["x"] - a["size"]["x"] / 2, a["pos"]["x"] + a["size"]["x"] / 2
        z0, z1 = a["pos"]["z"] - a["size"]["z"] / 2, a["pos"]["z"] + a["size"]["z"] / 2
        eye = a["pos"]["y"] + 0.9
        for b in AREAS:
            if b["id"] == a["id"]:
                continue
            for o in b["objects"]:
                r = REACH.get(o["kind"])
                if not r:
                    continue
                ox = b["pos"]["x"] + o["pos"]["x"]
                oy = b["pos"]["y"] + o["pos"]["y"]
                oz = b["pos"]["z"] + o["pos"]["z"]
                cx = min(max(ox, x0), x1)
                cz = min(max(oz, z0), z1)
                d = math.dist((cx, eye, cz), (ox, oy, oz))
                if d <= r:
                    warns.append(f"{a['id']} 에 서서 {b['id']} 의 '{o['name']}' 에 손이 닿는다 "
                                 f"({d:.1f} / 사거리 {r:.0f}) — 가시성 검사가 막고 있다")

    # 13) 출구 잠금이 말이 되는가
    #
    #     · 퍼즐이 있는 방은 출구가 잠겨 있어야 한다 — 안 그러면 풀 이유가 없다
    #     · 퍼즐이 없는 방은 잠기면 안 된다 — 그건 소프트락이다
    #     · 잠금 페이지는 점프로 넘어지면 안 된다
    #     · 마지막 방에서 나가는 길은 없어도 된다
    by_id = {a["id"]: a for a in AREAS}
    gated = 0
    for r in RAMPS:
        a = by_id.get(r.get("fromId"))
        if a is None:
            errs.append(f"{r['name']}: fromId '{r.get('fromId')}' 가 어느 구역도 아니다")
            continue
        has_puzzle = bool(a.get("puzzle"))
        h = r.get("gateHeight", 0.0)
        if has_puzzle and h <= 0.01:
            errs.append(f"{r['name']}: {a['id']} 에 퍼즐({a['puzzle']['id']})이 있는데 "
                        f"출구가 안 잠긴다 — 풀 이유가 없어진다")
        if not has_puzzle and h > 0.01:
            errs.append(f"{r['name']}: {a['id']} 에 퍼즐이 없는데 출구가 잠긴다 — 소프트락")
        if h > 0.01:
            gated += 1
            if JUMP_HEIGHT >= h * 0.8:
                errs.append(f"{r['name']}: 잠금 페이지 높이 {h:.1f} 를 점프 {JUMP_HEIGHT} 로 넘는다")

    # 퍼즐이 있는 방이 전부 잠겨 있는지 뒤집어서도 확인한다.
    # 마지막 방(나가는 경사로가 없는 방)은 예외다.
    out_ids = {r.get("fromId") for r in RAMPS}
    for a in AREAS:
        if a.get("puzzle") and a["id"] not in out_ids and a["id"] != "HD":
            warns.append(f"{a['id']}: 퍼즐 {a['puzzle']['id']} 이 있는데 나가는 경사로가 없다 "
                         f"— 풀어도 열릴 길이 없다")

    # 14) 바닥이 갈라진 방을 실제로 지날 수 있는가
    #
    #     1B 는 바닥 가운데가 비어 있고, 입구와 출구가 그 간극의 반대편에 있다.
    #     점프 도달 거리는 4.0, 간극은 6.4 라 절대 못 뛴다 — 설계상 맞다.
    #     그런데 그러려면 두 가지가 반드시 성립해야 한다.
    #
    #       · 퍼즐을 **입구 쪽에서** 풀 수 있어야 한다.
    #         실제로 P3 의 접이면이 간극 **건너편**에 놓여 있었다.
    #         풀 수도, 건널 수도 없는 방이었는데 아무도 못 잡고 있었다.
    #       · 풀면 간극을 잇는 판이 올라와야 한다. 없으면 풀어도 못 지난다.
    for a in AREAS:
        g = a.get("splitGap", 0.0) * XZ_SCALE
        if g <= 0.01:
            continue

        ins = [r for r in RAMPS if r.get("toId") == a["id"]]
        outs = [r for r in RAMPS if r.get("fromId") == a["id"]]
        if not ins or not outs:
            continue

        entry_z = ins[0]["to"]["z"] - a["pos"]["z"]
        exit_z = outs[0]["from"]["z"] - a["pos"]["z"]
        if (entry_z > 0) == (exit_z > 0):
            warns.append(f"{a['id']}: 입구와 출구가 간극의 같은 쪽이다 — 간극을 건널 이유가 없다")
            continue

        near = 1.0 if entry_z > 0 else -1.0     # 입구가 있는 쪽

        q = a.get("puzzle") or {}
        need = set(q.get("panels", [])) | set(q.get("pieces", []))
        for o in a["objects"]:
            if o["name"] not in need and o.get("pieceId", "") not in need:
                continue
            side = 1.0 if o["pos"]["z"] > 0 else -1.0
            if side != near or abs(o["pos"]["z"]) < g * 0.5:
                errs.append(f"{a['id']} {q.get('id')}: '{o['name']}' 이 간극 건너편(z {o['pos']['z']:.1f})에 있다 "
                            f"— 건너야 풀 수 있는데 풀어야 건널 수 있다")

        bridges = [o for o in a["objects"] if o.get("bridgeFor")]
        if not bridges:
            errs.append(f"{a['id']}: 간극 {g:.1f} 을 잇는 판이 없다 — 풀어도 못 지난다")
        for b in bridges:
            if q and b["bridgeFor"] != q.get("id"):
                errs.append(f"{a['id']}: 다리 '{b['name']}' 가 '{b['bridgeFor']}' 를 보는데 "
                            f"이 방 퍼즐은 {q.get('id')} 다")
            if b["scale"]["z"] < g:
                errs.append(f"{a['id']}: 다리 '{b['name']}' 깊이 {b['scale']['z']:.1f} < 간극 {g:.1f} "
                            f"— 덮이지 않는 틈이 남는다")
            lo = b["pos"]["z"] - b["scale"]["z"] * 0.5
            hi = b["pos"]["z"] + b["scale"]["z"] * 0.5
            if lo > -g * 0.5 or hi < g * 0.5:
                errs.append(f"{a['id']}: 다리 '{b['name']}' 가 간극을 다 못 덮는다 "
                            f"(다리 z {lo:.1f}~{hi:.1f}, 간극 {-g/2:.1f}~{g/2:.1f})")

    # 15) 입구에서 출구까지 실제로 걸어갈 수 있는가
    #
    #     출구를 퍼즐로 잠그면서 "지나갈 수 있다"가 진행의 전제가 됐다.
    #     예전엔 아무 데서나 되돌아갈 수 있어서 길이 막혀도 티가 안 났는데,
    #     이제는 한 방이라도 못 지나가면 그 뒤를 통째로 못 본다.
    #
    #     그래서 방마다 바닥을 격자로 깔고 입구에서 출구까지 채워 본다.
    #     막는 것으로 치는 것: 점프(1.2)+턱(0.45)보다 높은 상자, 그리고 간극.
    #     (간극은 퍼즐을 풀면 다리가 올라오므로 '지나갈 수 있음'으로 본다 —
    #      풀기 전에 닿아야 하는 것은 검사 14번이 따로 본다.)
    #     접이면·조각·주름은 납작하거나 밟고 지나갈 수 있으니 안 친다.
    CELL = 0.5
    R_PLAYER = 0.25
    CLIMB = JUMP_HEIGHT + 0.45
    PLAYER_H = 1.1

    for a in AREAS:
        ins = [r for r in RAMPS if r.get("toId") == a["id"]]
        outs = [r for r in RAMPS if r.get("fromId") == a["id"]]
        if not ins or not outs:
            continue

        sx, sz = a["size"]["x"], a["size"]["z"]
        nx, nz = int(sx / CELL) + 1, int(sz / CELL) + 1

        def cell(px, pz):
            return (min(max(int((px + sx / 2) / CELL), 0), nx - 1),
                    min(max(int((pz + sz / 2) / CELL), 0), nz - 1))

        blocked = [[False] * nz for _ in range(nx)]
        for o in a["objects"]:
            if o["kind"] != "box":
                continue
            top = o["pos"]["y"] + o["scale"]["y"] * 0.5
            bottom = o["pos"]["y"] - o["scale"]["y"] * 0.5
            # 넘어갈 수 있으면(낮으면) 안 막고, 밑으로 지나갈 수 있으면(천장) 안 막는다
            if top <= CLIMB or bottom >= PLAYER_H or o.get("bridgeFor"):
                continue
            x0 = o["pos"]["x"] - o["scale"]["x"] * 0.5 - R_PLAYER
            x1 = o["pos"]["x"] + o["scale"]["x"] * 0.5 + R_PLAYER
            z0 = o["pos"]["z"] - o["scale"]["z"] * 0.5 - R_PLAYER
            z1 = o["pos"]["z"] + o["scale"]["z"] * 0.5 + R_PLAYER
            i0, j0 = cell(x0, z0)
            i1, j1 = cell(x1, z1)
            for i in range(i0, i1 + 1):
                for j in range(j0, j1 + 1):
                    blocked[i][j] = True

        si, sj = cell(ins[0]["to"]["x"] - a["pos"]["x"], ins[0]["to"]["z"] - a["pos"]["z"])
        ti, tj = cell(outs[0]["from"]["x"] - a["pos"]["x"], outs[0]["from"]["z"] - a["pos"]["z"])

        if blocked[si][sj] or blocked[ti][tj]:
            errs.append(f"{a['id']}: 입구나 출구 자리가 막혀 있다")
            continue

        seen = [[False] * nz for _ in range(nx)]
        stack = [(si, sj)]
        seen[si][sj] = True
        while stack:
            i, j = stack.pop()
            for di, dj in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p, q2 = i + di, j + dj
                if 0 <= p < nx and 0 <= q2 < nz and not seen[p][q2] and not blocked[p][q2]:
                    seen[p][q2] = True
                    stack.append((p, q2))

        if not seen[ti][tj]:
            errs.append(f"{a['id']}: 입구에서 출구까지 걸어갈 수 없다 — "
                        f"벽이 길을 완전히 막고 있다")

    # 16) 문을 그림자 면에 뚫은 곳
    #
    #     경사로가 붙는 자리를 벽이 막고 있으면 문을 뚫는데, 그 벽이 동시에
    #     그림자를 받는 면이면 그림자가 그 구멍으로 샌다. 판정은 논리값이라
    #     퍼즐이 깨지지는 않지만 "보고 푼다"가 성립하지 않는다.
    #     즉 방 배치가 잘못된 것이고, 사람이 결정할 문제다.
    for aid, nm, k in DOOR_ON_CANVAS:
        warns.append(f"{aid}: 그림자 면 '{nm}' 에 문을 {k}개 뚫었다 — "
                     f"경사로가 붙는 자리와 그림자 면이 겹친다. 배치를 다시 볼 것")

    # 17) 천창 빛이 그림자 퍼즐에 실제로 닿는가
    #
    #     방을 천장으로 덮었으므로 햇빛은 천창으로만 들어온다.
    #     접이면에 빛이 안 닿으면 그림자가 안 생기고, 그림자 퍼즐이 통째로 죽는다.
    #     천창을 빛 방향으로 바닥까지 내려 '빛 웅덩이'를 만들고 그 안에 있는지 본다.
    #     (조각·주름은 자기 등이 따로 있으므로 여기 대상이 아니다 — lit_targets 참고)
    for a in AREAS:
        holes = a.get("skylights") or []
        H = a.get("roomHeight", ROOM_H)
        ox, oz = sky_offset(H)

        if not holes:
            errs.append(f"{a['id']}: 천창이 하나도 없다 — 천장 아래가 통째로 어둡다")
            continue

        def lit(x, z):
            return any(h["x0"] - ox <= x <= h["x1"] - ox and
                       h["z0"] - oz <= z <= h["z1"] - oz for h in holes)

        for o in a["objects"]:
            if o["kind"] == "fold" and not lit(o["pos"]["x"], o["pos"]["z"]):
                errs.append(f"{a['id']}: 접이면 '{o['name']}' 에 천창 빛이 안 닿는다 "
                            f"— 그림자가 안 생긴다")
        q = a.get("puzzle")
        if q and q.get("hasAt") and not lit(q["at"]["x"], q["at"]["z"]):
            errs.append(f"{a['id']} {q['id']}: 맞추는 자리에 빛이 안 닿는다 — 찾을 수가 없다")

        area = a["size"]["x"] * a["size"]["z"]
        hole = sum((h["x1"] - h["x0"]) * (h["z1"] - h["z0"]) for h in holes)
        if hole / area > 0.35:
            warns.append(f"{a['id']}: 천창이 천장의 {hole/area*100:.0f}% 다 "
                         f"— 이 정도면 방을 닫은 의미가 옅다")

    print(f"구역 {len(AREAS)} / 경사로 {len(RAMPS)} / 잠긴 출구 {gated}")
    print(f"\n오류 {len(errs)}건")
    for e in errs: print("  ✗", e)
    print(f"\n경고 {len(warns)}건")
    for w in warns: print("  !", w)
    return len(errs)



# ═══════════════════════════════════════════════ 이동 시뮬레이션
MOVE_SPEED = 5.5    # PlayerController.moveSpeed (방을 키우면서 상향)

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
