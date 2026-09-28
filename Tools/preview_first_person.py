"""Encode generated key art as a gentle motion-comic timing preview (no source edits)."""
from pathlib import Path
import sys, subprocess, json
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg
art=ROOT/'Assets/_BookTower/Art/Cutscenes/FirstPersonV1'
out=ROOT/'Docs/ArtReview/FirstPersonAwakening'
ff=imageio_ffmpeg.get_ffmpeg_exe()
names=['01-dark','02-offering','03-received','04-awake']
args=[ff,'-hide_banner','-loglevel','error','-y','-filter_complex_threads','1']
for name in names:args+=['-i',str(art/(name+'.png'))]
filters=[]
for i in range(4):
    # Subtle forward drift, not a claim of character skeletal animation.
    z="1.025+0.00015*on" if i!=2 else "1.06-0.00012*on"
    s=f"[{i}:v]scale=1920:1080,zoompan=z='{z}':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=180:s=1280x720:fps=30,setsar=1,format=yuv420p"
    if i==0:s+=",eq=brightness=-0.10,fade=t=in:st=0:d=2.5"
    filters.append(s+f"[v{i}]")
filters += ["[v0][v1]xfade=transition=fade:duration=1.2:offset=4.8[a]",
            "[a][v2]xfade=transition=fade:duration=1.2:offset=9.6[b]",
            "[b][v3]xfade=transition=fade:duration=1.2:offset=14.4,format=yuv420p[v]"]
args+=['-filter_complex',';'.join(filters),'-map','[v]','-t','20.4','-an','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'awakening-motion-preview.mp4')]
subprocess.run(args,check=True)
reader=imageio_ffmpeg.read_frames(str(out/'awakening-motion-preview.mp4'));meta=next(reader);reader.close()
assert abs(meta['duration']-20.4)<.15
(out/'video-check.json').write_text(json.dumps(meta,indent=2))
print(meta)
