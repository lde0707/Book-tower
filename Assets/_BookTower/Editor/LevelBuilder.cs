using UnityEngine;
using UnityEditor;
using BookTower.Core;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.Presentation;
using System;
using System.IO;
using System.Collections.Generic;

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
        public string note;
        public ObjDef[] objects;
        public PuzzleDef puzzle;

        public int camYaw;
        public float camPad;

        public LightDef light;
        public AccentDef[] accents;
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

            public Material Get(string key) => key switch
            {
                "light" => paperLight,
                "shade" => paperShade,
                "accent" => accent,
                _ => paperMid,
            };
        }

        /// <summary>등불빛. 이 게임에서 채도가 있는 유일한 색이다</summary>
        private static readonly Color LightWarm = new(0.949f, 0.784f, 0.474f);

        private static Palette pal;
        private static float floorGap = 6.4f;

        /// <summary>
        /// 연필 외곽선. 흰 바탕 위에서는 이 선이 형태를 잡아 준다.
        /// 표식(marker)에는 안 붙인다 — 눈에 안 보이는 기준점이라 선만 남으면 이상하다.
        /// </summary>
        private static void Outline(GameObject go, float thickness = 0.035f)
        {
            if (go == null || pal.outline == null) return;
            PencilOutline.Attach(go, pal.outline, thickness);
        }
        private static Transform root;
        private static readonly Dictionary<string, BookTower.EchoSystem.EchoCrease> creaseLookup = new();

        public static Transform Build(Palette palette)
        {
            pal = palette;
            root = new GameObject("Map").transform;

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

            if (level.meta != null && level.meta.floorGap > 0.01f)
                floorGap = level.meta.floorGap;

            creaseLookup.Clear();
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
            area.SetParent(root, false);
            area.localPosition = a.pos.V;

            if (!string.IsNullOrEmpty(a.note)) Note(area.gameObject, a.note);

            // 바닥 — 1B(끊긴 길)는 가운데를 비워야 하므로 특수 처리
            if (a.id == "1B") BuildSplitFloor(area, a);
            else Outline(MakeBox(area, "바닥", Vector3.zero,
                                 new Vector3(a.size.x, 0.2f, a.size.z), pal.paperMid), 0.06f);

            // 그림자를 받아낼 뒷벽 — 없으면 퍼즐 검증이 불가능하다
            if (a.backWall > 0.01f)
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
                if (go != null) AttachWall(go, o.face);
                if (go != null && o.kind != "marker") Outline(go);
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
            float gap = floorGap;
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

            if (a.puzzle.assemblyRange > 0.01f && puzzle is PieceAssemblyPuzzle)
                SetFloat(puzzle, "assemblyRange", a.puzzle.assemblyRange);

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
            go.transform.localScale = new Vector3(r.width, 0.2f, dir.magnitude);
            go.GetComponent<MeshRenderer>().sharedMaterial = pal.Get(r.mat);
            Outline(go, 0.05f);

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
