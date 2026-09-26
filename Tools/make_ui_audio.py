import wave
from pathlib import Path
import numpy as np
out=Path('Assets/_BookTower/UI'); sr=24000; t=np.arange(sr*20)/sr
pad=sum(np.sin(2*np.pi*f*t)*(0.5-0.5*np.cos(2*np.pi*t/20)) for f in (220,277.2,329.6))/6
rng=np.random.default_rng(71); noise=np.convolve(rng.normal(0,1,len(t)),np.ones(160)/160,mode='same'); noise*=np.sin(np.pi*t/20)**2
for name,data in [('QuietPages',pad),('PaperAir',noise)]:
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(data,-1,1)*16000).astype('<i2').tobytes())
