# 정령 옷 수정 V4

레퍼런스의 종이 옷에 비해 V3는 작은 사각 조각 60개와 깊은 홈이 반복되어 비늘/악어가죽처럼 읽혔다. V4는 그 조각을 제거하고, 기존 다섯 망토 패널 위에 넓고 길이가 다른 종이층 10개를 비스듬히 겹쳤다. 일정한 행 배열을 없애고 가장자리 두께를 0.0015로 줄였으며, 들뜬 높이와 찢어진 윤곽을 완화했다.

옷과 소매에는 닫힌 다각형 균열 무늬를 쓰지 않는다. 고운 종이 섬유와 완만한 구김을 사용한다. 얼굴, 모자, 목도리, 기존 몸통과 움직임은 유지한다.

`01-three-quarter.png`와 `04-paper-detail.png`는 실제 모델 렌더다. `08-export-verified.png`는 베이크한 GLB 재반입 결과다. `unity-ingame.png`는 별도 검증 프로젝트의 실제 InGame 씬 실행 캡처다. 테스트 결과는 `import-check.txt`, `play-check.txt`, 적용 동일성 확인은 `apply-check.txt`에 보관한다.

재생성: Blender 5.1에서 `Tools/refine_spirit_garment.py -- model`, `-- bake`, `-- rig`. V1~V3 원본은 보존하고 소스는 `ArtSource/PaperHoodSpiritV4`에 저장한다. Unity 검증은 `SpiritHatReview.RunV4`로 수행한다. 검증한 FBX와 텍스처만 기존 에셋 경로에 복사하며 GUID와 InGame 씬은 유지한다.
