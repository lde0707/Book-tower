# F0 완성 아트 01 — 구조 적용 기록

작업일: 2026-10-01. 기준 씬: `Assets/Scenes/InGame.unity`. Unity 6000.3.10f1 / URP 17.3.0.

## 적용한 구조

- 시작·건너편 대지의 시각 메시를 교체했다. 큰 장지 패널, 완만한 접힘, 닫힌 측면, 봉제선, 다리 앞의 종이 적층 단면을 만들었다. 보행 바닥과 다리 충돌체는 유지했다.
- 천장 패널을 두께가 있는 종이판으로 교체하고 이중 테두리, 굽은 제본 보, 책등 기둥의 연결 받침을 추가했다.
- 좌우 벽의 하단 말림·받침·상부 마감과 문틀 안쪽 단면을 추가했다.
- 출구 뒤 회랑에 낮은 곡면 천장과 반복 아치, 벽의 겹장 마감, 연결 바닥 패널과 안내 실을 추가했다. 기존 1A 연결 터널은 유지하고 이동·가시성을 검증했다.
- 회랑 벽 마감이 카메라를 가리는 문제가 첫 촬영에서 발견됐다. 새 벽의 안쪽 면에 맞춘 충돌체로 수정한 뒤 동일 위치에서 재촬영했다.

재질은 현재 프로젝트의 종이·책표지·실 라이브러리를 사용했다. 미세 섬유와 반사 반응의 재제작은 2번, 과노출·광원 배치는 3번 문서의 작업이다. 이 결과를 최종 재질·조명까지 완성된 화면으로 판단하지 않는다.

## 실측 기준

| 항목 | 실측값 |
|---|---|
| 캐릭터 캡슐 | 높이 1.4u, 지름 0.6u, stepOffset 0.45u, skinWidth 0.02u |
| 정령 렌더 높이 | 기준 프레임 약 1.76u, 애니메이션 포즈에 따라 변동 |
| 이동 | 걷기 3.8u/s, Shift 달리기 5.9u/s, 점프 설정 1.2u |
| 본실 시각 외피 | 폭 약 32u, 깊이 약 36u, 바닥 3u, 천장 약 16u |
| 출구 | 경첩 x = ±5.8u, z = 15.2u; 문 높이 약 14u, 완전 개방 170° |
| 연결 경사로 | 길이 43.2666u, 폭 스케일 5.5u, 경사 5.3046° |
| 실제 카메라 | FOV 60°, near 0.08u, far 250u, 검토 PNG 1920×1080 |

광원·반사판·수광면·다리 피벗과 카메라의 정확한 좌표는 [수정 전](Before/review.txt), [수정 후](After/review.txt)에 기록했다. 신규 구조는 10개 소유 루트, 31개 직렬화 메시, 138,200삼각형이다. 이는 새 구조 부분의 수치이며 씬 전체 수치나 순증가량이 아니다.

## 화면 비교

| 시점 | 이전 | 적용 후 |
|---|---|---|
| 시작 | [보기](Before/01-start.png) | [보기](After/01-start.png) |
| 작업책 | [보기](Before/02-workbook.png) | [보기](After/02-workbook.png) |
| 바닥 | [보기](Before/03-floor.png) | [보기](After/03-floor.png) |
| 천장 | [보기](Before/04-ceiling.png) | [보기](After/04-ceiling.png) |
| 왼쪽 | [보기](Before/05-left.png) | [보기](After/05-left.png) |
| 오른쪽 | [보기](Before/06-right.png) | [보기](After/06-right.png) |
| 뒤쪽 | [보기](Before/07-rear.png) | [보기](After/07-rear.png) |
| 열린 문 | [보기](Before/08-open-door.png) | [보기](After/08-open-door.png) |
| 문 뒤 회랑 | [보기](Before/09-back-corridor.png) | [보기](After/09-back-corridor.png) |
| 회랑 코너 | [보기](Before/10-corner.png) | [보기](After/10-corner.png) |
| 연결 터널 | [보기](Before/11-link.png) | [보기](After/11-link.png) |
| 1A 입구 | [보기](Before/12-1A-entry.png) | [보기](After/12-1A-entry.png) |

PNG는 실제 Play의 PaperCamera에서 렌더한 월드 화면이며 Overlay UI는 포함하지 않는다. 비교 촬영의 문 개방은 검사 코드에서 Solve를 호출했다. 퍼즐이 실제 상호작용으로 풀리는지는 별도의 F0PlayVerifier 결과로 판단한다.

## 검증

- `F0PlayVerifier`: 41개 PASS. F/마우스 입력, 오답 가지, 올바른 반사 경로, 광원 닫기·재개방, 다리 점유 보호, 추락 복귀, 출구 확정, 개방 후 역조작, 다리·문·뒤 회랑 이동.
- `ReleaseStructureTraversal`: 실제 W 걷기 3.96u, Shift 달리기 6.24u(각 입력 1초+감속 0.25초), 점프 상승 1.15u와 착지. 본실 천장 25지점, 연결 터널 57지점의 상하좌우 충돌 표본 검사. 실제 RoomManager를 켠 상태에서 경사로를 걸어 1A에 진입하고 달리기 속도로 왕복했다.
- 구조를 저장·재로드한 뒤 31개 메시의 에셋 연결과 재질을 검사했다. 광학 피벗과 다리 콜라이더 값이 모델링 전후 동일한지도 생성 시 비교했다.
- [통로 이동 기록](Traversal/passage-walk.gif)은 실제 CharacterController를 프레임마다 이동시킨 검사 영상이다. 사람이 직접 조작한 플레이 영상은 아니다. 검사 시작점으로 순간 이동한 직후 카메라 보간 5프레임은 영상에서 제외했다. 960×540, 5fps이며 최종 게임 프레임률을 뜻하지 않는다.

- `FoyerTransitionVerifier`: 문이 열리는 중 이동·점프 잠금, 문 전체의 카메라 프레이밍, 소리 이벤트, 문 완료 즉시 대화 시작, 대화 종료 후 이동 복구, F0/1A 양쪽에서 천장 가시성 유지 PASS. [문 검사 로그](door-checks.txt), [퍼즐 검사 로그](puzzle-checks.txt), [이동 검사 로그](Traversal/checks.txt).
- [적용 파일 해시](applied-sha256.txt): 검증 프로젝트와 실제 프로젝트에 적용한 씬·생성 메시·검사 코드 66개 파일의 SHA-256 일치를 확인했다.
- Windows 64비트 개발 빌드 성공: 오류 0, 경고 8, 4분 35초, 643,084,205바이트. [빌드 결과](Traversal/build-player.txt). 실행 파일은 `Logs/F0ArtValidation/Builds/ReleaseStructure/BookTower.exe`에 있다. 이번 동작 검사는 Unity Editor Play에서 수행했으며 독립 실행 파일의 수동 플레이 검수는 수행하지 않았다.

충돌 광선 통과만으로 모든 시점의 틈이 없다고 보증하지 않는다. 아래에 남은 범위를 명시한다.

## 재생성 및 변경 파일

- `Assets/_BookTower/Editor/ReleaseStructureBuilder.cs`: 열린 InGame에 적용하는 생성 도구. `Book Tower/완성 아트 01 구조 적용` 메뉴. 실행 전에 씬 백업을 `Logs/ReleaseStructureBackups`에 저장한다.
- `ReleaseStructureReview.cs`: 동일 카메라의 Before/After 촬영과 치수 기록.
- `ReleaseStructureTraversal.cs`: 입력·이동·외피·저장 에셋 검사와 Windows 개발 빌드.
- `Assets/_BookTower/Generated/ReleaseStructure/Structure_000.asset`–`Structure_030.asset`: 영속 메시. 기존 재질을 참조한다.
- `Assets/Scenes/InGame.unity`: 위 구조를 적용한 씬.
- `Tools/build_structure_review_gif.py`: Unity 캡처를 검토용 GIF로 묶는 도구.

이전 전체 방 생성기를 다시 실행하면 이번 외피가 덮어써질 수 있다. 전체 씬 생성 이후에는 이 단계의 구조를 다시 적용하고 검증한다. 현재 도구는 기존 퍼즐 참조를 재생성하지 않는다.

## 다음 단계와 한계

- 기존 창문·등불 주변은 밝기가 강하고 재질의 노멀 강도와 스케일이 통일되지 않았다. 이는 2·3번 단계에서 조정한다.
- 출구 뒤에서 오른쪽으로 이어지는 바닥 안내는 유지했다. 목표 소품의 연출과 UI 의존성은 4·5번에서 별도로 검토한다.
- 벽 전체 밀착 이동, 가능한 모든 카메라 피치·Q/E 조합, 저사양 장치 성능까지 전수 검증한 결과는 아니다.
- 독립 시험용 조립 프리팹은 별도로 만들지 않았다. 이미 구축된 F0 한 방을 시험 구역으로 사용하고 생성 도구와 메시를 남겼다.
