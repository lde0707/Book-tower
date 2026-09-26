from pathlib import Path
import numpy as np,wave
out=Path('Assets/_BookTower/Audio/BindingPlay');out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(93);sr=24000
for name,dur in [('BookTone',1.2),('RopePull',.65),('PaperRustle',.7)]:
 t=np.arange(int(sr*dur))/sr
 if name=='BookTone':x=sum(np.sin(2*np.pi*f*t)*np.exp(-t*d)*a for f,d,a in [(196,4,.5),(393,7,.2),(589,10,.1)])
 else:
  noise=rng.normal(0,1,len(t));noise=np.convolve(noise,np.ones(8)/8,'same');x=noise*np.sin(np.pi*t/dur)**2*.6
  if name=='RopePull':x+=np.sin(2*np.pi*(170*t+40*t*t))*np.exp(-t*7)*.12
 x*=np.minimum(t/.015,1)
 with wave.open(str(out/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.int16(np.clip(x,-1,1)*22000).tobytes())
print('3 original audio clips saved')
