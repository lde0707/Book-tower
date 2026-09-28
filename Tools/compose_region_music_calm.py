"""Original gentle scores: sparse melodies, soft attacks, periodic reverb and filtering."""
from pathlib import Path
import json, wave, shutil, sys, subprocess
import numpy as np
from numpy.fft import rfft, irfft, rfftfreq

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/_BookTower/Audio/RegionMusic'
REVIEW=ROOT/'Docs/ArtReview/RegionMusic/CalmV2'
BACKUP=ROOT/'ArtSource/RegionMusicV1'
SR=44100
REVIEW.mkdir(parents=True,exist_ok=True); BACKUP.mkdir(parents=True,exist_ok=True)

def voice(midi,seconds,kind):
    t=np.arange(round(seconds*SR),dtype=np.float64)/SR
    f=440*2**((midi-69)/12)
    # Harmonic tones only: no metallic partials, percussive noise, or detuned beating.
    if kind=='pad':
        y=np.sin(2*np.pi*f*t)+.14*np.sin(4*np.pi*f*t)+.025*np.sin(6*np.pi*f*t)
        attack,release=.85,1.2
        envelope=np.sin(np.minimum(t/attack,1)*np.pi/2)**2*np.sin(np.clip((seconds-t)/release,0,1)*np.pi/2)**2
    elif kind=='felt':
        y=np.sin(2*np.pi*f*t)+.10*np.sin(4*np.pi*f*t)*np.exp(-t*2)+.022*np.sin(6*np.pi*f*t)*np.exp(-t*3)
        envelope=(1-np.exp(-t/0.09))**2*np.exp(-t/.95)*np.sin(np.clip((seconds-t)/.6,0,1)*np.pi/2)**2
    else:
        y=np.sin(2*np.pi*f*t)+.06*np.sin(4*np.pi*f*t)
        envelope=np.sin(np.minimum(t/.45,1)*np.pi/2)**2*np.sin(np.clip((seconds-t)/.85,0,1)*np.pi/2)**2
    return (y*envelope).astype(np.float32)

def compose(index,title,bpm,chords,melody):
    beat=60/bpm; n=round(64*beat*SR)
    mix=np.zeros((n,2),np.float32); events=[]
    def add(at,midi,duration,kind,gain,pan=0):
        y=voice(midi,duration,kind)*gain
        stereo=y[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)],np.float32)
        start=round(at*beat*SR)%n; first=min(n-start,len(y))
        mix[start:start+first]+=stereo[:first]
        if first<len(y):mix[:len(y)-first]+=stereo[first:]
        events.append(dict(beat=at,note=midi,voice=kind))
    for bar in range(16):
        chord=chords[bar%8]
        # Four warm mid-register voices, continuous slow harmony; no pounding bass.
        for j,m in enumerate(chord):
            add(bar*4+j*.045,m,4*beat+.8,'pad',.039 if j else .032,[-.3,-.1,.1,.3][j])
        # Two-bar phrases alternate statement and silence, with only occasional replies.
        m=melody[bar%8]
        add(bar*4+.65,m,3.4,'felt',.13, -.12 if bar%2 else .12)
        if bar%4 in (0,2):add(bar*4+2.7,melody[(bar+1)%8],2.8,'felt',.074,.06)
        if bar in (3,7,11,15):add(bar*4+.8,chord[2]+12,2.9,'air',.023,0)
    dry=mix.copy()
    for delay,gain in [(.083,.09),(.137,.07),(.227,.06),(.379,.045),(.613,.035),(.997,.025)]:
        mix+=np.roll(dry,round(delay*SR),axis=0)[:,::-1]*gain
    # FFT filter is periodic, so it cannot introduce an unfiltered loop boundary.
    spectrum=rfft(mix,axis=0);freq=rfftfreq(n,1/SR)
    response=(1/np.sqrt(1+(freq/2300)**8))*(1-np.exp(-(freq/48)**4))
    response[0]=0
    mix=irfft(spectrum*response[:,None],n=n,axis=0).astype(np.float32)
    rms=np.sqrt(np.mean(mix**2));mix*=10**(-23/20)/rms
    assert np.max(np.abs(mix))<.70
    pcm=np.round(mix*32767).astype('<i2')
    name=f'Region{index}_{title}.wav'; path=OUT/name
    if path.exists() and not (BACKUP/name).exists():shutil.copy2(path,BACKUP/name)
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    seam=float(np.max(np.abs(mix[0]-mix[-1])))
    delta=float(np.quantile(np.abs(np.diff(mix,axis=0)),.999))
    assert seam<max(.003,delta*1.5),f'Loop click: {seam}'
    energy=np.abs(rfft(mix,axis=0))**2
    high=float(energy[freq>3000].sum()/energy.sum())
    result=dict(file=str(path.relative_to(ROOT)),version='CalmV2',bpm=bpm,bars=16,seconds=n/SR,
                peak_db=float(20*np.log10(np.max(np.abs(mix)))),rms_db=float(20*np.log10(np.sqrt(np.mean(mix**2)))),
                seam_delta=seam,derivative_p999=delta,energy_above_3khz=high,notes=len(events))
    (REVIEW/f'region{index}-score.json').write_text(json.dumps(events,indent=2))
    sys.path.insert(0,str(ROOT/'Logs/media_runtime'))
    import imageio_ffmpeg
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-i',str(path),'-codec:a','libmp3lame','-b:a','192k',str(REVIEW/f'Region{index}_Calm.mp3')],check=True)
    print(json.dumps(result));return result

report=[
compose(1,'PaperLanterns',64,
 [[48,55,60,64],[48,55,60,64],[45,52,57,60],[45,52,57,60],[41,48,53,57],[41,48,53,57],[43,50,55,62],[43,50,55,62]],
 [64,67,64,60,65,64,62,60]),
compose(2,'FadedMemories',62,
 [[45,52,57,60],[45,52,57,60],[41,48,53,57],[41,48,53,57],[48,55,60,64],[48,55,60,64],[43,50,55,62],[43,50,55,62]],
 [64,60,60,57,60,64,62,59]),
compose(3,'TitleInTheLight',66,
 [[41,48,53,57],[41,48,53,57],[48,55,60,64],[48,55,60,64],[43,50,55,59],[43,50,55,59],[48,55,60,64],[48,55,60,64]],
 [65,69,67,64,62,67,64,60])]
(OUT/'music-manifest.json').write_text(json.dumps(report,indent=2))
(REVIEW/'audio-check.json').write_text(json.dumps(report,indent=2))
