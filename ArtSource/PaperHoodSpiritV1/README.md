# 후드 종이 정령 — 실제 3D 모델

기준: `Docs/ArtReview/SpiritRedesignV1/02-page-hood.png`와 `HoodTurnaroundV1`의 상·하·좌·우 이미지.

둥근 얼굴, 호박색 눈, 얇은 후드와 말린 끝, 겹친 종이 망토, 작은 손, 붉은 책갈피, 하나의 말린 꼬리를 실제 메시로 제작했습니다. 종이 면에는 얇은 두께가 있으며 뒷면도 존재합니다. 생성 이미지들의 서로 다른 세부 형태는 하나의 3D 구조로 정리했습니다. 사진의 픽셀 단위 복제는 아닙니다.

## 파일

- `PaperHoodSpirit.blend`: 편집 가능한 부품별 원본, 종이 재질, 스튜디오 카메라·조명. 사용 텍스처 내장.
- `PaperHoodSpirit.fbx`: 부품별 메시 교환 파일. Blender의 절차적 재질은 FBX만으로 동일하게 전달되지 않습니다.
- `PaperHoodSpirit_Baked.blend`: 종이 재질을 2K PBR 텍스처에 베이크한 통합 메시.
- `PaperHoodSpirit_Baked.fbx`: 베이크 메시 교환 파일. Unity에서는 아래 텍스처를 별도 머티리얼에 연결하는 방식이 안전합니다.
- `PaperHoodSpirit.glb`: 베이크한 색상·노멀·거칠기와 메시를 포함한 파일.
- `Textures/Spirit_BaseColor.png`: sRGB 색상.
- `Textures/Spirit_Normal.png`: 선형 tangent-space 노멀.
- `Textures/Spirit_Roughness.png`: 선형 거칠기. Unity Smoothness가 필요하면 `1 - Roughness`로 변환해야 합니다.

Blender 좌표계 Z-up, 전방 -Y. FBX/GLB는 Y-up으로 내보냅니다. 전체 크기와 삼각형 수는 리뷰 폴더의 `export-check.json` 참조.

## 렌더와 검증

`Docs/ArtReview/SpiritRedesignV1/ModelV1`에 실제 메시를 Cycles로 렌더한 3/4, 정면, 좌·우, 후면, 상·하 이미지가 있습니다. `08-export-verified.png`는 GLB를 다시 가져와 렌더한 모습입니다. 이미지 생성 도구로 모델의 완성 사진을 대신 만들지 않았습니다.

## 사용 범위

후속 작업으로 `PaperHoodSpirit_Rigged.blend`에 17본 리깅과 60Hz 애니메이션 9종을 추가했습니다. Unity용 FBX, PBR 재질, Animator와 `HoodSpirit.prefab`은 `Assets/_BookTower/Art/PaperHoodSpirit`에 있습니다. InGame의 기존 정령 시각 오브젝트를 교체했으며 이동 컨트롤러와 충돌 크기는 유지했습니다. 기존 정령 자산은 참조 및 복구용으로 남아 있습니다. 게임용 LOD는 포함하지 않으며 현재 약 12.3만 삼각형입니다.

자세한 동작, 실게임 Play 검증 및 미리보기는 `Docs/ArtReview/SpiritRedesignV1/AnimationV1/README.md`를 참고하세요.

## 재현

Blender 5.1.1에서 `Tools/model_hood_spirit.py`를 background 실행해 원본과 렌더를 만들고, 완료 후 `Tools/bake_hood_spirit.py`를 실행해 PBR 베이크 및 교환 파일 검증을 수행합니다. 기존 프로젝트의 `PaperFiber.png`와 `PaperFiberNormal.png`를 사용합니다.
