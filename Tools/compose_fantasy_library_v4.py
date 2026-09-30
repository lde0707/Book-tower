"""Five original fantasy-library loops; procedural instruments, no sampled recordings.
Notes, reverberation and filtering are circular to preserve tails across repeat.
"""
from pathlib import Path
from functools import lru_cache
import json, sys, subprocess
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt
from numpy.fft import rfft,irfft,rfftfreq
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg
FFMPEG=imageio_ffmpeg.get_ffmpeg_exe()
SR=44100
OUT=ROOT/'Assets/_BookTower/Audio/FantasyLibraryV4'
REVIEW=ROOT/'Docs/ArtReview/RegionMusic/FantasyLibraryV4'
OUT.mkdir(parents=True,exist_ok=True);REVIEW.mkdir(parents=True,exist_ok=True)

@lru_cache(maxsize=100)
def voice(note,seconds,kind):
 t=np.arange(round(seconds*SR),dtype=np.float64)/SR;f=440*2**((note-69)/12);rng=np.random.default_rng(note+781)
 if kind=='piano':
  y=np.zeros_like(t)
  for h,a in enumerate([1,.32,.14,.068,.032,.016,.007,.003],1):
   y+=a*np.cos(2*np.pi*f*h*np.sqrt(1+.000018*h*h)*t+.22*h)*np.exp(-t*(.65+.36*h)*(f/260)**.15)
  hammer=sosfilt(butter(2,1700,fs=SR,output='sos'),rng.normal(0,1,len(t)))*np.exp(-t*90)*.022
  y+=hammer;env=(1-np.exp(-t/.009))*np.minimum(np.maximum((seconds-t)/.24,0),1)**2
 elif kind=='harp':
  y=np.zeros_like(t)
  for h in range(1,9):
   a=np.sin(h*.64)/(h**1.65);y+=a*np.sin(2*np.pi*f*h*t+.1*h)*np.exp(-t*(.65+.55*h))
  env=(1-np.exp(-t/.010))*np.minimum(np.maximum((seconds-t)/.24,0),1)**2
 elif kind=='glock':
  y=sum(a*np.sin(2*np.pi*f*r*t)*np.exp(-t*d) for r,a,d in [(1,1,1.05),(2.756,.075,2.2),(5.404,.009,4)])
  env=(1-np.exp(-t/.014))*np.minimum(np.maximum((seconds-t)/.45,0),1)**2
 elif kind=='strings':
  y=np.zeros_like(t)
  for cents,phase,level in [(-3,.4,.30),(0,1.8,.40),(3.2,3.0,.30)]:
   fundamental=2*np.pi*f*2**(cents/1200)*t+.035*np.sin(2*np.pi*4.3*t+phase)
   for h in range(1,9):y+=level*np.sin(fundamental*h+phase)/(h**1.7)*np.exp(-(f*h/2400)**2)
  env=np.sin(np.minimum(t/.55,1)*np.pi/2)**2*np.sin(np.clip((seconds-t)/.75,0,1)*np.pi/2)**2
 elif kind=='bass':
  y=np.sin(2*np.pi*f*t)+.13*np.sin(4*np.pi*f*t);env=(1-np.exp(-t/.028))*np.exp(-t*2.7)*np.minimum(np.maximum((seconds-t)/.2,0),1)**2
 elif kind=='brush':
  noise=sosfilt(butter(2,[900,3700],btype='bandpass',fs=SR,output='sos'),rng.normal(0,1,len(t)))
  y=noise*.30;env=(1-np.exp(-t/.012))*np.exp(-t*28)*np.minimum(np.maximum((seconds-t)/.08,0),1)**2
 else:
  y=np.sin(2*np.pi*(165*t+9*(1-np.exp(-t*30))))+.15*sosfilt(butter(2,850,fs=SR,output='sos'),rng.normal(0,1,len(t)))
  env=(1-np.exp(-t/.006))*np.exp(-t*32)*np.minimum(np.maximum((seconds-t)/.08,0),1)**2
 return (y*env).astype(np.float32)

# Shared D-major / B-minor family; inversions keep inner voices close.
H={
 'D':[50,57,61,64,66], 'Bm':[47,54,57,61,66], 'G':[43,54,57,62,66],
 'A':[45,55,59,62,64], 'Em':[40,55,59,62,66], 'Fsm':[42,54,57,61,64],
 'Dinv':[42,57,61,64,66], 'Glyd':[43,54,57,61,66],
}
TRACKS=[
 dict(id='01_Region1',title='종이등 사이의 발걸음',bpm=96,role='1구역 · 따뜻한 발견',
      chords=['D','Bm','G','A','Dinv','G','Em','A'],lead=[69,74,76,78,76,71,69,66,69,74,73,71,69,66,64,69],arp=[1,2,3,4,2,3,4,2],glock=.043,harp=.105,pad=.028,percussion=.042),
 dict(id='02_Region2',title='접힌 페이지의 비밀',bpm=100,role='2구역 · 호기심과 숨은 기억',
      chords=['Bm','Glyd','Dinv','A','Em','Bm','G','A'],lead=[66,69,73,74,73,69,66,64,66,71,73,76,74,71,69,64],arp=[1,3,2,4,1,2,4,3],glock=.033,harp=.12,pad=.032,percussion=.047),
 dict(id='03_Region3',title='되찾은 빛의 서가',bpm=98,role='3구역 · 신비와 회복',
      chords=['Glyd','D','Bm','A','G','Fsm','Em','A'],lead=[74,78,81,78,76,74,71,69,74,76,78,81,78,76,73,69],arp=[1,2,4,3,2,4,3,2],glock=.040,harp=.095,pad=.038,percussion=.034),
 dict(id='04_MainMenu',title='잊힌 제목',bpm=92,role='메인 화면 · 책이 열리기 전',
      chords=['D','Glyd','Bm','A','Dinv','G','Em','A'],lead=[69,74,76,78,76,71,69,66,74,78,76,74,71,69,66,64],arp=[1,2,3,2,1,3,4,2],glock=.050,harp=.080,pad=.036,percussion=.026),
 dict(id='05_Ending',title='돌아올 자리는 남겨 둘게',bpm=90,role='엔딩 · 안도와 여운',
      chords=['G','Dinv','Em','A','D','Bm','G','A'],lead=[74,73,71,69,66,69,74,76,78,76,74,71,69,66,64,69],arp=[1,2,3,4,3,2,4,2],glock=.029,harp=.082,pad=.044,percussion=.022),
]

def compose(cfg):
 beat=60/cfg['bpm'];n=round(96*beat*SR);mix=np.zeros((n,2),np.float32);events=[];rng=np.random.default_rng(910+cfg['bpm'])
 def add(at,note,duration,kind,gain,pan=0):
  seconds=round(duration*beat+({'piano':1.2,'harp':1.4,'glock':1.8,'strings':.8}.get(kind,.1)),3)
  y=voice(int(note),seconds,kind)*gain
  stereo=y[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)],np.float32)
  start=round(at*beat*SR)%n;k=min(n-start,len(y));mix[start:start+k]+=stereo[:k]
  if k<len(y):mix[:len(y)-k]+=stereo[k:]
  events.append(dict(beat=round(at,4),midi=int(note),duration_beats=duration,instrument=kind,gain=round(gain,4),pan=pan))
 for bar in range(24):
  chord=H[cfg['chords'][bar%8]];base=bar*4;section=bar//8;dynamic=[.93,1,.96][section]
  for j,m in enumerate(chord[1:]):add(base-.16+j*.025,m,4.2,'strings',cfg['pad']*dynamic,[-.38,-.13,.13,.38][j])
  add(base,chord[0],1.1,'bass',.070*dynamic,-.03)
  if bar%2==0:add(base+2.45,chord[0]+12,.65,'bass',.026,0)
  # Eighth-note repeating harp with mild lilt and alternating accents.
  for j,k in enumerate(cfg['arp']):
   at=base+j*.5+(.025 if j%2 else 0)+rng.uniform(-.008,.008)
   gain=cfg['harp']*[1,.52,.67,.5,.81,.52,.65,.48][j]*dynamic
   add(at,chord[k]+(12 if section==1 and j in (3,6) else 0),.8,'harp',gain,-.30+j*.075)
  # Sparse, memorable four-bar question/answer; not a note on every beat.
  index=(bar%8)*2;mel=cfg['lead'];m1,m2=mel[index],mel[(index+1)%16]
  add(base+.08,m1,1.35,'piano',.126*dynamic, -.12)
  if bar%4!=3:add(base+1.65,m2,1.15,'piano',.096*dynamic,-.08)
  else:add(base+2.55,m2,.85,'piano',.086*dynamic,.05)
  if section==1 and bar%2==1:add(base+3.35,chord[2]+12,.6,'piano',.047,.12)
  # Glock is an occasional answering glint rather than a continuous sharp melody.
  if bar%2==0:add(base+2.65,m2+12,1.1,'glock',cfg['glock'],.22)
  if bar%8==6:add(base+3.5,chord[3]+12,.8,'glock',cfg['glock']*.60,.28)
  # Soft brushed pulse, no snare/claps or pounding kick.
  for step in [.5,1.5,2.5,3.5]:add(base+step,64,.12,'brush',cfg['percussion']*(.7 if step in (.5,2.5) else 1),.18)
  if bar%4!=3:add(base+2,57,.15,'tap',cfg['percussion']*.42,-.2)
 # Dense, low-level, damped reflections; wrap their tails to the opening.
 dry=mix.copy()
 for i,delay in enumerate([.041,.067,.103,.149,.211,.283,.373,.487,.619,.773,.953,1.159,1.393,1.651]):
  gain=.055*np.exp(-delay/1.1);mix+=np.roll(dry,round(delay*SR),axis=0)[:,::-1 if i%2 else 1]*gain
 freq=rfftfreq(n,1/SR);response=(1-np.exp(-(freq/42)**4))/np.sqrt(1+(freq/5400)**8);response[0]=0
 mix=irfft(rfft(mix,axis=0)*response[:,None],n=n,axis=0).astype(np.float32)
 mix*=10**(-22/20)/np.sqrt(np.mean(mix**2))
 peak=float(np.max(np.abs(mix)));assert peak<.85,peak
 path=OUT/(cfg['id']+'.wav');sf.write(path,mix,SR,subtype='PCM_24')
 # Preview encoders have their own padding; the WAV is the authoritative looping asset.
 preview=REVIEW/(cfg['id']+'.mp3')
 subprocess.run([FFMPEG,'-hide_banner','-loglevel','error','-y','-i',str(path),'-c:a','libmp3lame','-b:a','192k',str(preview)],check=True)
 seam=float(np.max(abs(mix[0]-mix[-1])));delta=float(np.quantile(abs(np.diff(mix,axis=0)),.999));assert seam<max(.003,delta*1.5)
 # Loudness/true peak are analysed, not dynamically normalised across a loop boundary.
 measured=subprocess.run([FFMPEG,'-hide_banner','-i',str(path),'-af','loudnorm=I=-20:TP=-1:LRA=7:print_format=json','-f','null','-'],capture_output=True,text=True,check=True).stderr
 loudness=json.loads(measured[measured.rfind('{'):measured.rfind('}')+1])
 row=dict(id=cfg['id'],title=cfg['title'],role=cfg['role'],bpm=cfg['bpm'],bars=24,seconds=n/SR,sample_rate=SR,frames=n,peak_dbfs=round(20*np.log10(peak),3),rms_dbfs=-22,seam_delta=seam,derivative_p999=delta,integrated_lufs=loudness['input_i'],true_peak_dbtp=loudness['input_tp'],lra=loudness['input_lra'],notes=len(events),source='Original composition and procedural instrument synthesis; no live recordings or external samples',wav=str(path.relative_to(ROOT)),preview=str(preview.relative_to(ROOT)))
 (REVIEW/(cfg['id']+'-score.json')).write_text(json.dumps(events,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(row,ensure_ascii=False),flush=True);return row

if __name__=='__main__':
 report=[compose(c) for c in TRACKS]
 for p in [OUT/'manifest.json',REVIEW/'audio-check.json']:p.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
