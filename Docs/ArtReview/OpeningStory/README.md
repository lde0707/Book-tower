> 이 폴더는 이전 3D 스토리 연출의 검증 기록이다. 현재 적용된 2D 컷신과 입력 검증은 [Cutscene2D](../Cutscene2D/README.md)를 참고한다.

# F0–1A 스토리 Play 검증

- `01-first-meeting.png`: 첫 만남. 눌린 종이 정령과 빛을 나누는 종이새.
- `02-promise.png`: 깨어난 정령의 동행 약속.
- `03-workbook.png`: 작업책 읽기와 기록장 보관.
- `04-returning-place.png`: M01 제본사의 수선 기억.
- `05-entering-picturebook.png`: 1A 진입 대사.
- `06-moa-name.png`: M02 이름을 되찾는 순간.
- `07-playing-with-companion.png`: 건너뛰기 뒤 정상 플레이와 동행.
- `play-check.txt`: UI 상태, 입력 차단, Esc/설정 복귀, 키 재지정, 읽기 중 연출 대기, 보상, 체크포인트/재방문, 실제 1A 완료, 도입 건너뛰기를 검증한 결과.

기본 상호작용 키는 F다. 중간 스크린샷의 R은 키 재지정 테스트 값이다. 캡처는 UI 글씨 검토를 위해 게임 후처리를 끄고 1600×900으로 렌더링했다. 원본 게임 조명과 카메라를 사용하지만 Game View의 색보정과 완전히 동일한 화면이라고 보장하지 않는다.

F0 완료 이벤트는 스토리 테스트에서 직접 발생시킨다. 실제 광학 경로와 이동은 `../InteriorRoomsV3/F0-play-verification.txt`에서 별도로 검사한다. 1A 스토리 검사는 실제 세 접이면 조합으로 완료하며 전체 이동 회귀는 `../Storybook1A/play-check.txt`를 따른다.

전체 적용/제약 설명: [실행 흐름](../../21_F0_1A_스토리_실행흐름.md).
