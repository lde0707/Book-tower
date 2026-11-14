# 접힌 빛 — A Fold of Light 실행 안내

<!-- WINDOWS-RELEASE-START -->
## Windows에서 바로 플레이

**[접힌 빛 — A Fold of Light · Windows 실행 파일 다운로드](https://github.com/lde0707/Book-tower/releases/tag/portfolio-windows-2026-11-14)**

종이 세계의 빛·그림자와 접힘을 활용하는 3D 퍼즐 탐험 게임.

1. 위 Releases 페이지의 `AFoldOfLight-Windows-x64.zip`을 내려받습니다. `Source code` 파일은 실행 파일이 아닙니다.
2. ZIP 전체를 한 폴더에 압축 해제합니다.
3. `AFoldOfLight.exe`를 실행합니다. Unity 설치는 필요 없습니다.

`AFoldOfLight_Data`, `UnityPlayer.dll`, 나머지 동봉 폴더를 함께 유지해야 합니다. Windows 64비트용이며 모바일·macOS에서는 실행되지 않습니다.

**조작:** WASD 이동 / Space 점프·활공 / 마우스 조준. 상호작용은 화면 안내를 따르세요.

[실행·검증 범위](PLAY.md) · [QA 포트폴리오](https://leedoeon.vercel.app/qa/index.html)
<!-- WINDOWS-RELEASE-END -->


## 배포 버전과 확인 범위

- 패키지: `portfolio-windows-2026-11-14` / Windows x64
- 빌드 날짜: 2026-11-14
- Unity: 6000.3.10f1
- 소스: 이 릴리스 태그가 가리키는 커밋
- 빌드된 실행 파일에서 타이틀·설정·종료 확인·게임 진입 자동 검사를 통과했습니다. 전체 퍼즐 완주 검사는 포함하지 않습니다.
- 게임의 기존 사용자 저장 파일을 삭제하거나 초기화하지 않습니다.

## 소스에서 열기

Unity Hub에서 `.` 폴더를 엽니다. `ProjectSettings/ProjectVersion.txt`와 같은 버전의 에디터 및 Windows Build Support가 필요합니다.
현재 Build Settings에 등록된 씬으로 Windows x64를 빌드합니다. `Library`, `Temp`, `Logs`, `Builds`는 재생성 파일이므로 Git에서 제외합니다.

## 문제가 있을 때

ZIP 전체를 다시 압축 해제하고 EXE와 Data 폴더가 같은 위치인지 확인하세요. 실행 오류를 보고할 때 OS·발생 화면·재현 순서를 함께 남겨주세요.
게임의 조작·기능 범위는 실제 빌드와 최신 코드가 기준이며, 아래 기획·과거 개발 문서에는 아직 구현되지 않은 내용이 포함될 수 있습니다.
