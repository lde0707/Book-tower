from pathlib import Path
import re, json, shutil
root=Path.cwd(); val=root/'Logs/F0ArtValidation'
base=root/'Logs/embedded-last-merged.unity'
def split(p):
 s=p.read_text(encoding='utf-8-sig'); m=list(re.finditer(r'^--- !u!\d+ &(\d+)[\s\S]*?(?=^--- !u!|\Z)',s,re.M)); return s[:m[0].start()],{x.group(1):x.group(0) for x in m}
_,b=split(base);_,a=split(val/'Assets/Scenes/InGame.unity');head,o=split(root/'Assets/Scenes/InGame.unity')
changed=[k for k in b if a.get(k)!=b[k]];added=[k for k in a if k not in b]
conflicts=[k for k in changed if o.get(k)!=b[k]];collisions=[k for k in added if k in o]
print(json.dumps({'changed':len(changed),'added':len(added),'conflicts':conflicts,'collisions':collisions}))

# Apply only changed Unity YAML objects after checking all original base versions.
if __name__ == '__main__':
 import sys, datetime
 if '--apply' in sys.argv:
  assert not conflicts and not collisions, 'Scene changed concurrently; refusing merge'
  scene=root/'Assets/Scenes/InGame.unity'
  backup=root/'Logs/EmbeddedOpticsBackups'/('InGame-before-scoped-merge-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.unity')
  backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(scene,backup)
  for k in changed:
   if k in a:o[k]=a[k]
   else:o.pop(k,None)
  for k in added:o[k]=a[k]
  assets=Path('Assets/_BookTower/Generated/F0EmbeddedOptics')
  shutil.copytree(val/assets,root/assets,dirs_exist_ok=True)
  shutil.copy2(str(val/assets)+'.meta',str(root/assets)+'.meta')
  scene.write_text(head+''.join(o.values()),encoding='utf-8')
  print('APPLIED scoped scene objects; backup:',backup)
