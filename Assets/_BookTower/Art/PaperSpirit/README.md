# Paper Spirit — reference-based 3D model

첨부된 정령 이미지를 보고 재구성한 3D 메시입니다. 한 장의 이미지에 보이지 않는 옆면·뒷면은 형태에 맞춰 설계했습니다.

- 9개 메시 파트 / 10,013 정점 / 19,368 삼각형
- 높이 약 1.4m, Y-up, Unity에서 +Z가 얼굴 방향
- UV / 섬유 베이스컬러 / 노멀맵 / 불투명 URP Lit 재질
- 둥근 머리, 잉크 눈, 말린 몸통, 나선형 겹침 종이, 목 리본과 양쪽 꼬리, 뒤 매듭
- Unity: PaperSpirit.prefab. InGame 플레이어에 적용됨.
- OBJ: paper-spirit.obj와 .mtl 및 PaperFiber.png를 같은 폴더에 두고 임포트하세요.
- 노멀맵 PaperFiberNormal.png는 사용하는 3D 도구에서 수동 연결하세요.
- OBJ는 정적 메시입니다. Unity 프리팹에서는 PaperSpiritAnimator와 Animator가 60fps 기준 Idle 72프레임, Move 48프레임, Jump 44프레임 클립을 연속 보간해 재생합니다. 스킨/본 애니메이션 클립은 포함하지 않습니다.

생성 원본: Tools/make_paper_spirit.py
Unity 빌더: Assets/_BookTower/Editor/ReferenceSpiritBuilder.cs
Unity 메뉴: Book Tower > 참조 이미지 종이 정령 적용
이 메뉴는 InGame의 정령 모델만 교체합니다. 전체 맵을 다시 만들지 않습니다.

검증: Unity 6000.3.10f1 컴파일·씬 저장, 앞/옆/뒤 편집기 렌더, 정점 유효성·삼각형 인덱스·UV 개수 검사.
전체 게임 수동 플레이 테스트는 수행하지 않았습니다.
