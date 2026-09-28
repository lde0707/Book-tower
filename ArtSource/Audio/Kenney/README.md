# Quiet paper foley — 2026-09-28

Original asset packs by Kenney, both licensed CC0 (commercial use and edits permitted):

- Interface Sounds: https://kenney.nl/assets/interface-sounds
- RPG Audio: https://kenney.nl/assets/rpg-audio

Original ZIPs and their unpacked License.txt files are retained here. No third-party music was imported. `quiet-edits.json` records each source filename, SHA-256, output duration and peak level.

Run `Tools/prepare_quiet_sfx.py` with Python, numpy, scipy and soundfile to reproduce the 13 mono PCM WAV files in `Assets/_BookTower/Resources/Sfx`. Processing: 90–3600 Hz bandpass, silence trim, 12 ms edge fades, controlled peak, 48 kHz resample. No clipping or normalization to full scale. Final playback is attenuated further by the game effects/master controls.

| Game event | Kenney source |
|---|---|
| UI click / settings options | click_001 |
| ESC/menu page | bookFlip2 |
| Fold / page mechanisms | bookFlip1, bookFlip2, bookFlip3 |
| Gentle success / tapping book | bookClose |
| Collect a piece | clothBelt |
| Gate | bookOpen |
| Footsteps / paper movement | cloth1, cloth2, cloth4 |
| Subtle hint / rope handling | cloth3, bookFlip3 |

Click voices are limited to one and rate-limited using unscaled time so paused menus still work. Footsteps alternate three clips with a 0.22 second minimum interval. UI respects the global listener/master volume and current effects setting. Existing scene-assigned F0, decorative object and opening story rustle clips are reassigned by `QuietHoodIntegration.Run`.

The test checks digital levels and routing; subjective loudness still depends on the user's speakers/headphones and master settings.
