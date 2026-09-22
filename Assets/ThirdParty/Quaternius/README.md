# Quaternius Ghost — 정령 플레이어

`Quaternius_Ghost.glb` (203,360 B)

## 출처 · 라이선스

- https://poly.pizza/m/Iip30bDHmu
- 팩: **Ultimate Monsters** — "50 Monsters with attack, death, running,
  walking, and many more animations. In .FBX, and glTF formats.
  Free to use in any project, even commercially! (CC0)"
- 제작자: Quaternius — https://quaternius.com/
- **CC0 1.0 Universal** (퍼블릭 도메인). 출처 표기 의무 없음.
  포트폴리오로 낼 거라면 크레딧에 적어 두는 쪽을 권합니다.

## 뜯어본 내용

| | |
|---|---|
| glTF | 2.0 binary |
| 삼각형 | 3,192 |
| 노드 / 스킨 | 51 / 1 (본 리깅 있음) |
| 머티리얼 | 3 — `Ghost_Main`(몸), `Eye_White`(눈알), `Eye_Black`(눈꺼풀 선 **+ 입**) |
| 텍스처 · UV | 없음 — 그래서 GhostPlayerBuilder 가 복제 메시에 UV 를 만든다 |
| alphaMode | 모두 OPAQUE — 그림자가 제대로 진다 |
| 원본 높이 | 3.13 유닛 (빌더가 렌더러 경계로 재서 1.40 에 맞춘다) |

### 애니메이션 8개

| 클립 | 길이(초) | 쓰임 |
|---|---|---|
| `Flying_Idle` | 1.17 | 정지 |
| `Fast_Flying` | 0.83 | 이동 |
| `No` / `Yes` | 1.17 | 연출용 (아직 안 씀) |
| `Death` `Headbutt` `HitReact` `Punch` | — | 안 씀 |

## 가공

- 팔: `UpperArm.L/R` 본을 LateUpdate 에서 크기 0 으로 접는다 (메시는 그대로)
- 색: 몸 T_Ghost_Ramp(위 따뜻한 종이 → 아래 차가운 회색), 눈알 #F5F0E8,
  눈동자·입 #4A4038, 목 뒤 리본 크레파스 빨강

## 받는 법 (다시 받아야 할 때)

작업 환경 네트워크가 poly.pizza 를 막는다. Claude 내장 브라우저로 페이지를 열고
`https://static.poly.pizza/810f60a2-6e45-4c4e-a0d5-da91cd7288bd.glb.br` 를 fetch 해
Blob 다운로드. 다운로드 폴더에 `.tmp` 이름으로 떨어진다.
