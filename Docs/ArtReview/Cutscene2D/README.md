# 2D 종이 컷신 캐릭터 · v1

2026-09-27. 내장 image_gen 도구로 제작한 투명 배경 캐릭터 원화.

## 결과

- [정령](../../../Assets/_BookTower/Art/Cutscenes/Characters/Spirit_Paper2D_v1.png): 둥근 머리, 두 개의 먹색 눈, 붉은 리본, 몸통을 감싸며 꼬리로 이어지는 종이 겹.
- [모아](../../../Assets/_BookTower/Art/Cutscenes/Characters/Moa_Paper2D_v1.png): 같은 종이 콜라주 질감, 접힌 날개와 꼬리, 날개 끝 초승달 모양의 찢어진 자국.
- 배경이 분리된 단일 PNG 원화 각각 1장. 관절/종이 겹이 분리된 애니메이션 레이어 파일은 아님.
- 후속 구현에서 F0~1A의 실제 2D 컷신으로 연결했다. `OpeningCutsceneArt.asset`이 캐릭터 2장과 배경 3장을 참조한다. 실행 흐름은 [21번 문서](../../21_F0_1A_스토리_실행흐름.md), 검증은 [Play 결과](Play/play-check.txt)를 참고.
- 컷신에서 모아를 정령 전체 높이의 약 40~45%로 배치하는 것을 권장. 개별 캔버스의 크기는 실제 등장 크기를 뜻하지 않음.

## 생성 방식

내장 image_gen, transparent_background=true. 정령은 기존 원본 이미지에서 캐릭터 정체성을 참조하고, 모아는 이번 정령 결과에서 색과 재질을 참조함. CLI/API 대체 경로는 사용하지 않음. 이미지에 후처리나 배경 제거를 하지 않고 생성된 알파 채널을 그대로 보존함.

## 최종 프롬프트 — 정령

```text
Use case: illustration-story.
Asset: one finished transparent-background 2D character illustration for a quiet, mysterious illustrated game cutscene, The Forgotten Title.
Input image: identity reference only. Translate this existing rounded ivory ghost spirit into a genuinely FLAT 2D hand-painted cut-paper storybook illustration, not a rendering of the reference sculpture.
Subject: one full-body tiny friendly floating paper spirit, oversized nearly circular head, just two soft dark-brown vertical oval ink eyes, no mouth, no nose, no eyebrows, no hair. Head about 45 percent of total height. Rounded pear-shaped torso, wrapped in three broad overlapping ivory paper sheets that taper into a graceful single curled ghost tail. No arms, no legs. A muted terracotta-red paper scarf ties at the neck, with two gently drifting ribbon ends. Keep the instantly recognizable simple round silhouette of the reference. Gentle curious pose, head subtly turned toward the right, little upward tilt as if noticing a companion.
Style: sumptuous yet restrained 2D illustrated picture-book collage. Broad FLAT cream color shapes, dry gouache washes, subtle hand-drawn sepia crease lines, visible fine mulberry-paper fibers and small torn-edge flecks, uneven deckled paper edges. The wrapped torso layers have readable overlapping curved edges. Very limited warm ochre shadow shapes at paper overlaps, no smoothly rendered 3D shading, no volumetric sphere lighting. Fine texture must be visible without making the character dirty or cracked.
Palette: warm ivory, pale oatmeal, light muted ochre, dark coffee-brown ink eyes and terracotta scarf. Tender mysterious personality, elegant minimal face, cohesive handmade paper aesthetic.
Composition: single character only, fully visible, centered with clear margin around all extremities; large enough to read paper grain. Square canvas. True transparent background with alpha, no floor, no ground shadow, no decorative border, no typography, no watermark, no swatches, no extra character or prop, no checkerboard baked into image. Avoid 3D render, photography, stop-motion puppet, photorealistic paper sculpture, glossy eyes, anime eyes, hair, crown, human hands, humanoid legs or ornate clothing.
```

## 최종 프롬프트 — 모아

```text
Use case: illustration-story.
Asset: one finished transparent-background 2D companion-bird character illustration for The Forgotten Title, matching the supplied spirit's paper collage illustration style.
Input image: STYLE AND MATERIAL reference only. Draw ONLY a bird, do not include the spirit or scarf.
Subject: Moa, a tiny tender little bird made from folded ivory mulberry paper. A friendly rounded head and plump teardrop body built from broad paper shapes, one small dark coffee-brown dot eye visible in three-quarter profile facing left and slightly upward, a short muted ochre triangular folded beak. Two short ochre paper legs with tiny simple folded feet, a short fanlike folded paper tail extending right. A near wing formed from two or three broad folded petal-like paper layers, resting loosely with its tip lifted a little; distant wing only peeks behind. The near wing contains ONE clearly visible small crescent-shaped tear near its lower outer edge, a recognizable story detail, an actual crescent hole through the paper revealing transparent background with a fine ochre torn-paper edge. Keep the hole away from the body silhouette so it reads clearly. A few quiet warm-gold painted flecks around that wing edge suggest fading light, very restrained, no surrounding effects cloud.
Style: beautiful flat 2D storybook collage illustration with dry gouache color and handmade paper fibers, gentle uneven cut/torn edges, subtle crease lines, warm cream/oatmeal surfaces, broad simple flat value shapes and just small overlap shadows like the supplied illustration. A drawn character asset, not an origami sculpture product photo. The bird should feel kind, curious and slightly fragile, with clear readable silhouettes. Rounded overall shape with folded-paper wing planes; recognizable small bird, not a crane or realistic feathered sparrow.
Composition: single full-body bird centered, body occupies about 75 percent of square canvas width, generous transparent margins including feet and tail; absolutely true transparent alpha background. No floor or contact shadow.
Constraints: no typography, no border, no swatches, no watermark, no second pose, no other character, no scarf, no hair, no photorealism, no 3D rendering, no shiny plastic eye, no realistic individual feathers, no checkerboard background.
```

