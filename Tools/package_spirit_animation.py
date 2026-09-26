"""Package the already-rendered smooth Unity animations and 30fps GIF previews."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from PIL import Image

root = Path(__file__).resolve().parent.parent
out = root / 'Docs/ArtReview/AnimationSmooth'
for name, count in [('Idle', 72), ('Move', 48), ('Jump', 44)]:
    frames = []
    for i in range(0, count, 2):
        with Image.open(out / f'{name}-{i+1:02}.png') as frame:
            frames.append(frame.convert('RGB'))
    # GIF stores centiseconds. Quantize the cumulative time, preserving duration.
    durations = [10 * (round((i+1)*100/30) - round(i*100/30)) for i in range(len(frames))]
    frames[0].save(out / f'{name}.gif', save_all=True, append_images=frames[1:],
                   duration=durations, loop=0, disposal=2)

archive = root / 'Docs/ArtReview/paper-spirit-animations-smooth.zip'
with ZipFile(archive, 'w', ZIP_DEFLATED) as package:
    for file in sorted(out.iterdir()):
        if file.is_file():
            package.write(file, 'Preview/' + file.name)
    for file in sorted((root / 'Assets/_BookTower/Art/PaperSpirit/Animation').iterdir()):
        if file.is_file():
            package.write(file, 'Unity/' + file.name)
print(archive)
