# 모아 Flight V1 — 정령 옆에서 귀엽게 날아다니기

2026-09-27. Sculpt V3 모델에 만든 실제 3D 애니메이션이다. 게임의 기존 `PaperSpirit/paper-spirit.obj`를 옆에 놓고 비율과 동행 동선을 검토했다.

## 동작

- 공중 대기: 작은 원을 그리며 위아래로 움직이고 약 초당 3회 날갯짓한다.
- 따라가기: 몸을 앞으로 기울이고 선회 방향으로 약하게 기울어진다. 머리는 별도 본으로 부드럽게 움직인다.
- 짧은 활공: 날개를 펼친 자세에서 날갯짓 폭을 줄였다가 다시 펄럭인다.
- 세부 반응: 발을 배 쪽으로 접고, 꼬리로 방향을 보정하며, 날개 끝 종이는 날갯짓보다 늦게 휘어진다.
- 위치는 정령 옆의 작은 범위 안에서 변한다. 8초 동안의 동작이 처음으로 이어지는 루프다.

## 결과

[동영상 MP4](moa-flight.mp4) · [반복 GIF](moa-flight.gif) · [공중 대기](hover.png) · [날개 올리기](wing-up.png) · [따라가기](follow.png) · [활공](glide.png)

원본은 `ArtSource/PaperMoaV3/Moa_FlightV1.blend`, 교환용 애니메이션은 `ArtSource/PaperMoaV3/Moa_FlightV1.fbx`다. 60Hz로 키를 만들고 Blender의 프레임 사이 보간을 사용했다. 검토 영상은 30fps, GIF는 24fps다. 영상은 EEVEE로 실제 3D 프레임을 렌더했고 정지 이미지는 Cycles 렌더다. 영상의 재생 속도는 실제 동작과 같다.

## 적용 범위

현재는 게임에 넣기 전 **동작 검토용 원본과 영상**이다. 원본 InGame 씬과 2D 스토리 컷신은 변경하지 않았다. 정령의 이동과 새의 동행 경로는 검토용으로 구성한 것이며 실제 게임 입력이나 벽 회피를 실행한 영상은 아니다.

FBX에서는 정령과 검토용 동행 경로를 제외하고 새의 로컬 비행 동작을 내보냈다. Blender 원본에는 부드러운 목 변형, 날개·발·꼬리 관절, 날개 끝 변형과 검토용 정령을 포함한다. Unity용 최적화·재질 변환·상태별 클립 정리·충돌을 고려한 추종 제어 연결은 후속 적용 단계다.

재현: `Tools/animate_moa_flight.py` → `Tools/render_moa_flight.py` → `Tools/encode_moa_flight.py`.

검사: [동작 설정](animation-check.json), [루프와 렌더](render-check.json), [영상 길이와 fps](video-check.json).
