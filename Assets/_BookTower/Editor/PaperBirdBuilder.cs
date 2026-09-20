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
            new(-0.12f,  0.32f),   // 어깨 = 경첩
            new(-0.26f,  0.10f),
            new(-0.22f, -0.04f),
            new( 0.04f, -0.06f),   // 끝을 뭉툭하게 — 뾰족하면 그림자가 가시처럼 선다
        };

        private static readonly Vector2 HeadHinge = new(0.04f, 0.40f);
        private static readonly Vector2 WingHinge = new(-0.12f, 0.32f);

        // ── 접는 방식
        //
        // 처음엔 판 전체를 세로축으로 회전시켰다. 두 가지가 잘못됐다.
        //   · **부리가 두 갈래로 벌어졌다.** 축이 머리 뒤에 있어 앞쪽이 벌어진다
        //   · **그림자가 가시처럼 삐죽했다.** 배가 활짝 열려 아래가 두 갈래로 갈라지고,
        //     모든 끝점이 둘씩 생겼다
        //
        // 그래서 회전 대신 **꼭짓점마다 벌어지는 정도를 달리** 준다.
        // 0 인 곳에서 두 판이 만나 하나의 모서리가 되고, 1 인 곳이 가장 벌어진다.
        // 종이로 치면 곡선 접기다 — 실제 종이접기에서 부리가 만들어지는 방식이기도 하다.
        //
        // 수치는 카메라 4방향 + 햇빛 그림자를 렌더해 보며 맞췄다.
        private const float BodyFold = 39f;
        private const float HeadFold = 11f;

        //                              발치  등   어깨  턱   가슴  앞발치
        private static readonly float[] BodyW = { 0.10f, 0f, 0.40f, 0.62f, 1.00f, 0.18f };
        //                              목뒤  정수리 부리  턱
        private static readonly float[] HeadW = { 0.55f, 0f, 0f, 0.42f };
        // 정수리와 부리를 0 으로 닫아야 정면에서 V 자 틈이 안 남는다

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

            // ── 몸통: 등과 발치에서 닫히고 가슴에서 가장 벌어진다
            float spread = Mathf.Sin(BodyFold * Mathf.Deg2Rad) * 0.66f * s;
            var bodyRoot = Pivot("몸통", root, Vector3.zero);
            foreach (float sgn in new[] { 1f, -1f })
                Panel($"몸통_{(sgn > 0 ? "좌" : "우")}", bodyRoot, Body, Vector2.zero,
                      s, paper, outline, 0.012f, BodyW, sgn * spread);

            // ── 고개: 부리 끝에서 두 판이 만난다. 목에서만 벌어진다
            float hspread = Mathf.Sin(HeadFold * Mathf.Deg2Rad) * 1.5f * s;
            var headPivot = Pivot("고개", root,
                new Vector3(0f, HeadHinge.y * s, HeadHinge.x * s));
            foreach (float sgn in new[] { 1f, -1f })
                Panel($"고개_{(sgn > 0 ? "좌" : "우")}", headPivot, Head, HeadHinge,
                      s, paper, outline, 0.010f, HeadW, sgn * hspread);

            // ── 날개: 어깨 경첩. 접히면 몸에 붙고 들리면 옆으로 벌어진다
            var wings = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                var pv = Pivot($"날개_{(sgn > 0 ? "좌" : "우")}", root,
                    new Vector3(sgn * 0.02f * s, WingHinge.y * s, WingHinge.x * s));
                var g = Panel("면", pv, Wing, WingHinge, s, shade, outline, 0.010f);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * 4f, 0f);
                wings[i] = pv;
            }

            // ── 꼬리 리본: 납작한 띠 그대로가 맞다. 접지 않는다.
            //    직사각형으로 뽑았더니 몸에서 옆으로 뻗은 막대로 보였다 —
            //    뒤로 가면서 아래로 처지고 끝이 가늘어져야 '리본' 으로 읽힌다.
            var tail = Pivot("리본", root, new Vector3(0f, -0.34f * s, -0.24f * s));
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                var top = new System.Collections.Generic.List<Vector2>();
                var bot = new System.Collections.Generic.List<Vector2>();
                for (int t = 0; t <= 6; t++)
                {
                    float u = t / 6f;
                    float z = -0.52f * u;
                    float y = -0.34f * u * u - 0.02f;
                    float half = 0.095f * (1f - 0.45f * u);
                    top.Add(new Vector2(z, y + half));
                    bot.Add(new Vector2(z, y - half));
                }
                bot.Reverse();
                top.AddRange(bot);

                var g = Panel($"가닥_{i + 1}", tail, top.ToArray(), Vector2.zero, s,
                              ribbon, outline, 0.008f);
                g.transform.localPosition = new Vector3(sgn * 0.045f * s, 0f, 0f);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * (i == 0 ? 15f : 11f), 0f);
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

        /// <summary>
        /// 평면 한 장. 종이는 뒷면도 보여야 하므로 삼각형을 양쪽으로 넣는다 —
        /// 단면 메시로 두면 한쪽에서 볼 때 통째로 사라진다.
        /// </summary>
        private static GameObject Panel(string name, Transform parent, Vector2[] prof,
                                        Vector2 origin, float s, Material mat,
                                        Material outline, float thickness,
                                        float[] weights = null, float spread = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            int n = prof.Length;
            var verts = new Vector3[n * 2];
            for (int i = 0; i < n; i++)
            {
                Vector2 p = prof[i] - origin;
                float w = (weights != null && i < weights.Length) ? weights[i] : 0f;
                verts[i] = new Vector3(spread * w, p.y * s, p.x * s);
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
