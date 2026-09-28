from pathlib import Path
import re
R=Path(__file__).resolve().parents[1]
p=(R/'Assets/_BookTower/Scripts/Story/OpeningStoryData.cs').read_text(encoding='utf-8')
part=p.split('Promise => new[] {')[1].split('},')[0]
rows=re.findall(r'new StoryBeat\(StoryShot\.(\w+), "([^"]+)", "([^"]+)"\)',part)
text='# 도입 대사 최신본\n\n상황 → 장소 → 기억 공백 → 새의 귀갓길 → 동행의 이유 → 첫 조사 목표 순서. 2D 컷씬의 F/좌클릭 진행 규칙은 유지합니다. 새의 이름과 정령의 출생 비밀은 이후 장에 남깁니다.\n\n'
text+='\n\n'.join(str(i+1)+'. **'+sp+'** ('+shot+'): '+line.replace('\\n',' / ') for i,(shot,sp,line) in enumerate(rows))
(R/'Docs/ArtReview/MoaModelViewsV1/UnityV3/OpeningDialogue.md').write_text(text+'\n',encoding='utf-8')
assert len(rows)==16
print('16 dialogue beats saved')
