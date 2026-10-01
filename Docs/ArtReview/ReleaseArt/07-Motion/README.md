# 07 종이와 환경 움직임

`PaperRoomMotion`은 Dormant, Ambient, Reacting, Settling, Paused 상태를 사용한다. 종이 끝만 움직이며 퍼즐의 위치·법선·충돌을 흔들지 않는다. 방을 숨기거나 일시정지하면 로컬 시간이 멈추고, 움직임 완화에서는 자세가 정지 상태로 수렴한다. 낮은 품질에서는 폭을 줄인다.

기존 `FloatingPage`의 래핑 순간이동을 닫힌 완만한 경로로 바꿨다. `StorybookPaperMotion`도 로컬 시간과 가시성·접근성 처리를 사용한다. 기존 먼지 시스템 전체를 재작성한 것은 아니며 `PaperRoomMotion`은 자신의 자식 입자만 제어한다.

[60초 관찰](../08-Final/Motion/sixty-second-observation.gif)은 1fps 기록이다. [빌드 회전 장면](../08-Final/Standalone/F0-clip.gif)은 약 12fps 캡처 묶음이다. 두 자료를 30/60fps 애니메이션 품질 시험으로 확대 해석하지 않는다. 실제 빌드 프레임 시간은 [성능표](../08-Final/performance.md)에 따로 있다.

[일시정지·숨김·움직임 완화 검사](../08-Final/review.txt) · [통합 결과](../08-Final/README.md)
