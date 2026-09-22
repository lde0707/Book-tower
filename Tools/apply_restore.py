# -*- coding: utf-8 -*-
"""복구 키트 적용기 — 원래 컴퓨터를 잃어서 사라진 작업을 다시 얹는다.

    python Tools/apply_restore.py          # 무엇을 할지 보고만 한다
    python Tools/apply_restore.py --apply  # 실제로 고친다

왜 스크립트인가
  사라진 커밋 8개 중 새로 만든 파일은 키트에 통째로 들어 있어 덮어쓰면 끝이다.
  하지만 **원래 있던 파일을 고친 것**(RoomManager, PrototypeSceneBuilder,
  manifest)은 통째로 넣을 수 없다 — 이 컴퓨터의 저장소가 어느 시점인지 모르고,
  그 사이 도언이 직접 고친 게 있으면 덮어써서 날아간다.
  그래서 "이 줄 뒤에 이걸 넣는다" 식으로 **찾아서 고친다.** 못 찾으면 고치지
  않고 이유를 알려 준다. 조용히 넘어가는 일은 없다.

  고치기 전 원본은 _restore_backup/ 에 둔다 (Assets 밖이라 유니티가 안 읽는다).
  두 번 돌려도 안전하다 — 이미 들어간 건 건너뛴다.
"""
import io
import json
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APPLY = "--apply" in sys.argv
BACKUP = os.path.join(ROOT, "_restore_backup")

P_ROOM = "Assets/_BookTower/Scripts/Rooms/RoomManager.cs"
P_ROOMBOUND = "Assets/_BookTower/Scripts/Rooms/RoomBound.cs"
P_SCENE = "Assets/_BookTower/Editor/PrototypeSceneBuilder.cs"
P_MANIFEST = "Packages/manifest.json"
P_OUTLINE = "Assets/_BookTower/Scripts/Presentation/PencilOutline.cs"
P_PLAYER = "Assets/_BookTower/Scripts/Player/PlayerController.cs"

report = []          # (상태, 파일, 설명)


def note(status, path, msg):
    report.append((status, path, msg))


def read(path):
    full = os.path.join(ROOT, path)
    if not os.path.exists(full):
        return None
    with io.open(full, encoding="utf-8-sig") as f:
        return f.read()


def write(path, text):
    full = os.path.join(ROOT, path)
    if not APPLY:
        return
    os.makedirs(BACKUP, exist_ok=True)
    bk = os.path.join(BACKUP, path.replace("/", "__"))
    if not os.path.exists(bk):
        shutil.copyfile(full, bk)
    # 줄바꿈은 원래 파일 것을 따른다 (Windows 에서 CRLF 가 섞이면 diff 가 통째로 바뀐다)
    with io.open(full, encoding="utf-8-sig", newline="") as f:
        orig = f.read()
    nl = "\r\n" if "\r\n" in orig else "\n"
    text = text.replace("\r\n", "\n").replace("\n", nl)
    with io.open(full, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def norm(s):
    return s.replace("\r\n", "\n")


# ------------------------------------------------------------------ 기준선 확인

def baseline():
    """이 저장소가 어느 시점인지 본다. 키트의 새 파일들이 기대는 것들이다."""
    ok = True
    if read(P_ROOMBOUND) is None:
        note("주의", P_ROOMBOUND,
             "없음 — '방을 닫는다' 커밋(a9feb89) 이전 저장소다. 경사로 컬링 수정은 건너뛴다.")
    if read(P_OUTLINE) is None:
        note("실패", P_OUTLINE, "없음 — PaperBirdBuilder 가 컴파일되지 않는다. 새 세션에서 확인 필요.")
        ok = False
    pc = read(P_PLAYER)
    if pc is None or "HorizontalVelocity" not in pc:
        note("실패", P_PLAYER,
             "HorizontalVelocity 가 없다 — GhostRig·PaperBirdRig 가 컴파일되지 않는다.")
        ok = False
    return ok


# ------------------------------------------------------------------ manifest

def fix_manifest():
    raw = read(P_MANIFEST)
    if raw is None:
        note("실패", P_MANIFEST, "파일이 없다")
        return
    d = json.loads(raw)
    dep = d.get("dependencies", {})
    cur = dep.get("com.unity.cloud.gltfast")
    if cur == "6.20.0":
        note("이미", P_MANIFEST, "glTFast 6.20.0 이 들어 있다")
        return
    # 순서를 지키며 넣는다 (collab-proxy 앞)
    new = {}
    placed = False
    for k, v in dep.items():
        if k == "com.unity.cloud.gltfast":
            continue
        if not placed and k > "com.unity.cloud.gltfast":
            new["com.unity.cloud.gltfast"] = "6.20.0"
            placed = True
        new[k] = v
    if not placed:
        new["com.unity.cloud.gltfast"] = "6.20.0"
    d["dependencies"] = new
    write(P_MANIFEST, json.dumps(d, indent=2, ensure_ascii=False) + "\n")
    note("수정", P_MANIFEST,
         "glTFast %s → 6.20.0 (유니티가 .glb 를 읽게 한다. 6.0.1 은 2020.3 빌드라 안 된다)"
         % (cur or "없음"))


# ------------------------------------------------------------------ RoomManager

SWITCH_ANCHOR = "            current.ApplyYaw(appliedYaw);\n"
SWITCH_ADD = '''
            // 경사로와 출구 잠금도 같이 바꿔 준다.
            //
            // 예전엔 이 줄이 없었다. ApplyBounds 는 Tab(전체 조망)을 켜고 끌 때만
            // 불렸고, 그래서 **게임을 켜자마자도, 방을 옮겨도 한 번도 안 불렸다.**
            // 경사로 14개 + 잠금 11개가 처음부터 끝까지 전부 그려지고 있었고,
            // 나선형 탑이라 그것들이 화면에서 전부 겹쳤다 —
            // "다른 구조물이 보인다" 는 지적의 정체다.
            //
            // 컬링을 켜는 지점은 여기 하나여야 한다. 방이 바뀌는 곳이 여기뿐이니
            // 여기에 붙여 두면 다시 빠질 일이 없다.
            ApplyBounds(false);
'''

START_ANCHOR = "            Switch(Pick());\n            UpdateTransit();\n        }\n"
WARN = '''            Switch(Pick());
            UpdateTransit();
            WarnUncovered();
        }

        /// <summary>
        /// 룸 컬링이 못 건드리는 렌더러가 남아 있으면 알려 준다.
        ///
        /// "다른 구조물이 보인다" 는 지적을 두 번 받았고, 두 번 다 원인이
        /// **컬링 대상이 아닌 오브젝트**였다. 한 번은 경사로에 RoomBound 가 없었고,
        /// 한 번은 RoomBound 는 붙었는데 그걸 켜고 끄는 줄이 없었다.
        ///
        /// 사람 눈으로 찾으면 또 놓친다. 씬을 켤 때 기계가 한 번 세게 한다.
        /// 배경과 플레이어는 원래 방에 안 속하므로 빼고 센다.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void WarnUncovered()
        {
            var loose = new System.Text.StringBuilder();
            int n = 0;

            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null) continue;
                if (r.GetComponentInParent<Room>() != null) continue;
                if (r.GetComponentInParent<RoomBound>() != null) continue;

                Transform top = r.transform;
                while (top.parent != null) top = top.parent;
                if (top.name == "Background" || top.name == "Player") continue;

                n++;
                if (n <= 8) loose.Append("\\n  · ").Append(top.name).Append(" / ").Append(r.name);
            }

            if (n > 0)
                Debug.LogWarning($"[RoomManager] 방에도 RoomBound 에도 안 걸린 렌더러 {n}개 — " +
                                 $"이건 어느 방에 있든 항상 그려집니다.{loose}");
        }
'''


def fix_room_manager():
    s = read(P_ROOM)
    if s is None:
        note("실패", P_ROOM, "파일이 없다")
        return
    s = norm(s)
    if read(P_ROOMBOUND) is None or "void ApplyBounds(" not in s:
        note("건너뜀", P_ROOM,
             "RoomBound/ApplyBounds 가 없는 옛 저장소 — 이 수정은 그 위에서만 의미가 있다")
        return
    changed = False

    # Switch 안에서만 찾는다
    i = s.find("private void Switch(")
    if i < 0:
        note("실패", P_ROOM, "Switch() 를 못 찾았다")
    else:
        body_end = s.find("\n        }\n", i)
        body = s[i:body_end]
        if "ApplyBounds(" in body:
            note("이미", P_ROOM, "Switch() 가 이미 ApplyBounds 를 부른다")
        elif SWITCH_ANCHOR in body:
            j = i + body.index(SWITCH_ANCHOR) + len(SWITCH_ANCHOR)
            s = s[:j] + SWITCH_ADD + s[j:]
            changed = True
            note("수정", P_ROOM, "Switch() 에서 ApplyBounds(false) — 다른 방 경사로 25개가 보이던 원인")
        else:
            note("실패", P_ROOM,
                 "Switch() 안에서 'current.ApplyYaw(appliedYaw);' 를 못 찾았다 — 손으로 넣어야 한다")

    if "WarnUncovered" in s:
        note("이미", P_ROOM, "WarnUncovered 가 있다")
    elif START_ANCHOR in s:
        s = s.replace(START_ANCHOR, WARN, 1)
        changed = True
        note("수정", P_ROOM, "시작할 때 컬링 안 걸린 렌더러를 경고")
    else:
        note("실패", P_ROOM, "Start() 끝을 못 찾아 WarnUncovered 를 못 넣었다")

    if changed:
        write(P_ROOM, s)


# ------------------------------------------------------------------ PrototypeSceneBuilder

CLEAR_OLD = re.compile(
    r"    private static void ClearGenerated\(Scene scene\)\n    \{\n"
    r"        foreach \(GameObject go in scene\.GetRootGameObjects\(\)\.ToList\(\)\)\n"
    r"            if \(GeneratedRoots\.Contains\(go\.name\)\)\n"
    r"                UnityEngine\.Object\.DestroyImmediate\(go\);\n    \}\n")

CLEAR_NEW = '''    private static bool IsGenerated(GameObject go)
        => go.GetComponent<BookTower.Core.GeneratedRoot>() != null
           || GeneratedRoots.Contains(go.name);

    private static void ClearGenerated(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects().ToList())
            if (IsGenerated(go))
                UnityEngine.Object.DestroyImmediate(go);
    }

    /// <summary>
    /// 이번에 새로 생긴 루트에 표식을 붙인다. 다음에 다시 지을 때 이것만 보고 지운다.
    ///
    /// 이름 목록만 쓰던 때는 목록에 없는 "Map (히든)" 이 매번 살아남아
    /// 열 개까지 쌓였다. 히든 구역이 Room 을 갖고 있어 화면엔 안 보였지만,
    /// 콜라이더와 조명은 그대로 열 벌이었다.
    /// </summary>
    private static void TagGeneratedRoots(Scene scene, HashSet<GameObject> preexisting)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (preexisting.Contains(go)) continue;
            if (go.GetComponent<BookTower.Core.GeneratedRoot>() == null)
                go.AddComponent<BookTower.Core.GeneratedRoot>();
        }
    }
'''

COLORS = '''    // 값은 종이 텍스처 팔레트(ihatecolors "Paper Texture": #F5F0E8 / #E0D8C8 /
    // #C0B8A8 / #4A4038)에서 가져왔다. 눈대중으로 찍던 회갈색보다
    // 노란기가 있어 종이로 읽힌다.
    private static readonly Color BirdPaper = Hex("#E0D8C8");   // 몸통
    private static readonly Color BirdHead = Hex("#E8E0D0");    // 고개 — 한 단계 밝게
    private static readonly Color BirdShade = Hex("#C9C0AE");   // 날개

    // 정령. 몸통은 T_Ghost_Ramp 가 색까지 맡는다(위 #F2EADB → 아래 #B9B8B2).
    private static readonly Color GhostEyeWhite = Hex("#F5F0E8");   // 눈알 — 몸보다 한 단계 밝게
    private static readonly Color GhostInk = Hex("#4A4038");        // 눈동자·입 — 같은 팔레트의 잉크색
'''

FLAG = '''    /// <summary>
    /// 플레이어를 정령(Quaternius Ghost, CC0)으로 쓸지, 접힌 종이 새로 쓸지.
    ///
    /// false 로 되돌리면 새가 그대로 돌아온다 — 새 코드는 지우지 않았다.
    /// 정령 쪽은 아직 외곽선이 없다(스킨드 메시라 껍질 방식이 안 먹는다).
    /// </summary>
    private const bool UseGhostPlayer = true;

'''

CALL = '''        // 플레이어 모습 — 접힌 종이 새냐, 정령이냐.
        //
        // 새는 전부 코드로 만든 것이라 팔레트·그림자·라이선스가 전부 맞춰져 있고,
        // 정령은 기성 CC0 에셋이라 본 애니메이션이 딸려 온다.
        // 둘을 다 살려 두고 이 값 하나로 고른다 — 한쪽을 지우면 되돌릴 수가 없다.
        if (UseGhostPlayer)
        {{
            // 역할별로 색을 가른다. 몸통 명암은 텍스처가 맡으므로 바탕색은 흰색.
            // 리본은 월드 전체의 포인트 색(크레파스 빨강)을 그대로 쓴다 —
            // 새에서 정령으로 바뀌어도 "책갈피" 는 이어져야 한다.
            GhostPlayerBuilder.Build(
                player.transform,
                MakeMaterial("M_Ghost_Body", Color.white, "T_Ghost_Ramp"),
                MakeMaterial("M_Ghost_EyeWhite", GhostEyeWhite, "T_Paper_Soft"),
                MakeMaterial("M_Ghost_Ink", GhostInk, "T_Paper_Soft"),
                {ribbon});
        }}
        else
        {{
            PaperBirdBuilder.Build(
                player.transform,
                MakeMaterial("M_Bird_Paper", BirdPaper, "T_Paper_Soft"),
                MakeMaterial("M_Bird_Head", BirdHead, "T_Paper_Soft"),
                MakeMaterial("M_Bird_Shade", BirdShade, "T_Paper_Soft"),
                {ribbon},
                {outline});
        }}'''


def split_args(argstr):
    out, depth, cur = [], 0, ""
    for ch in argstr:
        if ch in "([{":
            depth += 1
        elif ch in ")]}":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur.strip())
            cur = ""
        else:
            cur += ch
    if cur.strip():
        out.append(cur.strip())
    return out


def fix_scene_builder():
    s = read(P_SCENE)
    if s is None:
        note("실패", P_SCENE, "파일이 없다")
        return
    s = norm(s)
    changed = False

    # 1) using
    if "using System.Collections.Generic;" not in s:
        s = s.replace("using System.Linq;", "using System.Collections.Generic;\nusing System.Linq;", 1)
        changed = True

    # 2) 생성 루트 표식
    if "TagGeneratedRoots" in s:
        note("이미", P_SCENE, "생성 루트 표식이 들어 있다")
    elif CLEAR_OLD.search(s):
        s = CLEAR_OLD.sub(lambda m: CLEAR_NEW, s, count=1)
        a = "        ClearGenerated(scene);\n"
        if a in s:
            s = s.replace(a, a + "\n        // 여기 남아 있는 것들은 사람이 직접 넣은 것이다. 건드리지 않는다.\n"
                             "        var preexisting = new HashSet<GameObject>(scene.GetRootGameObjects());\n", 1)
        b = "        EditorSceneManager.MarkSceneDirty(scene);\n"
        if b in s:
            s = s.replace(b, "        TagGeneratedRoots(scene, preexisting);\n\n" + b, 1)
        changed = True
        note("수정", P_SCENE, "'Map (히든)' 이 다시 지을 때마다 쌓이던 것 — GeneratedRoot 표식으로")
    else:
        note("실패", P_SCENE, "ClearGenerated 모양이 예상과 달라 표식 수정을 못 했다")

    # 3) 색
    if "GhostInk" in s:
        note("이미", P_SCENE, "정령 색이 들어 있다")
    else:
        m = re.search(r"(?m)^    private static readonly Color BirdPaper = .*\n"
                      r"(?:^    private static readonly Color BirdHead = .*\n)?"
                      r"^    private static readonly Color BirdShade = .*\n", s)
        if m:
            s = s[:m.start()] + COLORS + s[m.end():]
            changed = True
            note("수정", P_SCENE, "새·정령 팔레트 (#E0D8C8 계열, 눈알 #F5F0E8, 잉크 #4A4038)")
        else:
            anchor = '    [MenuItem("Book Tower/InGame 씬 구성하기"'
            if anchor in s:
                s = s.replace(anchor, COLORS + "\n" + anchor, 1)
                changed = True
                note("수정", P_SCENE, "새·정령 팔레트를 새로 넣었다 (BirdPaper 가 원래 없었음)")
            else:
                note("실패", P_SCENE, "색을 넣을 자리를 못 찾았다")

    # 4) 스위치
    if "UseGhostPlayer" not in s:
        anchor = '    [MenuItem("Book Tower/InGame 씬 구성하기"'
        if anchor in s:
            s = s.replace(anchor, FLAG + anchor, 1)
            changed = True
        else:
            note("실패", P_SCENE, "UseGhostPlayer 를 넣을 자리([MenuItem ...])를 못 찾았다")

    # 5) 플레이어 모습 호출
    if "GhostPlayerBuilder.Build(" in s:
        note("이미", P_SCENE, "정령 빌더를 부르고 있다")
    else:
        m = re.search(r"(?s)        PaperBirdBuilder\.Build\((.*?)\);", s)
        if not m:
            note("실패", P_SCENE,
                 "PaperBirdBuilder.Build 호출이 없다 — 새를 붙인 커밋(f438b95) 이전 저장소다. "
                 "BuildPlayer 안에서 캡슐·부리를 만드는 부분을 이 호출로 바꿔야 한다")
        else:
            args = split_args(m.group(1))
            ribbon, outline = (args[-2], args[-1]) if len(args) >= 5 else ("ribbonMat", "matOutline")
            s = s[:m.start()] + CALL.format(ribbon=ribbon, outline=outline) + s[m.end():]
            changed = True
            note("수정", P_SCENE, "플레이어를 정령으로 (새는 UseGhostPlayer=false 로 돌아온다)")

    if changed:
        write(P_SCENE, s)


# ------------------------------------------------------------------ 실행

if __name__ == "__main__":
    base_ok = baseline()
    fix_manifest()
    fix_room_manager()
    fix_scene_builder()

    width = max(len(p) for _, p, _ in report) if report else 10
    print("\n" + ("[적용]" if APPLY else "[미리보기 — 아무것도 안 바꿨다. --apply 로 적용]") + "\n")
    for st, p, msg in report:
        print(f"  {st:4s}  {p:<{width}}  {msg}")
    bad = [r for r in report if r[0] == "실패"]
    print()
    if bad:
        print(f"실패 {len(bad)}건 — 위 설명대로 손봐야 컴파일된다.")
        sys.exit(1)
    print("완료. 유니티를 열어 glTFast 설치를 기다린 뒤 "
          "Book Tower → InGame 씬 구성하기 를 다시 돌리면 된다.")
