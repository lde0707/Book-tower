"""Encode the actual Unity opening recording at 1.5x with no audio track."""
from pathlib import Path
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
CAPTURE = ROOT / "Logs/OpeningStoryCapture"
OUTPUT = ROOT / "Docs/Videos"
sys.path.insert(0, str(ROOT / "Logs/VideoTools"))
import imageio_ffmpeg


def main():
    result = (CAPTURE / "capture-result.txt").read_text(encoding="utf-8-sig")
    assert result.startswith("PASS"), result
    fields = dict(re.findall(r"(\w+)=([^;\n]+)", result))
    assert fields["completed"] == "True", result
    source_frames = int(fields["frames"])
    source_fps = int(fields["fps"])
    source_seconds = source_frames / source_fps
    OUTPUT.mkdir(parents=True, exist_ok=True)
    target = OUTPUT / "opening-story-1.5x.mp4"
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    subprocess.run([
        ffmpeg, "-hide_banner", "-loglevel", "error", "-y",
        "-i", str(CAPTURE / "opening-story-1x-silent.mp4"),
        "-vf", "setpts=(PTS-STARTPTS)/1.5,fps=30", "-an", "-c:v", "libx264", "-preset", "medium",
        "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(target)
    ], check=True)
    reader = imageio_ffmpeg.read_frames(str(target))
    metadata = next(reader)
    reader.close()
    assert tuple(metadata["size"]) == (1920, 1080), metadata
    assert abs(metadata["duration"] - source_seconds / 1.5) < 0.15, metadata
    # Decode the complete video, and ensure the delivered file contains no audio stream.
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-xerror", "-i", str(target), "-f", "null", "-"], check=True)
    probe = subprocess.run([ffmpeg, "-hide_banner", "-i", str(target)], capture_output=True, text=True, encoding="utf-8", errors="replace")
    assert "Audio:" not in probe.stderr, probe.stderr
    checks = {
        "result": "PASS", "source": "Unity Editor play mode, actual opening story",
        "story_beats": 8, "source_frames": source_frames, "source_fps": source_fps,
        "source_seconds": source_seconds, "speed": 1.5, "audio_streams": 0,
        "video": metadata, "bytes": target.stat().st_size,
        "full_video_decode": "PASS"
    }
    (OUTPUT / "opening-story-video-check.json").write_text(json.dumps(checks, ensure_ascii=False, indent=2), encoding="utf-8")
    # Contact sheet from the delivered MP4 for visual review.
    from PIL import Image, ImageDraw
    sheet = Image.new("RGB", (960, 3 * 294), "#1e1b18")
    draw = ImageDraw.Draw(sheet)
    for index, fraction in enumerate((0.04, 0.2, 0.36, 0.53, 0.72, 0.94)):
        seconds = metadata["duration"] * fraction
        frame = CAPTURE / f"delivered-{index}.png"
        subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-ss", str(seconds), "-i", str(target), "-frames:v", "1", str(frame)], check=True)
        image = Image.open(frame).convert("RGB"); image.thumbnail((480, 270))
        x, y = index % 2 * 480, index // 2 * 294
        sheet.paste(image, (x, y)); draw.text((x + 8, y + 274), f"{seconds:.1f}s", fill="white")
    sheet.save(OUTPUT / "opening-story-contact-sheet.jpg", quality=92)
    print(json.dumps({"result": "PASS", "source_seconds": source_seconds, "delivered_seconds": metadata["duration"], "speed": 1.5, "bytes": target.stat().st_size}))


if __name__ == "__main__":
    main()
