using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using BookTower.Core;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Rooms;
using BookTower.Interaction;
using BookTower.Puzzles;
using BookTower.Presentation;
using BookTower.Info;
using BookTower.EchoSystem;
using BookTower.EditorTools;

/// <summary>
/// 작업 씬(InGame)을 통째로 구성한다.
/// 메뉴: Book Tower ▸ InGame 씬 구성하기
///
/// 실제 레벨 지오메트리는 LevelBuilder가 만든다 (Docs/06_레벨디자인.md 기준).
/// 이 파일은 시스템 · 조명 · 배경 · 플레이어 · 카메라를 배치한다.
///
/// 여러 번 눌러도 안전하다 — 이 도구가 만든 루트만 지우고 다시 만든다.
/// </summary>
public static class PrototypeSceneBuilder
{
    private const string TargetScene = "Assets/Scenes/InGame.unity";
    private const string RootDir = "Assets/_BookTower";
    private const string MatDir = RootDir + "/Materials";

    private static readonly string[] GeneratedRoots =
    {
        "[Systems]", "Lighting", "Camera", "Player", "Map", "Background",
        "Main Camera", "Directional Light"
    };

    // 아트 컨셉 문서의 팔레트
    private static readonly Color PaperLight = Hex("#EDE6D6");
    private static readonly Color PaperMid = Hex("#D6CBB4");
    private static readonly Color PaperShade = Hex("#A99B84");
    private static readonly Color VoidColor = Hex("#2E2E33");
    private static readonly Color SeaColor = Hex("#22222A");
    private static readonly Color LightWarm = Hex("#F2C879");
    private static readonly Color Ribbon = Hex("#A8705E");

    [MenuItem("Book Tower/InGame 씬 구성하기", priority = 0)]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog(
                "InGame 씬 구성",
                $"{TargetScene} 을 열고 레벨 전체를 다시 구성합니다.\n" +
                "(표지 F0 + 1장 1-A~1-E)\n\n" +
                "이 도구가 만든 오브젝트만 지워집니다. 직접 추가한 건 유지됩니다.",
                "구성하기", "취소"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolders();

        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Book Tower] 씬을 열지 못했습니다: {TargetScene}");
            return;
        }

        ClearGenerated(scene);

        Material matPaperLight = MakeMaterial("M_Paper_Light", PaperLight);
        Material matPaperMid = MakeMaterial("M_Paper_Mid", PaperMid);
        Material matPaperShade = MakeMaterial("M_Paper_Shade", PaperShade);
        Material matRibbon = MakeMaterial("M_Ribbon", Ribbon);
        Material matAccent = MakeMaterial("M_Accent", LightWarm);

        BuildSystems();
        BuildLighting();
        BuildBackground(matPaperLight, matPaperShade);

        GameObject player = BuildPlayer(matRibbon);

        // 레벨 디자인 문서의 배치를 그대로 생성
        Transform map = LevelBuilder.Build(new LevelBuilder.Palette
        {
            paperLight = matPaperLight,
            paperMid = matPaperMid,
            paperShade = matPaperShade,
            accent = matAccent
        });

        PaperCamera paperCam = BuildCamera(player.transform, map);

        // 룸 매니저 — 지금 있는 방만 그리게 한다.
        // 이게 없으면 나선형으로 쌓인 위층이 카메라와 플레이어 사이를 가로막는다.
        BuildRoomManager(player.transform, paperCam);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        RegisterInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Book Tower] InGame 씬 구성 완료 — 표지(F0) + 1장(1-A~1-E)\n" +
                  "WASD 이동 / Space 활공 / Q·E 카메라 회전 / Tab 전체조망 / 좌클릭 접기");
    }

    private static void ClearGenerated(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects().ToList())
            if (GeneratedRoots.Contains(go.name))
                UnityEngine.Object.DestroyImmediate(go);
    }

    // ---------------------------------------------------------------- 시스템

    private static void BuildSystems()
    {
        var systems = new GameObject("[Systems]");
        systems.AddComponent<GameFlow>();          // 게임 페이즈
        systems.AddComponent<PuzzleDirector>();    // 퍼즐 추적 + 플레이테스트 기록
        systems.AddComponent<ScreenTransition>();  // 화면 전환
        systems.AddComponent<Keepsakes>();         // 유품함 — 정보=열쇠 저장소
    }

    // ------------------------------------------------------------------ 조명

    private static void BuildLighting()
    {
        Transform group = new GameObject("Lighting").transform;

        var sunGo = new GameObject("Sun (탑 꼭대기 빛)");
        sunGo.transform.SetParent(group);
        sunGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = LightWarm;
        sun.intensity = 1.15f;

        // 그림자가 곧 퍼즐의 정답이므로 품질은 타협하지 않는다
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        sun.shadowBias = 0.02f;
        sun.shadowNormalBias = 0.2f;

        // 두 번째 그림자를 만드는 광원은 두지 않는다 (정답이 흐려짐)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.31f, 0.30f);
    }

    // ------------------------------------------------------------------ 배경

    /// <summary>잊혀진 책들의 바다 + 안개 + 떠다니는 낱장</summary>
    private static void BuildBackground(Material paperMat, Material shadeMat)
    {
        Transform group = new GameObject("Background").transform;

        Material seaMat = MakeMaterial("M_Sea", SeaColor);

        var sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sea.name = "Sea (잊혀진 책들의 바다)";
        sea.transform.SetParent(group, false);
        sea.transform.localPosition = new Vector3(0f, -22f, 0f);
        sea.transform.localScale = new Vector3(500f, 0.5f, 500f);
        sea.GetComponent<MeshRenderer>().sharedMaterial = seaMat;
        UnityEngine.Object.DestroyImmediate(sea.GetComponent<Collider>());   // 떨어지면 복귀 처리로 잡는다

        // 안개 — 수평선을 지워 끝이 없어 보이게. 플레이 영역은 안 가리도록 멀리서 시작
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = VoidColor;
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 220f;

        // 떠다니는 낱장 — 스케일 감을 주는 게 목적. 진행 방향으로 흘러 길잡이도 겸한다
        Transform pagesRoot = new GameObject("FloatingPages").transform;
        pagesRoot.SetParent(group, false);

        for (int i = 0; i < 20; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leaf.name = $"Leaf_{i:00}";
            leaf.transform.SetParent(pagesRoot, false);

            leaf.transform.localPosition = new Vector3(
                Random.Range(-50f, 50f),
                Random.Range(-8f, 24f),
                Random.Range(-50f, 50f));

            leaf.transform.localRotation = Random.rotation;

            float s = Random.Range(0.35f, 0.9f);
            leaf.transform.localScale = new Vector3(s, s * 1.3f, 1f);

            leaf.GetComponent<MeshRenderer>().sharedMaterial = (i % 3 == 0) ? shadeMat : paperMat;
            UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());

            leaf.AddComponent<FloatingPage>();
        }
    }

    // ---------------------------------------------------------------- 플레이어

    private static GameObject BuildPlayer(Material ribbonMat)
    {
        Transform group = new GameObject("Player").transform;

        var player = new GameObject("BookmarkBird");
        player.transform.SetParent(group);
        player.transform.position = new Vector3(0f, 1.2f, -6.4f);  // 표지(F0)의 시작 표식 (XZ 1.6배 확대 반영)

        var cc = player.AddComponent<CharacterController>();
        cc.height = 0.9f;
        cc.radius = 0.25f;
        cc.center = new Vector3(0f, 0.45f, 0f);
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.25f;
        cc.skinWidth = 0.02f;

        player.AddComponent<PlayerInputReader>();
        player.AddComponent<PlayerController>();
        player.AddComponent<FoldInteractor>();   // 접기
        player.AddComponent<EchoInteractor>();   // 조각 줍기 + 주름 깨우기

        // 임시 비주얼 — 실제 책갈피새 모델로 교체 예정
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "TEMP_Body";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<MeshRenderer>().sharedMaterial = ribbonMat;

        var beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beak.name = "TEMP_Beak";
        beak.transform.SetParent(player.transform, false);
        beak.transform.localPosition = new Vector3(0f, 0.45f, 0.28f);
        beak.transform.localScale = new Vector3(0.08f, 0.08f, 0.22f);
        UnityEngine.Object.DestroyImmediate(beak.GetComponent<Collider>());
        beak.GetComponent<MeshRenderer>().sharedMaterial = ribbonMat;

        return player;
    }

    // ---------------------------------------------------------------- 카메라

    private static PaperCamera BuildCamera(Transform target, Transform frameTarget)
    {
        Transform group = new GameObject("Camera").transform;

        var camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(group);
        camGo.tag = "MainCamera";

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = VoidColor;

        camGo.AddComponent<AudioListener>();

        var paperCam = camGo.AddComponent<PaperCamera>();
        paperCam.target = target;

        // Tab으로 전체 조망 전환할 때 이 대상을 화면에 맞춘다
        SetPrivate(paperCam, "frameTarget", frameTarget);

        return paperCam;
    }

    // ---------------------------------------------------------------- 룸

    private static void BuildRoomManager(Transform player, PaperCamera cam)
    {
        GameObject systems = GameObject.Find("[Systems]");
        GameObject host = systems != null ? systems : new GameObject("[Systems]");

        var mgr = host.AddComponent<RoomManager>();
        SetPrivate(mgr, "player", player);
        SetPrivate(mgr, "paperCamera", cam);
    }

    // ---------------------------------------------------------------- 유틸

    private static void SetPrivate(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RegisterInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == TargetScene)) return;

        scenes.Insert(0, new EditorBuildSettingsScene(TargetScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(RootDir))
            AssetDatabase.CreateFolder("Assets", "_BookTower");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder(RootDir, "Materials");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = $"{MatDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
