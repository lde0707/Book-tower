# 안감 간격과 턱 뒤쪽 흐름 조정

V3의 세 겹 종이 안감 가장자리 간격을 곡면 매개변수 기준 0.046에서 0.115로 넓혔다. 각 종이 띠의 폭도 0.105에서 0.18로 늘려 겹 사이가 벌어져도 안쪽에 구멍이 생기지 않게 했다.

아래 볼에서 턱으로 내려갈수록 안감 개구부가 뒤쪽으로 감겨 들어간다. 가장 안쪽 후드의 경계도 함께 옮겨 기존 선이 턱 아래에 남지 않도록 했다. 얼굴의 가로 타원 비율, 해진 종이 끝과 미세 섬유는 유지했다.

01-hero.png, 02-front.png~07-bottom.png, 08-detail.png는 동일한 실제 3D 모델 렌더다. export-check.json은 베이크된 GLB 재반입 검사 결과다.

원본과 FBX, GLB: ArtSource/TurnaroundSpiritModelV4. 외형 검토용 정적 모델이며 Unity 교체 및 리깅은 포함하지 않는다. V3는 보존했다.

재생성: Blender 5.1에서 Tools/refine_spirit_cowl_spacing.py -- model, 이어서 -- bake.
