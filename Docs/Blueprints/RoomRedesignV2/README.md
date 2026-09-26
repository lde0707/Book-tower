# Room Redesign V2 · 인게임 블록아웃 설계도

이 폴더는 컨셉 레퍼런스를 Unity 블록아웃으로 옮기기 위한 제작 기준이다. 모든 수치는 Unity unit이며 정령 높이는 1.2u다. 설계 상태는 `proposed_not_playtested`다.

## 도면 구성

- [00-world-blockout.svg](00-world-blockout.svg): 16개 고유 방의 월드 배치, 진행 순서, 스트리밍 단위, 3A–3B–3C 역광 축.
- 방별 SVG: 로컬 평면도, 주요 구역 중심과 반경, 추천 동선, 문 소켓, C1 카메라, 높이 단면, 완료 조건과 복구 규칙.
- [implementation-layout.json](implementation-layout.json): 월드 중심, 공통 클리어런스, 씬 이름, 3A 상태 변형.
- [build-order.md](build-order.md): Unity 적용 순서와 통과 기준.
- 생성기는 [generate_ingame_blueprints.js](../../../Tools/generate_ingame_blueprints.js)이며 원본 설계 JSON을 다시 읽어 도면을 재생성한다.

## 방별 도면

- [F0 잠금쇠의 요람](01-F0-blueprint.svg)
- [1A 부채잎 관측소](02-1A-blueprint.svg)
- [1B 찢어진 초승달 협곡](03-1B-blueprint.svg)
- [1C 숨 쉬는 아코디언 정원](04-1C-blueprint.svg)
- [1D 발자국이 자라는 정원](05-1D-blueprint.svg)
- [1E 매달린 인장의 저울](06-1E-blueprint.svg)
- [2A 주름을 짜는 계곡](07-2A-blueprint.svg)
- [2B 빈자리 그림자 극장](08-2B-blueprint.svg)
- [2C 돌아가는 지도 꽃](09-2C-blueprint.svg)
- [2D 거짓 기록의 분류 폭포](10-2D-blueprint.svg)
- [2E 원인의 나선 시계](11-2E-blueprint.svg)
- [2F 아이의 접힌 둥지](12-2F-blueprint.svg)
- [3A 펼쳐진 스승의 성소](13-3A-blueprint.svg)
- [3B 세 갈래 빛의 매듭](14-3B-blueprint.svg)
- [3C 뒤돌아보는 등대](15-3C-blueprint.svg)
- [HD 책 안의 작은 책](16-HD-blueprint.svg)

## 수치 해석

- 외곽 치수는 컨셉의 체감 규모를 유지하기 위한 최대 범위다. 첫 블록아웃은 외곽을 100% 만들되, 실제 걷는 길은 3–5개 구역 사이에 집중한다.
- 구역 원의 기본 반경 8u는 퍼즐 소품 배치 영역이다. 플레이어 상호작용 반경 2.4u와 혼동하지 않는다.
- 문 유효 폭 5u, 높이 4u, 다리 접합부 겹침 깊이 4u. 닫힌 문 주변은 벽 콜라이더를 연속시켜 우회 통과를 막는다.
- 주 동선 폭 5.5u, 보조 동선 최소 4u, 난간 높이 1.15u. 점프 필수 구간은 수평 2u, 상승 0.6u 이하에서 시작해 실제 컨트롤러로 다시 측정한다.
- 움직이는 면의 최소 여유 0.35u. 서로 다른 장식 면은 0.025u 이상 분리하거나 하나의 메시로 합쳐 지직거림을 막는다.
- 낙하는 최근 안전 구역의 바닥보다 2.5u 낮아지면 복귀시키고, 운반물과 기록 상태는 유지한다.

## 제작 원칙

각 방은 `Room_<ID>.unity` additive scene으로 저작하고 로컬 원점에서 작업한다. Base scene에는 플레이어, GameManager, 단일 태양, 오디오만 둔다. 현재 방과 앞뒤 방을 불러오며 체크포인트 통과 뒤 두 방 전 셀을 해제한다. 3A 재방문은 별도 방을 복사하지 않고 같은 3A 씬의 상태 변형으로 만든다.

월드 중심은 기존 단일 씬에 그대로 덮어쓰는 좌표가 아니라 스트리밍/연결 검증용 staging 좌표다. 먼저 방별 씬에서 완성한 뒤 연결 소켓으로 배치한다. 기존 레벨을 한 번에 교체하면 현재 통과한 문·다리 회귀 결과가 무효가 되므로 F0–1B 수직 슬라이스부터 적용한다.

3A, 3B, 3C는 +Z 직선 광축을 공유한다. 역광은 Y=4u의 별도 통광 개구를 통과하고 보행로/장식과 분리한다. 3C의 높은 등대 광원은 연출 광선이며, 논리 광선은 고정된 P15 축으로 내려와 3A까지 전달한다.


