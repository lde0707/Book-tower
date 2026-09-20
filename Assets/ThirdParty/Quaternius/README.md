# Quaternius Ghost — 정령 플레이어 후보

원격 브라우저로 받아 왔습니다. `Quaternius_Ghost.glb` (203,360 B)

## 출처 · 라이선스

- https://poly.pizza/m/Iip30bDHmu
- 팩: **Ultimate Monsters** — "50 Monsters with attack, death, running,
  walking, and many more animations. In .FBX, and glTF formats.
  Free to use in any project, even commercially! (CC0)"
- 제작자: Quaternius — https://quaternius.com/
- **CC0 1.0 Universal** (퍼블릭 도메인). 출처 표기 의무 없음.
  다만 포트폴리오로 낼 거라면 크레딧에 적어 두는 쪽을 권합니다.

## 뜯어본 내용

| | |
|---|---|
| glTF | 2.0 binary |
| 삼각형 | **3,192** |
| 노드 / 스킨 | 51 / 1 (본 리깅 있음) |
| 머티리얼 | 3 — `Ghost_Main`, `Eye_White`, `Eye_Black` |
| **텍스처** | **없음.** baseColorFactor 단색뿐 |
| alphaMode | 세 개 모두 **OPAQUE** |
| 원본 크기 | 높이 3.13 유닛 → 1.40 으로 쓰려면 **×0.447** |

### 애니메이션 8개

| 클립 | 길이(초) | 쓸 곳 |
|---|---|---|
| `Flying_Idle` | 1.17 | **정지** |
| `Fast_Flying` | 0.83 | **이동** |
| `No` | 1.17 | 고갯짓 — 연출용 |
| `Yes` | 1.17 | 고갯짓 — 연출용 |
| `Death` | 0.67 | 안 씀 |
| `Headbutt` | 1.17 | 안 씀 |
| `HitReact` | 0.46 | 안 씀 |
| `Punch` | 1.17 | 안 씀 |

전투 클립이 넷이지만 `Flying_Idle` + `Fast_Flying` 둘이면
"떠 있고 / 떠서 이동" 이 성립합니다.

## 이 프로젝트 기준으로 좋은 점

1. **텍스처가 없다.** 색이 단색 세 개뿐이라 종이 팔레트로 갈아입히는 게
   값 세 개 수정으로 끝납니다. 보통은 이게 제일 큰 비용인데 그게 없습니다.
2. **OPAQUE 라 그림자가 제대로 진다.** 반투명·발광 유령이었으면
   P15 왜상과 1C 교차그림자가 성립하지 않습니다. 실제로 투영해 확인했습니다.
3. 3.2k 삼각형 — 이 게임 규모에 충분히 가볍습니다.

## 걸리는 점

1. **머리 위 돌기 두 개가 귀/뿔로 읽힙니다.** 정령이라기보다 고양이·악마 쪽.
   "뾰족하고 딱딱하다" 는 이 프로젝트의 기존 지적과도 겹칩니다.
2. **팔이 둘 달려 앞으로 뻗어 있습니다.** 인물 없음 컨셉과 어긋나고,
   yaw 0/180 정면에서 위협적으로 보입니다.
3. **눈이 있습니다.** 아트 규칙은 "눈·표정 없음. 고개 각도, 날개 접힘
   정도, 정지 시간으로만" 입니다.

1·2·3 은 전부 메시를 손대야 하는 문제라 값 수정으로는 안 됩니다.

## 검증 렌더

`Claude outputs/qa_ghost_vs_bird.png` — 4방향 × (원본색 / 종이팔레트 /
Fast_Flying / 현재 책갈피새), 실제 크기·태양각·바닥 그림자 포함
`Claude outputs/qa_ghost_shadow.png` — 그림자만
