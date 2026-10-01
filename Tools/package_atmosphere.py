"""Package real Unity renders; resizing/layout only, no image retouching."""
from pathlib import Path
import shutil
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
clone = root / 'Logs/F0ArtValidation'
out = root / 'Docs/ArtReview/Atmosphere'
out.mkdir(parents=True, exist_ok=True)
for p in (clone / 'Docs/ArtReview/Atmosphere').glob('*'):
    if p.is_file():
        shutil.copy2(p, out / p.name)
before = clone / 'Docs/ArtReview/ReleaseArt/03-Lighting/Before'
(out / 'Before').mkdir(exist_ok=True)
for name in ['01-F0-start.png', '03-1A-stage.png', '05-1B-entry.png', 'review.txt', 'lights.csv', 'environment.txt']:
    shutil.copy2(before / name, out / 'Before' / name)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
bg = (27, 25, 23)

def panel(canvas, source, xy, size, label):
    im = Image.open(source).convert('RGB').resize(size, Image.Resampling.LANCZOS)
    x, y = xy
    canvas.paste(im, (x, y + 32))
    ImageDraw.Draw(canvas).text((x+8, y+4), label, font=small, fill=(237, 219, 190))

comparison = Image.new('RGB', (1600, 3*482+50), bg)
ImageDraw.Draw(comparison).text((14, 8), 'Actual Unity camera captures | BEFORE / AFTER', font=font, fill='white')
for row, (rid, old) in enumerate([('F0','01-F0-start.png'),('1A','03-1A-stage.png'),('1B','05-1B-entry.png')]):
    panel(comparison, out/'Before'/old, (0, 50+row*482), (800,450), rid+' - BEFORE')
    panel(comparison, out/(rid+'-0.png'), (800,50+row*482), (800,450), rid+' - AFTER')
comparison.save(out/'before-after.jpg', quality=94)
sheet = Image.new('RGB', (4*480,3*302+50), bg)
ImageDraw.Draw(sheet).text((14,8), 'F0 / 1A / 1B | Actual play camera - four directions', font=font, fill='white')
for row, rid in enumerate(['F0','1A','1B']):
    for col, yaw in enumerate([0,90,180,270]):
        panel(sheet,out/f'{rid}-{yaw}.png',(col*480,50+row*302),(480,270),f'{rid} - {yaw} deg')
sheet.save(out/'three-room-four-views.jpg', quality=94)
effects = Image.new('RGB',(1600,3*482+50),bg)
ImageDraw.Draw(effects).text((14,8),'Same new lighting | AIR OFF / AIR ON',font=font,fill='white')
for row,rid in enumerate(['F0','1A','1B']):
    panel(effects,out/f'{rid}-air-off.png',(0,50+row*482),(800,450),rid+' - AIR OFF')
    panel(effects,out/f'{rid}-0.png',(800,50+row*482),(800,450),rid+' - AIR ON')
effects.save(out/'air-comparison.jpg',quality=94)
reports = {
    'Docs/ArtReview/ShadowDuet/play-check.txt': 'shadow-duet.txt',
    'Docs/ArtReview/ReadingRoom1B/play-check.txt': 'reading-room.txt',
    'Docs/ArtReview/ReadingEntrance/play-check.txt': 'reading-entrance.txt',
    'Docs/ArtReview/InteriorRoomsV3/F0-play-verification.txt': 'foyer.txt',
    'Logs/foyer-transition-play-check.txt': 'foyer-transition.txt',
    'Docs/ArtReview/Cutscene2D/Play/play-check.txt': 'opening-story.txt',
    'Docs/ArtReview/RunSettings/checks.txt': 'run-settings.txt',
    'Docs/ArtReview/ReadingMusic/checks.txt': 'reading-music.txt',
    'Docs/ArtReview/ReleaseArt/08-Final/coverage-diagnostic.txt': 'rendered-envelope.txt',
    'Docs/ArtReview/ReleaseArt/F0/01-Structure/Traversal/checks.txt': 'passage-traversal.txt',
}
(out/'Checks').mkdir(exist_ok=True)
for source, dest in reports.items():
    if (clone/source).exists():
        shutil.copy2(clone/source, out/'Checks'/dest)
quality = clone/'Docs/ArtReview/ReleaseArt/03-Lighting/PuzzleQualities'
if quality.exists():
    shutil.copytree(quality, out/'PuzzleQualities', dirs_exist_ok=True)
traversal = clone/'Docs/ArtReview/ReleaseArt/F0/01-Structure/Traversal'
traversal_log = root/'Logs/atmosphere-ReleaseStructureTraversal.Run.log'
if traversal_log.exists():
    frames = [p for p in sorted(traversal.glob('frame-*.png')) if p.stat().st_mtime >= traversal_log.stat().st_ctime][4:]
    if frames:
        strip = Image.new('RGB',(1600,2*482+50),bg)
        ImageDraw.Draw(strip).text((14,8),'F0 to 1A | Actual camera along the covered passage',font=font,fill='white')
        for i in range(4):
            source=frames[round((len(frames)-1)*i/3)]
            panel(strip,source,((i%2)*800,50+(i//2)*482),(800,450),source.stem)
        strip.save(out/'passage-walk.jpg',quality=94)
regressions = root/'Logs/atmosphere-regressions.json'
if regressions.exists():
    shutil.copy2(regressions,out/'Checks/regressions.json')
standalone = root/'Logs/AtmosphereStandalone'
if (standalone/'standalone-report.txt').exists():
    (out/'Standalone').mkdir(exist_ok=True)
    for name in ['standalone-report.txt','measurement-config.txt','slow-frames.txt','F0-standalone.png','1A-standalone.png','1B-standalone.png']:
        if (standalone/name).exists():
            shutil.copy2(standalone/name,out/'Standalone'/name)
print(out)
