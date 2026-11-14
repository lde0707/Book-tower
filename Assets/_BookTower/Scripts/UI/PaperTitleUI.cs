using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BookTower.UI
{
    public sealed partial class PaperUI
    {
        public bool TitleMode { get; private set; }
        public bool TitleArtReady => titleAssets && titleAssets.background;
        public bool TitleMenuArtReady => titleAssets && titleAssets.menuPaper && titleAssets.menuSelected && titleAssets.menuLeaf && titleAssets.motionShader;
        public AudioSource TitleMusic => titleMusic;
        PaperTitleAssets titleAssets;
        RectTransform titleBackground;
        AudioSource titleMusic;
        Camera titleCamera;
        int titleCameraMask;
        CameraClearFlags titleCameraClear;
        Color titleCameraColor;

        sealed class TitleState : UIState { public override PaperScreen Kind => PaperScreen.Title; }

        static bool ShouldShowTitle
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Contains("-titleQA") || args.Contains("BookTower.EditorTools.TitleScreenReview.Run")) return true;
                // Existing automation keeps its explicit direct-to-game entry point.
                return !args.Any(a => a == "-skipTitle" || a == "-gravityQA" || a == "-legacyQA" || a == "-releaseArtProbe" ||
                    a.StartsWith("BookTower.EditorTools.", StringComparison.Ordinal));
            }
        }

        void ShowTitle() { TitleMode = true; Switch(new TitleState()); }
        void ReturnToMenu() { if (TitleMode) Switch(new TitleState()); else Switch(new PausedState()); }

        public void StartTitleStory()
        {
            if (!TitleMode || ScreenState != PaperScreen.Title) return;
            TitleMode = false;
            Switch(new PlayingState());
        }

        void EnsureTitleBackground()
        {
            if (titleBackground) return;
            titleAssets = Resources.Load<PaperTitleAssets>("TitleScreen");
            var canvas = stage.parent;
            var surround = Rect(canvas, "Title paper surround", 0, 0, 0, 0);
            surround.anchorMin = Vector2.zero; surround.anchorMax = Vector2.one;
            surround.offsetMin = surround.offsetMax = Vector2.zero;
            surround.SetAsFirstSibling();
            var matte = surround.gameObject.AddComponent<Image>();
            matte.color = new Color(.94f, .88f, .77f); matte.raycastTarget = false;
            titleBackground = surround;
            var composition = Rect(surround, "Title safe composition", 0, 0, 1920, 1080);
            composition.anchorMin = composition.anchorMax = composition.pivot = new Vector2(.5f, .5f);
            composition.anchoredPosition = Vector2.zero;
            if (TitleArtReady)
            {
                var art = Rect(composition, "Bound light", 0, 0, 1920, 1080).gameObject.AddComponent<RawImage>();
                art.texture = titleAssets.background; art.raycastTarget = false;
                art.gameObject.AddComponent<PaperTitleMotion>().Initialize(art, titleAssets.motionShader);
            }
        }

        void DrawTitle()
        {
            EnsureTitleBackground();
            var content = Rect(dialog, "Title menu", 0, 0, 1920, 1080);
            var start = TitleButton(content, "게임 시작", 494, StartTitleStory);
            var settings = TitleButton(content, "옵션", 630, OpenSettings);
            var quit = TitleButton(content, "나가기", 754, () => Switch(new ConfirmationState(PaperScreen.ConfirmQuit)));
            var buttons = new[] { start, settings, quit };
            for (int i = 0; i < buttons.Length; i++)
            {
                var navigation = buttons[i].navigation; navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
                navigation.selectOnDown = buttons[(i + 1) % buttons.Length]; buttons[i].navigation = navigation;
            }
            Label(content, "↑ ↓  선택   Enter  확인   Esc  돌아가기", 84, 879, 448, 34, 17).color = new Color(.39f, .35f, .27f, .8f);
            if (ScreenState == PaperScreen.ConfirmQuit)
            {
                var group = content.gameObject.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
            }
        }

        Button TitleButton(Transform parent, string caption, float y, Action action)
        {
            var button = Button(parent, caption, 83, y, 410, 98, action);
            button.image.sprite = null; button.image.color = Color.clear; button.transition = Selectable.Transition.None;
            var text = button.GetComponentInChildren<Text>();
            var plaque = Rect(button.transform, "Paper plaque", 205, 49, 500, 170);
            plaque.pivot = new Vector2(.5f, .5f);
            Image(plaque, "Handmade paper", 0, 0, 500, 170, Color.white, titleAssets.menuPaper);
            var glow = Image(plaque, "Golden edge light", 0, 0, 500, 170, Color.clear, titleAssets.menuSelected);
            // These children use the plaque's center as their origin.
            foreach (Transform child in plaque)
            {
                var rect = (RectTransform)child; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
            }
            var marker = Image(plaque, "Luminous leaf", 0, 0, 72, 64, Color.clear, titleAssets.menuLeaf);
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = marker.rectTransform.pivot = new Vector2(.5f, .5f);
            marker.rectTransform.anchoredPosition = new Vector2(-125, 0);
            text.transform.SetParent(plaque, false);
            var labelRect = text.rectTransform; labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(.5f, .5f);
            labelRect.anchoredPosition = new Vector2(23, 0); labelRect.sizeDelta = new Vector2(285, 98);
            text.fontSize = Mathf.RoundToInt(40 * TextFactor); text.color = new Color(.10f, .21f, .21f); text.alignment = TextAnchor.MiddleCenter;
            if (titleAssets.titleFont) text.font = titleAssets.titleFont;
            var focus = button.gameObject.AddComponent<PaperTitleFocus>(); focus.illuminated = glow; focus.leaf = marker; focus.plaque = plaque;
            return button;
        }

        void RefreshTitlePresentation()
        {
            if (titleBackground) titleBackground.gameObject.SetActive(TitleMode);
            if (!TitleMode)
            {
                if (titleMusic) titleMusic.Stop();
                RestoreTitleCamera();
                return;
            }
            if (!titleCamera && Camera.main)
            {
                titleCamera = Camera.main; titleCameraMask = titleCamera.cullingMask;
                titleCameraClear = titleCamera.clearFlags; titleCameraColor = titleCamera.backgroundColor;
                titleCamera.cullingMask = 0; titleCamera.clearFlags = CameraClearFlags.SolidColor;
                titleCamera.backgroundColor = new Color(.94f, .91f, .84f);
            }
            if (!titleMusic && titleAssets && titleAssets.music)
            {
                var owner = new GameObject("Title music"); owner.transform.SetParent(transform, false);
                titleMusic = owner.AddComponent<AudioSource>(); titleMusic.clip = titleAssets.music;
                titleMusic.playOnAwake = false; titleMusic.loop = true; titleMusic.volume = .18f;
                PaperAudioLevel.Bind(titleMusic, PaperAudioBus.Music);
            }
            if (titleMusic) { titleMusic.ignoreListenerPause = true; if (!titleMusic.isPlaying) titleMusic.Play(); }
        }

        void RestoreTitleCamera()
        {
            if (!titleCamera) return;
            titleCamera.cullingMask = titleCameraMask; titleCamera.clearFlags = titleCameraClear;
            titleCamera.backgroundColor = titleCameraColor; titleCamera = null;
        }
    }
}
