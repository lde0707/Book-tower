# 모자 챙 수정 · 조용한 효과음

2026-09-28

## 정령

기준: `02-page-hood.png`의 둥근 얼굴과 종이 후드. 이전 모델의 별도 `Spirit_Hood_curled_page_tip` 조각을 제거하고, 얼굴을 감싸는 챙부터 뒤쪽 뾰족한 끝까지 연결된 후드 메시로 교체했다. 앞이마의 중심을 조금 돌출시키고, 비대칭 접힘과 얕은 구김을 실제 버텍스에 추가했다. 종이 두께와 안쪽 면을 유지했다. 1차 측면 렌더에서 얼굴 뒤 노출을 발견해 후드 아래 곡면을 넓혔다.

몸통, 눈, 망토와 책갈피의 디자인은 유지한다. 뒤쪽 끝은 기존 HoodTip 본으로 부드럽게 가중치를 분배하고, 17개 본·9개 60 Hz 애니메이션을 다시 내보냈다. PBR 텍스처 2048px를 새 UV에 다시 베이크했다.

실제 메시 렌더: `ModelV1/01-three-quarter.png`, `ModelV1/04-right.png`, `ModelV1/08-export-verified.png`. 생성형 컨셉 이미지가 아닌 Blender 모델의 렌더 결과다. Unity 확인 이미지는 `AnimationV1/unity-Idle.png`와 `AnimationV1/unity-ingame.png`.

## 효과음

Kenney Interface Sounds / RPG Audio의 CC0 원본에서 13개 조용한 WAV를 편집했다. 소스, 라이선스, 처리 방법과 이벤트 매핑은 `ArtSource/Audio/Kenney/README.md` 및 `quiet-edits.json` 참조.

버튼/체크박스 클릭과 ESC 메뉴 전환, F0의 종이 장치, 장식 상호작용, 시작 이야기의 종이 소리, 수집/성공/문/발걸음에 연결한다. 반복 클릭은 한 음성만 재생하며, 효과음 음소거 시 첫 프레임에 소리가 새는 문제도 수정했다. 배경음악은 변경하지 않았다.

Unity 적용 및 확인: `QuietHoodIntegration.Run`, `HoodSpiritPlayVerifier.Run`. 확인 결과는 `Logs/quiet-hood-integration.txt`, `Logs/quiet-audio-play-check.txt`, `AnimationV1/play-check.txt`에서 확인한다.
