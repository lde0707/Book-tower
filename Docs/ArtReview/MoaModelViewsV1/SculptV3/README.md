# 모아 Sculpt V3 — 새의 형태를 먼저 만든 모델

후속 비행 애니메이션: [Flight V1 — 정령 옆 동행 동작과 영상](../FlightV1/README.md).

2026-09-27. “귀여운 퀄리티 좋은 새를 모델링하고 질감과 날개 끝에 종이 표현을 더하자”는 피드백을 반영했다. 이 폴더의 이미지는 Blender 5.1.1 / Cycles에서 **실제 3D 모델을 렌더한 것**이다. AI 생성 이미지나 기존 이미지의 리터칭이 아니다.

## 변경

- 통통한 새의 몸통과 머리를 조형하고 표면을 합친 뒤, 목·가슴·볼의 접합부를 다듬었다.
- 작은 부리, 짧은 다리, 곡선 발가락, 얼굴 표면에 들어가는 검갈색 눈과 반사광을 만들었다.
- 날개 뿌리를 몸에 묻히게 다듬었다. 끝으로 갈수록 네 겹의 얇은 종잇장, 불규칙한 가장자리와 소량의 미세 섬유가 드러난다.
- 정령의 `PaperFiber.png`를 기반으로 작은 종이 결과 무광 재질을 적용했다. 표면 전체를 큰 구김이나 접힌 판으로 덮지 않았다.

## 보기

[사선](three-quarter.png) · [정면](front.png) · [측면](left.png) · [날개를 펼친 포즈](flutter.png)

원본: `ArtSource/PaperMoaV3/Moa_SculptV3.blend`  
교환용 모델: `ArtSource/PaperMoaV3/Moa_SculptV3.fbx`  
생성 스크립트: `Tools/model_moa_v3.py`  
원본 재열기 검사: `Tools/verify_moa_v3.py`, [결과](source-check.json)

## 현재 단계

외형을 먼저 보여달라는 요청에 맞춘 **모델 검토 단계**다. 원본 `Assets/Scenes/InGame.unity`는 변경하지 않았다. 이 모델이 NaturalV2보다 최신 아트 후보이며, 이전 Unity 절차 생성 프리팹과는 별도 원본이다.

Blender 파일에는 종이 재질·텍스처·스튜디오·카메라와 간단한 숨쉬기/날갯짓 검토 동작이 들어 있다. 고밀도 조형 원본이므로 인게임용 메시 최적화, 재질 베이크, 정식 Idle/Run/Flutter 리깅과 Unity 연결은 후속 작업이다. FBX는 현재 애니메이션을 제외한 모델 교환용이다. Blender의 절차 재질과 Cycles 조명이 Unity에서 자동으로 똑같이 재현되는 상태는 아니다.
