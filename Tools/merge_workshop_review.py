"""Merge the reviewed 1A scene delta, never replace unrelated original scene objects."""
from pathlib import Path
import re, json, shutil, sys, datetime, os

root = Path(__file__).resolve().parents[1]
validation = root / "Logs/F0ArtValidation"
scene = Path("Assets/Scenes/InGame.unity")

def split(path):
    source = path.read_text(encoding="utf-8-sig")
    matches = list(re.finditer(r"^--- !u!\d+ &(\d+)[\s\S]*?(?=^--- !u!|\Z)", source, re.M))
    return source[:matches[0].start()], {m.group(1): m.group(0) for m in matches}

_, base = split(root / "Logs/workshop-real-base.unity")
_, reviewed = split(validation / scene)
header, original = split(root / scene)
changed = [key for key in base if reviewed.get(key) != base[key]]
added = [key for key in reviewed if key not in base]
conflicts = [key for key in changed if original.get(key) != base[key]]
collisions = [key for key in added if key in original]
result = dict(changed=len(changed), added=len(added), conflicts=conflicts, collisions=collisions)
print(json.dumps(result))
if "--apply" not in sys.argv:
    sys.exit(0)
assert not conflicts and not collisions, "Original changed concurrently; inspect before merging"
report = validation / "Docs/ArtReview/StorybookWorkshop/play-check.txt"
assert report.read_text(encoding="utf-8-sig").rstrip().endswith("PASS: workshop play verification")
story_report = validation / "Docs/ArtReview/Cutscene2D/Play/play-check.txt"
assert story_report.read_text(encoding="utf-8-sig").rstrip().endswith("PASS: F0–1A illustrated cutscene integration")
assert report.stat().st_mtime > (root / "Logs/workshop-real-base.unity").stat().st_mtime
assert story_report.stat().st_mtime > report.stat().st_mtime
backup = root / "Logs/WorkshopBackups" / ("before-merge-" + datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + ".unity")
backup.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(root / scene, backup)

asset_dir = Path("Assets/_BookTower/Generated/StorybookWorkshop")
shutil.copytree(validation / asset_dir, root / asset_dir, dirs_exist_ok=True)
shutil.copy2(str(validation / asset_dir) + ".meta", str(root / asset_dir) + ".meta")
for category in ("Scripts/Puzzle", "Scripts/Rooms", "Scripts/Interaction", "Editor"):
    for filename in (validation / "Assets/_BookTower" / category).glob("*.cs.meta"):
        target = root / filename.relative_to(validation)
        if target.with_suffix("").exists() and not target.exists():
            shutil.copy2(filename, target)

# Newly authored atlas/shader and tested pipeline shadow settings are part of this review.
relative = Path("Assets/_BookTower/Textures/StorybookV2")
shutil.copytree(validation / relative, root / relative, dirs_exist_ok=True)
shutil.copy2(str(validation / relative) + ".meta", str(root / relative) + ".meta")
for relative in (Path("Assets/_BookTower/Shaders/CrayonAtlasRepeat.shader"), Path("Assets/_BookTower/Shaders/CrayonAtlasRepeat.shader.meta"), Path("Assets/Settings/PC_RPAsset.asset"), Path("Assets/Settings/Mobile_RPAsset.asset")):
    shutil.copy2(validation / relative, root / relative)

# Existing illustration material IDs must continue to resolve; copy only changed files.
for name in ("Bird", "Rabbit", "Fox"):
    relative = Path("Assets/_BookTower/Generated/Storybook1A") / (name + ".mat")
    if (validation / relative).read_bytes() != (root / relative).read_bytes():
        shutil.copy2(validation / relative, root / relative)

for key in changed:
    if key in reviewed:
        original[key] = reviewed[key]
    else:
        original.pop(key, None)
for key in added:
    original[key] = reviewed[key]
pending = root / "Logs/workshop-reviewed-merged.unity"
pending.write_text(header + "".join(original.values()), encoding="utf-8")
os.replace(pending, root / scene)
for source in (validation / "Docs/ArtReview/StorybookWorkshop").iterdir():
    if source.suffix in (".png", ".txt", ".json"):
        shutil.copy2(source, root / "Docs/ArtReview/StorybookWorkshop" / source.name)
shutil.copy2(validation / "Docs/ArtReview/Cutscene2D/Play/play-check.txt", root / "Docs/ArtReview/StorybookWorkshop/story-check.txt")
shutil.copy2(validation / "Docs/ArtReview/Envelope/structure.txt", root / "Docs/ArtReview/StorybookWorkshop/envelope-check.txt")
(root / "Docs/ArtReview/StorybookWorkshop/merge-check.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print("Applied reviewed scene delta. Backup:", backup)
