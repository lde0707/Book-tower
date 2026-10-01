"""Export this project's visible conversation, never agent internals or credentials.

Usage: python Tools/export_visible_handoff.py --rollout <jsonl> --cutoff <UTC ISO>
Only user text and assistant commentary/final messages are accepted. The raw
rollout stays local. Embedded user reference images and explicitly linked local
attachments are copied, with a manifest; unavailable media is reported honestly.
"""
import argparse
import base64
import collections
import hashlib
import json
import pathlib
import re
import shutil
from urllib.parse import unquote
from datetime import datetime, timezone, timedelta

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Docs/Handoff'


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--rollout', type=pathlib.Path, required=True)
    ap.add_argument('--cutoff', required=True)
    args = ap.parse_args()
    OUT.mkdir(exist_ok=True)
    archive = OUT / 'Conversation'
    media = OUT / 'Attachments'
    archive.mkdir(exist_ok=True)
    media.mkdir(exist_ok=True)
    records, attachments, missing, seen = [], {}, set(), set()
    discarded = collections.Counter()
    allowed = {'.png', '.jpg', '.jpeg', '.webp', '.gif', '.mp4', '.wav', '.mp3', '.txt', '.md', '.pdf'}
    automation_tags = ('<environment_context>', '<recommended_plugins>', '<app-context>', '<permissions', '<collaboration', '<turn_aborted>', '# AGENTS.md instructions')

    def preserve(data, ext, source):
        digest = hashlib.sha256(data).hexdigest()
        target = media / (digest[:20] + ext.lower())
        if not target.exists():
            target.write_bytes(data)
        item = attachments.setdefault(digest, {'file': target.relative_to(ROOT).as_posix(), 'sha256': digest, 'bytes': len(data), 'sources': []})
        if source not in item['sources']:
            item['sources'].append(source)
        return '../Attachments/' + target.name

    for line in args.rollout.open(encoding='utf-8'):
        if '"response_item"' not in line:
            continue
        obj = json.loads(line)
        if obj.get('timestamp', '') > args.cutoff:
            continue
        q = obj.get('payload', {})
        if q.get('type') != 'message' or q.get('role') not in ('user', 'assistant'):
            continue
        role = q['role']
        phase = q.get('phase')
        if role == 'assistant' and phase not in ('commentary', 'final_answer', 'final'):
            discarded['non_visible_assistant'] += 1
            continue
        identity = q.get('id')
        if identity and identity in seen:
            continue
        if identity:
            seen.add(identity)
        texts, images = [], []
        for c in q.get('content', []):
            if c.get('type') in ('text', 'input_text', 'output_text'):
                t = c.get('text', '').strip()
                if t.startswith(automation_tags):
                    discarded['environment_blocks'] += 1
                    continue
                if t:
                    texts.append(t)
            elif role == 'user' and c.get('type') == 'input_image':
                url = c.get('image_url', '')
                m = re.fullmatch(r'data:image/(png|jpeg|webp);base64,(.*)', url, re.S)
                if m:
                    images.append(preserve(base64.b64decode(m[2]), '.' + m[1], 'embedded-user-reference:' + str(identity)))
        if not texts and not images:
            continue
        original = '\n\n'.join(texts)
        # Preserve explicitly referenced local files only, never enumerate a home directory.
        paths = set(re.findall(r'(?<![A-Za-z])[A-Za-z]:[/\\](?![/\\])[^\n<>"|\[\]()]*?\.(?:png|jpe?g|webp|gif|mp4|wav|mp3|txt|md|pdf)', original, re.I))
        links = []
        for raw in sorted(paths):
            path = pathlib.Path(unquote(raw).replace('\\', '/'))
            if not path.is_file():
                missing.add(path.as_posix())
                continue
            if path.suffix.lower() not in allowed:
                continue
            try:
                rel = path.resolve().relative_to(ROOT)
                if rel.parts[0] not in ('Assets', 'Docs', 'ArtSource', 'Tools'):
                    continue
                links.append((raw, '../../../' + rel.as_posix()))
            except ValueError:
                # Original user clipboard/attachments or generated visuals explicitly linked in this chat.
                norm = path.as_posix().lower()
                if not any(part in norm for part in ('/temp/codex-clipboard-', '/.codex/attachments/', '/.codex/generated_images/', '/.codex/visualizations/')):
                    continue
                links.append((raw, preserve(path.read_bytes(), path.suffix, raw)))
        records.append({'time': obj.get('timestamp'), 'role': role, 'phase': phase, 'text': original, 'images': images, 'links': links})

    days = collections.defaultdict(list)
    for i, r in enumerate(records, 1):
        local = datetime.fromisoformat(r['time'].replace('Z', '+00:00')).astimezone(timezone(timedelta(hours=9)))
        r['number'] = i
        r['local_time'] = local.isoformat(timespec='seconds')
        days[local.strftime('%Y-%m-%d')].append(r)
    for day, rs in days.items():
        body = [f'# 대화 기록 {day} (한국 시간)', '', '> 당시 사용자 요청과 공개 답변/진행 안내의 기록이다. 과거 완료 보고보다 최신 구현 문서와 현재 코드를 우선한다. 원문의 로컬 경로는 보존하며, 가능한 경우 아래에 휴대 가능한 파일 링크를 추가했다.', '']
        for r in rs:
            who = '사용자' if r['role'] == 'user' else ('Codex 답변' if r['phase'] in ('final', 'final_answer') else 'Codex 진행 안내')
            body += [f"## {r['number']:04d} · {r['local_time']} · {who}", '', r['text'], '']
            body += [f'![첨부 이미지 {n}]({link})\n' for n, link in enumerate(r['images'], 1)]
            if r['links']:
                body += ['보존된 참조 파일:', '']
                body += [f'- [{pathlib.PureWindowsPath(raw).name}]({link})' for raw, link in r['links']]
                body += ['']
        (archive / (day + '.md')).write_text('\n'.join(body), encoding='utf-8')
    counts = collections.Counter(r['role'] for r in records)
    meta = {'cutoff_utc': args.cutoff, 'messages': len(records), 'user_messages': counts['user'], 'assistant_messages': counts['assistant'], 'days': {d: len(rs) for d, rs in days.items()}, 'excluded': dict(discarded), 'attachments': list(attachments.values()), 'missing_original_paths': sorted(missing), 'scope': 'Visible response_item user input_text + assistant commentary/final only; not system/developer/reasoning/tool payloads. Inline generated media outside those records is not guaranteed. Original local links retained.'}
    (OUT / 'conversation-manifest.json').write_text(json.dumps(meta, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    overview = ['# 대화 원문 인덱스', '', f"스냅샷: {args.cutoff}까지. 사용자 {counts['user']}개, Codex 공개 답변·진행 안내 {counts['assistant']}개, 총 {len(records)}개 메시지.", '', '사용자 요청에 따라 현재 한 대화만 내보냈다. 시스템 설정, 내부 추론, 도구 원시 응답, 로그인 정보, 다른 대화는 포함하지 않았다. 원문을 새로 창작한 요약이 아니다. 첨부 이미지와 외부 생성 도구의 미디어는 텍스트만으로 전부 복원할 수 없어 보존 여부를 별도로 기록한다.', '', '## 날짜별 전문', '']
    overview += [f'- [{d} · {len(rs)}개 메시지](Conversation/{d}.md)' for d, rs in days.items()]
    overview += ['', '## 이미지·참조 파일', '', f'고유 첨부 파일 {len(attachments)}개를 Attachments에 보존했다. 저장소 안에 있는 기존 자료는 중복 복사하지 않고 연결했다. 삭제되었거나 이동한 옛 로컬 경로 {len(missing)}개는 conversation-manifest.json에 남겨 두었다.', '', '과거 답변의 절대 경로·인용 표시는 당시 원문 그대로다. 노트북에서는 각 메시지 아래의 보존 링크와 문서 인덱스를 우선 사용한다. 생성 도구 내부에만 남았던 미디어는 모두 포함되었다고 보장하지 않는다. 현재 적용 모델/재질/컷씬은 Assets, 원화·제작 파일은 ArtSource, 기존 시안·검증 화면은 Docs/ArtReview에 있다.', '', '## 원문 보존 범위', '', '이 기록은 ChatGPT 대화 목록에 자동 등록되는 파일이 아니다. Git으로 내려받아 Markdown으로 열거나 ChatGPT에 첨부한다. 기획 변경의 순서를 찾는 자료이며, 최신 설계와 적용 상태는 01~03 인계 문서를 우선한다.', '']
    (OUT / '04_대화원문_인덱스.md').write_text('\n'.join(overview), encoding='utf-8')
    print(json.dumps({k: meta[k] for k in ('messages', 'user_messages', 'assistant_messages', 'days', 'excluded')}, ensure_ascii=False))
    print('preserved_attachments', len(attachments), 'missing_old_paths', len(missing))


if __name__ == '__main__':
    main()
