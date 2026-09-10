# -*- coding: utf-8 -*-
"""유니티를 켜지 않고 잡을 수 있는 C# 실수만 검사한다.

    python3 Tools/cs_lint.py

컴파일러를 대신하려는 게 아니다. 유니티를 켜야만 드러나는 오류 때문에
Safe Mode 로 들어가 작업이 끊기는 일이 반복돼서, 그중 정적으로 잡히는
몇 가지만 미리 거른다. 실제로 이 프로젝트에서 났던 오류들이다.

  CS0579  같은 속성이 두 번 붙음 — 필드를 끼워 넣다가 기존 Tooltip 을 밀어냈다
  CS0104  Object 가 System 과 UnityEngine 양쪽에 있어 모호함
  CS1501  같은 파일 안 static 메서드를 인자 개수가 안 맞게 부름
          — 시그니처를 고치면서 호출부만, 혹은 호출부만 고쳤을 때 난다
  괄호    편집 중 짝이 깨짐
"""
import io, re, glob, sys

ROOT = "Assets/_BookTower"
ONE_SHOT = {"Tooltip", "Header", "Space", "Range", "TextArea",
            "SerializeField", "HideInInspector"}


def split_args(text):
    """괄호 안의 인자를 최상위 쉼표로만 나눈다.

    Dictionary<string, FoldPanel> 처럼 타입 안에 쉼표가 있는 경우가 있어서
    그냥 split(",") 하면 인자 수를 잘못 센다.
    """
    text = text.strip()
    if not text:
        return []
    out, depth, cur = [], 0, ""
    for ch in text:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur); cur = ""
        else:
            cur += ch
    out.append(cur)
    return [x.strip() for x in out if x.strip()]


def strip_code(t):
    t = re.sub(r'/\*.*?\*/', ' ', t, flags=re.S)
    t = re.sub(r'//[^\n]*', ' ', t)
    return re.sub(r'"(\\.|[^"\\])*"', '""', t)


def main():
    errs = []
    for f in sorted(glob.glob(ROOT + "/**/*.cs", recursive=True)):
        raw = io.open(f, encoding="utf-8").read()
        code = strip_code(raw)
        short = f.replace(ROOT + "/", "")

        if code.count("{") != code.count("}"):
            errs.append(f"{short}: 중괄호 짝이 안 맞는다 ({code.count('{')} vs {code.count('}')})")
        if code.count("(") != code.count(")"):
            errs.append(f"{short}: 소괄호 짝이 안 맞는다")

        # 한 선언에 같은 속성이 두 번 — 필드 사이에 끼워 넣다가 잘 생긴다
        lines = raw.split("\n")
        seen, start = {}, 0
        for i, ln in enumerate(lines):
            names = re.findall(r'\[\s*(\w+)\s*[\(\]]', ln)
            for n in names:
                if n in ONE_SHOT:
                    if n in seen:
                        errs.append(f"{short}:{i+1}: '{n}' 속성이 {seen[n]}번 줄과 겹친다 "
                                    f"— 선언 하나에 두 번은 못 붙는다 (CS0579)")
                    else:
                        seen[n] = i + 1
            # 선언이 끝나면(세미콜론·중괄호) 누적을 비운다
            if ";" in ln or "{" in ln or ln.strip() == "":
                seen = {}

        # using System 과 using UnityEngine 을 같이 쓰면 맨 Object 가 모호해진다
        if re.search(r'^using System;', code, re.M) and re.search(r'^using UnityEngine;', code, re.M):
            for m in re.finditer(r'(^|[^.\w])Object\s*\.', code):
                ln = code[:m.start()].count("\n") + 1
                errs.append(f"{short}:{ln}: 맨 'Object.' 은 System 과 UnityEngine 사이에서 "
                            f"모호하다 (CS0104). UnityEngine.Object 로 쓸 것")

        # 같은 파일 안에서 정의한 static 메서드를 인자 개수가 안 맞게 부르는가.
        # 이름을 조금 바꾸거나 인자를 하나 추가할 때 한쪽만 고치기 쉽다.
        defs = {}
        for m in re.finditer(r'(?:private|public|internal)\s+static\s+[\w<>\[\],\.]+\s+(\w+)\s*\(([^)]*)\)\s*\{', code):
            name, params = m.group(1), m.group(2).strip()
            parts = split_args(params)
            hi = len(parts)
            lo = len([x for x in parts if "=" not in x])
            defs[name] = (lo, hi)

        for name, (lo, hi) in defs.items():
            for m in re.finditer(r'(^|[^.\w])' + name + r'\s*\(([^();]*)\)', code):
                argtext = m.group(2).strip()
                if "=>" in argtext:
                    continue
                n = len(split_args(argtext))
                if n < lo or n > hi:
                    ln = code[:m.start()].count("\n") + 1
                    errs.append(f"{short}:{ln}: {name}(...) 를 인자 {n}개로 부르는데 "
                                f"정의는 {lo}~{hi}개다 (CS1501)")

    print(f"검사 파일 {len(glob.glob(ROOT + '/**/*.cs', recursive=True))}개")
    print(f"\n문제 {len(errs)}건")
    for e in errs:
        print("  ✗", e)
    return len(errs)


if __name__ == "__main__":
    sys.exit(1 if main() else 0)
