from pathlib import Path
import re,shutil,json,hashlib
R=Path(__file__).resolve().parents[1];T=R/'Logs/F0ArtValidation';review=Path('Docs/ArtReview/MoaModelViewsV1/UnityV3')
base=R/'Logs/MoaSculptBackup/original-before-moa.unity';scene=R/'Assets/Scenes/InGame.unity';new=T/'Assets/Scenes/InGame.unity'
assert scene.read_bytes()==base.read_bytes(),'Original scene changed during work; do not overwrite'
def docs(path):
    chunks=re.split(r'(?m)(?=^--- !u!)',path.read_text(encoding='utf-8-sig'))
    return {re.search(r'^--- !u!\d+ &(\d+)',c).group(1):c for c in chunks if c.startswith('--- !u!')}
a,b=docs(base),docs(new);changed=[k for k in a.keys()&b.keys() if a[k]!=b[k]]
for k in changed:
    if a[k].startswith('--- !u!4 '):
        strip=lambda s:re.sub(r'  m_Children:\n(?:  - .*\n)*','  m_Children:\n',s)
    elif a[k].startswith('--- !u!114 ') and '  companion: ' in a[k]:
        strip=lambda s:re.sub(r'  companion: .*','  companion: CHANGED',s)
    else:raise AssertionError(('Unexpected existing component change',k,a[k][:150]))
    assert strip(a[k])==strip(b[k]),('Nonvisual property changed',k)
assert len(changed)<=4,changed
art=Path('Assets/_BookTower/Art/MoaSculpt');shutil.copytree(T/art,R/art,dirs_exist_ok=True);shutil.copy2(T/(str(art)+'.meta'),R/(str(art)+'.meta'))
for p in ['Scripts/Story/MoaSculptMotion.cs.meta','Editor/MoaSculptBuilder.cs.meta']:
    q=Path('Assets/_BookTower')/p;shutil.copy2(T/q,R/q)
shutil.copy2(new,scene)
for p in (T/review).glob('*'):
    if p.is_file():shutil.copy2(p,R/review/p.name)
story=Path('Docs/ArtReview/Cutscene2D/Play')
shutil.copy2(T/story/'play-check.txt',R/review/'opening-play-check.txt')
for name in ['01-first-meeting.png','03-promise-large-text.png']:shutil.copy2(T/story/name,R/review/name)
(R/'Temp/MoaSculptApply.request').write_text('Apply approved sculpt bird and preserve live scene edits.\n')
report={'changed_existing_components':changed,'removed_old_visual_documents':len(a.keys()-b.keys()),'added_new_visual_documents':len(b.keys()-a.keys()),'scene_sha256':hashlib.sha256(scene.read_bytes()).hexdigest(),'existing_gameplay_properties_preserved':True}
(R/review/'scene-check.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
