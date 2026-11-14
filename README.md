# 접힌 빛 — A Fold of Light

<!-- WINDOWS-RELEASE-START -->
## Windows에서 바로 플레이

**[접힌 빛 — A Fold of Light · Windows 실행 파일 다운로드](https://github.com/lde0707/portfolio-game-builds/releases/tag/afoldoflight-2026-11-14)**

종이 세계의 빛·그림자와 접힘을 활용하는 3D 퍼즐 탐험 게임.

1. 위 Releases 페이지의 `AFoldOfLight-Windows-x64.zip`을 내려받습니다. `Source code` 파일은 실행 파일이 아닙니다.
2. ZIP 전체를 한 폴더에 압축 해제합니다.
3. `AFoldOfLight.exe`를 실행합니다. Unity 설치는 필요 없습니다.

`AFoldOfLight_Data`, `UnityPlayer.dll`, 나머지 동봉 폴더를 함께 유지해야 합니다. Windows 64비트용이며 모바일·macOS에서는 실행되지 않습니다.

**조작:** WASD 이동 / Space 점프·활공 / 마우스 조준. 상호작용은 화면 안내를 따르세요.

[실행·검증 범위](PLAY.md) · [QA 포트폴리오](https://leedoeon.vercel.app/qa/index.html)
<!-- WINDOWS-RELEASE-END -->

작은 종이 정령이 거대한 책 속 실내를 탐색하며 접힘·그림자·기억을 복원하는 3D 퍼즐 게임.

- 최신 기획: [종이 백룸 통합 설계](Docs/00_종이백룸_통합설계.md)
- 실제 적용 범위: [구현 현황](Docs/08_구현현황.md)
- 아트: [아트 컨셉](Docs/02_아트컨셉.md)
- 조작: [플레이어 스펙](Docs/04_플레이어스펙.md)
- 현재 F0·1A 공략: [플레이 확인 방법](Docs/12_프로토타입_공략.md)
- 정리 기록: [사용 종료 콘텐츠](Docs/Maintenance/2026-09-26-cleanup.md)

Unity 6000.3.10f1 / URP. 시작 씬은 Assets/Scenes/InGame.unity.
마우스로 중앙 조준, WASD 이동, F/좌클릭 상호작용, Q/E 월드 축 관찰 고정, 이동 시 해제.

F0·1A의 신규 배경과 통로가 적용되어 있다. 나머지 방과 새 문장 수집·회상·엔딩은 통합 설계의 목표이며 전부 구현된 상태는 아니다.

전체 배치의 기반은 Tools/level_layout.py다. 현재 씬에는 별도 F0·1A 모델링 패스가 추가되므로 전체 재생성 뒤 각 패스를 적용하고 검증한다. 과거 ArtReview 문서는 당시의 기록이며 최신 설계보다 우선하지 않는다.

[2026-11-14 실제 플레이·수정 재검증](PLAYTEST.md)
