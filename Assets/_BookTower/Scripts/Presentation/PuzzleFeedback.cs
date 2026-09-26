using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BookTower.Core;

namespace BookTower.Presentation
{
    /// <summary>
    /// 퍼즐이 풀렸을 때 세상이 반응하게 한다.
    ///
    /// 지금까지 퍼즐이 풀리면 Debug.Log 한 줄이 전부였다.
    /// 재미의 최소 단위는 "내가 뭘 했더니 세상이 반응했다"인데 그게 없으면
    /// 아무리 아트를 입혀도 심심하다. 실제로 그런 반응이 나왔다.
    ///
    /// 대사도 UI 도 없는 게임이라 반응은 **빛**으로 한다 —
    /// 그 방의 등이 한 번 확 밝아졌다가 가라앉는다.
    /// "빛 = 존재"라는 규칙과도 맞는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PuzzleFeedback : MonoBehaviour
    {
        [SerializeField] private float flareMultiplier = 3.2f;
        [SerializeField] private float flareUp = 0.18f;
        [SerializeField] private float flareDown = 1.6f;

        private void OnEnable()
        {
            GameEvents.PuzzleSolved += OnSolved;
            GameEvents.PuzzleActivated += OnActivated;
        }

        private void OnDisable()
        {
            GameEvents.PuzzleSolved -= OnSolved;
            GameEvents.PuzzleActivated -= OnActivated;
        }

        private void OnActivated(MonoBehaviour puzzle)
        {
            if (puzzle != null) Debug.Log($"[퍼즐] {puzzle.name} 시작", puzzle);
        }

        private void OnSolved(MonoBehaviour puzzle)
        {
            if (puzzle == null) return;

            // 퍼즐이 속한 방의 등을 전부 찾는다
            Transform room = puzzle.transform.parent;
            var lights = new List<Light>();
            if (room != null) lights.AddRange(room.GetComponentsInChildren<Light>(false));

            if (lights.Count > 0) StartCoroutine(Flare(lights));
            Debug.Log($"[퍼즐] {puzzle.name} 해결", puzzle);
        }

        private IEnumerator Flare(List<Light> lights)
        {
            float peak=BookTower.UI.PaperSettingsRuntime.Values.reduceFlash?1.12f:flareMultiplier;
            float rise=BookTower.UI.PaperSettingsRuntime.Values.reduceFlash?.65f:flareUp;
            var baseline = new float[lights.Count];
            for (int i = 0; i < lights.Count; i++)
                baseline[i] = lights[i] != null ? lights[i].intensity : 0f;

            // 확 밝아진다 — 빠르게
            for (float t = 0f; t < rise; t += Time.deltaTime)
            {
                float k = t / rise;
                Apply(lights, baseline, Mathf.Lerp(1f, peak, k));
                yield return null;
            }

            // 천천히 가라앉는다. 내려오는 게 올라가는 것보다 느려야 여운이 남는다
            for (float t = 0f; t < flareDown; t += Time.deltaTime)
            {
                float k = t / flareDown;
                Apply(lights, baseline, Mathf.Lerp(peak, 1f, k * k));
                yield return null;
            }

            Apply(lights, baseline, 1f);
        }

        private static void Apply(List<Light> lights, float[] baseline, float mul)
        {
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = baseline[i] * mul;
        }
    }
}
