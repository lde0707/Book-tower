"""Original, quiet synthesized paper-air hop. No external audio samples."""
from pathlib import Path
import numpy as np
import soundfile as sf
from scipy.signal import butter,sosfiltfilt
root=Path(__file__).resolve().parents[1];sr=48000
t=np.arange(int(sr*.34))/sr;rng=np.random.default_rng(216)
air=sosfiltfilt(butter(2,[250,2400],btype='bandpass',fs=sr,output='sos'),rng.normal(0,1,len(t)))
env=(1-np.exp(-t/.012))*np.exp(-t/.065)
phase=2*np.pi*(410*t+135*t*t)
x=(.20*air+.13*np.sin(phase)*np.exp(-t/.075))*env
x*=np.sin(np.pi*np.minimum(t/.025,1)/2)**2
x[-1200:]*=np.linspace(1,0,1200)
x*=.18/np.max(np.abs(x))
path=root/'Assets/_BookTower/Resources/Sfx/spirit_jump.wav'
sf.write(path,x,sr,subtype='PCM_16')
print('Created',path,'0.34 seconds, peak',np.max(np.abs(x)))
