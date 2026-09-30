# 종이 안감의 겹과 가로 타원 얼굴

V2의 단일 안감 가장자리를 세 겹의 얇은 종이 가장자리로 변경했다. 각 겹에는 서로 다른 미세한 요철과 작은 찢김, 짧은 종이 섬유를 추가했다. 큰 모자와 목도리의 형태는 유지했다.

얼굴의 가로 반경은 0.30537m, 세로 반경은 0.28891m로, 세로가 가로보다 약 5.4% 짧다. 눈의 높이와 안감의 세로 곡면도 함께 조정했다.

`01-hero.png`, `02-front.png`~`07-bottom.png`, `08-detail.png`는 동일한 실제 3D 모델의 렌더다. 베이크된 GLB를 재반입하여 크기와 메시 유효성을 확인했으며 결과는 `export-check.json`에 기록했다. 2K PBR 텍스처를 포함한 FBX와 GLB는 ArtSource/TurnaroundSpiritModelV3에 있다.

외형 검토용 정적 모델이다. 이번 버전은 Unity에 교체하지 않았고 리깅도 포함하지 않는다. 이전 버전은 보존했다.

재생성: Blender 5.1에서 `Tools/refine_spirit_face_lining.py -- model`, 이어서 `-- bake`.
