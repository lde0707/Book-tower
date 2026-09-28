"""Encode the actual rendered Blender frame sequence into MP4 and looping GIF."""
from pathlib import Path
import sys, subprocess, json
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg
out=ROOT/'Docs/ArtReview/MoaModelViewsV1/FlightV1';frames=ROOT/'Logs/MoaFlightRenderFrames'
assert len(list(frames.glob('*.png')))==240,'Wait for all 240 rendered frames'
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
def run(args):
    p=subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y']+args,capture_output=True,text=True)
    if p.returncode:raise RuntimeError(p.stderr)
run(['-framerate','30','-i',str(frames/'%04d.png'),'-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'moa-flight.mp4')])
run(['-framerate','30','-i',str(frames/'%04d.png'),'-filter_complex','[0:v]fps=24,scale=600:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a','-loop','0',str(out/'moa-flight.gif')])
video=imageio_ffmpeg.read_frames(str(out/'moa-flight.mp4'));metadata=next(video);video.close()
assert abs(metadata['duration']-8)<.1 and metadata['fps']==30,metadata
(out/'video-check.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')
print(json.dumps({'duration':metadata['duration'],'fps':metadata['fps'],'size':metadata['size'],'mp4_bytes':(out/'moa-flight.mp4').stat().st_size,'gif_bytes':(out/'moa-flight.gif').stat().st_size}))
