# 후드 종이 정령 — 리깅·애니메이션·Unity 적용

## 현재 적용

InGame 씬의 BookmarkBird 아래 기존 정령 시각 오브젝트를 `HoodSpirit.prefab`으로 교체했습니다. 씬의 기존 문서 중 바뀐 것은 플레이어 Transform의 자식 참조 하나이며, 위치·회전·스케일과 모든 비시각 컴포넌트는 그대로입니다. 교체 직전 파일은 `Logs/HoodSpiritBackup/original-before-hood.unity`에 보관했습니다.

에디터가 기존 씬을 메모리에 연 채 작업하는 경우를 위해 `Temp/HoodSpiritApply.request`를 사용합니다. 새 C# 파일 임포트가 끝나면 저장된 씬뿐 아니라 열려 있는 InGame에도 같은 교체를 적용합니다. 실행 중이면 Edit 모드까지 기다리며, 적용 직전 열려 있던 씬을 별도 파일로 백업합니다. 결과는 `Logs/hood-unity-apply.txt`에 기록합니다.

## 동작

- Idle: 4초 부유 호흡, 눈 깜빡임, 후드와 망토의 작은 후행 흔들림.
- Move: 0.9초 루프. 속도에 따라 재생 속도가 부드럽게 바뀌며 앞으로 기울고 팔·망토·꼬리가 엇박자로 따라갑니다.
- JumpRise / Fall / Glide / Land: 실제 PlayerController 상태와 수직 속도를 따릅니다. 이동 중 착지하면 이동으로 바로 이어집니다.
- LookAround: 4.8초, 고개를 좌우로 돌려 주변을 살핍니다.
- TidyBookmark: 3.8초, 고개를 숙이고 책갈피 쪽으로 양손을 모읍니다.
- Stretch: 4.5초, 팔을 올리며 살짝 떠오르는 기지개.

대기 행동은 입력 없이 8~15초 기다리면 시작하며 같은 행동이 연속되지 않습니다. 이동·점프·상호작용으로 중단할 수 있습니다. 0.09~0.35초 크로스페이드, 이동 속도 댐핑, 회전 시 작은 기울기를 사용합니다. 60Hz로 저작했으며 실제 재생은 Unity 곡선 보간을 사용합니다. 일시정지는 게임 시간과 함께 정지합니다. 대화와 컷씬 중에는 특별 대기 행동을 시작하지 않습니다.

## 파일

- `ArtSource/PaperHoodSpiritV1/PaperHoodSpirit_Rigged.blend`: 편집 가능한 17본 스켈레톤·스킨·9개 Action.
- `Assets/_BookTower/Art/PaperHoodSpirit`: Rigged FBX, 2K PBR 텍스처, URP 머티리얼, Animator Controller, prefab.
- `Assets/_BookTower/Scripts/Presentation/HoodSpiritAnimator.cs`: 시각 전용 상태 전환. Root motion 없이 기존 물리 이동을 따릅니다.
- `hood-spirit-animation.mp4`: Blender에서 실제 리깅한 모델을 렌더한 18.63초 동작 시연. 클립 사이에는 시연용 컷 전환이 있습니다. Unity의 상태 크로스페이드는 별도 구현됩니다.
- `unity-ingame.png`: 검증 프로젝트의 실제 InGame Play 모드에서 촬영한 가까운 시점.
- `unity-Idle/Move/TidyBookmark/Stretch.png`: Unity에서 스킨·클립·머티리얼을 샘플링한 이미지.

## 검증

`rig-check.json`: 원본과 베이크 메시 간 스킨 매칭 오차, 뼈 수, 클립 명세.

`unity-check.txt`: 17개 뼈, 정규화된 스킨 가중치, 9개 클립, 대기 행동 시작과 이동 취소, 점프/활공/착지 상태 전환, 유한한 변형 좌표, root motion 없음.

`play-check.txt`: 실제 InGame Play 모드에서 PlayerController 상태를 주입하고 시각 FSM이 따라가는지 확인했습니다. 이 테스트에서는 이동 경로의 영향을 없애려고 PlayerController.Update를 일시 비활성화했습니다. 물리 점프 거리나 방 퍼즐을 다시 검증하는 테스트는 아닙니다. 일시정지와 충돌 크기도 확인했습니다.

`scene-change-check.json`: 기존 씬의 비시각 컴포넌트와 플레이어 Transform 보존 확인.

기존 게임은 열어 둔 채 별도 `Logs/F0ArtValidation` 프로젝트에서 Unity 임포트·빌드·Play 검증 후 원본에 결과를 반영했습니다. LOD 제작이나 저사양 성능 측정은 이번 범위에 포함하지 않았습니다.
