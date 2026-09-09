using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BookTower.Core;

namespace BookTower.Presentation
{
    /// <summary>
    /// 화면 전환(페이드). 무대사 게임이라 챕터 전환·연출 진입에 자주 쓴다.
    ///
    /// UI를 코드로 만드는 이유: 프리팹 의존을 없애서 어느 씬에 놔도 그냥 동작하게.
    /// 나중에 종이 질감 전환(페이지 넘김)으로 교체할 자리이기도 하다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenTransition : MonoBehaviour
    {
        public static ScreenTransition Instance { get; private set; }

        [SerializeField] private Color fadeColor = new(0.18f, 0.18f, 0.20f, 1f);
        [SerializeField] private float defaultDuration = 0.6f;

        private CanvasGroup group;
        private Coroutine running;

        public bool IsBusy => running != null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;   // 항상 맨 위

            group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var imageGo = new GameObject("Fade");
            imageGo.transform.SetParent(canvasGo.transform, false);

            var image = imageGo.AddComponent<Image>();
            image.color = fadeColor;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>화면을 덮는다</summary>
        public void FadeOut(Action onComplete = null, float duration = -1f)
            => Run(1f, duration, onComplete);

        /// <summary>화면을 걷어낸다</summary>
        public void FadeIn(Action onComplete = null, float duration = -1f)
            => Run(0f, duration, onComplete);

        /// <summary>덮었다가 중간에 작업을 실행하고 다시 걷어낸다 (챕터 전환용)</summary>
        public void Cover(Action inBetween, float duration = -1f)
        {
            FadeOut(() =>
            {
                inBetween?.Invoke();
                FadeIn(null, duration);
            }, duration);
        }

        private void Run(float targetAlpha, float duration, Action onComplete)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(targetAlpha, duration < 0f ? defaultDuration : duration, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            float start = group.alpha;
            float t = 0f;

            group.blocksRaycasts = true;

            while (t < duration)
            {
                // 일시정지(timeScale 0) 중에도 페이드는 진행되어야 하므로 unscaled 사용
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
                yield return null;
            }

            group.alpha = targetAlpha;
            group.blocksRaycasts = targetAlpha > 0.99f;

            running = null;
            onComplete?.Invoke();
        }

        /// <summary>씬에 없으면 만들어서 반환</summary>
        public static ScreenTransition EnsureExists()
        {
            if (Instance != null) return Instance;

            var found = FindFirstObjectByType<ScreenTransition>();
            if (found != null) return found;

            var go = new GameObject("[ScreenTransition]");
            return go.AddComponent<ScreenTransition>();
        }
    }
}
