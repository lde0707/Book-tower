"""Original piano/violin-inspired synthesized chamber loops, 20 bars each."""
from pathlib import Path
import json, wave, shutil, sys, subprocess
import numpy as np
from numpy.fft import rfft, irfft, rfftfreq

ROOT=Path(__file__).resolve().parents[1];SR=44100
OUT=ROOT/'Assets/_BookTower/Audio/RegionMusic'
REVIEW=ROOT/'Docs/ArtReview/RegionMusic/PianoViolinV3'
BACKUP=ROOT/'ArtSource/RegionMusicCalmV2'
REVIEW.mkdir(parents=True,exist_ok=True);BACKUP.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
import imageio_ffmpeg

def voice(midi,duration,kind):
    t=np.arange(round(duration*SR))/SR;f=440*2**((midi-69)/12)
    if kind=='piano':
        # Decaying string partials and a restrained hammer attack, no bell partials.
        y=np.zeros(len(t))
        for h,a in enumerate([1,.40,.19,.085,.037,.018,.008],1):
            frequency=f*h*np.sqrt(1+.000035*h*h)
            y+=a*np.sin(2*np.pi*frequency*t)*np.exp(-t*(.70+h*.38))
        env=(1-np.exp(-t/.007))*np.sin(np.clip((duration-t)/.22,0,1)*np.pi/2)**2
    else:
        # Bowed harmonic spectrum and gentle delayed vibrato; no sampled performance.
        vibrato=.30*(1-np.exp(-t/.65))*np.sin(2*np.pi*5.1*t)
        phase=2*np.pi*f*t+vibrato
        y=np.zeros(len(t))
        for h in range(1,13):
            freq=f*h
            body=.55+.55*np.exp(-((freq-550)/340)**2)+.25*np.exp(-((freq-1300)/600)**2)
            gain=body/(h**1.35)/(1+(freq/3000)**4)
            y+=gain*np.sin(h*phase+.08*h)
        env=np.sin(np.minimum(t/.18,1)*np.pi/2)**2*np.sin(np.clip((duration-t)/.34,0,1)*np.pi/2)**2
        env*=.88+.12*np.sin(np.pi*np.clip(t/duration,0,1))
        y*=.68
    return (y*env).astype(np.float32)

def compose(index,title,bpm,chords,melody):
    beat=60/bpm;n=round(80*beat*SR);mix=np.zeros((n,2),np.float32);events=[]
    def add(at,midi,beats,kind,gain,pan):
        duration=beats*beat+(.9 if kind=='piano' else .22)
        y=voice(midi,duration,kind)*gain
        stereo=y[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)],np.float32)
        start=round(at*beat*SR)%n;k=min(n-start,len(y));mix[start:start+k]+=stereo[:k]
        if k<len(y):mix[:len(y)-k]+=stereo[k:]
        events.append(dict(beat=round(at,3),midi=midi,length=beats,instrument=kind))
    # Piano steps steadily; the violin breathes between phrases.
    for bar in range(20):
        chord=chords[bar%len(chords)];at=bar*4
        add(at,chord[0],3,'piano',.125,-.16)
        for j,k in enumerate([1,2,3,2]):
            add(at+j+.045,chord[k],1.2,'piano',[.13,.095,.115,.085][j],-.18+j*.06)
        if bar%4==2:add(at+3.5,chord[1]+12,.65,'piano',.044,.1)
        m1,m2=melody[bar%len(melody)]
        add(at+.15,m1,1.65 if m2 else 3.15,'violin',.073,.16)
        if m2:add(at+2.05,m2,1.65,'violin',.065,.16)
        # One quieter piano response ends every four-bar phrase.
        if bar%4==3:add(at+3.2,chord[2]+12,.7,'piano',.055,-.05)
    dry=mix.copy()
    for delay,gain in [(.043,.085),(.079,.075),(.127,.067),(.199,.050),(.313,.043),(.487,.035),(.769,.025),(1.09,.014)]:
        mix+=np.roll(dry,round(delay*SR),axis=0)[:,::-1]*gain
    freq=rfftfreq(n,1/SR);response=(1-np.exp(-(freq/42)**4))/np.sqrt(1+(freq/3400)**8);response[0]=0
    mix=irfft(rfft(mix,axis=0)*response[:,None],n=n,axis=0).astype(np.float32)
    mix*=10**(-22/20)/np.sqrt(np.mean(mix**2))
    peak=float(np.max(np.abs(mix)));assert peak<.8
    seam=float(np.max(np.abs(mix[0]-mix[-1])));derivative=float(np.quantile(np.abs(np.diff(mix,axis=0)),.999))
    assert seam<max(.004,derivative*1.5)
    name=f'Region{index}_{title}.wav';old=OUT/name
    path=OUT/f'Region{index}_{title}_PianoViolinV3.wav'
    if old.exists() and not (BACKUP/name).exists():shutil.copy2(old,BACKUP/name)
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(mix*32767).astype('<i2').tobytes())
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-i',str(path),'-codec:a','libmp3lame','-b:a','192k',str(REVIEW/f'Region{index}_PianoViolin.mp3')],check=True)
    result=dict(file=str(path.relative_to(ROOT)),version='PianoViolinV3',sound_source='original synthesized piano and bowed-string voices',bpm=bpm,bars=20,seconds=n/SR,peak_db=20*np.log10(peak),rms_db=-22,seam_delta=seam,derivative_p999=derivative,notes=len(events))
    (REVIEW/f'region{index}-score.json').write_text(json.dumps(events,indent=2));print(json.dumps(result));return result

report=[
compose(1,'PaperLanterns',82,
 [[48,55,60,64],[45,52,57,60],[41,48,53,57],[43,50,55,59]],
 [(64,67),(69,64),(65,64),(62,0),(67,64),(64,60),(60,65),(62,0)]),
compose(2,'FadedMemories',80,
 [[45,52,57,60],[41,48,53,57],[48,55,60,64],[43,50,55,59]],
 [(64,60),(60,57),(64,67),(62,0),(60,64),(65,60),(64,60),(59,0)]),
compose(3,'TitleInTheLight',86,
 [[41,48,53,57],[48,55,60,64],[45,52,57,60],[43,50,55,59]],
 [(65,69),(67,64),(64,69),(67,0),(69,65),(64,67),(69,64),(62,0)])]
(OUT/'music-manifest.json').write_text(json.dumps(report,indent=2))
(REVIEW/'audio-check.json').write_text(json.dumps(report,indent=2))
