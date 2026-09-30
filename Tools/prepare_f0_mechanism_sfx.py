"""Quiet F0 mechanical foley from the existing licensed Kenney CC0 originals."""
from pathlib import Path
import json
import numpy as np
import soundfile as sf
from scipy.signal import butter,sosfiltfilt,resample_poly
root=Path(__file__).resolve().parents[1]
src=root/'ArtSource/Audio/Kenney';dst=root/'Assets/_BookTower/Resources/Sfx'
def read(name,rate=1):
 p=next(src.rglob(name+'.ogg'));x,s=sf.read(p,always_2d=True);x=x.mean(axis=1)
 x=resample_poly(x,48000,s);x=np.interp(np.arange(0,len(x),rate),np.arange(len(x)),x)
 return x,str(p.relative_to(root))
specs={'mechanism_press': [('metalClick',1,0,.5),('click_001',1,0,.35)],
 'foil_move':[('bookFlip2',.75,0,.8),('cloth2',.8,.1,.35),('cloth2',.75,.65,.25)],
 'mirror_move':[('creak2',1.15,0,.25),('clothBelt',.85,0,.65),('clothBelt2',.85,.65,.45),('cloth2',.9,1.25,.25)],
 'binding_door_open':[('metalLatch',.9,0,.4),('doorOpen_1',.8,.15,.6),('creak1',.8,.45,.3),('bookOpen',.7,1.2,.5),('cloth4',.8,2.1,.25),('bookPlace1',.8,2.65,.22)]}
report=[]
for name,parts in specs.items():
 tracks=[(read(source,rate),int(at*48000),gain) for source,rate,at,gain in parts]
 x=np.zeros(max(len(a[0])+at for a,at,gain in tracks))
 for (a,p),at,gain in tracks:x[at:at+len(a)]+=a*gain
 x=sosfiltfilt(butter(2,[100,3200],btype='bandpass',fs=48000,output='sos'),x)
 peak=.25 if name=='binding_door_open' else .18
 x*=peak/max(abs(x));fade=min(720,len(x)//4);x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
 sf.write(dst/(name+'.wav'),x,48000,subtype='PCM_16')
 report.append({'asset':name,'seconds':len(x)/48000,'peak_dbfs':float(20*np.log10(max(abs(x)))),'sources':[a[1] for a,at,gain in tracks]})
(src/'f0-mechanism-edits.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
