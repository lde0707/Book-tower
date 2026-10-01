"""Package unedited Unity traversal captures as a 5 fps inspection clip."""
from pathlib import Path
import argparse
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument("folder", type=Path)
parser.add_argument("--skip", type=int, default=0, help="Omit initial camera interpolation after test teleport")
args = parser.parse_args()
sources = sorted(args.folder.glob("frame-*.png"))[args.skip:]
if not sources:
    raise SystemExit("No Unity traversal frames found")
frames = [Image.open(p).convert("RGB") for p in sources]
output = args.folder / "passage-walk.gif"
frames[0].save(output, save_all=True, append_images=frames[1:], duration=200, loop=0)
with Image.open(output) as check:
    assert check.n_frames == len(sources)
    print(f"{output}: {check.n_frames} frames, {check.size}, 200 ms/frame")
for frame in frames:
    frame.close()
