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

        /// <summary>움직이기 시작했다고 볼 속도. PlayerController 의 걷기 속도보다 한참 아래</summary>
        private const float MoveThreshold = 0.9f;

        public static GameObject Build(Transform parent, Material paper,
                                       Material head, Material shade)
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
            Repaint(model, paper, head, shade);

            Animator anim = model.GetComponentInChildren<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = BuildController(src);
            anim.applyRootMotion = false;

            var rig = root.gameObject.AddComponent<GhostRig>();
            rig.Wire(anim, RemoveArms ? FindBones(model.transform, ArmBones) : null);
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
        /// 텍스처가 없어 baseColor 만 갈아 끼우면 되는 게 이 에셋의 장점이다.
        /// </summary>
        private static void Repaint(GameObject model, Material paper,
                                    Material head, Material shade)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = r.sharedMaterials[i] != null
                        ? r.sharedMaterials[i].name : "";
                    if (n.Contains("Eye_Black")) mats[i] = shade;
                    else if (n.Contains("Eye")) mats[i] = head;
                    else mats[i] = paper;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
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
