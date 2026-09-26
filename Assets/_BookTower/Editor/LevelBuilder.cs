using UnityEngine;
using UnityEditor;
using BookTower.Core;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.Presentation;
using BookTower.Interaction;
using System;
using System.IO;
using System.Collections.Generic;
using MatMap = System.Collections.Generic.Dictionary<string, UnityEngine.Material>;

namespace BookTower.EditorTools
{
    // ─────────────────────────────────────────── JSON 스키마
    // 단일 원본은 Tools/level_layout.py 다.
    //   python3 Tools/level_layout.py build     → 이 JSON 생성
    //   python3 Tools/level_layout.py validate  → 경사·겹침·도달성 검증
    //   python3 Tools/level_layout.py sim       → 이동 시간 시뮬레이션
    // 좌표를 손으로 고치지 말고 파이썬 쪽을 고칠 것. 안 그러면 검증이 무의미해진다.

    [Serializable] public class V3 { public float x, y, z; public Vector3 V => new(x, y, z); }
    [Serializable] public class V2 { public float x, z; }

    [Serializable]
    public class ObjDef
    {
        public string kind;      // box | fold | marker
        public string name;
        public V3 pos;
        public V3 scale;
        public string mat;       // light | mid | shade | accent
        public int state;        // fold 전용 시작 단계
        public float[] angles;
        public float interactionRange;
        public bool noStanding;
        public string note;

        // piece 전용
        public string pieceId;
        public string label;
        public string areaId;
        public bool decoy;

        // crease 전용
        public string creaseId;
        public float wear;
        public bool active;

        // 외벽 면 (north|south|east|west|ceiling). 비어 있으면 벽이 아니다
        public string face;

        // 다리 전용 — 이 퍼즐이 풀리면 bridgeY 까지 올라와 길이 된다
        public string bridgeFor;
        public float bridgeY;

        // 꾸미기(Tools/dressing.py) 전용
        public V3 rot;           // 유니티 Euler. 비어 있으면(0,0,0) 회전 없음
        public bool noOutline;   // 연필 외곽선 생략 — 납작한 러그·이음선 등
        public bool noCollider;  // 콜라이더 제거 — 발목 아래 흩뿌림 등
    }

    /// <summary>꾸미기 전용 색. mat 키 → 색</summary>
    [Serializable]
    public class PalEntry
    {
        public string key;
        public string hex;
    }

    [Serializable]
    public class PuzzleDef
    {
        public string id;
        public string type;      // fold | assembly | sequence
        public float targetSeconds;
        public int[] answer;
        public string[] panels;
        public string[] pieces;
        public string[] creases;
        public string note;

        public V3 at;              // 이 퍼즐을 푸는 자리 (조립 퍼즐용)
        public int hasAt;
        public float assemblyRange;
    }

    [Serializable]
    public class AreaDef
    {
        public string id;
        public string name;
        public int floor;
        public V3 pos;
        public V2 size;
        public float backWall;
        public bool backWallInObjects;
        public float splitGap;   // >0 이면 바닥 가운데가 비어 있는 방 (1B)
        public string note;
        public ObjDef[] objects;
        public PuzzleDef puzzle;

        public int camYaw;
        public float camPad;

        public LightDef light;
        public AccentDef[] accents;
        public string goal;
    }

    [Serializable]
    public class LightDef
    {
        public float sun;
        public float ambient;
        public float fog;
        public float shadowStrength;
    }

    [Serializable]
    public class AccentDef
    {
        public string name;
        public V3 pos;
        public float intensity;
        public float range;
    }

    [Serializable]
    public class RampDef
    {
        public string name;
        public V3 from;
        public V3 to;
        public float width;
        public string mat;
        public string fromId;
        public string toId;
        public string gatePuzzle;   // 이 퍼즐이 풀려야 열린다. 빈 문자열이면 처음부터 열린 길
        public float gateHeight;    // 0 이면 잠금 없음
    }

    [Serializable]
    public class MetaDef
    {
        public float floorGap;
        public float jumpHeight;
        public float xzScale;
    }

    [Serializable]
    public class LevelDef
    {
        public AreaDef[] areas;
        public RampDef[] ramps;
        public MetaDef meta;
        public PalEntry[] palette;
    }

    /// <summary>
    /// level_layout.json 을 읽어 실제 지오메트리를 만든다.
    /// 배치 수치는 이 파일에 없다 — 전부 JSON에서 온다.
    /// </summary>
    public static class LevelBuilder
    {
        private const string JsonPath = "Assets/_BookTower/Level/level_layout.json";

        public struct Palette
        {
            public Material paperLight, paperMid, paperShade, accent, outline;

            /// <summary>꾸미기 색 → 재질. PrototypeSceneBuilder 가 MakeMaterial 을 넘긴다</summary>
            public Func<string, Color, Material> makeExtra;
            public MatMap extra;

            public Material Get(string key)
            {
                if (key != null && extra != null && extra.TryGetValue(key, out Material m)) return m;
                return key switch
                {
                    "light" => paperLight,
                    "shade" => paperShade,
                    "accent" => accent,
                    _ => paperMid,
                };
            }
        }

        /// <summary>등불빛. 이 게임에서 채도가 있는 유일한 색이다</summary>
        private static readonly Color LightWarm = new(0.949f, 0.784f, 0.474f);

        private static Palette pal;
        private static float floorGap = 6.4f;
        private static Transform root;

        /// <summary>
        /// 히든 방(HD)만 따로 담는 뿌리.
        ///
        /// HD 는 탑에서 x 로 128 떨어진 곳에 있다. Map 아래에 같이 두면
        /// 전체 조망이 그 빈 공간까지 담으려고 화면을 훨씬 멀리 물린다.
        /// 조망은 '탑'을 보는 기능이므로 히든 방은 뺀다.
        /// RoomManager 는 씬 전체에서 Room 을 찾으므로 동작에는 영향이 없다.
        /// </summary>
        private static Transform hiddenRoot;

        private static readonly Dictionary<string, BookTower.EchoSystem.EchoCrease> creaseLookup = new();

        /// <summary>구역 id → 그 방의 퍼즐. 경사로 잠금이 이걸 보고 연결된다.</summary>
        private static readonly Dictionary<string, PuzzleBase> puzzleLookup = new();

        /// <summary>풀면 올라오는 판. 퍼즐보다 먼저 만들어지므로 모아 뒀다가 나중에 잇는다.</summary>
        private static readonly List<(string areaId, string puzzleId, GameObject go, float openY)>
            bridges = new();

        /// <summary>
        /// 연필 외곽선. 흰 바탕 위에서는 이 선이 형태를 잡아 준다.
        /// 표식(marker)에는 안 붙인다 — 눈에 안 보이는 기준점이라 선만 남으면 이상하다.
        /// </summary>
        private static void Outline(GameObject go, float thickness = 0.035f)
        {
            if (go == null || pal.outline == null) return;
            PencilOutline.Attach(go, pal.outline, thickness);
        }

        public static Transform Build(Palette palette)
        {
            pal = palette;
            root = new GameObject("Map").transform;
            hiddenRoot = new GameObject("Map (히든)").transform;

            if (!File.Exists(JsonPath))
            {
                Debug.LogError($"[LevelBuilder] 레이아웃 파일이 없습니다: {JsonPath}\n" +
                               "python3 Tools/level_layout.py build 를 먼저 실행하세요.");
                return root;
            }

            LevelDef level = JsonUtility.FromJson<LevelDef>(File.ReadAllText(JsonPath));
            if (level?.areas == null)
            {
                Debug.LogError("[LevelBuilder] 레이아웃 파싱 실패");
                return root;
            }

            pal.extra = new MatMap();
            if (level.palette != null && pal.makeExtra != null)
                foreach (PalEntry e in level.palette)
                    if (ColorUtility.TryParseHtmlString(e.hex, out Color c))
                        pal.extra[e.key] = pal.makeExtra("M_Dress_" + e.key, c);

            if (level.meta != null && level.meta.floorGap > 0.01f)
                floorGap = level.meta.floorGap;

            creaseLookup.Clear();
            puzzleLookup.Clear();
            bridges.Clear();
            var panelLookup = new Dictionary<string, FoldPanel>();

            foreach (AreaDef a in level.areas)
                BuildArea(a, panelLookup);

            foreach (AreaDef a in level.areas)
                WirePuzzle(a, panelLookup);

            if (level.ramps != null)
                foreach (RampDef r in level.ramps)
                    MakeRamp(r);

            Debug.Log($"[LevelBuilder] 구역 {level.areas.Length}개 / 경사로 {level.ramps?.Length ?? 0}개 생성");
            return root;
        }

        // ─────────────────────────────────────────── 구역

        private static void BuildArea(AreaDef a, Dictionary<string, FoldPanel> lookup)
        {
            var area = new GameObject(a.name).transform;
            area.SetParent(a.id == "HD" ? hiddenRoot : root, false);
            area.localPosition = a.pos.V;

            if (!string.IsNullOrEmpty(a.note)) Note(area.gameObject, a.note);

            // 바닥 — 1B(끊긴 길)는 가운데를 비워야 하므로 특수 처리
            // 예전엔 `a.id == "1B"` 였다. 그러면 검증기 쪽에서 "어느 방이 갈라져
            // 있는지"를 알 수가 없고, 실제로 그 탓에 P3 의 접이면이 간극 건너편에
            // 놓인 걸 오래 못 잡았다. 판단 근거는 데이터에 둔다.
            if (a.splitGap > 0.01f) BuildSplitFloor(area, a);
            else Outline(MakeBox(area, "바닥", Vector3.zero,
                                 new Vector3(a.size.x, 0.2f, a.size.z), pal.paperMid), 0.06f);

            // 그림자를 받아낼 뒷벽 — 없으면 퍼즐 검증이 불가능하다
            if (a.backWall > 0.01f && !a.backWallInObjects)
            {
                GameObject back = MakeBox(area, "뒷벽",
                    new Vector3(0f, a.backWall * 0.5f, a.size.z * 0.5f),
                    new Vector3(a.size.x, a.backWall, 0.3f), pal.paperShade);
                AttachWall(back, "north");
                Outline(back);
            }

            foreach (ObjDef o in a.objects ?? Array.Empty<ObjDef>())
            {
                GameObject go = o.kind switch
                {
                    "fold" => MakeFold(area, o, lookup),
                    "marker" => MakeMarker(area, o),
                    "piece" => MakePiece(area, o),
                    "crease" => MakeCrease(area, o),
                    _ => MakeBox(area, o.name, o.pos.V, o.scale.V, pal.Get(o.mat)),
                };
                if (go != null && !string.IsNullOrEmpty(o.note)) Note(go, o.note);
                if (go != null && o.rot != null && o.rot.V != Vector3.zero && o.kind != "fold")
                    go.transform.localRotation = Quaternion.Euler(o.rot.V);
                if (go != null && o.noCollider)
                {
                    var col = go.GetComponent<Collider>();
                    if (col != null) UnityEngine.Object.DestroyImmediate(col);
                }
                if (go != null && o.name != null && o.name.StartsWith("장식_"))
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                if (go != null) PaperAssetBuilder.DressBook(go, o, pal.paperMid);
                if (go != null) AttachWall(go, o.face);
                if (go != null && o.kind != "marker" && !o.noOutline)
                    Outline(go, o.name != null && o.name.StartsWith("장식_") ? 0.02f : 0.035f);
                if (go != null) MakeInteractable(go, o);
                if (go != null && !string.IsNullOrEmpty(o.bridgeFor))
                    bridges.Add((a.id, o.bridgeFor, go, o.bridgeY));
            }

            BuildAccents(area, a);
            ConfigureRoom(area, a);
        }

        /// <summary>
        /// 강조광. 전부 그림자를 만들지 않는다.
        ///
        /// 아트 문서가 "두 번째 그림자를 만드는 광원은 금지"라고 못 박아 뒀다.
        /// 그림자를 안 만드는 등은 그 규칙을 어기지 않으면서,
        /// "빛이 닿는 곳만 존재한다"는 컬러 규칙을 실제 조명으로 구현해 준다.
        /// 어디에 두는지는 level_layout.py 가 정한다 — 그쪽에 그림자 면을
        /// 씻어내지 않는지 검사하는 규칙이 들어 있다.
        /// </summary>
        private static void BuildAccents(Transform area, AreaDef a)
        {
            if (a.accents == null || a.accents.Length == 0) return;

            var group = new GameObject("조명").transform;
            group.SetParent(area, false);

            foreach (AccentDef ac in a.accents)
            {
                var go = new GameObject(ac.name);
                go.transform.SetParent(group, false);
                go.transform.localPosition = ac.pos.V;

                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = LightWarm;
                l.intensity = ac.intensity;
                l.range = ac.range;
                l.shadows = LightShadows.None;   // 두 번째 그림자 금지
            }
        }

        /// <summary>
        /// "여기 뭔가 할 수 있다"를 물체 자신이 알리게 한다.
        ///
        /// 대사도 UI 도 없으므로 이게 없으면 화면에 놓인 게 전부 배경으로 보인다.
        /// 거리는 실제 상호작용 거리와 맞춰야 한다 — 반응은 하는데 눌리지 않거나
        /// 그 반대면, 플레이어는 조작이 고장 난 줄 안다.
        /// </summary>
        private static void MakeInteractable(GameObject go, ObjDef o)
        {
            string label, verb;
            float range;

            switch (o.kind)
            {
                case "fold":
                    label = "접이면"; verb = "접는다"; range = o.interactionRange > 0 ? o.interactionRange : 7f;
                    break;
                case "piece":
                    label = "조각"; verb = "줍는다"; range = 3f;         // CollectiblePiece.pickupRange
                    break;
                case "crease":
                    if (!o.active) return;                               // 죽은 주름은 반응하면 안 된다
                    label = "주름"; verb = "깨운다"; range = 3.5f;
                    break;
                default:
                    return;
            }

            var it = go.AddComponent<Interactable>();
            SetString(it, "label", label);
            SetString(it, "verb", verb);
            SetFloat(it, "range", range);
        }

        /// <summary>
        /// 맞추는 자리를 '누를 수 있는 물건'으로 만든다.
        ///
        /// 퍼즐 오브젝트 자체는 콜라이더도 외형도 없어서 클릭할 수가 없다.
        /// 그래서 그 자리에 실제로 놓여 있는 물건(걸쇠·발자국 그림·복원대)을 찾아
        /// 거기에 붙인다. 플레이어가 보고 누르는 건 그 물건이다.
        /// </summary>
        private static void AttachAssemblyPoint(Transform area, AreaDef a, PieceAssemblyPuzzle asm)
        {
            Vector3 at = a.puzzle.at.V;

            Transform best = null;
            float bd = float.MaxValue;
            foreach (Transform child in area)
            {
                if (child.name.StartsWith("빛_") || child.name == "조명") continue;
                if (child.GetComponent<Renderer>() == null) continue;

                float d = Vector3.Distance(child.localPosition, at);
                if (d < bd) { bd = d; best = child; }
            }

            if (best == null || bd > 3.0f)
            {
                Debug.LogWarning($"[LevelBuilder] {a.id} {a.puzzle.id}: 맞추는 자리에 놓인 물건을 " +
                                 $"못 찾았다 (가장 가까운 것 {bd:F1} 떨어짐). 누를 대상이 없다.");
                return;
            }

            var spot = best.gameObject.AddComponent<BookTower.Info.AssemblyPoint>();
            SetObject(spot, "puzzle", asm);
            SetFloat(spot, "range", a.puzzle.assemblyRange);

            var it = best.gameObject.GetComponent<Interactable>();
            if (it == null) it = best.gameObject.AddComponent<Interactable>();
            SetString(it, "label", "맞추는 자리");
            SetString(it, "verb", "맞춘다");
            SetFloat(it, "range", a.puzzle.assemblyRange);
        }

        // ─────────────────────────────────────────── 룸

        /// <summary>face 문자열이 있으면 카메라를 향할 때 숨겨지는 벽으로 표시한다.</summary>
        private static void AttachWall(GameObject go, string face)
        {
            WallFace f = face switch
            {
                "north" => WallFace.North,
                "south" => WallFace.South,
                "east" => WallFace.East,
                "west" => WallFace.West,
                "ceiling" => WallFace.Ceiling,
                _ => WallFace.None,
            };
            if (f == WallFace.None) return;

            var w = go.AddComponent<RoomWall>();
            SetEnum(w, "face", (int)f);
        }

        /// <summary>
        /// 구역에 Room 을 붙이고 부피를 잰다.
        /// 부피는 JSON 의 size 가 아니라 실제로 생성된 렌더러들에서 구한다.
        /// 벽 높이나 천장까지 포함해야 카메라가 방을 제대로 담을 수 있고,
        /// size 만 쓰면 높이 정보가 아예 없다.
        /// </summary>
        private static void ConfigureRoom(Transform area, AreaDef a)
        {
            var room = area.gameObject.AddComponent<Room>();

            SetString(room, "roomId", a.id);
            SetInt(room, "camYaw", a.camYaw);
            SetFloat(room, "camPad", a.camPad > 0.01f ? a.camPad : 1.18f);

            var rends = area.GetComponentsInChildren<Renderer>(true);
            Vector3 center, size;

            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                center = b.center - area.position;   // 로컬로 환산
                size = b.size;
            }
            else
            {
                center = new Vector3(0f, 4f, 0f);
                size = new Vector3(a.size.x, 8f, a.size.z);
            }

            // 가장자리에 서 있어도 방 안으로 잡히게 살짝만 넉넉히.
            // 크게 주면 옆 방과 부피가 겹쳐서 판정이 흔들린다.
            size += new Vector3(1f, 1f, 1f);

            SetVector3(room, "boundsCenter", center);
            SetVector3(room, "boundsSize", size);
            SetFloat(room, "floorY", a.pos.y);
            SetString(room, "goal", a.goal ?? "");

            if (a.light != null)
            {
                SetFloat(room, "sunIntensity", a.light.sun);
                SetFloat(room, "ambient", a.light.ambient);
                SetFloat(room, "fog", a.light.fog);
                SetFloat(room, "shadowStrength", a.light.shadowStrength);
            }
        }

        /// <summary>P3 · 찢겨나간 간극 — 바닥을 앞뒤로 나눠 가운데를 비운다</summary>
        private static void BuildSplitFloor(Transform area, AreaDef a)
        {
            // 틈 너비는 데이터에서 온다. 예전엔 4 로 박아 놨는데,
            // 방을 1.6배로 키울 때 이 숫자만 안 커져서 표식(6.4)보다 틈이 좁아져 있었다.
            // 게다가 이 값은 점프 검증의 기준이라 코드에 박아 두면 안 된다.
            float gap = a.splitGap;
            float half = (a.size.z - gap) * 0.5f;
            float off = (gap + half) * 0.5f;

            Outline(MakeBox(area, "바닥_앞", new Vector3(0f, 0f, -off),
                            new Vector3(a.size.x, 0.2f, half), pal.paperMid), 0.06f);
            Outline(MakeBox(area, "바닥_뒤", new Vector3(0f, 0f, off),
                            new Vector3(a.size.x, 0.2f, half), pal.paperMid), 0.06f);
        }

        // ─────────────────────────────────────────── 퍼즐 연결

        private static void WirePuzzle(AreaDef a, Dictionary<string, FoldPanel> lookup)
        {
            if (a.puzzle == null || string.IsNullOrEmpty(a.puzzle.id)) return;

            Transform area = root.Find(a.name);
            if (area == null && hiddenRoot != null) area = hiddenRoot.Find(a.name);
            if (area == null) return;

            var go = new GameObject($"{a.puzzle.id}_퍼즐");
            go.transform.SetParent(area, false);

            // 조립 퍼즐은 '푸는 자리'가 따로 있다 — 걸쇠, 발자국 그림, 복원대.
            // 자리를 안 옮기면 퍼즐 오브젝트가 방 한가운데 있게 되고,
            // 거리 판정이 엉뚱한 지점을 기준으로 돌아간다.
            if (a.puzzle.hasAt != 0 && a.puzzle.at != null)
                go.transform.localPosition = a.puzzle.at.V;

            string type = string.IsNullOrEmpty(a.puzzle.type) ? "fold" : a.puzzle.type;
            PuzzleBase puzzle = null;

            switch (type)
            {
                case "assembly":
                {
                    var ap = go.AddComponent<PieceAssemblyPuzzle>();
                    SetStringList(ap, "requiredPieceIds", a.puzzle.pieces);
                    puzzle = ap;
                    break;
                }
                case "sequence":
                {
                    var sp = go.AddComponent<SequencePuzzle>();
                    var creases = new List<BookTower.EchoSystem.EchoCrease>();
                    foreach (string cid in a.puzzle.creases)
                    {
                        if (creaseLookup.TryGetValue(cid, out var ec)) creases.Add(ec);
                        else Debug.LogWarning($"[LevelBuilder] {a.id} {a.puzzle.id}: 주름 '{cid}' 못 찾음");
                    }
                    SetList(sp, "order", creases);
                    puzzle = sp;
                    break;
                }
                default:
                {
                    var panels = new List<FoldPanel>();
                    foreach (string pn in a.puzzle.panels)
                    {
                        if (lookup.TryGetValue(a.id + "/" + pn, out FoldPanel fp)) panels.Add(fp);
                        else Debug.LogWarning($"[LevelBuilder] {a.id} {a.puzzle.id}: 접이면 '{pn}' 못 찾음");
                    }
                    if (panels.Count == 0) { UnityEngine.Object.DestroyImmediate(go); return; }

                    var group = go.AddComponent<FoldGroup>();
                    var fc = go.AddComponent<FoldCombinationPuzzle>();
                    SetList(group, "panels", panels);
                    SetObject(fc, "group", group);
                    SetIntArray(fc, "answer", a.puzzle.answer);
                    puzzle = fc;
                    break;
                }
            }

            if (puzzle == null) { UnityEngine.Object.DestroyImmediate(go); return; }

            SetString(puzzle, "puzzleId", a.puzzle.id);
            SetFloat(puzzle, "targetSeconds", a.puzzle.targetSeconds);

            // 경사로 잠금이 나중에(경사로를 만들 때) 이걸 찾는다
            puzzleLookup[a.id] = puzzle;

            // 풀면 올라오는 판 (P3 의 간극). 데이터가 가리키는 퍼즐과 일치할 때만 잇는다 —
            // level_layout.py 가 그 일치를 검사하고 있으므로 여기서 조건을 다시 쓰지 않는다.
            foreach (var b in bridges)
            {
                if (b.areaId != a.id || b.puzzleId != a.puzzle.id || b.go == null) continue;
                var br = b.go.AddComponent<SolveBridge>();
                SetObject(br, "puzzle", puzzle);
                SetFloat(br, "openY", b.openY);
            }

            if (a.puzzle.assemblyRange > 0.01f && puzzle is PieceAssemblyPuzzle asm)
            {
                SetFloat(puzzle, "assemblyRange", a.puzzle.assemblyRange);
                AttachAssemblyPoint(area, a, asm);
            }

            // 큰 방에서 구석에 서 있어도 켜지도록 구역 크기에 맞춘다
            float reach = Mathf.Max(a.size.x, a.size.z) * 0.5f + 6f;
            SetFloat(puzzle, "activateRadius", reach);

            if (!string.IsNullOrEmpty(a.puzzle.note)) Note(go, a.puzzle.note);
        }

        // ─────────────────────────────────────────── 프리미티브

        private static GameObject MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject MakeFold(Transform area, ObjDef o, Dictionary<string, FoldPanel> lookup)
        {
            var hinge = new GameObject(o.name);
            hinge.transform.SetParent(area, false);
            hinge.transform.localPosition = o.pos.V;

            var fold = hinge.AddComponent<FoldPanel>();
            if (o.angles != null && o.angles.Length == 4)
            {
                var so = new SerializedObject(fold);
                var angles = so.FindProperty("foldAngles"); angles.arraySize = 4;
                for (int i = 0; i < 4; i++) angles.GetArrayElementAtIndex(i).floatValue = o.angles[i];
                so.FindProperty("noStanding").boolValue = o.noStanding;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (o.state != 0) SetEnum(fold, "state", o.state);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(hinge.transform, false);
            panel.transform.localPosition = new Vector3(0f, 0f, o.scale.z * 0.5f);
            panel.transform.localScale = o.scale.V;
            panel.GetComponent<MeshRenderer>().sharedMaterial = pal.Get(o.mat);

            string areaId = area.name.Split('_')[0];
            lookup[areaId + "/" + o.name] = fold;
            return hinge;
        }

        /// <summary>주워서 유품함에 넣는 조각 (정보 = 열쇠)</summary>
        private static GameObject MakePiece(Transform area, ObjDef o)
        {
            GameObject go = MakeBox(area, o.name, o.pos.V, o.scale.V, pal.Get(o.mat));
            var cp = go.AddComponent<BookTower.Info.CollectiblePiece>();

            SetString(cp, "pieceId", o.pieceId);
            SetString(cp, "label", string.IsNullOrEmpty(o.label) ? o.name : o.label);
            SetString(cp, "areaId", o.areaId);
            SetBool(cp, "isDecoy", o.decoy);
            return go;
        }

        /// <summary>문지르면 과거가 재생되는 주름 (에코)</summary>
        private static GameObject MakeCrease(Transform area, ObjDef o)
        {
            GameObject go = MakeBox(area, o.name, o.pos.V, o.scale.V, pal.Get(o.mat));
            var ec = go.AddComponent<BookTower.EchoSystem.EchoCrease>();

            SetString(ec, "creaseId", o.creaseId);
            SetFloat(ec, "wear", o.wear);
            SetBool(ec, "active", o.active);

            creaseLookup[o.creaseId] = ec;
            return go;
        }

        private static GameObject MakeMarker(Transform parent, ObjDef o)
        {
            var go = new GameObject(o.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = o.pos.V;
            return go;
        }

        /// <summary>두 지점을 잇는 경사로. 실제 게임에선 일부가 접이면 퍼즐이 된다</summary>
        private static void MakeRamp(RampDef r)
        {
            Vector3 from = r.from.V, to = r.to.V;
            Vector3 dir = to - from;
            if (dir.magnitude < 0.01f) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = r.name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            go.transform.localScale = new Vector3(r.width, 0.2f, dir.magnitude + .3f);
            go.GetComponent<MeshRenderer>().sharedMaterial = pal.Get(r.mat);
            Outline(go, 0.012f);

            // 이 경사로가 잇는 두 방을 기록해 둔다.
            // 경사로 위에서 어느 방을 보여줄지 추측하지 않기 위해서다.
            if (!string.IsNullOrEmpty(r.fromId) && !string.IsNullOrEmpty(r.toId))
            {
                var link = go.AddComponent<RampLink>();
                SetString(link, "fromRoomId", r.fromId);
                SetString(link, "toRoomId", r.toId);
                SetVector3(link, "fromPoint", from);
                SetVector3(link, "toPoint", to);
            }

            // 경사로도 방과 함께 사라져야 한다. 안 그러면 탑의 모든 경사로가
            // 항상 그려져서, 나선 구조상 화면에서 전부 겹친다.
            string[] linked = { r.fromId, r.toId };
            var rb = go.AddComponent<RoomBound>();
            SetStringList(rb, "roomIds", linked);

            MakeRampTransitions(r, from, to);

            MakeGate(r, from, dir, go.GetComponent<Collider>());
        }

        // Folded paper rims close the excess doorway clearance and continue onto the bridge.
        // Flat end leaves overlap the room floor; all parts share the ramp's visibility binding.
        private static void MakeRampTransitions(RampDef r, Vector3 from, Vector3 to)
        {
            var group=new GameObject(r.name+"_접합부").transform;
            group.SetParent(root,false);
            var binding=group.gameObject.AddComponent<RoomBound>();
            SetStringList(binding,"roomIds",new[]{r.fromId,r.toId});
            Vector3 direction=(to-from).normalized;
            Vector3 horizontal=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,horizontal);
            const float inset=6f, rimHeight=2.4f, thickness=.16f;
            var points=new[]{from-horizontal*inset,from,to,to+horizontal*inset};
            for(int end=0;end<2;end++)
            {
                Vector3 point=end==0?from:to;
                Vector3 inward=end==0?-horizontal:horizontal;
                var leaf=MakeBox(group,end==0?"시작_겹친페이지":"도착_겹친페이지",point+inward*(inset*.5f-.15f),new Vector3(r.width,.16f,inset+.3f),pal.paperLight);
                leaf.transform.localRotation=Quaternion.LookRotation(horizontal);
                Outline(leaf,.018f);
            }
            for(int side=-1;side<=1;side+=2)
                for(int segment=0;segment<3;segment++)
                {
                    Vector3 a=points[segment]+right*side*(r.width*.5f+thickness*.5f);
                    Vector3 b=points[segment+1]+right*side*(r.width*.5f+thickness*.5f);
                    Vector3 span=b-a;
                    // Upright mesh follows the slope without leaning its end face away from the gate.
                    var wall=new GameObject($"접힌측벽_{side}_{segment}");wall.transform.SetParent(group,false);
                    var mesh=new Mesh();mesh.name=wall.name;
                    Vector3 offset=right*(thickness*.5f), up=Vector3.up*rimHeight;
                    mesh.vertices=new[]{a-offset,b-offset,b-offset+up,a-offset+up,a+offset,b+offset,b+offset+up,a+offset+up};
                    mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
                    var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i];triangles[i]=triangles[i+2];triangles[i+2]=swap;}
                    mesh.triangles=triangles;
                    mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up,Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
                    mesh.RecalculateNormals();mesh.RecalculateBounds();
                    string path=$"Assets/_BookTower/Generated/Direction/Rim_{r.fromId}_{r.toId}_{side}_{segment}.asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}
                    wall.AddComponent<MeshFilter>().sharedMesh=saved;
                    wall.AddComponent<MeshRenderer>().sharedMaterial=pal.paperMid;
                    wall.AddComponent<MeshCollider>().sharedMesh=saved;
                }
        }

        /// <summary>
        /// 출구를 막고 선 페이지. 그 방의 퍼즐이 풀리면 펴져서 길이 된다.
        ///
        /// 형태를 벽이 아니라 접이면과 똑같이 잡는 이유는 접기가 이 게임의
        /// 언어이기 때문이다. 그래서 MakeFold 와 같은 관례를 쓴다 —
        /// 경첩은 경사로 입구에 두고 판은 로컬 +z 로 뻗는다.
        /// 회전 0 이면 경사로 위에 납작하게 눕고(열림),
        /// Vector3.left 축으로 90도 돌리면 길을 가로막고 선다(잠김).
        ///
        /// 잠금 여부는 데이터가 정한다. level_layout.py 가 "퍼즐이 있는 방은
        /// 반드시 잠기고, 없는 방은 절대 안 잠긴다"를 검사하므로
        /// 여기서 조건을 다시 쓰면 두 곳이 어긋날 수 있다.
        /// </summary>
        private static void MakeGate(RampDef r, Vector3 from, Vector3 dir, Collider bridgeCollider)
        {
            if (r.gateHeight <= 0.01f) return;

            if (!puzzleLookup.TryGetValue(r.fromId ?? "", out PuzzleBase puzzle))
            {
                Debug.LogWarning($"[LevelBuilder] {r.name}: 잠금이 지정됐는데 " +
                                 $"'{r.fromId}' 의 퍼즐을 못 찾았다. 열어 둔다.");
                return;
            }

            var hinge = new GameObject($"잠금_{r.fromId}").transform;
            hinge.SetParent(root, false);
            // 경첩 축은 월드 수평이어야 닫힌 페이지가 정확히 수직이 된다.
            // 경사 벡터까지 LookRotation에 넣으면 긴/가파른 연결에서 판이 기울어져
            // CharacterController가 위로 타고 넘어갈 수 있다.
            Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            // 경사 콜라이더와 정확히 같은 모서리에 세우면 CharacterController가
            // 오르막 접촉면을 따라 잠금판 너머로 밀리는 경우가 있다. 방 안쪽의
            // 평평한 종이 위에 경첩을 두어 충돌면을 분리한다.
            hinge.localPosition = from - flatDir * 1.25f;
            hinge.localRotation = Quaternion.LookRotation(flatDir, Vector3.up);

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "페이지";
            plate.transform.SetParent(hinge, false);
            plate.transform.localPosition = new Vector3(0f, 0.06f, r.gateHeight * 0.5f);
            float gateWidth = r.width + 4f; // 중앙 + 좌우 바깥 접근까지 한 장으로 연속 차단
            plate.transform.localScale = new Vector3(gateWidth, 0.2f, r.gateHeight);
            plate.GetComponent<MeshRenderer>().sharedMaterial = pal.paperShade;
            // 실제 진행 차단은 경첩 루트의 단일 BoxCollider가 담당한다.
            // 렌더 메시 자식의 콜라이더에만 의존하면 긴 경사에서 Transform 갱신 순서에
            // 따라 CharacterController가 페이지를 통과하는 경우가 있었다.
            UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
            var blocker = hinge.gameObject.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 0.06f, r.gateHeight * 0.5f);
            // CharacterController.skinWidth(0.02)와 경사 접촉을 함께 고려한 물리 두께.
            // 보이는 종이는 얇게 유지하되 잠금 충돌면은 1.2u로 잡아 터널링을 막는다.
            blocker.size = new Vector3(gateWidth, 1.2f, r.gateHeight);
            Outline(plate, .012f);

            // 문 판보다 살짝 바깥으로 비켜 지나가는 우회 경로를 막는 접힌 문틀.
            // CharacterController 지름 0.6u를 고려해 판 가장자리에서 양쪽 2u를 연속해 막는다.
            // 길 방향 두께를 크게 잡으면 열린 판과 겹치므로 0.24u만 사용한다.
            const float jambWidth = 2f;
            for (int side = -1; side <= 1; side += 2)
            {
                var jamb = MakeBox(hinge, side < 0 ? "문틀_좌" : "문틀_우",
                    new Vector3(side * (gateWidth * .5f + jambWidth * .5f), r.gateHeight * .5f, .08f),
                    new Vector3(jambWidth, r.gateHeight, .24f), pal.paperMid);
                Outline(jamb, .012f);
            }

            // 열렸을 때 켜지는 빛. 그림자는 만들지 않는다(두 번째 그림자 금지).
            // 퍼즐이 풀린 뒤에만 켜지므로 그 방의 그림자 퍼즐을 씻어낼 수 없다.
            var lightGo = new GameObject("빛_출구");
            lightGo.transform.SetParent(hinge, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.0f, r.gateHeight * 0.5f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = LightWarm;
            l.range = 8f;
            l.intensity = 0f;
            l.shadows = LightShadows.None;

            var gate = hinge.gameObject.AddComponent<RampGate>();
            SetObject(gate, "puzzle", puzzle);
            SetObject(gate, "lockedBridge", bridgeCollider);

            string[] linked = { r.fromId, r.toId };
            var gb = hinge.gameObject.AddComponent<RoomBound>();
            SetStringList(gb, "roomIds", linked);
        }

        // ─────────────────────────────────────────── 유틸

        private static void Note(GameObject go, string text)
        {
            var note = go.AddComponent<LevelNote>();
            SetString(note, "note", text);
        }

        private static void SetObject(UnityEngine.Object t, string f, UnityEngine.Object v)
        { var so = new SerializedObject(t); so.FindProperty(f).objectReferenceValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetString(UnityEngine.Object t, string f, string v)
        { var so = new SerializedObject(t); so.FindProperty(f).stringValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetFloat(UnityEngine.Object t, string f, float v)
        { var so = new SerializedObject(t); so.FindProperty(f).floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetBool(UnityEngine.Object t, string f, bool v)
        { var so = new SerializedObject(t); so.FindProperty(f).boolValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetStringList(UnityEngine.Object t, string f, string[] v)
        {
            var so = new SerializedObject(t);
            SerializedProperty p = so.FindProperty(f);
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) p.GetArrayElementAtIndex(i).stringValue = v[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object t, string f, int v)
        { var so = new SerializedObject(t); so.FindProperty(f).intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetVector3(UnityEngine.Object t, string f, Vector3 v)
        { var so = new SerializedObject(t); so.FindProperty(f).vector3Value = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetEnum(UnityEngine.Object t, string f, int v)
        { var so = new SerializedObject(t); so.FindProperty(f).enumValueIndex = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        private static void SetIntArray(UnityEngine.Object t, string f, int[] v)
        {
            var so = new SerializedObject(t);
            SerializedProperty p = so.FindProperty(f);
            p.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) p.GetArrayElementAtIndex(i).intValue = v[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList<T>(UnityEngine.Object t, string f, List<T> v) where T : UnityEngine.Object
        {
            var so = new SerializedObject(t);
            SerializedProperty p = so.FindProperty(f);
            p.arraySize = v.Count;
            for (int i = 0; i < v.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
