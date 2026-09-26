"""Original 24-bar instrumental loops. Deterministic synthesis, no sampled recordings.
Notes and reverberation wrap over the loop boundary, retaining tails on repeat.
"""
from pathlib import Path
import json, wave
import numpy as np


SR=44100
OUT=Path('Assets/_BookTower/Audio/RegionMusic')
OUT.mkdir(parents=True,exist_ok=True)

def instrument(midi,duration,kind,rng):
    t=np.arange(int(SR*duration))/SR
    f=440*2**((midi-69)/12)
    if kind=='harp':
        y=sum(a*np.sin(2*np.pi*f*h*t+.08*h)*np.exp(-t*(1.8+.7*h)) for h,a in [(1,1),(2,.38),(3,.15),(4,.045)])
        env=np.minimum(t/.007,1)*np.minimum((duration-t)/.09,1)
    elif kind=='bell':
        y=sum(a*np.sin(2*np.pi*f*h*t)*np.exp(-t*d) for h,a,d in [(1,1,1.4),(2,.18,2.5),(3.01,.06,4),(4.02,.025,6)])
        env=np.minimum(t/.012,1)*np.minimum((duration-t)/.18,1)
    elif kind=='flute':
        phase=2*np.pi*f*t+.016*f/440*np.sin(2*np.pi*4.7*t)*np.minimum(t/.4,1)
        y=np.sin(phase)+.12*np.sin(2*phase)+.035*np.sin(3*phase)
        env=np.minimum(t/.13,1)*np.minimum((duration-t)/.25,1)
    elif kind=='pad':
        y=sum(np.sin(2*np.pi*f*r*t+q)*a for r,q,a in [(1,0,.5),(1.0013,.3,.24),(.9987,.7,.24),(2,0,.06)])
        env=np.sin(np.pi*np.minimum(t/.5,1)/2)**2*np.sin(np.pi*np.minimum((duration-t)/1.1,1)/2)**2
    elif kind=='bass':
        y=np.sin(2*np.pi*f*t)+.13*np.sin(4*np.pi*f*t)
        env=np.minimum(t/.02,1)*np.exp(-t*1.7)*np.minimum((duration-t)/.13,1)
    else:
        n=rng.normal(0,1,len(t));n=np.convolve(np.diff(n,prepend=0),np.ones(4)/4,mode='same')
        y=n;env=np.minimum(t/.008,1)*np.exp(-t*35)
    return (y*env).astype(np.float32)

def compose(index,bpm,chords,title):
    rng=np.random.default_rng(1900+index)
    beat=60/bpm;frames=round(96*beat*SR);length=frames/SR
    mix=np.zeros((frames,2),np.float32)
    events=[]
    def note(at,midi,dur,kind,gain,pan=0):
        y=instrument(midi,dur,kind,rng)*gain
        gains=np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)],np.float32)
        start=round(at*beat*SR)%frames
        end=min(len(y),frames-start)
        mix[start:start+end]+=y[:end,None]*gains
        if end<len(y):mix[:len(y)-end]+=y[end:,None]*gains
        events.append([round(at,3),midi,kind])
    # Three eight-bar phrases: invitation, reply, return. The shared 0-2-4-1 motif
    # ties all three regions to the same story while harmony and orchestration differ.
    motifs=[[0,2,4,1,2,0,3,1],[2,1,0,4,3,1,2,0],[0,4,2,1,3,2,1,4]]
    for bar in range(24):
        chord=chords[bar%8];root=chord[0];phrase=bar//8;at=bar*4
        for j,m in enumerate(chord):note(at,m,4*beat+1.1,'pad',.024 if index!=2 else .032,(-.6+j*.4))
        note(at,root-12,2.4*beat,'bass',.12,0)
        note(at+2,root-5,1.8*beat,'bass',.07,0)
        arp=[0,1,2,1,3,2,1,2] if index!=2 else [0,2,1,3,1,2,3,2]
        for step,k in enumerate(arp):
            if index==2 and step in (3,7):continue
            note(at+step*.5,chord[k]+12,1.7,'harp',(.047 if step%2 else .063)*(1+.06*phrase),-.35 if step%2 else .25)
        # A composed two-note call and a breathing space in every bar.
        pent=[chord[0]+12,chord[1]+12,chord[2]+12,chord[3]+12,chord[0]+24]
        pattern=motifs[phrase]
        melody_kind='bell' if index==1 else 'flute' if index==2 else 'harp'
        gain=.11 if index==1 else .045 if index==2 else .14
        note(at+.5,pent[pattern[bar%8]],1.7*beat,melody_kind,gain,.08)
        if bar%4!=3:note(at+2.5,pent[pattern[(bar+1)%8]],1.3*beat,melody_kind,gain*.82,-.1)
        if phrase==1 and bar%2==0:note(at+3.5,chord[2]+24,1.6,'bell',.035,.5)
        if index==3 and bar%2==0:note(at+1,chord[1]+12,2*beat,'flute',.035,-.35)
        for step in range(8):
            note(at+step*.5,0,.16,'brush',.009 if index==2 else (.018 if step%2 else .012),-.25 if step%2 else .25)
    # Periodic stereo room/delay: no fade-to-silence or tail cut at the seam.
    dry=mix.copy()
    for delay,gain in [(.071,.12),(.113,.10),(.179,.08),(beat*.75,.15),(beat*1.5,.09),(beat*2.25,.05)]:
        mix+=np.roll(dry,round(delay*SR),axis=0)[:,::-1]*gain
    mix-=mix.mean(axis=0)
    peak=np.max(np.abs(mix));mix*=min(.87/peak,.12/np.sqrt(np.mean(mix**2)))
    pcm=np.round(np.clip(mix,-1,1)*32767).astype('<i2')
    path=OUT/f'Region{index}_{title}.wav'
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    derivative=np.abs(np.diff(mix,axis=0));seam=float(np.max(np.abs(mix[0]-mix[-1])))
    assert seam<max(.015,float(np.quantile(derivative,.999))*2), 'Loop seam discontinuity'
    result=dict(file=str(path),bpm=bpm,bars=24,seconds=length,peak_db=float(20*np.log10(np.max(np.abs(mix)))),rms_db=float(20*np.log10(np.sqrt(np.mean(mix**2)))),seam_delta=seam,notes=len(events))
    print(json.dumps(result));return result

report=[
compose(1,96,[[60,64,67,71],[57,60,64,67],[53,57,60,64],[55,59,62,69],[60,64,67,74],[57,60,64,71],[53,57,60,67],[55,59,62,65]],'PaperLanterns'),
compose(2,92,[[57,60,64,71],[53,57,60,64],[50,53,57,64],[55,59,62,69],[57,60,64,67],[52,55,59,62],[53,57,60,64],[52,56,59,64]],'FadedMemories'),
compose(3,100,[[62,66,69,73],[59,62,66,69],[55,59,62,66],[57,61,64,71],[62,66,69,76],[66,69,73,76],[55,59,62,69],[57,61,64,67]],'TitleInTheLight')]
(OUT/'music-manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
