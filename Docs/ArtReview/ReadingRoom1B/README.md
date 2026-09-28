# 1B 이미지와 Unity 검수

AI 생성 레퍼런스와 실제 Unity 렌더를 구분했다. 상세 규칙·이야기·제작 기록은 [28번 문서](../../28_1B_찢어진열람실_제작기록.md)를 참고한다.

| 시점 / 장치 | 재생성 레퍼런스 | 4차 Unity 렌더 |
|---|---|---|
| 정면 | [원화](References/01-front.png) | [실제 렌더](Iterations/Pass4/01-front.png) |
| 좌측 책상 | [원화](References/02-left.png) | [실제 렌더](Iterations/Pass4/02-left.png) |
| 우측 서가 | [원화](References/03-right.png) | [실제 렌더](Iterations/Pass4/03-right.png) |
| 입구 방향 | [원화](References/04-back.png) | [실제 렌더](Iterations/Pass4/04-back.png) |
| 기억등 | [원화](References/05-puzzle-lantern.png) | [실제 렌더](Iterations/Pass4/05-lantern.png) |
| 도르래 창 | [원화](References/06-puzzle-window.png) | [실제 렌더](Iterations/Pass4/06-window.png) |

[실제 플레이의 3인칭 카메라 렌더](play-third-person.png) · [플레이 검사 결과](play-check.txt) · [구조 검사 결과](Iterations/Pass4/structure.txt)

카메라 렌더에는 Screen Space Overlay UI가 포함되지 않는다. AI 원화와 똑같은 실시간 화면이라는 의미는 아니다. 카메라 위치·장치 배치·재질·라이팅 차이는 위 비교에서 직접 확인할 수 있다.

## 개별 소품과 Unity 프리팹

프리팹 폴더: `Assets/_BookTower/Generated/ReadingRoom1B/Modules/`

| 소품 원화 | 대응 프리팹 |
|---|---|
| [책상](References/asset-01-desk.png) | 02_ArchedWritingDesk.prefab |
| [서가](References/asset-02-bookcase.png) | 01_TallBookcase.prefab |
| [기억등](References/asset-03-lantern.png) | 03_MemoryLantern.prefab |
| [도르래 창](References/asset-04-window.png) | 04_DoorWindowPulley.prefab |
| [샹들리에](References/asset-05-chandelier.png) | 05_PaperChandelier.prefab |
| [깃발](References/asset-06-banner.png) | 06_RavenBanner.prefab |
| [러너 다리](References/asset-07-rug.png) | 07_RunnerBridge.prefab |
| [의자](References/asset-08-chair.png) | 08_GothicReadingChair.prefab |
| [사다리](References/asset-09-ladder.png) | 09_LibraryLadder.prefab |
| [제본사 판화](References/asset-10-portrait.png) | 10_BookbinderPortrait.prefab |

개별 프리팹은 아트 모듈이다. 퍼즐 진행·기존 P3·룸 컬링 연결은 제작기가 InGame 씬에 배치할 때 설정한다. 단일 모듈을 빈 씬에 놓는 것만으로 전 방 진행 시스템이 만들어지지는 않는다.

## 비교 이력

- [1차 정면](Iterations/Pass1/01-front.png): 배치·끝벽·조도 문제 확인
- [2차 정면](Iterations/Pass2/01-front.png): 문양 도입, 메시 재생성 문제 확인
- [3차 정면](Iterations/Pass3/01-front.png): 메시·조명 수정 후 과도한 패턴과 입구 빈 면 확인
- [4차 정면](Iterations/Pass4/01-front.png): 배색 정리·입구 보강·통로 연결·퍼즐 조작 검사 후 최종 수정

원화의 미세한 닳음, 불규칙한 표면 조각과 장식 밀도는 아직 실시간 모델보다 복잡하다. 현재 결과의 동일성은 색·주요 물건·기본 공간 배치 수준이며, 원화의 모든 픽셀·형태를 복제한 결과로 평가하면 안 된다.
