using System.Collections;
using UnityEngine;

namespace BookTower.Presentation
{
    /// <summary>
    /// 힌트 에스컬레이션 3단계(기획서 §8)의 공용 연출 부품.
    ///
    /// 퍼즐 타입마다 "무엇을 흔들지 / 무엇을 밝힐지"는 다르지만(접이면 vs 조각 vs 주름),
    /// "어떻게 흔들지 / 어떻게 밝힐지"는 똑같다. 그래서 타입별 퍼즐(OnHintEscalated)이
    /// 대상만 골라 넘기고, 실제 트윈은 여기 하나로 모은다 — 세 군데에 같은 코루틴을
    /// 복붙하지 않기 위해서다.
    ///
    /// 전역 태양광은 건드리지 않는다. `Docs/02`의 "주광원 1개, 각도 고정 — 방마다
    /// 빛이 달라지면 접힘→그림자 인과가 무너진다"는 규칙이 있어서, 2단계 "빛이
    /// 기울어짐"은 그 오브젝트가 원래 갖고 있는 점 빛(`빛_<이름>`)을 흔드는 것으로
    /// 옮겼다 — 태양이 아니라 그 물건 자신이 반응하는 쪽이 규칙과도 안 맞지 않는다.
    /// </summary>
    public static class HintFx
    {
        /// <summary>1단계 — 미세하게 떨림. 위치만 흔들고 원래 자리로 되돌린다</summary>
        public static IEnumerator Tremble(Transform target, float duration = 1.1f, float amplitude = 0.035f)
        {
            if (target == null) yield break;

            Vector3 basePos = target.localPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float fade = 1f - t / duration;             // 갈수록 잦아듦
                float ox = (Mathf.PerlinNoise(Time.time * 11f, 0f) - 0.5f) * 2f;
                float oz = (Mathf.PerlinNoise(0f, Time.time * 11f) - 0.5f) * 2f;
                target.localPosition = basePos + new Vector3(ox, 0f, oz) * amplitude * fade;
                yield return null;
            }
            target.localPosition = basePos;
        }

        /// <summary>2단계 — 근처 점 빛이 천천히 밝아졌다 가라앉는다. 태양은 손대지 않는다</summary>
        public static IEnumerator PulseLight(Light light, float duration = 1.6f, float multiplier = 1.8f)
        {
            if (light == null) yield break;

            if(BookTower.UI.PaperSettingsRuntime.Values.reduceFlash)multiplier=Mathf.Min(multiplier,1.12f);
            float baseline = light.intensity;
            float half = duration * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                light.intensity = Mathf.Lerp(baseline, baseline * multiplier, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                light.intensity = Mathf.Lerp(baseline * multiplier, baseline, t / half);
                yield return null;
            }
            light.intensity = baseline;
        }

        /// <summary>3단계 — 위 둘을 합쳐 더 세고 길게. "나머지는 플레이어 몫"이라 강도만 올린다</summary>
        public static IEnumerator StrongNudge(MonoBehaviour host, Transform target, Light light)
        {
            if (host == null) yield break;
            if (target != null) host.StartCoroutine(Tremble(target, 1.8f, 0.07f));
            if (light != null) host.StartCoroutine(PulseLight(light, 2.2f, 2.6f));
            yield return null;
        }
    }
}
