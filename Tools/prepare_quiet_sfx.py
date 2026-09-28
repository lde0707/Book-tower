"""Reproducible quiet foley edits from licensed Kenney CC0 source archives."""
from pathlib import Path
import json, hashlib
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfiltfilt, resample_poly
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'ArtSource/Audio/Kenney'; DEST=ROOT/'Assets/_BookTower/Resources/Sfx'
DEST.mkdir(parents=True,exist_ok=True)
mapping={
 'fold_step':('bookFlip1',.18), 'fold_step_2':('bookFlip2',.18), 'fold_step_3':('bookFlip3',.18),
 'puzzle_solve':('bookClose',.20), 'piece_pickup':('clothBelt',.17),
 'gate_open':('bookOpen',.22), 'rustle_step':('cloth1',.12),
 'rustle_step_2':('cloth2',.12), 'rustle_step_3':('cloth4',.12),
 'hint_tremble':('cloth3',.10), 'hint_nudge':('bookFlip3',.14),
 'ui_click':('click_001',.13), 'ui_page':('bookFlip2',.12)
}
report=[]
for target,(original,peak) in mapping.items():
 path=next(SRC.rglob(original+'.ogg')); x,sr=sf.read(path,always_2d=True);x=x.mean(axis=1)
 # Remove DC/rumble and soften bright scratch transients; no synthesized tones.
 x=sosfiltfilt(butter(2,[90,3600],btype='bandpass',fs=sr,output='sos'),x)
 active=np.flatnonzero(abs(x)>max(abs(x))*.008)
 if len(active):x=x[max(0,active[0]-int(sr*.01)):min(len(x),active[-1]+int(sr*.04))]
 fade=min(int(sr*.012),len(x)//4);x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
 x*=peak/max(abs(x));x=resample_poly(x,48000,sr);x=np.clip(x,-peak,peak)
 dest=DEST/(target+'.wav');sf.write(dest,x,48000,subtype='PCM_16')
 report.append(dict(asset=target,source=str(path.relative_to(ROOT)),seconds=round(len(x)/48000,3),peak_dbfs=round(20*np.log10(max(abs(x))),2),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(SRC/'quiet-edits.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
