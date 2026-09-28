from pathlib import Path
import sys,subprocess,json
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg
out=ROOT/'Docs/ArtReview/SpiritRedesignV1/AnimationV1';frames=ROOT/'Logs/HoodAnimationFrames'
data=json.loads((out/'preview-timeline.json').read_text());n=data['frames']
assert all((frames/('%04d.png'%i)).exists() for i in range(n))
ff=imageio_ffmpeg.get_ffmpeg_exe()
subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-framerate','24','-i',str(frames/'%04d.png'),'-frames:v',str(n),'-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'hood-spirit-animation.mp4')],check=True)
v=imageio_ffmpeg.read_frames(str(out/'hood-spirit-animation.mp4'));meta=next(v);v.close()
assert abs(meta['duration']-n/24)<.1
(out/'video-check.json').write_text(json.dumps(meta,indent=2));print(meta)
