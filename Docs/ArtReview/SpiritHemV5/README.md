# 종이 옷의 살짝 해진 끝 — V5

V4의 넓고 비스듬한 종이층을 유지한다. 자유 가장자리에 서로 다른 폭의 작은 찢김을 넣고, 각 종이층 끝에 짧은 납작한 펄프 섬유를 드문드문 추가했다. 원래 망토 패널의 끝도 약하게 닳도록 조형했다. 반복적인 톱니/술 장식처럼 보이지 않도록 길이와 간격을 달리했다. 모자·목도리·얼굴은 유지한다.

`01-three-quarter.png`, `04-paper-detail.png`: 실제 메시 렌더. `08-export-verified.png`: 베이크 GLB 재반입 확인. `unity-ingame.png`: Unity 실제 InGame 씬 실행 확인. `import-check.txt`, `play-check.txt`, `apply-check.txt`: 애니메이션/참조/적용 검증.

Blender 5.1에서 `Tools/refine_spirit_hems.py -- model`, `-- bake`, `-- rig` 순으로 재생성한다. 소스는 `ArtSource/PaperHoodSpiritV5`에 보존. Unity 검증 메서드는 `SpiritHatReview.RunV5`. 검증한 FBX와 텍스처만 원래 경로에 반영하며 기존 메타 GUID와 씬은 그대로 유지한다.
