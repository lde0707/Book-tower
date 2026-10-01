"""Build the document index and portable Markdown bundle from project files.

Does not package caches, credentials, arbitrary home files or the raw rollout.
The dated visible-conversation export is made separately.
"""
import hashlib
import json
import pathlib
import re
import subprocess
import zipfile
from urllib.parse import quote, unquote

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Docs/Handoff'


def tracked_and_new():
    raw = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], cwd=ROOT)
    return sorted({p for p in raw.decode('utf-8').split('\0') if p and (ROOT / p).is_file()})


def portable_links(text, source):
    # The combined upload file uses repository-root-relative paths, stated in its header.
    def replace(m):
        target = m.group(1).strip('<>')
        if target.startswith(('#', 'http:', 'https:', 'app:', 'codex:')) or re.match(r'^[A-Za-z]:', target):
            return m.group(0)
        path, sep, fragment = target.partition('#')
        resolved = (source.parent / unquote(path)).resolve()
        try:
            rel = resolved.relative_to(ROOT).as_posix()
        except ValueError:
            return m.group(0)
        return '](' + quote(rel, safe='/._-') + (sep + fragment if sep else '') + ')'
    return re.sub(r'\]\(([^\n)]+)\)', replace, text)


def main():
    OUT.mkdir(exist_ok=True)
    generated = {'Docs/Handoff/05_전체_MD_인덱스.md', 'Docs/Handoff/CHATGPT_GAME_CONTEXT.md'}
    docs = [p for p in tracked_and_new() if p.lower().endswith('.md') and p not in generated]
    index = ['# 전체 Markdown 문서 인덱스', '', '2026-10-02. Git에 포함되는 프로젝트 문서와 이번 인계 문서를 경로별로 모았다. Unity 캐시·검증 복제본·다른 대화는 제외한다. 이 목록의 과거 보고서는 당시 구현을 설명하므로 최신 상태와 구분한다.', '', '## 읽는 순서', '', 'START_HERE → Handoff/01~03 → AGENTS → Docs/37·38 → 최신 Atmosphere/README → Docs/27 → 작업별 번호 문서/검증 기록. 과거 원문은 Handoff/Conversation에 있다.', '', f'## 원본 문서 {len(docs)}개', '', '| 경로 | 첫 제목 | 크기(bytes) |', '|---|---|---:|']
    hashes = []
    for rel in docs:
        p = ROOT / rel
        data = p.read_bytes()
        txt = data.decode('utf-8-sig', errors='replace')
        title = next((line.lstrip('#').strip() for line in txt.splitlines() if line.startswith('#')), p.name).replace('|', '\\|')
        from_handoff = pathlib.PurePosixPath('../../') / rel
        index.append(f'| [{rel}]({quote(str(from_handoff), safe="/._-")}) | {title} | {len(data)} |')
        hashes.append({'path': rel, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
    (OUT / '05_전체_MD_인덱스.md').write_text('\n'.join(index) + '\n', encoding='utf-8')
    context_sources = ['START_HERE.md', 'AGENTS.md'] + [f'Docs/Handoff/{name}' for name in ('01_게임방향과_전체변경이력.md', '02_현재구현과_검증_기술인계.md', '03_노트북에서_이어하기.md')]
    context_sources += [p for p in docs if re.match(r'^Docs/\d\d_', p)]
    context_sources += ['Docs/ArtReview/Atmosphere/README.md', 'Docs/ArtReview/ReleaseArt/08-Final/README.md']
    combined = ['# A Fold of Light · ChatGPT용 통합 작업 맥락', '', '2026-10-02 스냅샷. 가장 앞의 인계 01~03이 최신 상태의 안내다. 뒤의 번호 문서는 역사와 설계를 포함하므로 작성 시점/구현 여부를 구분한다. 이 통합본은 읽기용이며 수정 원본은 각 SOURCE 경로다. 아래 링크의 상대 경로는 저장소 루트 기준으로 재표기했으므로 파일 첨부만 받은 ChatGPT에서는 경로 지도처럼 사용한다.', '', '공개 대화 전문은 별도 Conversation 날짜 파일 7개에 있으며 본문을 새로 요약한 인계와 구별한다. 게임 코드/3D 에셋/음원은 이 텍스트 안에 들어 있지 않다.', '']
    for rel in context_sources:
        combined += ['\n---\n', f'## SOURCE: {rel}', '', portable_links((ROOT / rel).read_text(encoding='utf-8-sig'), ROOT / rel), '']
    (OUT / 'CHATGPT_GAME_CONTEXT.md').write_text('\n'.join(combined), encoding='utf-8')
    meta = {'snapshot_date': '2026-10-02', 'source_markdown_count': len(docs), 'combined_sources': context_sources, 'source_documents': hashes, 'archive_scope': 'All Git-visible original project Markdown + handoff index + combined context, no binary game media. Dates in original documents are preserved.'}
    (OUT / 'document-manifest.json').write_text(json.dumps(meta, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    packed = docs + sorted(generated) + ['Docs/Handoff/document-manifest.json', 'Docs/Handoff/conversation-manifest.json']
    with zipfile.ZipFile(OUT / 'ChatGPT_Documents.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for rel in packed:
            info = zipfile.ZipInfo(rel, (2026, 10, 2, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, (ROOT / rel).read_bytes())
    with zipfile.ZipFile(OUT / 'ChatGPT_Documents.zip') as z:
        assert z.testzip() is None
        assert len(z.namelist()) == len(set(z.namelist())) == len(packed)
    print(json.dumps({'source_md': len(docs), 'zip_entries': len(packed), 'zip_bytes': (OUT / 'ChatGPT_Documents.zip').stat().st_size, 'combined_bytes': (OUT / 'CHATGPT_GAME_CONTEXT.md').stat().st_size}, ensure_ascii=False))


if __name__ == '__main__':
    main()
