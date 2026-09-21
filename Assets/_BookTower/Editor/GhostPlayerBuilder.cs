using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using BookTower.Presentation;

namespace BookTower.EditorTools
{
    /// <summary>
    /// 정령 플레이어 — Quaternius Ghost (CC0) 를 씬에 붙인다.
    ///
    /// 왜 기성 에셋인가
    ///   "플레이어를 새 말고 정령으로" 라는 요구에 맞는 무료 에셋을 찾았고,
    ///   이 프로젝트 기준으로는 운이 좋은 물건이었다.
    ///     · 텍스처가 없다 — 머티리얼 3개가 단색뿐이라 종이 팔레트로 갈아입히는 게
    ///       값 세 개 수정으로 끝난다. 보통 외부 에셋에서 제일 비싼 게 이 작업이다
    ///     · 세 머티리얼 모두 OPAQUE — 반투명·발광이었으면 그림자가 안 져서
    ///       P15 왜상과 1C 교차그림자가 통째로 성립하지 않는다
    ///     · Flying_Idle + Fast_Flying 이 있어 "떠 있고 / 떠서 이동" 이 된다
    ///
    /// 왜 glTFast 를 붙였나
    ///   유니티는 .glb 를 기본으로 못 읽는다. 받을 수 있는 건 glb 뿐이었고
    ///   (FBX 다운로드는 브라우저 패널에서 안 떨어졌다), 그래서
    ///   com.unity.cloud.gltfast 6.20.0 (Unity 6000.0 대상 최신) 을 manifest 에 추가했다.
    ///   glTFast 는 자기 셰이더로 임포트하지만, 어차피 머티리얼을 전부
    ///   갈아 끼우므로 상관없다.
    ///
    /// 색과 명암 (요청: "포인트 컬러는 다르게, 명암도 넣어 줘")
    ///   머티리얼 셋을 전부 비슷한 베이지로 칠했더니 얼굴이 몸에 묻혔다.
    ///   역할별로 가른다.
    ///     몸통       T_Ghost_Ramp — 위는 따뜻한 종이, 아래는 차가운 회색
    ///     눈알       밝은 종이 #F5F0E8 — 몸보다 한 단계 떠오르게
    ///     눈동자·입  잉크 #4A4038 — 얼굴을 읽게 하는 유일한 진한 색
    ///     리본       크레파스 빨강 — **포인트.** 책갈피새의 정체성을 이어받는다
    ///   명암은 텍스처로 굽는다. 방이 밝아 실시간 음영만으로는 덩어리가 안 선다.
    ///   그런데 **모델에 UV 가 없다** — 그래서 메시를 복제해 UV 를 새로 만든다.
    ///
    /// 아직 안 된 것
    ///   **외곽선이 없다.** PencilOutline 은 메시를 살짝 키운 껍질을 자식으로
    ///   다는 방식인데, 스킨드 메시는 정점이 본 공간에 있어서 트랜스폼을 키워도
    ///   껍질이 안 생긴다. 제대로 하려면 법선 방향으로 밀어내는 셰이더가 필요하고,
    ///   그건 잘못 쓰면 프로젝트가 통째로 컴파일이 안 되므로 따로 다룬다.
    /// </summary>
    public static class GhostPlayerBuilder
    {
        public const float Height = 1.40f;          // 책갈피새와 같은 키

        private const string ModelPath = "Assets/ThirdParty/Quaternius/Quaternius_Ghost.glb";
        private const string CtrlPath = "Assets/_BookTower/Animation/AC_Ghost.controller";

        // 클립 이름은 "CharacterArmature|Flying_Idle" 처럼 앞에 아마추어 이름이
        // 붙어 들어온다. 임포터에 따라 접두사가 떨어지기도 하므로 **부분 일치**로 찾는다.
        private const string IdleClip = "Flying_Idle";
        private const string MoveClip = "Fast_Flying";

        /// <summary>
        /// 팔을 없앨지. 요청: "팔이 없는 정령이면 좋겠어".
        /// 위팔 본을 접는 방식이라 메시는 그대로다 — false 로 두면 팔이 돌아온다.
        /// </summary>
        private const bool RemoveArms = true;
        private static readonly string[] ArmBones = { "UpperArm.L", "UpperArm.R" };

        private const string GradedMeshPath = "Assets/_BookTower/Generated/Ghost_Graded.asset";
        private const string RampTexPath = "Assets/_BookTower/Textures/T_Ghost_Ramp.png";

        /// <summary>
        /// 리본을 묶는 자리. 목 본에서 머리 쪽으로 15% 올리고 뒤로 뺀다.
        /// 꼬리 끝에 매달았을 때는 아래로 늘어져 다리·발톱처럼 읽혔다 —
        /// 포인트 색은 얼굴 가까이 있어야 시선이 모인다.
        /// </summary>
        private const float KnotTowardHead = 0.15f;
        private const float KnotBack = 0.20f;

        /// <summary>움직이기 시작했다고 볼 속도. PlayerController 의 걷기 속도보다 한참 아래</summary>
        private const float MoveThreshold = 0.9f;

        public static GameObject Build(Transform parent, Material body,
                                       Material eyeWhite, Material ink,
                                       Material ribbon)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (src == null)
            {
                Debug.LogError(
                    $"[GhostPlayerBuilder] 모델을 못 찾았습니다: {ModelPath}\n" +
                    "glTFast 패키지가 아직 안 붙었을 수 있습니다. " +
                    "Packages/manifest.json 의 com.unity.cloud.gltfast 를 확인하고 " +
                    "에디터가 패키지를 받아올 때까지 기다린 뒤 다시 실행하세요.");
                return null;
            }

            var root = new GameObject("정령").transform;
            root.SetParent(parent, false);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src, root);
            model.name = "모델";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            FitHeight(model, root);
            EnsureRampImport();
            GradeMesh(model);
            Repaint(model, body, eyeWhite, ink);

            Animator anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = BuildController(src);
            anim.applyRootMotion = false;

            Transform[] ribbons = TieRibbons(model.transform, root, ribbon);

            var rig = root.gameObject.AddComponent<GhostRig>();
            rig.Wire(anim, RemoveArms ? FindBones(model.transform, ArmBones) : null, ribbons);
            return root.gameObject;
        }

        /// <summary>
        /// 이름으로 본을 찾는다. 임포터가 "UpperArm.L" 을 "UpperArm_L" 로
        /// 바꾸는 경우가 있어 '.' 과 '_' 를 같은 것으로 본다. 못 찾으면 경고한다 —
        /// 조용히 넘어가면 팔이 그대로 남아 있는데도 된 줄 안다.
        /// </summary>
        private static Transform[] FindBones(Transform root, string[] names)
        {
            string Norm(string n) => n.Replace('_', '.').Replace(' ', '.');
            var all = root.GetComponentsInChildren<Transform>(true);
            var found = new List<Transform>();
            foreach (string want in names)
            {
                Transform t = all.FirstOrDefault(x => Norm(x.name) == Norm(want))
                              ?? all.FirstOrDefault(x => Norm(x.name).EndsWith(Norm(want)));
                if (t != null) found.Add(t);
                else Debug.LogWarning($"[GhostPlayerBuilder] 본 '{want}' 을 못 찾았습니다. 팔이 남습니다.");
            }
            return found.ToArray();
        }

        /// <summary>
        /// 키를 1.40 에 맞춘다.
        ///
        /// 배율을 상수로 박지 않는 이유: 임포터가 단위를 어떻게 잡을지 모른다.
        /// 실제로 생성된 렌더러의 경계에서 재는 쪽이 임포터가 바뀌어도 안 깨진다.
        /// 발끝은 y=0 에 맞춘다 — 띄우는 건 GhostRig 가 연출로 한다.
        /// </summary>
        private static void FitHeight(GameObject model, Transform root)
        {
            var rends = model.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);

            float h = Mathf.Max(b.size.y, 0.0001f);
            float s = Height / h;
            model.transform.localScale = Vector3.one * s;

            // 배율을 바꿨으니 경계를 다시 잰다
            b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);
            float footGap = b.min.y - root.position.y;
            model.transform.localPosition -= new Vector3(0f, footGap, 0f);
        }

        /// <summary>
        /// 원본 색은 거의 검정이라 종이 배경 위에서 구멍처럼 보인다.
        /// 텍스처가 없어 머티리얼만 갈아 끼우면 된다.
        ///
        /// 서브메시 셋의 실제 역할 (렌더해서 확인했다 — 이름과 다르다)
        ///   Eye_White  눈알 두 개
        ///   Eye_Black  눈꺼풀 선 + **입** — 이름은 눈인데 입까지 이쪽이다
        ///   Ghost_Main 몸통
        /// </summary>
        private static void Repaint(GameObject model, Material body,
                                    Material eyeWhite, Material ink)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = r.sharedMaterials[i] != null
                        ? r.sharedMaterials[i].name : "";
                    if (n.Contains("Eye_Black")) mats[i] = ink;
                    else if (n.Contains("Eye")) mats[i] = eyeWhite;
                    else mats[i] = body;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        /// <summary>
        /// 명암 텍스처는 세로로 한 번만 깔린다. 기본값(Repeat)이면
        /// 맨 위 밝은 줄과 맨 아래 어두운 줄이 서로 번져 정수리에 회색 띠가 생긴다.
        /// </summary>
        private static void EnsureRampImport()
        {
            var ti = AssetImporter.GetAtPath(RampTexPath) as TextureImporter;
            if (ti == null) return;
            bool dirty = false;
            if (ti.wrapModeV != TextureWrapMode.Clamp) { ti.wrapModeV = TextureWrapMode.Clamp; dirty = true; }
            if (ti.wrapModeU != TextureWrapMode.Repeat) { ti.wrapModeU = TextureWrapMode.Repeat; dirty = true; }
            if (!ti.sRGBTexture) { ti.sRGBTexture = true; dirty = true; }
            if (dirty) ti.SaveAndReimport();
        }

        /// <summary>
        /// 모델에 UV 가 없다. 텍스처로 명암을 주려면 새로 만들어야 한다.
        ///
        ///   v = 높이 (0 꼬리 끝 → 1 정수리) — 명암 그러데이션이 이걸 따라간다
        ///   u = 몸을 한 바퀴 도는 각도 — 종이결이 둘레를 따라 이어지게
        ///
        /// 원본 메시를 고치지 않고 **복제본**에 쓴다. 임포트된 에셋은 다시
        /// 임포트될 때 되돌아가므로, 복제본을 에셋으로 저장해 씬이 그걸 물게 한다.
        /// 스킨 가중치·본은 그대로 딸려 온다.
        /// </summary>
        private static void GradeMesh(GameObject model)
        {
            foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh src = smr.sharedMesh;
                if (src == null) continue;

                Mesh m = Object.Instantiate(src);
                m.name = src.name + "_Graded";

                Vector3[] v = m.vertices;
                float lo = float.MaxValue, hi = float.MinValue;
                Vector3 c = Vector3.zero;
                foreach (Vector3 p in v)
                {
                    lo = Mathf.Min(lo, p.y);
                    hi = Mathf.Max(hi, p.y);
                    c += p;
                }
                c /= Mathf.Max(v.Length, 1);
                float span = Mathf.Max(hi - lo, 1e-5f);

                var uv = new Vector2[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    float ang = Mathf.Atan2(v[i].x - c.x, v[i].z - c.z) / (2f * Mathf.PI) + 0.5f;
                    uv[i] = new Vector2(ang, (v[i].y - lo) / span);
                }
                m.uv = uv;

                string dir = System.IO.Path.GetDirectoryName(GradedMeshPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets/_BookTower", "Generated");
                AssetDatabase.DeleteAsset(GradedMeshPath);
                AssetDatabase.CreateAsset(m, GradedMeshPath);

                smr.sharedMesh = m;
            }
        }

        /// <summary>
        /// 목 뒤에 책갈피 끈 두 가닥을 묶는다.
        ///
        /// 만드는 건 모델 기준 월드 좌표로 하고, 붙이는 건 목 본에 붙인다
        /// (worldPositionStays). 그래야 고개를 끄덕일 때 리본이 따라가고,
        /// 본의 로컬 축이 임포터마다 어떻게 돌아 있든 상관없다.
        ///
        /// 각 가닥의 피벗을 매듭 자리에 둔다 — GhostRig 가 그 점을 축으로 흔든다.
        /// </summary>
        private static Transform[] TieRibbons(Transform model, Transform root, Material mat)
        {
            Transform neck = FindBones(model, new[] { "Neck" }).FirstOrDefault();
            Transform head = FindBones(model, new[] { "Head" }).FirstOrDefault();
            if (neck == null || mat == null) return new Transform[0];

            Vector3 up = root.up, fwd = root.forward, right = root.right;
            Vector3 knot = neck.position
                           + (head != null ? (head.position - neck.position) * KnotTowardHead : Vector3.zero)
                           - fwd * KnotBack;

            var outs = new List<Transform>();
            for (int i = 0; i < 2; i++)
            {
                float sgn = i == 0 ? 1f : -1f;
                float spread = (i == 0 ? 26f : 20f) * Mathf.Deg2Rad;
                float amp = i == 0 ? 1f : 0.85f;

                // 옆모습 프로파일 (뒤로 얼마나, 아래로 얼마나) — 끝으로 갈수록 처지고 가늘다
                var top = new List<Vector2>();
                var bot = new List<Vector2>();
                for (int k = 0; k <= 7; k++)
                {
                    float u = k / 7f;
                    float back = 0.46f * u * amp;
                    float drop = (0.10f * u + 0.16f * u * u) * amp;
                    float half = 0.070f * (1f - 0.40f * u);
                    top.Add(new Vector2(back, -drop + half));
                    bot.Add(new Vector2(back, -drop - half));
                }
                bot.Reverse();
                top.AddRange(bot);

                var go = new GameObject($"리본_{i + 1}");
                go.transform.position = knot + right * (sgn * 0.05f);
                go.transform.rotation = root.rotation;

                int n = top.Count;
                var verts = new Vector3[n * 2];
                for (int k = 0; k < n; k++)
                {
                    Vector2 q = top[k];
                    // 로컬 좌표: x 옆, y 위, z 앞. 뒤로 가면서 옆으로 벌어진다
                    verts[k] = new Vector3(sgn * q.x * Mathf.Sin(spread), q.y, -q.x * Mathf.Cos(spread));
                    verts[k + n] = verts[k];
                }
                var tris = new List<int>();
                for (int k = 1; k < n - 1; k++)
                {
                    tris.Add(0); tris.Add(k); tris.Add(k + 1);          // 앞면
                    tris.Add(n); tris.Add(n + k + 1); tris.Add(n + k);  // 뒷면 — 종이 끈은 양면
                }
                var uvs = new Vector2[n * 2];
                for (int k = 0; k < n * 2; k++)
                    uvs[k] = new Vector2(verts[k].z, verts[k].y) * 2f;

                var mesh = new Mesh { name = go.name };
                mesh.vertices = verts;
                mesh.triangles = tris.ToArray();
                mesh.uv = uvs;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                go.transform.SetParent(neck, true);
                outs.Add(go.transform);
            }
            return outs.ToArray();
        }

        /// <summary>
        /// 정지 ↔ 이동 두 상태짜리 컨트롤러. Speed 하나로만 오간다.
        ///
        /// 모델 파일 안의 클립을 이름 **부분 일치**로 찾는다.
        /// glTF 는 "CharacterArmature|Flying_Idle" 로 들어오는데 임포터가
        /// 접두사를 떼는 경우가 있어서, 정확히 일치로 찾으면 조용히 실패한다.
        /// </summary>
        private static RuntimeAnimatorController BuildController(GameObject src)
        {
            string dir = System.IO.Path.GetDirectoryName(CtrlPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(dir).Replace('\\', '/'),
                                           System.IO.Path.GetFileName(dir));

            List<AnimationClip> clips = AssetDatabase
                .LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(src))
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview"))
                .ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning("[GhostPlayerBuilder] 모델에서 애니메이션 클립을 못 찾았습니다. " +
                                 "정령이 움직이지 않습니다. glb 임포트 설정을 확인하세요.");
                return null;
            }

            AnimationClip Find(string key)
            {
                AnimationClip c = clips.FirstOrDefault(
                    x => x.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (c == null)
                    Debug.LogWarning($"[GhostPlayerBuilder] '{key}' 클립이 없습니다. " +
                                     $"있는 것: {string.Join(", ", clips.Select(x => x.name))}");
                return c;
            }

            AnimationClip idle = Find(IdleClip) ?? clips[0];
            AnimationClip move = Find(MoveClip) ?? idle;

            // 씬을 다시 지을 때마다 새로 만든다. 남아 있는 걸 고쳐 쓰면
            // 상태·전이가 겹겹이 쌓인다.
            AssetDatabase.DeleteAsset(CtrlPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState sIdle = sm.AddState("정지");
            sIdle.motion = idle;
            AnimatorState sMove = sm.AddState("이동");
            sMove.motion = move;
            sm.defaultState = sIdle;

            // 전이는 0.15초로 섞는다. 0 으로 두면 클립이 바뀌는 순간 몸이 튄다.
            AnimatorStateTransition t1 = sIdle.AddTransition(sMove);
            t1.AddCondition(AnimatorConditionMode.Greater, MoveThreshold, "Speed");
            t1.hasExitTime = false;
            t1.duration = 0.15f;

            AnimatorStateTransition t2 = sMove.AddTransition(sIdle);
            t2.AddCondition(AnimatorConditionMode.Less, MoveThreshold, "Speed");
            t2.hasExitTime = false;
            t2.duration = 0.20f;

            EditorUtility.SetDirty(ac);
            return ac;
        }
    }
}
