# 뒤쪽으로 휘어진 모자 끝

V5 모델의 모자 상단만 부드럽게 변형했다. 기존 옆으로 말린 끝을 뒤쪽(+Y)으로 일부 돌리고 상단을 조금 뒤로 기울였다. 높이 1.72m 아래의 모자 챙과 얼굴 안감은 고정했다. 겹쳐진 종이 띠도 같은 변형을 적용했다.

01-hero.png와 03-left.png에서 전체와 옆모습을 확인할 수 있다. 6방향 이미지는 같은 3D 모델 렌더다. 팔·다리 연결, 발목까지 내려오는 옷감, 얼굴 비율은 V5를 유지했다.

원본 및 FBX/GLB: ArtSource/TurnaroundSpiritModelV6. 베이크된 GLB 재반입 결과는 export-check.json에 기록한다. 이후 Unity 적용을 위해 PaperHoodSpirit_Rigged.blend에 23개 관절과 10개 애니메이션을 추가했다. 실제 Unity 적용 및 플레이 검증 기록은 Animation/README.md를 참고한다.

재생성: Blender 5.1에서 Tools/refine_spirit_hat_backcurl.py -- model, 이어서 -- bake.
