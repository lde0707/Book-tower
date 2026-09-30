using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BookTower.CameraSystem;
using BookTower.Core;
using BookTower.PlayerSystem;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.UI;

namespace BookTower.Story
{
    [DefaultExecutionOrder(100)]
    public sealed partial class OpeningStoryDirector : MonoBehaviour
    {
        public PlayerController player;
        public PaperCamera storyCamera;
        public Room foyer, theatreRoom;
        public FoldBridgePuzzle bridge;
        public PuzzleBase theatrePuzzle;
        public PaperCompanion companion;
        public AudioClip rustle, nameMotif;
        public Transform clueBookPage;
        public string ActiveSequence { get; private set; } = "";
        public int BeatIndex { get; private set; }
        public float BeatElapsed { get; private set; }
        public bool NameKnown => completed.Contains(OpeningStoryData.Name);
        public bool Completed(string id) => completed.Contains(id);
        public int SequenceStarts { get; private set; }
        public const string WorkNote = "F0.Book.Work", ShadowNote = "F0.Book.Shadow", LampNote = "1A.Book.Lamp";

        readonly HashSet<string> completed = new();
        readonly Queue<string> pending = new();
        StoryBeat[] beats;
        PaperUI ui;
        AudioSource paperSound;
        bool initialized;
        BindingDoor[] openingDoors;
        bool doorPlayerWasEnabled;
        public bool WaitingForFoyerDoor { get; private set; }
        Vector3 spawn;
        Quaternion pageRest;
        float barkDelay;
        ReadableNote workNote;
        Light workBookLight;
        bool workGuideActive;
        public bool WorkBookEmphasized=>workGuideActive&&workBookLight&&workBookLight.enabled;

        IEnumerator Start()
        {
            // All room/camera/UI Start methods must finish before taking ownership.
            yield return null;
            ui = PaperUI.Instance;
            if (!ui || !ui.CutsceneView || !player || !storyCamera || !companion || !foyer || !theatreRoom || !bridge || !theatrePuzzle)
            { Debug.LogError("[OpeningStory] Missing scene reference; story disabled without blocking gameplay."); enabled = false; yield break; }
            companion.player = player;
            BindWorkbenchInteraction();
            spawn = player.transform.position;
            if (clueBookPage) pageRest = clueBookPage.localRotation;
            paperSound = gameObject.AddComponent<AudioSource>(); paperSound.playOnAwake = false; paperSound.volume = .3f;
            PaperAudioLevel.Bind(paperSound, PaperAudioBus.Effects);
            GameEvents.PuzzleSolved += OnSolved;
            ui.NoteRead += OnNoteRead;
            initialized = true;
            // Each Play starts a new puzzle session. Checkpoints keep this director and its flags.
            // Persistent records are a journal, not a partial puzzle save/continue system.
            if (foyer.Contains(player.transform.position)) Queue(OpeningStoryData.Promise);
        }
        void OnDisable()
        {
            if(WaitingForFoyerDoor)
            {
                WaitingForFoyerDoor=false;
                EndDoorReveal();if(storyCamera)storyCamera.ReturnToFollow();
                if(player)player.enabled=doorPlayerWasEnabled;
                if(GameFlow.Instance&&GameFlow.Instance.Phase==GamePhase.Cutscene)GameFlow.Instance.EndCutscene();
            }
            GameEvents.PuzzleSolved -= OnSolved;
            if (ui) ui.NoteRead -= OnNoteRead;
            if (companion) companion.Directed = false;
            if(workBookLight)workBookLight.enabled=false;
            if(initialized&&clueBookPage)clueBookPage.localRotation=pageRest;
            if(ui)ui.GuideTo(null);
            if (storyCamera && ActiveSequence.Length > 0) { storyCamera.ReturnToFollow(); if (ui) ui.EndStory(); }
        }
        void BindWorkbenchInteraction()
        {
            ReadableNote work=null;
            foreach(var note in foyer.GetComponentsInChildren<ReadableNote>())if(note.id==WorkNote){work=note;break;}
            if(!work)return;
            workNote=work;
            var lamp=new GameObject("작업책에 머무는 황금빛");lamp.transform.SetParent(work.transform,false);lamp.transform.localPosition=Vector3.up*1.15f;
            workBookLight=lamp.AddComponent<Light>();workBookLight.type=LightType.Point;workBookLight.color=new Color(1,.77f,.36f);workBookLight.range=4.2f;workBookLight.intensity=6;workBookLight.shadows=LightShadows.None;workBookLight.enabled=false;
            Collider nearest=null;float distance=6f;
            foreach(var collider in foyer.GetComponentsInChildren<Collider>())
            {
                if(collider.name!="두꺼운제본상판")continue;
                float d=Vector3.Distance(collider.bounds.center,work.transform.position);
                if(d<distance){nearest=collider;distance=d;}
            }
            if(!nearest)return;
            var proxy=nearest.GetComponent<BookTower.Interaction.InteractionTargetProxy>();
            if(!proxy)proxy=nearest.gameObject.AddComponent<BookTower.Interaction.InteractionTargetProxy>();
            proxy.target=work.GetComponent<BookTower.Interaction.Interactable>();
        }
        void Queue(string id)
        { if (!completed.Contains(id) && ActiveSequence != id && !pending.Contains(id)) pending.Enqueue(id); }
        void OnSolved(MonoBehaviour solved)
        {
            if (solved == bridge)
            {
                SetWorkBookGuide(false);
                // The door animation owns the transition. Do not wait for walking/grounding
                // or allow a movement frame between the opened door and the memory.
                openingDoors=FindObjectsByType<BindingDoor>(FindObjectsSortMode.None);
                WaitingForFoyerDoor=true;
                BeginDoorReveal();
                doorPlayerWasEnabled=player.enabled;player.enabled=false;
                player.HorizontalVelocity=Vector3.zero;player.VerticalVelocity=0;
                GameFlow.EnsureExists().BeginCutscene();
            }
            if (solved == theatrePuzzle)
            {
                Queue(OpeningStoryData.Theatre); Queue(OpeningStoryData.Name);
            }
        }
        void OnNoteRead(string id)
        {
            if (id == WorkNote) {completed.Add("F0.Book.Read");SetWorkBookGuide(false);ui.Objective("Goal.F0.Light", "은박에 빛을 이어 문 옆 잎 문양 깨우기");}
            if (id == ShadowNote) completed.Add("F0.Clue.NextRoom");
        }
        void Update()
        {
            if (!initialized) return;
            if(WaitingForFoyerDoor)
            {
                if(GameFlow.Instance.Phase==GamePhase.Paused)return;
                GameFlow.EnsureExists().BeginCutscene();
                UpdateDoorReveal();
                bool ready=true;
                foreach(var door in openingDoors)if(door&&door.isActiveAndEnabled&&door.puzzle==bridge&&!door.IsOpen){ready=false;break;}
                if(ready){EndDoorReveal();WaitingForFoyerDoor=false;player.enabled=doorPlayerWasEnabled;Begin(OpeningStoryData.Return);}
                return;
            }
            UpdateWorkBookGuide();
            if (ActiveSequence.Length > 0)
            {
                if (ui.ScreenState != PaperScreen.Story) return;
                BeatElapsed += Time.deltaTime;
                if(ActiveSequence==OpeningStoryData.Promise&&BeatIndex==2&&ui.AwakeningReady&&ui.StoryTextComplete)Advance();
                return;
            }
            if (ui.ScreenState != PaperScreen.Playing || !player.Controller.isGrounded) return;
            bool inTheatre = theatreRoom.Contains(player.transform.position) &&
                theatreRoom.transform.InverseTransformPoint(player.transform.position).x > -29 &&
                theatreRoom.transform.InverseTransformPoint(player.transform.position).z > -25;
            if (inTheatre && Completed(OpeningStoryData.Promise)) Queue(OpeningStoryData.Theatre);
            if (pending.Count > 0) { Begin(pending.Dequeue()); return; }
            barkDelay -= Time.deltaTime;
            if (Completed(OpeningStoryData.Promise) && foyer.IsVisible)
            {
                if (!completed.Contains("F0.FirstWalk") && Vector3.Distance(spawn, player.transform.position) > 1.2f)
                { completed.Add("F0.FirstWalk"); if(workGuideActive)Bark("종이새", "왼쪽 작업대야. 빛이 머무는 펼쳐진 책, 보여?"); }
                if (bridge.BridgeDown && bridge.IsSettled && barkDelay <= 0 && !Completed("F0.Bridge"))
                { completed.Add("F0.Bridge"); Bark("나", "이제 같이 건널 수 있겠다."); }
                if (bridge.ShutterOpen && barkDelay <= 0 && !Completed("F0.Light"))
                { completed.Add("F0.Light"); Bark("종이새", "빛이… 아직 여기 남아 있었네."); }
            }
        }
        void SetWorkBookGuide(bool active)
        {
            workGuideActive=active&&workNote;
            ui.GuideTo(workGuideActive?workNote.GetComponent<BookTower.Interaction.Interactable>():null);
            if(!workGuideActive){if(workBookLight)workBookLight.enabled=false;if(clueBookPage)clueBookPage.localRotation=pageRest;}
        }
        void UpdateWorkBookGuide()
        {
            if(!workBookLight)return;
            workBookLight.enabled=workGuideActive&&foyer.IsVisible;
            if(!workGuideActive||!foyer.IsVisible||ui.ScreenState!=PaperScreen.Playing)return;
            bool reduced=PaperSettingsRuntime.Values.reduceStoryMotion;
            float wave=reduced?0:Mathf.Sin(Time.time*1.4f);
            workBookLight.intensity=6+wave*.75f;
            if(clueBookPage)clueBookPage.localRotation=pageRest*Quaternion.Euler(wave*1.3f,0,0);
        }
        void Bark(string speaker, string text) { ui.Bark(speaker == "종이새" && NameKnown ? "모아" : speaker, text); barkDelay = 8; }
        void Begin(string id)
        {
            if (Completed(id)) return;
            ActiveSequence = id; BeatIndex = 0; SequenceStarts++;
            beats = OpeningStoryData.Beats(id);
            player.HorizontalVelocity = Vector3.zero; player.VerticalVelocity = -2;
            // Lock gameplay input; the 3D camera and actors keep their exact poses behind the illustration.
            storyCamera.SetMode(PaperCamera.CameraMode.Cinematic);
            companion.Directed = true;
            ui.BeginStory(id, Advance);
            SetBeat();
        }
        void SetBeat()
        {
            BeatElapsed = 0;
            var beat = beats[BeatIndex];
            if(ActiveSequence==OpeningStoryData.Promise&&BeatIndex>=3&&!ui.WalkingStory)
            {
                companion.Directed=false;storyCamera.ReturnToFollow();
                ui.ContinueStoryWhileWalking();
            }
            ui.StoryLine(beat.speaker, beat.text, beat.shot, BeatIndex == beats.Length - 1);
            if (rustle) paperSound.PlayOneShot(rustle, .24f);
            if (ActiveSequence == OpeningStoryData.Name && BeatIndex == 0 && nameMotif) paperSound.PlayOneShot(nameMotif, .45f);
        }
        void Advance()
        {
            if (ActiveSequence.Length == 0 || ui.ScreenState != PaperScreen.Story) return;
            if(ActiveSequence==OpeningStoryData.Promise&&BeatIndex==2&&!ui.AwakeningReady)return;
            if (++BeatIndex >= beats.Length) { FinishSequence(); return; }
            SetBeat();
        }
        void FinishSequence()
        {
            string id = ActiveSequence;
            completed.Add(id); ActiveSequence = "";
            string record = id == OpeningStoryData.Promise ? "Story.Promise" : id == OpeningStoryData.Return ? "M01" : id == OpeningStoryData.Name ? "M02" : "";
            if(record.Length>0) ui.RecordStory(record,"","");
            companion.Directed = false; storyCamera.ReturnToFollow(); ui.EndStory();
            if (id == OpeningStoryData.Promise)
            { ui.Objective("Goal.Promise", "종이새와 마지막 장의 집 찾기\n먼저 왼쪽 작업대의 황금빛 책 살펴보기");SetWorkBookGuide(true);
              if(PaperSettingsRuntime.Values.prompts) PaperUI.Notify("마우스로 둘러보기 · 이동 키로 걷기 · " + (UnityEngine.InputSystem.Key)PaperSettingsRuntime.Values.keys[5] + " / 좌클릭으로 책 읽기"); }
            else if (id == OpeningStoryData.Return)
            { ui.Objective("Goal.F0.Exit", "열린 문 너머로 종이새와 함께 가기"); Bark("종이새", "저 안쪽이 낯설지 않아. 조금 더 가 보자."); }
            else if (id == OpeningStoryData.Theatre) ui.Objective("Goal.1A", "토끼와 여우의 그림자 속에서 새의 기억 찾기");
            else if (id == OpeningStoryData.Name)
            { ui.Objective("Goal.Moa", "모아와 함께 다음 장의 빛을 찾아가기"); Bark("모아", "내 이름을 다시 들으니까… 집이 조금 가까워진 것 같아.", 6); }
        }
        void Bark(string speaker, string text, float seconds) { ui.Bark(speaker, text, seconds); barkDelay = seconds+2; }
    }
}
