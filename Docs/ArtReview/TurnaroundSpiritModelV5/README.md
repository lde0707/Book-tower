# 팔·다리 연결과 긴 종이 옷자락

V4를 기준으로 발과 다리를 한 표면으로 연결하고, 소매 안쪽에서 손까지 이어지는 팔과 손목을 추가했다. 각 팔다리는 voxel union과 smoothing 후 하나의 연결된 메시인지 검사했다. 검사 결과는 limb-check.json에 기록했다.

하단 종이 겉옷과 부착된 겹·섬유를 함께 늘렸다. 해진 겉옷 사이로 다리가 보이지 않게 안쪽에 얇고 주름진 종이 옷감을 발목까지 추가했다. 바닥과 옷자락 사이 간격은 약 6~8cm이며 작은 발끝이 보인다. 얼굴, 모자, 안감의 간격은 V4를 유지했다.

01-hero.png 및 02-front.png~07-bottom.png는 같은 모델의 실제 렌더다. 원본 및 베이크된 FBX/GLB는 ArtSource/TurnaroundSpiritModelV5에 저장한다. 내보낸 GLB의 재반입 검사 결과는 export-check.json에 기록한다.

외형 검토용 정적 모델이다. 리깅과 Unity 교체는 포함하지 않는다. 이전 V4는 보존했다.

재생성: Blender 5.1에서 Tools/refine_spirit_connected_limbs.py -- model, 이어서 -- bake.
