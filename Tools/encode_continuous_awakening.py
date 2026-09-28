from pathlib import Path
import sys,subprocess,json
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg
out=ROOT/'Docs/ArtReview/ContinuousAwakening';out.mkdir(parents=True,exist_ok=True)
frames=ROOT/'Logs/F0ArtValidation/Logs/ContinuousFrames'
assert all((frames/f'{i:04d}.png').exists() for i in range(360))
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(frames/'%04d.png'),'-frames:v','360','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'continuous-awakening.mp4')],check=True)
r=imageio_ffmpeg.read_frames(str(out/'continuous-awakening.mp4'));meta=next(r);r.close()
(out/'video-check.json').write_text(json.dumps(meta,indent=2));print(meta)
