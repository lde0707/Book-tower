# 종이 정령 애니메이션

| 클립 | 고유 포즈 | 길이 | 재생 |
|---|---:|---:|---|
| Idle | 6 | 1.20초 | 반복 |
| Move | 8 | 0.80초 | 반복 |
| Jump | 6 | 0.72초 | 1회, 착지 시 지상 상태로 전환 |

프레임마다 포즈를 유지하는 Constant 곡선입니다. 마지막 경계 키는 첫 포즈(루프) 또는 마지막 포즈(점프)를 복제하므로 새 포즈에 포함하지 않습니다.
클립 전환만 0.06초로 부드럽게 이어지며, 점프 진입은 즉시입니다.

클립은 모델 내부 Motion과 리본 Transform만 바꿉니다. 루트 모션은 꺼져 있습니다.
점프 클립은 몸의 움츠림/펴짐 포즈이며, 실제 포물선 이동은 기존 PlayerController 물리를 사용합니다.
미리보기 GIF의 점프는 제자리 포즈이며 보기 편하도록 반복하지만 게임의 Jump 클립은 반복하지 않습니다.

Animator Moving/Jumping 파라미터를 PaperSpiritAnimator가 실제 수평 속도 및 상승으로 시작한 점프 상태에 연결합니다.
기존 GhostRig는 새 정령 프리팹에서 제거해 부유·리본 애니메이션이 덮어써지지 않도록 했습니다.
이동속도·점프높이·콜라이더·퍼즐 데이터는 변경하지 않았습니다.

Unity 클립: Assets/_BookTower/Art/PaperSpirit/Animation
생성 코드: Assets/_BookTower/Editor/SpiritAnimationBuilder.cs
재생성: Book Tower > 참조 이미지 종이 정령 적용

검증: Unity 컴파일 및 실제 Animator Idle→Move→Jump→Move→Idle 전환,
모든 곡선의 6/8/6 포즈 수, 게임플레이 루트 위치 불변, 20프레임 렌더 확인.
전체 게임 수동 플레이 검증은 하지 않았습니다.
