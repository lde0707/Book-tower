"""Original three-note felt chime; no samples or third-party recording."""
from pathlib import Path
import wave
import numpy as np

rate = 44100
samples = np.zeros(rate * 4)
for start, frequency, amplitude in [(0.15, 523.251, .14), (.62, 659.255, .12), (1.18, 783.991, .13)]:
    t = np.arange(int(rate * 2.7)) / rate
    envelope = (1 - np.exp(-t * 100)) * np.exp(-t * 2.5)
    note = (np.sin(2*np.pi*frequency*t) + .18*np.sin(2*np.pi*frequency*2.003*t)) * envelope * amplitude
    offset = int(start * rate)
    samples[offset:offset+len(note)] += note[:len(samples)-offset]
path = Path(__file__).resolve().parents[1] / 'Assets/_BookTower/Generated/OpeningStory/MoaName.wav'
path.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(path), 'wb') as output:
    output.setnchannels(1); output.setsampwidth(2); output.setframerate(rate)
    output.writeframes((np.clip(samples, -1, 1)*32767).astype('<i2').tobytes())
print(path)
