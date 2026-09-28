from pathlib import Path
import re,uuid,wave,json
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/_BookTower/Audio/RegionMusic'
catalog=root/'Assets/_BookTower/Resources/RegionMusic.asset'
text=catalog.read_text(encoding='utf-8')
names=['Region1_PaperLanterns','Region2_FadedMemories','Region3_TitleInTheLight']
guids=[]
for name in names:
    original=folder/(name+'.wav.meta');source=original.read_text()
    path=folder/(name+'_PianoViolinV3.wav');meta=Path(str(path)+'.meta')
    with wave.open(str(path)) as w:
        assert w.getnchannels()==2 and 50<w.getnframes()/w.getframerate()<65
    if not meta.exists():meta.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,source))
    guids.append(re.search(r'guid: ([a-f0-9]+)',meta.read_text()).group(1))
start=text.index('  clips:');end=text.index('  volume:',start)
text=text[:start]+'  clips:\n'+''.join('  - {fileID: 8300000, guid: '+g+', type: 3}\n' for g in guids)+text[end:]
catalog.write_text(text,encoding='utf-8')
print('PASS: three valid stereo WAVs linked to regional music library.')
