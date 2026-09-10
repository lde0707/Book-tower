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
using BookTower.Presentation;
using UnityEngine.Rendering.Universal;
using BookTower.Interaction;
using BookTower.Puzzles;
using BookTower.Presentation;
using UnityEngine.Rendering.Universal;
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
    // ─────────────────────────────────────────── 팔레트 (스케치북)
    //
    // 흰 종이가 바탕이고, 물체는 연필로 그린 뒤 크레파스로 칠한 것처럼 보여야 한다.
    // 그래서 면은 거의 흰색이고, 형태를 읽게 하는 건 색이 아니라 '선'과 '그림자'다.
    //
    // 이 방향이 그림자 퍼즐에 특히 유리하다 —
    // 화면에서 어두운 덩어리가 그림자밖에 없게 되므로 정답이 가장 잘 읽힌다.
    private static readonly Color PaperLight = Hex("#FBFAF6");   // 빛을 정면으로 받은 면
    private static readonly Color PaperMid = Hex("#F2EFE7");     // 기본 면
    private static readonly Color PaperShade = Hex("#DED8CC");   // 그늘진 면
    private static readonly Color Graphite = Hex("#3A3733");     // 연필 선 · 외곽선

    // 탑 바깥은 검정 공허가 아니라 **아직 그려지지 않은 빈 종이**다.
    // 떨어지면 어둠이 아니라 백지로 사라진다 — 스케치북 세계관에 맞고,
    // 흰 배경 위에서는 외곽선이 형태를 잡아 주므로 물체가 오히려 또렷해진다.
    private static readonly Color VoidColor = Hex("#FDFCFA");
    private static readonly Color SeaColor = Hex("#F4F1EA");

    private static readonly Color LightWarm = Hex("#F2C879");    // 채도가 있는 유일한 색
    private static readonly Color Ribbon = Hex("#D9694F");       // 책갈피새 — 크레파스 빨강
    /// <summary>그림자. 완전 검정 금지 — 종이에 지는 그림자는 세피아 그레이다</summary>
    /// <summary>그림자. 연필로 문지른 회색이지 검정이 아니다</summary>
    private static readonly Color ShadowTint = Hex("#6B6B73");   // 살짝 차갑게 — 연필 흑연색

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
        Material matPaperShade = MakeMaterial("M_Paper_Shade", PaperShade, "T_Pencil");
        Material matRibbon = MakeMaterial("M_Ribbon", Ribbon, "T_Crayon");
        Material matAccent = MakeMaterial("M_Accent", LightWarm, "T_Crayon");
        Material matOutline = MakeOutlineMaterial();

        BuildSystems();
        BuildLighting();
        BuildBackground(matPaperLight, matPaperShade);

        GameObject player = BuildPlayer(matRibbon, matOutline);

        // 레벨 디자인 문서의 배치를 그대로 생성
        Transform map = LevelBuilder.Build(new LevelBuilder.Palette
        {
            paperLight = matPaperLight,
            paperMid = matPaperMid,
            paperShade = matPaperShade,
            accent = matAccent,
            outline = matOutline
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

        // 각도는 고정이다. 방마다 바꾸지 않는다.
        // "접힘 → 그림자" 인과가 방마다 달라지면 60분 동안 배운 규칙이 무너진다.
        // 세기만 LightingDirector 가 층에 따라 바꾼다.
        sunGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        // 주광원은 거의 흰색이다.
        //
        // 여기에 #F2C879 를 쓰면 흰 종이가 전부 누렇게 물든다. 실제로 그랬다.
        // "빛 = 유일한 채도색" 규칙은 **강조광**이 지킨다 — 그 등이 닿은 자리만
        // 따뜻한 색을 얻고, 나머지는 흰 종이로 남는다.
        // 오히려 이쪽이 "빛이 닿는 곳만 색을 얻는다"에 더 충실하다.
        sun.color = Hex("#FFFCF4");
        sun.intensity = 1.0f;      // 실제 값은 방에 들어갈 때 정해진다

        // 그림자가 곧 퍼즐의 정답이므로 품질은 타협하지 않는다.
        // Soft 를 쓰되 해상도를 올려서 엣지를 붙잡는다 — Hard 는 계단이 심하게 져서
        // 오히려 모양이 안 읽힌다. 아트 문서의 "엣지는 항상 선명하게"는
        // '흐리지 않게'라는 뜻이지 '계단지게'가 아니다.
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.62f;   // 흰 종이 위에서는 0.8이면 그림자가 먹처럼 진해진다
        sun.shadowBias = 0.02f;
        sun.shadowNormalBias = 0.2f;
        sun.shadowNearPlane = 0.1f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        // 방 하나만 보이므로 그림자 거리를 짧게 잡을수록 픽셀 밀도가 올라간다
        var urp = UniversalRenderPipeline.asset;
        if (urp != null)
        {
            var so = new SerializedObject(urp);
            SetIfExists(so, "m_ShadowDistance", 120f);
            SetIfExists(so, "m_Cascade2Split", 0.25f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }

        // 두 번째 그림자를 만드는 광원은 두지 않는다 (정답이 흐려짐).
        // 강조광은 전부 LightShadows.None 이라 이 규칙을 어기지 않는다.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ShadowTint * 0.42f;

        // 안개도 흰색이다. 멀어질수록 백지로 흐려지면서
        // "여기까지가 그려진 부분"이라는 느낌이 난다
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = VoidColor;
        RenderSettings.fogDensity = 0.010f;

        var director = group.gameObject.AddComponent<LightingDirector>();
        SetPrivate(director, "sun", sun);
        SetColor(director, "shadowTint", ShadowTint);
    }

    private static void SetIfExists(SerializedObject so, string field, float v)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.floatValue = v;
    }

    private static void SetColor(Object target, string field, Color v)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p != null) { p.colorValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
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

    private static GameObject BuildPlayer(Material ribbonMat, Material matOutline)
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
        // 작은 턱에 걸려 멈추는 게 조작 불만의 대부분이다.
        // 점프보다 이쪽이 먼저 할 일이고, 퍼즐에는 아무 영향이 없다.
        cc.stepOffset = 0.45f;
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
        BookTower.Presentation.PencilOutline.Attach(body, matOutline, 0.03f);
        body.GetComponent<MeshRenderer>().sharedMaterial = ribbonMat;

        var beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beak.name = "TEMP_Beak";
        beak.transform.SetParent(player.transform, false);
        beak.transform.localPosition = new Vector3(0f, 0.45f, 0.28f);
        beak.transform.localScale = new Vector3(0.08f, 0.08f, 0.22f);
        UnityEngine.Object.DestroyImmediate(beak.GetComponent<Collider>());
        BookTower.Presentation.PencilOutline.Attach(beak, matOutline, 0.02f);
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

        // 퍼즐이 풀리면 방의 등이 확 밝아진다. 이게 없으면 풀어도 아무 일이 없다
        if (host.GetComponent<PuzzleFeedback>() == null) host.AddComponent<PuzzleFeedback>();

        // 개발용 조작 안내. 출시 빌드에서는 빼야 한다
        if (host.GetComponent<PrototypeHud>() == null) host.AddComponent<PrototypeHud>();

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

    private const string TexDir = "Assets/_BookTower/Textures";

    private static Material MakeMaterial(string name, Color color, string texture = "T_Paper")
    {
        string path = $"{MatDir}/{name}.mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // 이미 있으면 새로 만들지 않고 값만 덮어쓴다.
        // 예전엔 있으면 그대로 반환했는데, 그러면 팔레트를 바꿔도
        // 씬을 다시 지을 때 옛날 색이 그대로 남는다.
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader) { name = name };
        else if (mat.shader != shader) mat.shader = shader;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        // 종이는 반사하지 않는다. 광택이 있으면 종이가 아니라 플라스틱으로 보인다
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);

        // 결이 없으면 아무리 색을 맞춰도 '3D 판때기'로 보인다.
        // 종이 결·크레파스 자국이 손으로 만든 물건이라는 신호를 준다
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{texture}.png");
        if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (tex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        if (isNew) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>외곽선용 — 빛을 안 받는 단색. 연필 선처럼 보여야 한다</summary>
    private static Material MakeOutlineMaterial()
    {
        string path = $"{MatDir}/M_Outline.mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader) { name = "M_Outline" };
        else if (mat.shader != shader) mat.shader = shader;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Graphite);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Graphite);

        // 앞면을 잘라내면 뒷면만 남는다. 살짝 키운 껍질에 이걸 쓰면
        // 물체 가장자리에만 어두운 테두리가 보인다 — 셰이더를 새로 안 써도 된다
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 1f);   // Front

        if (isNew) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }


    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
