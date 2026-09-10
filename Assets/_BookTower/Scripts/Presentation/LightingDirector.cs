using UnityEngine;
using UnityEngine.Rendering;

namespace BookTower.Presentation
{
    /// <summary>
    /// 방마다 다른 빛을 적용한다.
    ///
    /// 아트 문서(Docs/02) 7절의 규칙을 코드로 옮긴 것이다.
    ///   · 주광원은 1개. 방향이 항상 명확해야 플레이어가 그림자를 예측할 수 있다
    ///   · 광량은 층이 올라갈수록 증가한다 — 표지가 가장 어둡고 3장이 가장 밝다
    ///   · 두 번째 그림자를 만드는 광원은 금지
    ///
    /// 그래서 방이 바뀌어도 태양의 '각도'는 절대 안 바꾼다. 세기만 바꾼다.
    /// 각도가 방마다 달라지면 "접힘 → 그림자" 인과가 방마다 달라져서,
    /// 60분 동안 배운 규칙이 무너진다.
    ///
    /// 앰비언트를 0으로 두지 않는 이유는 팔레트에 '그림자는 완전 검정 금지'라고
    /// 적혀 있기 때문이기도 하지만, 실질적으로는 그림자 안의 형태가 안 읽히면
    /// 퍼즐 자체가 불가능해지기 때문이다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LightingDirector : MonoBehaviour
    {
        [Header("주광원 — 탑 꼭대기의 빛")]
        [SerializeField] private Light sun;

        [Tooltip("그림자 색. 완전 검정 금지 — 종이에 지는 그림자는 세피아 그레이다")]
        [SerializeField] private Color shadowTint = new(0.36f, 0.34f, 0.28f);

        [Header("전환")]
        [Tooltip("방이 바뀔 때 빛이 옮겨가는 속도. 즉시 바뀌면 눈이 놀란다")]
        [SerializeField] private float blendSpeed = 2.2f;

        private float targetSun = 1f, targetAmbient = 0.3f, targetFog = 0.01f;
        private float curSun = -1f, curAmbient, curFog;

        public static LightingDirector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (sun == null) sun = FindFirstObjectByType<Light>();
        }

        private void Update()
        {
            if (curSun < 0f) return;

            float k = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
            curSun = Mathf.Lerp(curSun, targetSun, k);
            curAmbient = Mathf.Lerp(curAmbient, targetAmbient, k);
            curFog = Mathf.Lerp(curFog, targetFog, k);
            Push();
        }

        /// <summary>방의 조명 프로필을 목표로 삼는다. RoomManager 가 호출한다.</summary>
        public void ApplyRoom(float sunIntensity, float ambient, float fog,
                              float shadowStrength, bool snap)
        {
            targetSun = sunIntensity;
            targetAmbient = ambient;
            targetFog = fog;

            if (sun != null) sun.shadowStrength = shadowStrength;

            if (snap || curSun < 0f)
            {
                curSun = targetSun; curAmbient = targetAmbient; curFog = targetFog;
                Push();
            }
        }

        private void Push()
        {
            if (sun != null) sun.intensity = curSun;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = shadowTint * curAmbient;

            RenderSettings.fog = curFog > 0.0001f;
            RenderSettings.fogDensity = curFog;
        }
    }
}
