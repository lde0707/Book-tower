# 6방향 시트 기반 정령 — 실제 3D 모델

기준: `ArtSource/SpiritTurnaround/spirit-six-views-v1.png`. 기존 종이 정령 소스를 바탕으로 작은 얼굴, 한쪽으로 모인 목도리 끝, 작은 발과 닫힌 긴 망토 형태로 조정했다. 기존 꼬리와 빨간 책갈피는 이 시트에 없으므로 제거했다. 옷과 소매에는 작은 찢김과 짧은 펄프 섬유를 실제 메시로 제작했다. 종이의 얇은 면과 안쪽 따뜻한 색, 겹침 그림자를 포함한다.

모든 이미지는 하나의 Blender 모델에서 카메라만 변경해 렌더링했다. 생성형 이미지를 모델 렌더로 대신 제시하지 않았다. 시트 자체의 서로 다른 시점 간 미세 불일치는 하나의 실제 구조로 정리했다. 시트의 모든 작은 주름과 조각을 일대일 복제한 모델은 아니다.

- `01-hero.png`: 사선 전체
- `02-front.png`: 정면
- `03-left.png`, `04-right.png`: 좌우
- `05-back.png`: 뒷면
- `06-top.png`, `07-bottom.png`: 위·아래
- `08-detail.png`: 얼굴·목도리·소매 확대
- `08-export-verified.png`: 텍스처 베이크 후 GLB 재반입 렌더
- `export-check.json`: 메시 수치, 재반입 크기, 유효성 검사

모델: `ArtSource/TurnaroundSpiritModelV1/PaperHoodSpirit.blend` (편집 가능한 분리 오브젝트), `PaperHoodSpirit.glb` (재질 포함), `PaperHoodSpirit_Baked.fbx` (베이크 모델). PBR 텍스처는 `Textures` 폴더에 있다.

재생성: Blender 5.1에서 `Tools/model_turnaround_spirit.py -- model`, 이어서 `-- bake`. 이 결과는 외형 확인용 정적 모델이다. 새 발에 대한 리깅/보행 애니메이션과 Unity 교체는 포함하지 않는다. 현재 인게임 정령은 그대로 유지한다.
