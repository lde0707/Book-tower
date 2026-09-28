# 책등 기둥 인게임 적용

F0의 입체 장식 루트에 있는 둥근 기둥 네 개를 정면·측면 생성 레퍼런스를 바탕으로 다시 제작했다. 높이 12.4m, 몸통 반지름 1.2m. 회전체 메시로 층진 받침, 볼록한 제본 띠, 상단 마감을 연결했다. 황동 띠, 리벳, 곡면에 맞춘 어두운 꽃 상감을 추가했다. 상감은 실제 관통 구멍이 아니다.

전용 가죽 알베도, 노멀, 금속도/매끄러움 맵은 `Tools/make_pillar_material.py`로 재현 가능하다. 사진을 투영한 것이 아니라 가죽 균열과 금박 식물 문양을 절차적으로 재구성한 재질이다. 생성 이미지와 동일한 미세 주름이나 마모를 복원한 것은 아니다.

`player-overview.png`와 `player-pillar.png`는 Play Mode에서 실제 `Camera.main` 및 `PaperCamera`로 촬영했다. 후자는 플레이어를 기둥 근처로 이동시키고 위쪽을 바라보게 한 화면이다. 별도의 스튜디오 조명을 추가하지 않아 현재 게임 조명의 어두운 면도 그대로 나타난다.

편집기 구현: `Assets/_BookTower/Editor/BookSpinePillarBuilder.cs`.
적용 씬: `Assets/Scenes/InGame.unity`.
재생성: `Book Tower/F0 레퍼런스 벽면 디테일 적용` 메뉴.
기능 검증 결과: `../InteriorRoomsV3/F0-play-verification.txt`.

메시 에셋 재생성 시 이전 렌더 데이터가 남던 문제를 수정하여 정점·노멀·UV·인덱스를 명시적으로 갱신한다.
