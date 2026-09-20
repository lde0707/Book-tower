using UnityEngine;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    /// <summary>
    /// 책갈피새 — 접힌 종이로 만든 낮은 폴리곤 모델.
    ///
    /// 왜 스프라이트가 아닌가
    ///   처음엔 2D 스프라이트로 만들었다가 버렸다. 이 게임에서는 못 쓴다.
    ///     · 카메라가 Q/E 로 90° 4방향 회전한다 — 옆모습 그림 한 장은 한 각도에서만 맞다
    ///     · 그림자가 곧 정답인 게임이다 — 납작한 판때기의 그림자는 각도에 따라 선이 된다
    ///     · 씬이 전부 3D 메시로 생성된다. 스프라이트가 들어갈 자리가 없다
    ///   빌보드로 회전 문제를 풀면 그림자가 더 망가진다. 그건 못 쓰는 타협이다.
    ///
    /// 아트 문서(Docs/02 §5)는 원래 3D 를 말하고 있었다 —
    /// "몸통은 접힌 종이", "뼈대 없이 **종이 접힘 단위로 회전**".
    /// 그래서 평면 판 몇 장을 접힘선 축으로 돌리는 구조로 만든다. 리그가 없다.
    ///
    /// 실루엣은 후보 A~D 중 **B(세로로 선 새)** 로 확정했다.
    /// 걷는 캐릭터라 세로 실루엣이 유리하고, 작을 때 가장 덜 뭉개지며,
    /// P15 왜상이 요구하는 "단순한 실루엣"에 가장 가깝다.
    ///
    /// 스프라이트 시안에서 건진 규칙 — 3D 에도 그대로 적용한다.
    ///   · **캐릭터에 흰색 금지.** 배경이 #FDFCFA 라 밝은 면은 그냥 사라진다
    ///   · 명암은 두 단계까지. 1유닛 = 67.5px(1080p) 라 네 단계는 다 뭉개진다
    /// </summary>
    public static class PaperBirdBuilder
    {
        public const float Height = 1.40f;          // 발끝에서 정수리까지 (월드 유닛)

        // ── 실루엣 B. (앞, 위) 평면. 앞은 +Z, 위는 +Y 로 간다
        private static readonly Vector2[] Body =
        {
            new(-0.22f, -0.52f),   // 꼬리쪽 발치
            new(-0.30f,  0.10f),   // 등 아래
            new(-0.14f,  0.46f),   // 어깨
            new( 0.22f,  0.34f),   // 턱
            new( 0.26f,  0.02f),   // 가슴
            new( 0.18f, -0.48f),   // 앞 발치
        };

        private static readonly Vector2[] Head =
        {
            new(-0.16f, 0.44f),    // 경첩 뒤
            new( 0.02f, 0.56f),    // 정수리
            new( 0.60f, 0.40f),    // 부리 끝
            new( 0.16f, 0.30f),    // 경첩 앞
        };

        private static readonly Vector2[] Wing =
        {
            new(-0.12f,  0.34f),   // 어깨 = 경첩
            new(-0.34f, -0.14f),
            new( 0.10f, -0.06f),
        };

        private static readonly Vector2 HeadHinge = new(0.04f, 0.40f);
        private static readonly Vector2 WingHinge = new(-0.12f, 0.34f);

        // 접힘각은 파이썬 검증기(qa_bird3d)로 4방향을 렌더해 보며 맞췄다.
        //   몸통 21° → 정면(yaw 0/180)에서 판이 거의 안 보이는 얇은 조각이 됐다. 39° 로 키움
        //   고개 27° → 두 판이 위로 벌어져 '토끼귀' 가 됐다. 11° 로 낮춰 앞으로 뻗는 쐐기로
        private const float BodyFold = 39f;
        private const float HeadFold = 11f;
        private const float RidgeZ = -0.30f;        // 접힘선(등) 위치
        private const float HeadRidgeZ = -0.14f;

        public static GameObject Build(Transform parent, Material paper,
                                       Material shade, Material ribbon,
                                       Material outline)
        {
            // 2D 좌표 1.14 가 곧 키 → 실제 키에 맞춘 배율
            float s = Height / (0.62f - (-0.52f));
            float footY = 0.52f * s;                // 발끝을 y=0 으로

            var root = new GameObject("책갈피새").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, footY, 0f);

            // ── 몸통: 한 장을 등에서 접어 두 면으로 세운다
            var bodyRoot = Pivot("몸통", root, Vector3.zero);
            foreach (float sgn in new[] { 1f, -1f })
            {
                var g = Panel($"몸통_{(sgn > 0 ? "좌" : "우")}", bodyRoot, Body,
                              Vector2.zero, s, paper, outline, 0.012f);
                g.transform.localPosition = new Vector3(0f, 0f, RidgeZ * s);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * BodyFold, 0f);
                // 접힘선이 회전축이므로 판을 축 기준으로 다시 밀어 준다
                Recenter(g, new Vector3(0f, 0f, -RidgeZ * s));
            }

            // ── 고개: 목 경첩에서 따로 접힌다. PaperBirdRig 이 각도만 바꾼다
            var headPivot = Pivot("고개", root, new Vector3(0f, HeadHinge.y * s, HeadHinge.x * s));
            foreach (float sgn in new[] { 1f, -1f })
            {
                var g = Panel($"고개_{(sgn > 0 ? "좌" : "우")}", headPivot, Head,
                              HeadHinge, s, paper, outline, 0.010f);
                g.transform.localPosition = new Vector3(
                    0f, 0f, (HeadRidgeZ - HeadHinge.x) * s);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * HeadFold, 0f);
                Recenter(g, new Vector3(0f, 0f, -(HeadRidgeZ - HeadHinge.x) * s));
            }

            // ── 날개: 어깨 경첩. 접히면 몸에 붙고 들리면 옆으로 벌어진다
            var wings = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                var p = Pivot($"날개_{(sgn > 0 ? "좌" : "우")}", root,
                              new Vector3(sgn * 0.02f * s, WingHinge.y * s, WingHinge.x * s));
                var g = Panel("면", p, Wing, WingHinge, s, shade, outline, 0.010f);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * 8f, 0f);
                wings[i] = p;
            }

            // ── 꼬리 리본: 납작한 띠 그대로가 맞다. 접지 않는다.
            //    직사각형으로 뽑았더니 몸에서 옆으로 뻗은 막대로 보였다 —
            //    뒤로 가면서 아래로 처지고 끝이 가늘어져야 '리본' 으로 읽힌다.
            var tail = Pivot("리본", root, new Vector3(0f, -0.34f * s, -0.24f * s));
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                var strip = new System.Collections.Generic.List<Vector2>();
                var back = new System.Collections.Generic.List<Vector2>();
                for (int t = 0; t <= 6; t++)
                {
                    float u = t / 6f;
                    float z = -0.52f * u;
                    float y = -0.34f * u * u - 0.02f;
                    float half = 0.075f * (1f - 0.55f * u);
                    strip.Add(new Vector2(z, y + half));
                    back.Add(new Vector2(z, y - half));
                }
                back.Reverse();
                strip.AddRange(back);

                var g = Panel($"가닥_{i + 1}", tail, strip.ToArray(), Vector2.zero, s,
                              ribbon, outline, 0.008f);
                g.transform.localPosition = new Vector3(sgn * 0.045f * s, 0f, 0f);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * (i == 0 ? 14f : 11f), 0f);
            }

            var rig = root.gameObject.AddComponent<PaperBirdRig>();
            rig.Wire(headPivot, wings[0], wings[1], tail);
            return root.gameObject;
        }

        private static Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        private static void Recenter(GameObject g, Vector3 delta)
        {
            var mf = g.GetComponent<MeshFilter>();
            Mesh m = mf.sharedMesh;
            Vector3[] v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] += delta;
            m.vertices = v;
            m.RecalculateBounds();
        }

        /// <summary>
        /// 평면 한 장. 종이는 뒷면도 보여야 하므로 삼각형을 양쪽으로 넣는다 —
        /// 단면 메시로 두면 한쪽에서 볼 때 통째로 사라진다.
        /// </summary>
        private static GameObject Panel(string name, Transform parent, Vector2[] prof,
                                        Vector2 origin, float s, Material mat,
                                        Material outline, float thickness)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            int n = prof.Length;
            var verts = new Vector3[n * 2];
            for (int i = 0; i < n; i++)
            {
                Vector2 p = prof[i] - origin;
                verts[i] = new Vector3(0f, p.y * s, p.x * s);
                verts[i + n] = verts[i];
            }

            var tris = new System.Collections.Generic.List<int>();
            for (int i = 1; i < n - 1; i++)
            {
                tris.Add(0); tris.Add(i); tris.Add(i + 1);
                tris.Add(n); tris.Add(n + i + 1); tris.Add(n + i);
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            if (outline != null) PencilOutline.Attach(go, outline, thickness);
            return go;
        }
    }
}
