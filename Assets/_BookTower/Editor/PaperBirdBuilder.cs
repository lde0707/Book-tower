using System.Collections.Generic;
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
    ///
    /// 왜 '판때기' 도 아닌가 (2차 수정)
    ///   판 두 장을 접어 세운 첫 3D 버전은 이런 말을 들었다 —
    ///   "너무 딱딱하고 뾰족하다, 입체감이 없다". 원인이 셋이었다.
    ///     · 옆에서 보면 두께가 0 이라 종잇장으로 보인다
    ///     · 면이 한 장뿐이라 음영도 한 단계다 — 덩어리로 안 읽힌다
    ///     · 외곽선 꼭짓점이 그대로 각이라 부리·날개 끝이 가시처럼 선다
    ///
    /// 그래서 지금은 **닫힌 껍질**을 만든다.
    ///   테두리(x=0) → 안쪽으로 인셋한 겹 셋 → 평평한 대(臺)로 닫음.
    ///   테두리에서 두 쪽이 맞물리므로 어느 각도에서도 실루엣이 한 겹이고,
    ///   그림자도 한 덩어리로 진다. (첫 버전은 끝점이 둘로 갈라져 그림자가
    ///   별 모양으로 삐죽했다.)
    ///
    ///   무게중심 쪽으로 줄이는 방식이 아니라 **인셋**이어야 한다.
    ///   중심으로 줄이면 몸통 같은 긴 모양이 달걀로 뭉개진다 — 실제로 그랬다.
    ///
    ///   겹은 셋까지만 둔다. 방 조명이 밝아서(ambient 0.62~0.92) 완만한 곡면은
    ///   음영이 뭉개진다. 면을 적게 쓰되 각도 차를 크게 두는 쪽이 덩어리로 읽히고,
    ///   그게 접힌 종이의 인상과도 맞는다.
    ///
    /// 실루엣은 후보 A~D 중 **B(세로로 선 새)** 계열. 통통하게 고쳐 잡았다.
    ///   · 목에 노치를 확실히 판다 — 이게 없으면 머리가 몸에 먹힌다
    ///   · 부리 끝은 점 하나로 모으지 않고 둘로 끊어 뭉툭하게
    ///   · 몸 폭을 깊이의 2/3 까지 키운다 — 정면에서 기둥처럼 보이던 걸 고친 것
    ///
    /// 색 규칙 (스프라이트 시안에서 건진 것, 3D 에도 그대로)
    ///   · **캐릭터에 순백 금지.** 배경이 #FDFCFA 라 흰 면은 그냥 사라진다
    ///   · 명암은 두 단계까지. 1유닛 = 67.5px(1080p) 라 네 단계는 다 뭉개진다
    /// </summary>
    public static class PaperBirdBuilder
    {
        public const float Height = 1.40f;          // 발끝에서 정수리까지 (월드 유닛)

        // ── 프로파일은 (z, y). z 가 앞(부리 쪽), y 가 위.
        //    발끝 -0.48 ~ 정수리 0.52 → 폭이 정확히 1.0 이라 배율이 곧 키가 된다.
        private static readonly Vector2[] Body =
        {
            new( 0.06f, -0.48f),   // 앞발치
            new( 0.16f, -0.34f),
            new( 0.22f, -0.14f),   // 배 — 가장 두껍다
            new( 0.21f,  0.04f),
            new( 0.15f,  0.18f),   // 윗가슴
            new( 0.07f,  0.25f),   // 목 앞
            new(-0.03f,  0.26f),   // 목 뒤 ← 여기가 좁아야 머리가 따로 읽힌다
            new(-0.13f,  0.17f),   // 등 위
            new(-0.22f, -0.02f),   // 등
            new(-0.32f, -0.18f),   // 꽁지 — 뒤로 살짝 내민다
            new(-0.27f, -0.33f),
            new(-0.15f, -0.42f),
            new(-0.06f, -0.48f),   // 뒷발치
        };

        /// <summary>꼭짓점마다 얼마나 부풀릴지(0~1). 배에서 최대, 발치·꽁지에서 얇다.</summary>
        private static readonly float[] BodyW =
        { 0.34f, 0.78f, 1.00f, 0.96f, 0.78f, 0.44f, 0.42f, 0.72f, 0.66f,
          0.26f, 0.30f, 0.38f, 0.34f };

        private static readonly Vector2[] Head =
        {
            new( 0.09f, 0.22f),    // 턱 앞 — 몸통 목까지 내려와 겹친다
            new( 0.17f, 0.29f),    // 부리 아래
            new( 0.27f, 0.335f),   // 부리 끝 — 한 점으로 모으지 않고 둘로 끊어 뭉툭하게
            new( 0.265f, 0.375f),
            new( 0.16f, 0.41f),    // 부리 위
            new( 0.09f, 0.47f),    // 이마
            new( 0.00f, 0.52f),    // 정수리
            new(-0.09f, 0.48f),    // 뒤통수
            new(-0.13f, 0.38f),    // 목덜미
            new(-0.07f, 0.26f),    // 목 뒤
        };

        private static readonly float[] HeadW =
        { 0.66f, 0.34f, 0.16f, 0.16f, 0.34f, 0.80f, 1.00f, 0.96f, 0.86f, 0.68f };

        private static readonly Vector2[] Wing =
        {
            new( 0.09f,  0.07f),   // 어깨 앞
            new( 0.11f, -0.05f),   // 앞깃
            new( 0.04f, -0.22f),
            new(-0.05f, -0.34f),   // 끝 — 뾰족하지 않게 둘로
            new(-0.12f, -0.33f),
            new(-0.17f, -0.16f),   // 뒷깃
            new(-0.13f,  0.02f),
            new(-0.04f,  0.09f),   // 어깨 뒤
        };

        private static readonly float[] WingW =
        { 0.85f, 0.70f, 0.45f, 0.26f, 0.26f, 0.55f, 0.80f, 0.90f };

        private static readonly Vector2 HeadHinge = new(0.00f, 0.25f);
        private static readonly Vector2 WingHinge = new(-0.02f, 0.05f);

        // 반두께(월드 유닛). 몸 폭 0.51 — CharacterController 지름 0.60 안에 든다.
        private const float BodyBulge = 0.320f;
        private const float HeadBulge = 0.225f;
        private const float WingBulge = 0.036f;

        private static readonly float[] BodyInsets = { 0.035f, 0.075f, 0.115f };
        private static readonly float[] HeadInsets = { 0.026f, 0.055f, 0.082f };
        private static readonly float[] WingInsets = { 0.022f, 0.048f, 0.072f };
        private static readonly float[] Curve = { 0.52f, 0.84f, 1.00f };

        public static GameObject Build(Transform parent, Material paper, Material head,
                                       Material shade, Material ribbon, Material outline)
        {
            float s = Height / (0.52f - (-0.48f));   // 정확히 1.40
            float footY = 0.48f * s;                 // 발끝을 y=0 으로

            var root = new GameObject("책갈피새").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, footY, 0f);

            // ── 몸통
            var bodyRoot = Pivot("몸통", root, Vector3.zero);
            Loft("껍질", bodyRoot, Body, BodyW, BodyBulge, Vector2.zero, s,
                 BodyInsets, paper, outline, 0.012f);

            // ── 고개. 경첩은 목, 회전은 X축(끄덕임)
            var headPivot = Pivot("고개", root,
                new Vector3(0f, HeadHinge.y * s, HeadHinge.x * s));
            Loft("껍질", headPivot, Head, HeadW, HeadBulge, HeadHinge, s,
                 HeadInsets, head, outline, 0.010f);

            // ── 날개. 어깨 경첩, 몸통 옆구리에 붙인다
            var wings = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                var pv = Pivot($"날개_{(sgn > 0f ? "좌" : "우")}", root,
                    new Vector3(sgn * BodyBulge * 0.50f,
                                WingHinge.y * s, WingHinge.x * s));
                var g = Loft("껍질", pv, Wing, WingW, WingBulge, WingHinge, s,
                             WingInsets, shade, outline, 0.009f);
                g.transform.localRotation = Quaternion.Euler(0f, sgn * 6f, 0f);
                wings[i] = pv;
            }

            // ── 꽁지의 책갈피 끈. 여기만 납작한 띠가 맞다 — 접지 않는다.
            //    길면 부리처럼 읽히고, 발끝 아래로 내려가면 다리로 보인다.
            //    뒤로 조금, 아래로 많이. 끝은 가늘게.
            var tail = Pivot("리본", root, new Vector3(0f, -0.09f * s, -0.26f * s));
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                float amp = i == 0 ? 1f : 0.78f;
                var top = new List<Vector2>();
                var bot = new List<Vector2>();
                for (int t = 0; t <= 6; t++)
                {
                    float u = t / 6f;
                    float z = -0.07f * u;
                    float y = (-0.30f * u * u - 0.03f) * amp;
                    float half = 0.058f * (1f - 0.28f * u);
                    top.Add(new Vector2(z, y + half));
                    bot.Add(new Vector2(z, y - half));
                }
                bot.Reverse();
                top.AddRange(bot);

                var g = Panel($"가닥_{i + 1}", tail, top.ToArray(), Vector2.zero, s,
                              ribbon, outline, 0.008f);
                g.transform.localPosition = new Vector3(sgn * 0.035f * s, 0f, 0f);
                g.transform.localRotation =
                    Quaternion.Euler(0f, sgn * (i == 0 ? 13f : 9f), 0f);
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

        // ------------------------------------------------------------ 모양 만들기

        /// <summary>
        /// 코너컷. 꼭짓점 하나를 둘로 갈라 각을 깎는다. 부푸는 가중치도 같이 섞어야
        /// 두께가 튀지 않는다. 두 번 돌리면 새 모양이 통째로 달걀이 되므로 한 번만.
        /// </summary>
        private static void Chaikin(Vector2[] p, float[] w,
                                    out Vector2[] op, out float[] ow)
        {
            int n = p.Length;
            op = new Vector2[n * 2];
            ow = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                op[i * 2] = p[i] * 0.75f + p[j] * 0.25f;
                op[i * 2 + 1] = p[i] * 0.25f + p[j] * 0.75f;
                ow[i * 2] = w[i] * 0.75f + w[j] * 0.25f;
                ow[i * 2 + 1] = w[i] * 0.25f + w[j] * 0.75f;
            }
        }

        /// <summary>
        /// 각 꼭짓점을 안쪽 이등분선 방향으로 d 만큼 민다.
        ///
        /// 안쪽 법선 부호를 뒤집으면 인셋이 바깥으로 나가 모델이 통째로 커진다.
        /// (실제로 40% 커진 적이 있다 — 옆모습이 감자가 됐다.)
        /// </summary>
        private static Vector2[] Inset(Vector2[] pts, float d)
        {
            int n = pts.Length;
            float area = 0f;
            Vector2 c = Vector2.zero;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
                c += a;
            }
            float sign = area > 0f ? 1f : -1f;
            c /= n;

            var res = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = pts[(i - 1 + n) % n];
                Vector2 next = pts[(i + 1) % n];
                Vector2 e0 = (pts[i] - prev).normalized;
                Vector2 e1 = (next - pts[i]).normalized;
                Vector2 n0 = new Vector2(-e0.y, e0.x) * sign;
                Vector2 n1 = new Vector2(-e1.y, e1.x) * sign;

                Vector2 bis = n0 + n1;
                bis = bis.sqrMagnitude > 1e-8f ? bis.normalized : n0;

                // 예각에서 이등분선이 짧아지는 보정(1/cos). 과하면 튀므로 제한한다
                float k = Mathf.Min(1f / Mathf.Max(Vector2.Dot(bis, n0), 0.35f), 2.2f);
                Vector2 q = pts[i] + bis * (d * k);

                // 폭보다 두꺼운 인셋이면 반대쪽으로 넘어간다 — 중심 근처로 붙인다
                if (Vector2.Dot(q - c, pts[i] - c) < 0f) q = c + (pts[i] - c) * 0.12f;
                res[i] = q;
            }
            return res;
        }

        private static Vector3 Vert(Vector2 xy, float b, float sgn, float s)
            => new(sgn * b, xy.y * s, xy.x * s);

        /// <summary>x 를 뒤집은 쪽은 감김이 반대다. 안 뒤집으면 법선이 안쪽을 향해
        /// 그 절반이 통째로 컬링된다 — 뒤에서 보면 새가 검게 나온다.</summary>
        private static void Tri(List<Vector3> t, float sgn, Vector3 a, Vector3 b, Vector3 c)
        {
            if (sgn > 0f) { t.Add(a); t.Add(b); t.Add(c); }
            else { t.Add(c); t.Add(b); t.Add(a); }
        }

        /// <summary>닫힌 2D 외곽선 → 접힌 종이 같은 입체 한 덩어리.</summary>
        private static GameObject Loft(string name, Transform parent, Vector2[] rawProf,
                                       float[] rawW, float bulge, Vector2 origin,
                                       float s, float[] insets, Material mat,
                                       Material outline, float thickness)
        {
            Chaikin(rawProf, rawW, out Vector2[] prof, out float[] w);

            int n = prof.Length;
            var flat = new Vector2[n];
            float wavg = 0f;
            for (int i = 0; i < n; i++) { flat[i] = prof[i] - origin; wavg += w[i]; }
            wavg /= n;

            int layers = insets.Length + 1;
            var lxy = new Vector2[layers][];
            var lb = new float[layers][];
            lxy[0] = flat;
            lb[0] = new float[n];
            for (int k = 0; k < insets.Length; k++)
            {
                lxy[k + 1] = Inset(flat, insets[k]);
                var bb = new float[n];
                for (int i = 0; i < n; i++)
                    bb[i] = Curve[k] * bulge * (w[i] * 0.55f + wavg * 0.45f);
                lb[k + 1] = bb;
            }

            Vector2 capXy = Vector2.zero;
            float capB = 0f;
            for (int i = 0; i < n; i++) { capXy += lxy[layers - 1][i]; capB += lb[layers - 1][i]; }
            capXy /= n;
            capB /= n;

            var t = new List<Vector3>();
            for (int m = 0; m < 2; m++)
            {
                float sgn = m == 0 ? 1f : -1f;
                for (int k = 0; k < layers - 1; k++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        Vector3 p0 = Vert(lxy[k][i], lb[k][i], sgn, s);
                        Vector3 p1 = Vert(lxy[k][j], lb[k][j], sgn, s);
                        Vector3 p2 = Vert(lxy[k + 1][j], lb[k + 1][j], sgn, s);
                        Vector3 p3 = Vert(lxy[k + 1][i], lb[k + 1][i], sgn, s);
                        Tri(t, sgn, p0, p1, p2);
                        Tri(t, sgn, p0, p2, p3);
                    }
                }
                Vector3 cv = Vert(capXy, capB, sgn, s);
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Tri(t, sgn,
                        Vert(lxy[layers - 1][i], lb[layers - 1][i], sgn, s),
                        Vert(lxy[layers - 1][j], lb[layers - 1][j], sgn, s),
                        cv);
                }
            }

            // 꼭짓점을 삼각형마다 따로 둔다 — 그래야 면마다 법선이 갈려
            // 매끈한 곡면이 아니라 '접힌 면' 으로 보인다.
            var verts = new Vector3[t.Count];
            var idx = new int[t.Count];
            for (int i = 0; i < t.Count; i++) { verts[i] = t[i]; idx[i] = i; }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.triangles = idx;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            if (outline != null) PencilOutline.Attach(go, outline, thickness);
            return go;
        }

        /// <summary>
        /// 납작한 띠 한 장. 리본에만 쓴다 — 종이 끈은 두께가 없는 게 맞다.
        /// 뒷면도 보여야 하므로 삼각형을 양쪽으로 넣는다.
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

            var tris = new List<int>();
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
