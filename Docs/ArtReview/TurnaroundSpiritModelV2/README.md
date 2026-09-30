# 얼굴을 감싸는 종이 안감 추가

6방향 시트 기반 모델 V1에 `Spirit_Hood_inner_face_wrap`과 `Spirit_Hood_inner_turned_edge`를 추가했다. 이마·관자놀이·볼·턱 주변을 감싸는 얇은 타원형 종이 후드이며 아래쪽은 목도리 뒤로 들어간다. 바깥 꼬깔과 별개인 메시다. 얼굴보다 조금 따뜻한 안쪽 종이색과 아주 얕은 접힘을 사용하고, 눈을 가리지 않는 크기로 얼굴 개구부를 유지했다.

모든 시점은 같은 모델의 렌더다. `01-hero.png`는 전체, `08-detail.png`는 확대, `02-front.png`~`07-bottom.png`는 6방향. `08-export-verified.png`와 `export-check.json`은 PBR 베이크 및 GLB 재반입 확인이다.

원본은 `ArtSource/TurnaroundSpiritModelV2/PaperHoodSpirit.blend`, 모델 배포 형식은 같은 폴더의 `PaperHoodSpirit.glb`, `PaperHoodSpirit_Baked.fbx`. 재생성은 Blender 5.1에서 `Tools/add_spirit_inner_hood.py -- model`, `-- bake`.

외형 확인용 정적 모델이며 이번 수정은 Unity 에셋 교체나 새 리깅을 포함하지 않는다. 이전 V1은 보존했다.
