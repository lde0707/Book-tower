# 모아 — 자연스러운 종이새 모델 검토 V2

2026-09-27. 사용자의 “도형을 붙여 만든 것 같다”는 피드백을 반영한 실제 Unity 3D 모델 렌더다. AI 생성 시안이나 사진 리터칭이 아니다.

- 머리·목·가슴·배를 연속된 하나의 메시로 만들었다. 머리와 몸통의 본 가중치를 부드럽게 연결해 움직일 때도 표면이 이어진다.
- 몸에 감기는 곡면 종이 날개 3겹, 들린 끝부분, 왼쪽 날개의 파임과 작은 황토색 흔적을 적용했다.
- 눈을 곡면에 맞춘 잉크 형태로 바꾸고 부리·다리·꼬리의 형태를 정리했다.
- 종이 정령의 실제 섬유·노멀 텍스처를 공유한다. 작은 모델에 맞게 노멀 강도와 색을 조정했다.

## 실제 렌더

[사선](model-three-quarter.png) · [정면](model-front.png) · [좌측](model-left.png) · [우측](model-right.png)

[Idle](idle.gif) · [Run](run.gif) · [날갯짓](flutter.gif)

GIF는 Unity에서 샘플링한 실제 모델 애니메이션을 24fps로 저장한 검토용이다. 원본 Unity 클립은 60Hz 곡선이며 프레임 사이를 보간한다. 고정 스튜디오 조명으로 렌더했으므로 인게임 조명과는 다르다.

## 적용 범위와 검증

이번 단계는 외형을 먼저 보여달라는 요청에 맞춘 모델 검토다. 원본 `Assets/Scenes/InGame.unity`는 변경하지 않았으며 작업 시작 시 백업과 SHA-256이 일치한다. 새 외형의 인게임 통합 검증은 아직 하지 않았다.

`MoaBirdBuilder`와 `MoaBirdSculpt`가 모델을, `MoaBirdAnimations`가 클립을 생성한다. `MoaBirdVerifier.PreviewBatch`로 별도 검증 프로젝트 `Logs/F0ArtValidation`에서 프리팹·재질·메시·애니메이션을 저장하고 렌더했다. [에셋 검사](asset-check.txt)는 유한 좌표, 본 가중치, 클립 경로와 루프 연결을 확인한다. 현재 생성된 후보 프리팹은 검증 프로젝트의 `Assets/_BookTower/Art/PaperMoa/PaperMoa.prefab`에 있다.
