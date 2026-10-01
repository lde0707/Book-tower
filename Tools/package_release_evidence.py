"""Copy selected Unity QA evidence; retain raw frame sequences inside Logs."""
from pathlib import Path
import json
import re
import shutil

root = Path(__file__).resolve().parents[1]
clone = root / 'Logs/F0ArtValidation'
source = clone / 'Docs/ArtReview/ReleaseArt/08-Final'
target = root / 'Docs/ArtReview/ReleaseArt/08-Final'

def copy(src, dst):
    if not src.is_file():
        raise FileNotFoundError(src)
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)

for file in source.iterdir():
    if file.is_file() and file.suffix in {'.png', '.jpg', '.txt'}:
        copy(file, target/file.name)
for directory in ('Standalone', 'EntranceBefore'):
    for file in (source/directory).iterdir():
        if file.is_file() and file.suffix in {'.png', '.gif', '.txt'}:
            copy(file, target/directory/file.name)
copy(source/'Motion/sixty-second-observation.gif', target/'Motion/sixty-second-observation.gif')

checks = {
 'ShadowDuet.txt':'Docs/ArtReview/ShadowDuet/play-check.txt',
 'ReadingRoom1B.txt':'Docs/ArtReview/ReadingRoom1B/play-check.txt',
 'ReadingEntrance.txt':'Docs/ArtReview/ReadingEntrance/play-check.txt',
 'F0.txt':'Docs/ArtReview/InteriorRoomsV3/F0-play-verification.txt',
 'FoyerTransition.txt':'Logs/foyer-transition-play-check.txt',
 'OpeningStory.txt':'Docs/ArtReview/Cutscene2D/Play/play-check.txt',
 'RunSettings.txt':'Docs/ArtReview/RunSettings/checks.txt',
 'ReadingMusic.txt':'Docs/ArtReview/ReadingMusic/checks.txt',
 'ReadingStructure.txt':'Docs/ArtReview/ReleaseArt/03-Lighting/1B-structure.txt',
 'FoyerTraversal.txt':'Docs/ArtReview/ReleaseArt/F0/01-Structure/Traversal/checks.txt',
}
for name, path in checks.items():
    copy(clone/path, target/'Regression'/name)
for file in (clone/'Docs/ArtReview/ReleaseArt/03-Lighting/PuzzleQualities').iterdir():
    if file.is_file():
        copy(file, target/'ProjectionQualities'/file.name)
for file in (root/'Logs').glob('release-finish-*.json'):
    copy(file, target/'Regression/run-records'/file.name)

report = (source/'Standalone/standalone-report.txt').read_text(encoding='utf-8-sig')
rows = [line.split(',') for line in report.splitlines() if re.match(r'^(F0|1A|1B),[012],', line)]
if len(rows) != 9 or 'PASS: standalone sequence completed' not in report:
    raise RuntimeError('Standalone measurement did not complete all nine samples')
text = '''# 최종 Windows 빌드 측정

Unity 6000.3.10f1, Windows64 Mono, non-development, 1920×1080, Direct3D12. Ryzen 5 7500F, RTX 4060 Ti 8GB, RAM 약 32GB. 이 PC의 결과이며 최소 사양 판정은 아니다.

기본 품질은 방마다 5초 워밍업 후 60초, 낮음·높음은 각각 15초다. 실제 CharacterController와 PaperCamera로 안전한 작은 이동 경로와 360도 시선을 반복했다. 측정 중에는 스크린샷을 찍지 않았다. 문 개방·방 전환을 포함한 전체 게임의 연속 성능 시험은 아니며, 기능은 별도 플레이 회귀 검사에서 확인했다.

| 방 | 품질 | 표본 프레임 | 평균 ms | p95 ms | p99 ms | 최대 ms | CPU 평균 ms | GPU 평균 ms | 평균 Draw Calls | 평균 삼각형 | 사용 메모리 MiB |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
'''
for r in rows:
    values=[r[0], {'0':'낮음','1':'기본','2':'높음'}[r[1]],r[2],r[3],r[5],r[6],r[7],r[8],r[9],r[10],r[11],r[12]]
    text+='| '+' | '.join(values)+' |\n'
text+='''
60fps의 프레임 예산은 약 16.7ms다. 평균과 p95가 이를 밑돌더라도 최대 지연까지 없어졌다는 뜻은 아니다. CPU와 GPU 시간은 더하지 않는다. 삼각형·Draw Calls는 렌더 패스가 집계한 프레임 수치이며, 고유 메시나 에셋 수와 다르다. 사용 메모리는 ProfilerRecorder의 Total Used Memory 최고값으로 프로세스 작업 집합과 다르다.

- [원본 측정](Standalone/standalone-report.txt)
- [각 구간의 실제 해상도·VSync·상한·GC 횟수](Standalone/measurement-config.txt)
- [33.33ms를 넘은 프레임](Standalone/slow-frames.txt)

GC 횟수는 구간 단위 관찰이다. 그것만으로 특정 프레임 지연의 원인을 확정하지 않는다. 이전 입구 마감 전 측정의 1A에는 250.89ms 지연이 한 차례 있었다. 이 이력을 삭제하거나 최종 p95만으로 무시하지 않는다. 이전 원본은 검증 복제본의 `Logs/F0ArtValidation/Docs/ArtReview/ReleaseArt/08-Final/StandaloneBeforeSeam/standalone-report.txt`에 보존했다.
'''
(target/'performance.md').write_text(text, encoding='utf-8')
latest={}
for name in ('regressions','postgrade','review-final','coverage','entrance-repair','final-seal','folio-contact'):
    path=root/f'Logs/release-finish-{name}.json'
    if path.exists():
        records=json.loads(path.read_text(encoding='utf-8-sig'))
        for record in records if isinstance(records,list) else [records]:
            latest[record['method']]=record
(target/'Regression/latest-results.json').write_text(json.dumps(list(latest.values()),ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Copied selected evidence and {len(rows)} performance samples to {target}')
