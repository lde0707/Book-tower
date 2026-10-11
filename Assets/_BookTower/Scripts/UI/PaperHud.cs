using BookTower.Core;
using BookTower.Interaction;
using UnityEngine.InputSystem;

namespace BookTower.UI
{
    public sealed partial class PaperUI
    {
        Interactable promptTarget;
        FoldBridgeControl promptBridge;
        string promptVerb;
        int promptKey, previousShadowPrompt;
        bool promptColorAssist;

        void UpdateHud()
        {
            if (!hud) return;
            UpdateStoryHud();
            if (stateText) stateText.text = Dirty ? "저장되지 않은 변경사항이 있습니다." : status;
            if (feedbackDot) feedbackDot.gameObject.SetActive(stateText && !string.IsNullOrEmpty(stateText.text));
            if (ScreenState != PaperScreen.Playing) return;

            hud.gameObject.SetActive(GameFlow.Instance.AcceptsPlayerInput);
            var target = TargetInteractor.Current;
            var settings = PaperSettingsRuntime.Values;
            bool targetChanged = promptTarget != target;
            if (targetChanged)
            {
                promptTarget = target;
                promptBridge = target ? target.GetComponent<FoldBridgeControl>() : null;
            }
            int shadowPrompt = PaperShadowDrag.Holding ? 2 : PaperShadowDrag.Aimed ? 1 : 0;
            bool hasPrompt = target || shadowPrompt != 0;
            brackets.gameObject.SetActive(hasPrompt);
            promptCard.SetActive(hasPrompt && settings.prompts);
            if (shadowPrompt != 0)
            {
                promptText.text = shadowPrompt == 2 ? "마우스 상하·좌우 / 이동 키: 옮기기 · 놓으면 고정" :
                    "기둥·오림을 좌클릭으로 잡아 이동";
            }
            else if (target)
            {
                string verb = target.Verb;
                if (promptBridge && promptBridge.opticalControl == 0)
                    verb = promptBridge.seal ? "빛 고정하기" : promptBridge.puzzle.BridgeDown ? "페이지 세우기" : "페이지 내리기";
                int key = settings.keys[5];
                if (targetChanged || previousShadowPrompt != 0 || promptVerb != verb ||
                    promptKey != key || promptColorAssist != settings.colorAssist)
                {
                    promptVerb = verb;
                    promptKey = key;
                    promptColorAssist = settings.colorAssist;
                    string mark = promptColorAssist ? Symbol(target) + "  " : "";
                    promptText.text = mark + (Key)key + " / 좌클릭  " + verb;
                }
            }
            previousShadowPrompt = shadowPrompt;
            toastCard.SetActive(UnityEngine.Time.unscaledTime < toastUntil);
            toastText.text = toast;
        }
    }
}
