using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using BookTower.Story;

namespace BookTower.UI
{
    public sealed partial class PaperUI
    {
        public event Action<string> NoteRead;
        public bool StoryActive { get; private set; }
        public bool WalkingStory { get; private set; }
        public bool BirdRevealed {get;private set;}
        public bool AwakeningReady=>!CutsceneView.Continuous||CutsceneView.Continuous.AwakeningReady;
        public bool PortraitIsSilhouette=>!BirdRevealed&&(storySpeaker=="종이새"||storySpeaker=="모아");
        public Sprite SpeakerPortrait=>PortraitFor(storySpeaker);
        Image storyPortrait,barkPortrait;
        Text storySpeakerLabel;
        public string StoryText => storyFullText;
        public bool StoryTextComplete => storyCharacters >= storyFullText.Length;
        public PaperCutsceneArt cutsceneArt;
        public PaperCutsceneView CutsceneView { get; private set; }
        public bool StoryLastLine { get; private set; }
        Action advanceStory;
        string storySpeaker = "", storyFullText = "";
        float storyCharacters, barkRemaining;
        readonly System.Collections.Generic.Dictionary<string,string> roomObjectives=new();
        BookTower.Rooms.RoomManager objectiveRooms;
        public string CurrentObjective=>objectiveLabel?objectiveLabel.text:"";
        public bool ObjectiveVisible=>objectivePanel&&objectivePanel.activeInHierarchy;
        int storyInputAfterFrame, storyActionFrame = -1;
        bool storyKeyArmed, storyMouseArmed;
        Text storyButtonLabel;
        Text storyBody, storyHint, barkLabel, objectiveLabel;
        GameObject barkPanel, objectivePanel;

        sealed class StoryState : UIState
        {
            public override PaperScreen Kind => PaperScreen.Story;
            public override void Tick(PaperUI o, float dt) => o.TickStory(dt);
        }
        public void BeginStory(string sequence, Action advance)
        {
            StoryActive = true; WalkingStory=false; advanceStory = advance;
            BirdRevealed=sequence!=OpeningStoryData.Promise;
            barkRemaining = 0; storyFullText = ""; storySpeaker = "";
            CutsceneView.Begin(sequence);
            Switch(new StoryState());
        }
        public void EndStory()
        {
            StoryActive = false; WalkingStory=false; advanceStory = null; CutsceneView.End();
            Switch(new PlayingState());
        }
        public void StoryLine(string speaker, string text, StoryShot shot, bool last)
        {
            storySpeaker = speaker; storyFullText = text; storyCharacters = 0;
            StoryLastLine = last; if(!WalkingStory)CutsceneView.ShowShot(shot);
            ResetStoryInput(); Draw();
        }
        public void ContinueStoryWhileWalking()
        {
            if(!StoryActive||WalkingStory)return;
            WalkingStory=true;BirdRevealed=true;CutsceneView.End();EnterScreen();
        }
        void ResetStoryInput()
        {
            storyKeyArmed = !PaperSettingsRuntime.Held(5);
            storyMouseArmed = Mouse.current == null || !Mouse.current.leftButton.isPressed;
            storyInputAfterFrame = Time.frameCount;
        }
        public void AdvanceStoryText()
        {
            if (ScreenState != PaperScreen.Story || Time.frameCount <= storyInputAfterFrame || storyActionFrame == Time.frameCount) return;
            storyActionFrame = Time.frameCount;
            if (!StoryTextComplete) storyCharacters = storyFullText.Length;
            else advanceStory?.Invoke();
            RefreshStoryText();
        }
        void AdvanceStoryButton()
        {
            // The screen-wide mouse press already handled this click. Button.onClick runs
            // on release; keep submit/accessibility activation without advancing twice.
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return;
            AdvanceStoryText();
        }
        void TickStory(float dt)
        {
            storyCharacters = Mathf.Min(storyFullText.Length, storyCharacters + dt * new[] { 18f, 28f, 44f }[PaperSettingsRuntime.Values.dialogueSpeed]);
            if(!WalkingStory)CutsceneView.Tick(dt);
            if(!BirdRevealed&&CutsceneView.Continuous&&CutsceneView.Continuous.LightArrived)BirdRevealed=true;
            bool down = PaperSettingsRuntime.Held(5);
            if (!down && !storyKeyArmed) storyKeyArmed = true;
            var mouse = Mouse.current;
            bool mouseDown = mouse != null && mouse.leftButton.isPressed;
            if (!mouseDown) storyMouseArmed = true;
            bool keyPressed = storyKeyArmed && PaperSettingsRuntime.Pressed(5);
            bool mousePressed = storyMouseArmed && mouse != null && mouse.leftButton.wasPressedThisFrame;
            if (keyPressed) storyKeyArmed = false;
            if (mousePressed) storyMouseArmed = false;
            if (keyPressed || mousePressed) AdvanceStoryText();
            RefreshStoryText();
        }
        void RefreshStoryText()
        {
            if(storyPortrait){storyPortrait.sprite=PortraitFor(storySpeaker);storyPortrait.color=PortraitIsSilhouette?Color.black:Color.white;}
            if(storySpeakerLabel)storySpeakerLabel.text=PortraitIsSilhouette?"희미한 목소리":storySpeaker;
            if (storyBody) storyBody.text = storyFullText.Substring(0, Mathf.FloorToInt(storyCharacters));
            string action = !StoryTextComplete ? "전체 표시" : StoryLastLine ? "이야기 마치기" : "다음 대사";
            if (storyButtonLabel) storyButtonLabel.text = action;
            if (storyHint) storyHint.text = $"{(Key)PaperSettingsRuntime.Values.keys[5]} / 좌클릭  {action}    ·    Esc  일시정지";
        }
        void DrawStory()
        {
            storyButtonLabel = null;
            if (storyFullText.Length > 0)
            {
                var panel = Image(dialog, "Story dialogue", 205, 770, 1510, 244, Color.white, textPaper, true);
                PaperBox(panel);
                storyPortrait=Image(panel.transform,"Speaker portrait",22,20,200,200,Color.white,PortraitFor(storySpeaker));storyPortrait.preserveAspect=true;
                storySpeakerLabel=Label(panel.transform, storySpeaker, 245, 16, 1080, 42, 26, true);
                storyBody = Label(panel.transform, storyFullText.Substring(0, Mathf.FloorToInt(storyCharacters)), 245, 62, 1205, 120, 29);
                storyButtonLabel = Button(panel.transform, "다음 대사", 1190, 194, 260, 38, AdvanceStoryButton).GetComponentInChildren<Text>();
                storyButtonLabel.fontSize = Mathf.RoundToInt(21 * TextFactor);
            }
            storyHint = Label(dialog, "", 300, 1025, 1320, 40, 20, false, TextAnchor.MiddleCenter);
            storyHint.color = new Color(.94f,.86f,.70f);
            var hintShadow=storyHint.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor=new Color(.08f,.04f,.02f,.9f);hintShadow.effectDistance=new Vector2(1,-1);
            RefreshStoryText();
        }
        Sprite PortraitFor(string speaker)
        {
            if(!cutsceneArt)return null;
            if(speaker=="나")return cutsceneArt.spiritPortrait?cutsceneArt.spiritPortrait:cutsceneArt.spirit;
            if(speaker=="종이새"||speaker=="모아")return cutsceneArt.continuousBird?cutsceneArt.continuousBird:cutsceneArt.moa;
            return leafSprite;
        }
        void BuildStoryHud()
        {
            var panel = Image(hud, "Companion words", 480, 775, 960, 100, paper);
            PaperBox(panel); barkPanel = panel.gameObject;
            barkPortrait=Image(panel.transform,"Companion speaker portrait",15,8,84,84,Color.white);barkPortrait.preserveAspect=true;
            barkLabel = Label(panel.transform, "", 115, 8, 815, 84, 25, false, TextAnchor.MiddleCenter);
            objectiveRooms=FindFirstObjectByType<BookTower.Rooms.RoomManager>();
            var goal = Image(hud, "Current story objective", 530, 90, 860, 96, paper);
            PaperBox(goal); objectivePanel = goal.gameObject;
            objectiveLabel = Label(goal.transform, "", 30, 8, 800, 80, 26, false, TextAnchor.MiddleCenter);
            barkPanel.SetActive(false); objectivePanel.SetActive(false);
        }
        void UpdateStoryHud()
        {
            UpdateObjectiveGuide();
            if (!barkPanel) return;
            if (ScreenState == PaperScreen.Playing)
            { barkRemaining -= Time.deltaTime; }
            var current=objectiveRooms?objectiveRooms.Current:null;
            if(current) objectiveLabel.text=roomObjectives.TryGetValue(current.RoomId,out var text)?text:current.Goal;
            barkPanel.SetActive(barkRemaining > 0); objectivePanel.SetActive(!string.IsNullOrWhiteSpace(objectiveLabel.text));
        }
        public void Bark(string speaker, string text, float seconds = 5)
        { if (!barkLabel) return; barkPortrait.sprite=PortraitFor(speaker);barkLabel.text = speaker + "\n" + text; barkRemaining = seconds; }
        public void Objective(string id, string text)
        { Store(id, "여행의 목표", text); objectiveLabel.text = text;
          var current=objectiveRooms?objectiveRooms.Current:null;if(current)roomObjectives[current.RoomId]=text; }
        public void RecordStory(string id, string title, string text) => Store(id, title, text);
        public bool HasRecord(string id) => records.ids.Contains(id);
    }
}
