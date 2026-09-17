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

    배열 초기화(new[] { a, b })의 중괄호도 마찬가지다. 이걸 안 세면
    인자 2개짜리 호출을 3개로 읽고 없는 오류를 만들어 낸다.
    """
    text = text.strip()
    if not text:
        return []
    out, depth, cur = [], 0, ""
    for ch in text:
        if ch in "<([{":
            depth += 1
        elif ch in ">)]}":
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


# MonoBehaviour · Component 에서 물려받아 선언 없이 쓰는 것들
INHERITED = {
    "transform", "gameObject", "name", "tag", "enabled", "isActiveAndEnabled",
    "hideFlags", "useGUILayout", "runInEditMode", "rigidbody", "collider",
    "renderer", "audio", "camera", "light", "animation", "hasStarted",
    "didStart", "didAwake", "destroyCancellationToken",
}

KEYWORDS = {
    "if","else","for","foreach","while","do","switch","case","default","break","continue",
    "return","new","this","base","null","true","false","var","in","out","ref","is","as",
    "using","namespace","class","struct","enum","interface","public","private","protected",
    "internal","static","readonly","const","void","get","set","value","try","catch","finally",
    "throw","lock","yield","where","when","params","override","virtual","abstract","sealed",
    "partial","operator","implicit","explicit","string","int","float","bool","double","long",
    "short","byte","char","object","decimal","uint","ulong","sizeof","typeof","nameof","goto",
    "checked","unchecked","unsafe","fixed","stackalloc","delegate","event","add","remove",
    "global","dynamic","async","await","from","select","let","orderby","group","into","join",
    "on","equals","by","ascending","descending","record","init","with","and","or","not",
}


def declared_names(code):
    """이 파일 안에서 선언된 이름을 전부 모은다."""
    d = set()
    PRIM = r'var|bool|int|float|double|long|short|byte|char|string|object|decimal|uint|ulong|sbyte'

    # 필드 · 프로퍼티
    d |= set(re.findall(r'(?:private|public|protected|internal)\s+(?:static\s+)?(?:readonly\s+)?'
                        r'(?:const\s+)?[\w<>\[\],\.\?]+\s+(\w+)\s*(?:[;=]|=>|\{)', code))
    # 지역 변수. 기본형은 소문자로 시작하므로 따로 적어 줘야 한다 —
    # 이게 빠지면 bool hide = ... 같은 흔한 선언을 전부 놓친다
    d |= set(re.findall(r'\b(?:' + PRIM + r'|[A-Z][\w<>\[\],\.\?]*)\s+(\w+)\s*=', code))
    # for (int i = 0; ...) 와 튜플 분해 var (a, b) = ...
    d |= set(re.findall(r'for\s*\(\s*(?:' + PRIM + r'|[A-Z][\w<>]*)\s+(\w+)', code))
    for m in re.finditer(r'\b(?:var|\([\w\s,]+\))\s*\(([^()]*)\)\s*=', code):
        d |= {x.strip().split()[-1] for x in m.group(1).split(",") if x.strip()}
    # foreach · out · 패턴
    d |= set(re.findall(r'foreach\s*\(\s*[\w<>\[\],\.\?]+\s+(\w+)\s+in', code))
    d |= set(re.findall(r'\bout\s+(?:var\s+|[\w<>\[\],\.\?]+\s+)(\w+)', code))
    # 메서드 · 람다 인자
    for m in re.finditer(r'\(([^()]*)\)', code):
        for part in split_args(m.group(1)):
            w = part.split("=")[0].strip().split()
            if len(w) >= 2 and re.fullmatch(r'\w+', w[-1]):
                d.add(w[-1])
    d |= set(re.findall(r'(\w+)\s*=>', code))
    # (a, b) => ... 처럼 괄호로 묶인 람다 인자
    for m in re.finditer(r'\(([\w\s,]+)\)\s*=>', code):
        d |= {x.strip() for x in m.group(1).split(",") if x.strip()}

    # 한 줄에 여러 개를 선언하는 경우.
    #   private GUIStyle box, small, big;
    #   Vector3 min = roomBounds.min, max = roomBounds.max;
    # 첫 이름만 잡으면 나머지가 '선언 안 된 이름'으로 잘못 걸린다.
    for m in re.finditer(r'\b(?:' + PRIM + r'|[A-Z][\w<>\[\],\.\?]*)\s+([^;{}()]+);', code):
        for part in m.group(1).split(","):
            w = part.strip().split("=")[0].strip()
            if re.fullmatch(r'\w+', w):
                d.add(w)
    return d


def undeclared_uses(code, short):
    """선언된 적 없는 이름을 값으로 쓰는 곳을 찾는다."""
    d = declared_names(code)
    errs = []
    reported = set()
    # 대입(=) 이 아니라 **멤버 접근(.)** 만 본다.
    #   · 대입은 객체 초기화 구문 new GUIStyle { padding = ... } 과 구분이 안 되고,
    #     한 줄에 여러 개를 선언하는 float a = 1, b = 2; 도 놓치기 쉽다
    #   · 반면 xxx.Foo() 는 xxx 가 반드시 존재해야 하므로 오탐이 거의 없다
    #   · 선언이 통째로 빠진 경우엔 대개 멤버 접근도 같이 있으므로 이걸로 충분하다
    for m in re.finditer(r'(?:^|[^.\w"])([a-z_]\w*)\s*\.', code):
        name = m.group(1)
        if name in d or name in KEYWORDS or name in INHERITED or name in reported:
            continue
        reported.add(name)
        ln = code[:m.start()].count("\n") + 1
        errs.append(f"{short}:{ln}: '{name}' 이 이 파일 어디에도 선언돼 있지 않다 (CS0103)")
    return errs


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

        # 같은 using 을 두 번 쓰면 경고가 뜬다 (CS0105)
        usings, dup = set(), set()
        for i, ln in enumerate(raw.split("\n")):
            t = ln.strip()
            if t.startswith("using ") and t.endswith(";"):
                if t in usings and t not in dup:
                    dup.add(t)
                    errs.append(f"{short}:{i+1}: {t} 가 중복이다 (CS0105)")
                usings.add(t)

        errs.extend(undeclared_uses(code, short))

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
