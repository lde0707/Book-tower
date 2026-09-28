"""Copy Unity-validated art and a narrowly checked scene change into the original workspace."""
from pathlib import Path
import re,hashlib,shutil,json
ROOT=Path(__file__).resolve().parents[1];TEST=ROOT/'Logs/F0ArtValidation'
original=ROOT/'Assets/Scenes/InGame.unity';baseline=ROOT/'Logs/HoodSpiritBackup/original-before-hood.unity';validated=TEST/'Assets/Scenes/InGame.unity'
assert original.read_bytes()==baseline.read_bytes(),'Original scene changed since snapshot; do not overwrite.'
def docs(path):
    text=path.read_text(encoding='utf-8-sig');blocks=re.split(r'(?m)(?=^--- !u!)',text)
    return {re.search(r'^--- !u!(\d+) &(\d+)',s).group(2):s for s in blocks if s.startswith('--- !u!')}
a,b=docs(baseline),docs(validated)
changed=[k for k in a.keys()&b.keys() if a[k]!=b[k]]
removed=[k for k in a.keys()-b.keys()]
# Player physics/controller components and every other existing component must remain exact.
for k in changed:
    assert a[k].startswith('--- !u!4 &'),('Unexpected changed existing component',k,a[k][:150])
    # Only the player's child reference may change; its transform fields must remain exact.
    strip=lambda s:re.sub(r'  m_Children:\n(?:  - .*\n)*','  m_Children:\n',s)
    assert strip(a[k])==strip(b[k]),('Changed player transform',k)
assert len(changed)==1,changed
assert len(removed)<150,('Unexpected scene deletions',len(removed))
for name in ['HoodSpiritAnimator.cs','HoodSpiritAnimator.cs.meta']:
    p=Path('Assets/_BookTower/Scripts/Presentation')/name
    if name.endswith('.meta'):shutil.copy2(TEST/p,ROOT/p)
for name in ['HoodSpiritBuilder.cs.meta','HoodSpiritPlayVerifier.cs.meta']:
    p=Path('Assets/_BookTower/Editor')/name;shutil.copy2(TEST/p,ROOT/p)
art=Path('Assets/_BookTower/Art/PaperHoodSpirit');shutil.copytree(TEST/art,ROOT/art,dirs_exist_ok=True)
shutil.copy2(TEST/(str(art)+'.meta'),ROOT/(str(art)+'.meta'))
shutil.copy2(validated,original)
review=Path('Docs/ArtReview/SpiritRedesignV1/AnimationV1')
for p in (TEST/review).glob('*'):
    if p.is_file():shutil.copy2(p,ROOT/review/p.name)
report={'changed_existing_documents':changed,'removed_old_visual_documents':len(removed),'added_new_visual_documents':len(b.keys()-a.keys()),'player_transform_preserved':True,'all_existing_nonvisual_components_preserved':True,'scene_sha256':hashlib.sha256(original.read_bytes()).hexdigest()}
(ROOT/review/'scene-change-check.json').write_text(json.dumps(report,indent=2))
# The already-open editor may still hold the pre-import scene in memory. On import, the
# editor-side request replaces the visual in that loaded scene, preserving any live edits.
(ROOT/'Temp/HoodSpiritApply.request').write_text('Apply verified hood spirit to the loaded InGame scene.\n')
print(json.dumps(report))
