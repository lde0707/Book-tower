using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using BookTower.Core;
using BookTower.Interaction;
using BookTower.PlayerSystem;
namespace BookTower.UI
{
 public enum PaperScreen{Playing,Paused,Settings,Reading,Rebinding,ConfirmDisplay,ConfirmDiscard,ConfirmQuit,Journal,Choosing,Story}
 [DefaultExecutionOrder(-200)] public sealed partial class PaperUI:MonoBehaviour
 {
  public static PaperUI Instance {get;private set;}
  public Font font,headingFont;public Sprite notebookSprite,leafSprite;public AudioClip previewClip,musicClip,ambienceClip;
  public StateMachine<PaperUI> Machine {get;private set;}
  public PaperScreen ScreenState {get;private set;}
  public PaperSettings Draft {get;private set;}
  public int Tab {get;private set;}
  public RectTransform SharedLeaf=>leaf.rectTransform;
  public bool Dirty=>Draft!=null&&!Draft.Same(PaperSettingsRuntime.Instance.Saved);
  public static bool BlocksGameplay=>Instance&&((Instance.ScreenState!=PaperScreen.Playing&&!(Instance.ScreenState==PaperScreen.Story&&Instance.WalkingStory))||Time.frameCount<=Instance.blockThrough);
  int blockThrough=-1,bindAction,page;float deadline,toastUntil;string status="",toast="";ReadableNote note;PaperScreen returnScreen=PaperScreen.Playing;
  AudioSource preview;Text modalTimer;PlayerController player;
  [Serializable] class Records {public List<string> ids=new(),titles=new(),texts=new();}
  Records records=new();string RecordPath=>PaperSettingsRuntime.TestStoragePath!=null?PaperSettingsRuntime.TestStoragePath+".records":Path.Combine(Application.persistentDataPath,"paper-records-v1.json");
  abstract class UIState:IState<PaperUI>{public abstract PaperScreen Kind{get;} public virtual void Enter(PaperUI o){o.ScreenState=Kind;o.EnterScreen();}public virtual void Tick(PaperUI o,float dt){}public virtual void Exit(PaperUI o){o.blockThrough=Time.frameCount+1;}}
  sealed class PlayingState:UIState{public override PaperScreen Kind=>PaperScreen.Playing;}
  sealed class PausedState:UIState{public override PaperScreen Kind=>PaperScreen.Paused;}
  sealed class SettingsState:UIState{public override PaperScreen Kind=>PaperScreen.Settings;}
  sealed class ReadingState:UIState{public override PaperScreen Kind=>PaperScreen.Reading;public override void Tick(PaperUI o,float dt){if(Time.frameCount>o.blockThrough&&PaperSettingsRuntime.Pressed(5))o.NextPage();}}
  sealed class RebindingState:UIState{public override PaperScreen Kind=>PaperScreen.Rebinding;public override void Tick(PaperUI o,float dt){if(Time.unscaledTime<o.deadline||Keyboard.current==null)return;foreach(var k in Keyboard.current.allKeys)if(k.wasPressedThisFrame&&k.keyCode!=Key.Escape){o.TryBind(k.keyCode);break;}}}
  sealed class ConfirmationState:UIState{readonly PaperScreen kind;public ConfirmationState(PaperScreen k){kind=k;}public override PaperScreen Kind=>kind;public override void Tick(PaperUI o,float dt){if(kind!=PaperScreen.ConfirmDisplay)return;int left=Mathf.CeilToInt(o.deadline-Time.unscaledTime);if(o.modalTimer)o.modalTimer.text=$"{Mathf.Max(0,left)}초 후 이전 화면 설정으로 돌아갑니다.";if(left<=0)o.RejectDisplay();}}
  sealed class JournalState:UIState{public override PaperScreen Kind=>PaperScreen.Journal;}
  void Awake(){if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;if(!GetComponent<PaperSettingsRuntime>())gameObject.AddComponent<PaperSettingsRuntime>();player=FindFirstObjectByType<PlayerController>();BuildChrome();Machine=new StateMachine<PaperUI>(this);try{if(File.Exists(RecordPath))records=JsonUtility.FromJson<Records>(File.ReadAllText(RecordPath))??new Records();}catch{records=new Records();}
   NormalizeRecords();
   preview=gameObject.AddComponent<AudioSource>();preview.playOnAwake=false;preview.ignoreListenerPause=true;preview.ignoreListenerVolume=true;
   if(!BookTower.Audio.RegionMusicPlayer.Attach(gameObject))Loop(musicClip,PaperAudioBus.Music,.07f);Loop(ambienceClip,PaperAudioBus.Ambience,.035f);
  }
  void Loop(AudioClip clip,PaperAudioBus bus,float volume){if(!clip)return;var go=new GameObject(bus.ToString());go.transform.SetParent(transform);var s=go.AddComponent<AudioSource>();s.clip=clip;s.loop=true;s.volume=volume;PaperAudioLevel.Bind(s,bus);s.Play();}
  void Start(){Switch(new PlayingState());}
  void OnApplicationFocus(bool focused){if(!focused&&PaperSettingsRuntime.Values.muteBackground&&preview)preview.Stop();}
  void OnEnable(){GameEvents.PieceCollected+=Collected;}
  void OnDisable(){GameEvents.PieceCollected-=Collected;}
  void OnDestroy(){if(pausePaper)Destroy(pausePaper);if(textPaper)Destroy(textPaper);skin?.Dispose();if(leafInk)Destroy(leafInk);if(Instance==this){if(ScreenState==PaperScreen.ConfirmDisplay&&PaperSettingsRuntime.Instance)PaperSettingsRuntime.Instance.Apply(PaperSettingsRuntime.Instance.Saved);Instance=null;Time.timeScale=1;AudioListener.pause=false;}}
  void Update(){if(Machine==null)return;if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Back();Machine.Tick(Time.unscaledDeltaTime);UpdateHud();}
  void Switch(UIState state){Machine.Change(state);}
  void EnterScreen(){bool story=ScreenState==PaperScreen.Story;bool playing=ScreenState==PaperScreen.Playing||(story&&WalkingStory);GameFlow.EnsureExists().SetPhase(playing?GamePhase.Playing:story?GamePhase.Cutscene:GamePhase.Paused);bool pauseAudio=!playing&&!story;if(!pauseAudio)AudioListener.pause=false;PaperAudioLevel.AllowMusicDuringReading(ScreenState==PaperScreen.Reading);AudioListener.pause=pauseAudio;if(story)ResetStoryInput();Cursor.lockState=playing?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!playing;Draw();}
  public void Pause(){if(ScreenState==PaperScreen.Playing||ScreenState==PaperScreen.Story)Switch(new PausedState());}
  public void Resume(){if(StoryActive)Switch(new StoryState());else Switch(new PlayingState());}
  public void OpenSettings(){Draft=PaperSettingsRuntime.Instance.Saved.Copy();status="";Switch(new SettingsState());}
  public void SelectTab(int index){Tab=Mathf.Clamp(index,0,3);Draw();}
  public void Back(){BookTower.Audio.SfxDirector.PlayUi(true);switch(ScreenState){case PaperScreen.Story:Pause();break;case PaperScreen.Playing:if(GameFlow.Instance.Phase==GamePhase.Playing)Pause();break;case PaperScreen.Paused:Resume();break;case PaperScreen.Settings:if(Dirty)Switch(new ConfirmationState(PaperScreen.ConfirmDiscard));else Switch(new PausedState());break;case PaperScreen.Choosing:Switch(new SettingsState());break;case PaperScreen.Rebinding:Switch(new SettingsState());break;case PaperScreen.ConfirmDisplay:RejectDisplay();break;case PaperScreen.ConfirmDiscard:Switch(new SettingsState());break;case PaperScreen.ConfirmQuit:Switch(new PausedState());break;case PaperScreen.Reading:CloseReading();break;case PaperScreen.Journal:Switch(new PausedState());break;}}
  public void ApplySettings(){if(!Dirty){status="변경된 설정이 없습니다.";Draw();return;}Draft.Validate();if(Draft.DisplayDiffers(PaperSettingsRuntime.Instance.Saved)){PaperSettingsRuntime.Instance.Apply(Draft);deadline=Time.unscaledTime+15;Switch(new ConfirmationState(PaperScreen.ConfirmDisplay));}else CommitDraft();}
  public void CommitDraft(){if(PaperSettingsRuntime.Instance.Commit(Draft,out string error)){Draft=PaperSettingsRuntime.Instance.Saved.Copy();status="설정을 저장했습니다.";}else status=error;Switch(new SettingsState());Draw();}
  public void RejectDisplay(){PaperSettingsRuntime.Instance.Apply(PaperSettingsRuntime.Instance.Saved);Draft=PaperSettingsRuntime.Instance.Saved.Copy();status="이전 설정으로 되돌렸습니다.";Switch(new SettingsState());}
  public void Discard(){PaperSettingsRuntime.Instance.Apply(PaperSettingsRuntime.Instance.Saved);Draft=PaperSettingsRuntime.Instance.Saved.Copy();Switch(new PausedState());}
  public void Defaults(){Draft=new PaperSettings();status="기본값입니다. 적용을 눌러 저장하세요.";Draw();}
  public void BeginBind(int action){bindAction=action;deadline=Time.unscaledTime+.2f;Switch(new RebindingState());}
  public bool TryBind(Key key){if(key==Key.None||key==Key.Escape){status="이 키는 메뉴용으로 예약되어 있습니다.";return false;}if(Draft.keys.Where((k,i)=>i!=bindAction).Contains((int)key)){status="다른 동작에서 사용하는 키입니다.";Draw();return false;}Draft.keys[bindAction]=(int)key;status="키를 변경했습니다. 적용을 눌러 저장하세요.";Switch(new SettingsState());return true;}
  public void PreviewSound(){preview.Stop();preview.clip=Resources.Load<AudioClip>("Sfx/ui_click");preview.volume=(Draft??PaperSettingsRuntime.Values).master*(Draft??PaperSettingsRuntime.Values).effects;preview.Play();}
  public void Read(ReadableNote readable){if(ScreenState!=PaperScreen.Playing)return;note=readable;page=0;returnScreen=PaperScreen.Playing;Switch(new ReadingState());NoteRead?.Invoke(readable.id);}
  void NextPage(){if(note&&page+1<note.pages.Length){page++;Draw();}else CloseReading();}
  void CloseReading(){if(returnScreen==PaperScreen.Journal)Switch(new JournalState());else Resume();}
  void NormalizeRecords(){var previous=records.ids;records=new Records();if(previous==null)return;foreach(var id in previous)if(!records.ids.Contains(id)&&StoryJournal.TryGet(id,out var title,out var text)){records.ids.Add(id);records.titles.Add(title);records.texts.Add(text);}}
  void Store(string id,string title,string text){if(!StoryJournal.TryGet(id,out title,out text))return;int index=records.ids.IndexOf(id);if(index>=0){records.titles[index]=title;records.texts[index]=text;}else{records.ids.Add(id);records.titles.Add(title);records.texts.Add(text);}try{File.WriteAllText(RecordPath,JsonUtility.ToJson(records,true));}catch(Exception e){Debug.LogWarning(e.Message);}}
  void Collected(string id){Notify("이야기 조각을 찾았습니다.");}
  public static void Notify(string text){if(!Instance)return;Instance.toast=text;Instance.toastUntil=Time.unscaledTime+3;}
  void Checkpoint(){if(StoryActive){Notify("이 장면을 마친 뒤 돌아갈 수 있습니다.");return;}player?.TeleportToSafePoint();Resume();Notify("마지막 안전한 자리로 돌아왔습니다.");}
  void Quit(){Time.timeScale=1;AudioListener.pause=false;
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
 }
}
