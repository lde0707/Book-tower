using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using BookTower.Interaction;
using BookTower.Core;
namespace BookTower.UI
{
 public sealed partial class PaperUI
 {
  RectTransform stage,book,body,hud,dialog;Image shade,leaf,feedbackDot;Text promptText,toastText,stateText;GameObject promptCard,toastCard;RectTransform brackets;string readingTitle,readingText;int journalPage;
  readonly Color ink=new(.22f,.14f,.09f),red=new(.62f,.27f,.18f),paper=new(.97f,.92f,.81f),gold=new(.64f,.53f,.36f);
  float TextFactor=>new[]{.85f,1f,1.15f}[PaperSettingsRuntime.Values.textSize];
  RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.gameObject.layer=5;t.SetParent(parent,false);t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(x,-y);t.sizeDelta=new Vector2(w,h);return t;}
  Image Image(Transform p,string n,float x,float y,float w,float h,Color c,Sprite sprite=null,bool ray=false){var t=Rect(p,n,x,y,w,h);var im=t.gameObject.AddComponent<Image>();im.color=c;im.sprite=sprite;im.raycastTarget=ray;return im;}
  Text Label(Transform p,string text,float x,float y,float w,float h,int size=28,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft){var t=Rect(p,text,x,y,w,h).gameObject.AddComponent<Text>();t.font=bold&&headingFont?headingFont:font;t.text=text;t.fontSize=Mathf.RoundToInt(size*TextFactor);t.fontStyle=FontStyle.Normal;t.color=ink;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
  Button Button(Transform p,string caption,float x,float y,float w,float h,Action action,bool primary=false){var bg=skin!=null?Art(p,caption,x,y,w,h,primary?skin.primary:skin.button,true):Image(p,caption,x,y,w,h,primary?red:paper,null,true);var b=bg.gameObject.AddComponent<Button>();var colors=b.colors;colors.highlightedColor=new Color(1,.92f,.8f);colors.selectedColor=new Color(1,.9f,.78f);colors.pressedColor=new Color(.76f,.62f,.5f);colors.fadeDuration=.12f;b.colors=colors;b.onClick.AddListener(()=>{BookTower.Audio.SfxDirector.PlayUi();action();});var txt=Label(bg.transform,caption,8,0,w-16,h,26,false,TextAnchor.MiddleCenter);if(primary)txt.color=paper;return b;}
  void Clear(RectTransform t){foreach(Transform child in t){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
  void BuildChrome()
  {
   if(skinAtlas)skin=new PaperUISkin(skinAtlas);InitStoryTheme();
   if(!EventSystem.current){var e=new GameObject("Paper UI EventSystem");e.transform.SetParent(transform);e.AddComponent<EventSystem>();e.AddComponent<InputSystemUIInputModule>();}
   var canvasObject=new GameObject("Paper UI Canvas",typeof(RectTransform));canvasObject.transform.SetParent(transform,false);var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
   var scaler=canvasObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;canvasObject.AddComponent<GraphicRaycaster>();
   stage=Rect(canvasObject.transform,"Safe frame",0,0,1920,1080);stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(.5f,.5f);stage.anchoredPosition=Vector2.zero;
   hud=Rect(stage,"Gameplay HUD",0,0,1920,1080);Image(hud,"Ink dot",957,537,6,6,ink);brackets=Rect(hud,"Target corners",932,512,56,56);
   foreach(int x in new[]{0,1})foreach(int y in new[]{0,1}){Image(brackets,"Corner",x*50,y*52,6,4,paper);Image(brackets,"Corner",x*52,y*44,4,12,paper);}
   promptCard=Image(hud,"Action prompt",790,590,340,65,paper).gameObject;PaperBox(promptCard.GetComponent<Image>());promptText=Label(promptCard.transform,"",10,0,320,65,26,false,TextAnchor.MiddleCenter);
   toastCard=Image(hud,"Brief feedback",520,890,880,75,paper).gameObject;PaperBox(toastCard.GetComponent<Image>());toastText=Label(toastCard.transform,"",20,0,840,75,27,false,TextAnchor.MiddleCenter);
   BuildStoryHud();
   if(cutsceneArt && cutsceneArt.IsReady)CutsceneView=PaperCutsceneView.Build(stage,cutsceneArt,headingFont?headingFont:font);
   shade=Image(stage,"Dim world",-1920,-1080,5760,3240,new Color(.07f,.04f,.02f,.65f),null,true);
   book=Rect(stage,"Shared notebook",240,60,1440,960);Image(book,"Retouched notebook",0,0,1440,960,Color.white,notebookSprite,true);
   body=Rect(book,"Content",0,0,1440,900);leaf=Image(book,"Shared leaf 120",1220,655,120,120,Color.white,skin!=null?skin.footerLeaf:leafSprite);leaf.preserveAspect=true;leaf.useSpriteMesh=true;if(inkOnlyMaterial){leafInk=new Material(inkOnlyMaterial);leafInk.SetFloat("_LeafCrop",1);leaf.material=leafInk;}
   dialog=Rect(stage,"Read and modal",0,0,1920,1080);
  }
  void Draw()
  {
   if(!body)return;Clear(body);Clear(dialog);storyBody=null;storyHint=null;shade.color=new Color(.07f,.04f,.02f,ScreenState==PaperScreen.Story?0:.65f);modalTimer=null;stateText=null;feedbackDot=null;
   bool playing=ScreenState==PaperScreen.Playing;hud.gameObject.SetActive(playing);shade.gameObject.SetActive(!playing);book.gameObject.SetActive(!playing&&ScreenState!=PaperScreen.Story&&ScreenState!=PaperScreen.Reading&&ScreenState!=PaperScreen.Paused&&ScreenState!=PaperScreen.ConfirmQuit);leaf.gameObject.SetActive(ScreenState!=PaperScreen.Reading);dialog.gameObject.SetActive(!playing);
   if(playing)return;
   switch(ScreenState){case PaperScreen.Story:DrawStory();break;case PaperScreen.Paused:DrawPause();break;case PaperScreen.Settings:DrawSettings();break;case PaperScreen.Choosing:DrawSettings();DrawOptions();break;case PaperScreen.Rebinding:DrawSettings();Modal("새 키를 눌러 주세요",status==""?"Esc로 취소할 수 있습니다.":status,"취소",()=>Switch(new SettingsState()));break;case PaperScreen.ConfirmDisplay:DrawSettings();Modal("이 화면 설정을 유지할까요?","","이전 설정",RejectDisplay,"유지",CommitDraft);modalTimer=Label(dialog,"",600,540,720,60,25,false,TextAnchor.MiddleCenter);break;case PaperScreen.ConfirmDiscard:DrawSettings();Modal("변경사항을 버릴까요?","아직 적용하지 않은 설정이 있습니다.","계속 편집",()=>Switch(new SettingsState()),"버리기",Discard);break;case PaperScreen.ConfirmQuit:DrawPause();Modal("이야기를 잠시 덮을까요?","설정과 기록은 보관됩니다. 진행은 현재 세션에만 유지됩니다.","취소",()=>Switch(new PausedState()),"나가기",Quit);break;case PaperScreen.Reading:DrawReading();break;case PaperScreen.Journal:DrawJournal();break;}
   bool modal=ScreenState==PaperScreen.Choosing||ScreenState==PaperScreen.Rebinding||ScreenState==PaperScreen.ConfirmDisplay||ScreenState==PaperScreen.ConfirmDiscard||ScreenState==PaperScreen.ConfirmQuit;
   var group=body.GetComponent<CanvasGroup>();if(!group)group=body.gameObject.AddComponent<CanvasGroup>();group.interactable=!modal;group.blocksRaycasts=!modal;
   var scope=modal||ScreenState==PaperScreen.Reading||ScreenState==PaperScreen.Paused?dialog:body;var first=scope.GetComponentsInChildren<Selectable>().FirstOrDefault(s=>s.gameObject.activeInHierarchy&&s.IsInteractable()&&s.name!="Close options");if(ScreenState==PaperScreen.Settings)first=body.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==new[]{"조작","화면","소리","접근성"}[Tab]&&b.gameObject.activeInHierarchy);if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
  }
  void Header(string title){Label(body,"일시정지 / "+title,85,45,310,40,23);if(skin!=null)Art(body,"Header leaf",90,100,42,61,skin.headerLeaf);Label(body,title,150,95,230,70,44,true);Rule(110,750,1220);}
  void DrawPause(){PauseMenu();}
  void DrawSettings()
  {
   if(Draft==null)Draft=PaperSettingsRuntime.Instance.Saved.Copy();Signature(body,880,210,400,490);Header("설정");string[] tabs={"조작","화면","소리","접근성"};for(int i=0;i<4;i++){int tab=i;if(skin!=null)Category(tabs[i],i);else Button(body,tabs[i],95,225+i*112,265,70,()=>SelectTab(tab),Tab==i);}
   Label(body,tabs[Tab]+" 설정",465,75,800,70,42,true);Rule(465,165,845);

   if(Tab==0)Controls();else if(Tab==1)Display();else if(Tab==2)Sound();else Access();
   if(skin!=null)feedbackDot=Art(body,"Feedback dot",465,725,16,16,skin.dot);stateText=Label(body,Dirty?"저장되지 않은 변경사항이 있습니다.":status,493,716,712,28,22);
   foreach(var footer in new[]{Button(body,"기본값 복원",155,770,230,56,Defaults),Button(body,"취소",900,770,170,56,Back),Button(body,"적용",1100,770,170,56,ApplySettings,true)}){if(textPaper){footer.image.sprite=textPaper;footer.image.type=UnityEngine.UI.Image.Type.Sliced;footer.image.color=footer.name=="적용"?red:Color.white;}PaperEdge(footer);footer.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(28*TextFactor);}
  }
  void SliderRow(string label,float y,float min,float max,float value,Action<float> set,string format,bool percent=false)
  {
   Label(body,label,465,y,300,48,27);var t=Rect(body,label+" slider",790,y,490,48);if(skin!=null)Art(t,"Track",0,18,420,12,skin.track);else Image(t,"Track",0,20,420,8,gold);var area=Rect(t,"Fill area",0,18,420,12);var fill=skin!=null?Art(area,"Fill",0,0,420,12,skin.fill):Image(area,"Fill",0,0,420,12,red);var handles=Rect(t,"Handle area",0,0,420,48);var knob=skin!=null?Art(handles,"Handle",0,10,28,28,skin.knob,true):Image(handles,"Handle",0,10,28,28,red,null,true);
   // Slider drives anchors itself: retained fixed widths would extend beyond the track.
   fill.rectTransform.sizeDelta=Vector2.zero;fill.rectTransform.anchoredPosition=Vector2.zero;fill.rectTransform.pivot=new Vector2(.5f,.5f);
   handles.anchoredPosition=new Vector2(0,-10);handles.sizeDelta=new Vector2(420,28);knob.rectTransform.sizeDelta=new Vector2(28,0);knob.rectTransform.anchoredPosition=Vector2.zero;knob.rectTransform.pivot=new Vector2(.5f,.5f);
   var hit=t.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;var slider=t.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=knob.rectTransform;slider.targetGraphic=knob;slider.minValue=min;slider.maxValue=max;slider.value=value;var readout=Label(t,percent?(value*100).ToString("0")+"%":value.ToString(format),435,0,80,48,24);
   slider.onValueChanged.AddListener(v=>{set(v);readout.text=percent?(v*100).ToString("0")+"%":v.ToString(format);});
  }
  void ToggleRow(string label,float y,bool value,Action<bool> set){Label(body,label,465,y,390,50,27);var hit=Image(body,label+" toggle",880,y,270,48,Color.clear,null,true);var b=hit.gameObject.AddComponent<Button>();var box=skin!=null?Art(hit.transform,"Checkbox",0,3,42,42,value?skin.check:skin.button):Image(hit.transform,"Checkbox",0,3,42,42,value?red:paper);b.targetGraphic=box;Label(hit.transform,value?"켜짐":"꺼짐",62,0,150,48,26);b.onClick.AddListener(()=>{BookTower.Audio.SfxDirector.PlayUi();set(!value);Draw();});}
  void Choice(string label,float y,string[] values,int selected,Action<int> set){Label(body,label,465,y,310,52,27);var b=Button(body,values[Mathf.Clamp(selected,0,values.Length-1)],790,y,505,50,()=>Choose(label,values,selected,set));b.name=label+" dropdown";if(skin!=null)b.image.sprite=skin.field;var caption=b.GetComponentInChildren<Text>();caption.alignment=TextAnchor.MiddleLeft;caption.rectTransform.anchoredPosition=new Vector2(22,0);caption.rectTransform.sizeDelta=new Vector2(420,50);}
  static string KeyCaption(int key)=>key==(int)Key.LeftShift?"Shift":key==(int)Key.RightShift?"Right Shift":((Key)key).ToString();
  void Controls(){SliderRow("마우스 감도",210,.2f,3,Draft.sensitivity,v=>Draft.sensitivity=v,"0.0");ToggleRow("상하 시점 반전",270,Draft.invertY,v=>Draft.invertY=v);Label(body,"키 설정",465,330,600,45,30,true);
   string[] labels={"앞","뒤","왼쪽","오른쪽"};for(int i=0;i<4;i++){int k=i;Button(body,labels[i]+"  "+KeyCaption(Draft.keys[i]),465+i*200,385,180,48,()=>BeginBind(k));}
   string[] names={"점프 / 활공","상호작용","왼쪽 시점","오른쪽 시점","달리기 (누르고 있기)"};for(int i=4;i<9;i++){int k=i;float y=450+(i-4)*47;Label(body,names[i-4],465,y,340,44,26);var binding=Button(body,KeyCaption(Draft.keys[i]),850,y,190,42,()=>BeginBind(k));binding.name="Binding "+i;if(i==5)Label(body,"/ 마우스 왼쪽",1060,y,230,44,23);}}
  void Display(){Label(body,"디스플레이",465,190,500,40,29,true);Choice("화면 모드",240,new[]{"전체 화면","테두리 없는 창","창 모드"},Draft.windowMode,v=>Draft.windowMode=v);string[] options={"1280 × 720","1600 × 900","1920 × 1080","2560 × 1440","3840 × 2160"};int[] w={1280,1600,1920,2560,3840},h={720,900,1080,1440,2160};int idx=Array.IndexOf(w,Draft.width);Choice("해상도",305,options,Mathf.Max(0,idx),v=>{Draft.width=w[v];Draft.height=h[v];});SliderRow("밝기",370,0,100,Draft.brightness,v=>Draft.brightness=v,"0");Rule(465,440,845);Label(body,"그래픽",465,460,500,40,29,true);Choice("그래픽 품질",510,new[]{"낮음","보통","높음"},Draft.quality,v=>Draft.quality=v);int[] fps={30,60,120,144,-1};Choice("프레임 제한",575,new[]{"30 FPS","60 FPS","120 FPS","144 FPS","제한 없음"},Array.IndexOf(fps,Draft.fps),v=>Draft.fps=fps[v]);ToggleRow("수직 동기화",635,Draft.vsync,v=>Draft.vsync=v);Label(body,Draft.vsync?"수직 동기화 중에는 모니터 주사율을 따릅니다.":"밝기는 종이의 결이 보이도록 조절하세요.",465,675,710,32,21);}
  void Sound(){SliderRow("전체 음량",215,0,1,Draft.master,v=>Draft.master=v,"0",true);SliderRow("배경 음악",295,0,1,Draft.music,v=>Draft.music=v,"0",true);SliderRow("효과음",375,0,1,Draft.effects,v=>Draft.effects=v,"0",true);SliderRow("환경음",455,0,1,Draft.ambience,v=>Draft.ambience=v,"0",true);Button(body,"종이 소리 미리 듣기",465,565,520,55,PreviewSound);ToggleRow("백그라운드에서 음소거",655,Draft.muteBackground,v=>Draft.muteBackground=v);}
  void Access(){Choice("글자 크기",200,new[]{"작게","보통","크게"},Draft.textSize,v=>Draft.textSize=v);ToggleRow("상호작용 안내",255,Draft.prompts,v=>Draft.prompts=v);ToggleRow("색 구분 보조",310,Draft.colorAssist,v=>Draft.colorAssist=v);ToggleRow("화면 흔들림",365,Draft.shake,v=>Draft.shake=v);ToggleRow("번쩍임 완화",420,Draft.reduceFlash,v=>Draft.reduceFlash=v);ToggleRow("카메라 자동 회전",475,Draft.autoCamera,v=>Draft.autoCamera=v);Choice("대사 표시 속도",530,new[]{"천천히","보통","빠르게"},Draft.dialogueSpeed,v=>Draft.dialogueSpeed=v);ToggleRow("컷신 움직임 줄이기",585,Draft.reduceStoryMotion,v=>Draft.reduceStoryMotion=v);Label(body,"대사 키: 전체 표시 → 다음 대사 → 마지막에서 플레이로 복귀",465,637,830,28,20);Label(body,"중요 대사는 항상 표시됩니다. Esc로 읽기를 멈출 수 있습니다.",465,675,830,32,20);}
  void Modal(string title,string message,string no,Action reject,string yes=null,Action accept=null){Image(dialog,"Modal dim",0,0,1920,1080,new Color(0,0,0,.45f),null,true);var panel=Image(dialog,"Modal paper",510,330,900,420,paper,null,true);Label(panel.transform,title,60,35,780,70,34,true,TextAnchor.MiddleCenter);Label(panel.transform,message,70,120,760,100,25,false,TextAnchor.MiddleCenter);Button(panel.transform,no,yes==null?320:130,290,260,56,reject);if(yes!=null)Button(panel.transform,yes,510,290,260,56,accept,true);}
  void DrawReading(){var panel=Image(dialog,"Reading paper",230,725,1460,285,Color.white,textPaper,true);panel.type=UnityEngine.UI.Image.Type.Sliced;Art(panel.transform,"Story bookmark",26,-8,42,110,skin.ribbon);Label(panel.transform,note?note.title:readingTitle,95,24,1170,45,29,true);Art(panel.transform,"Story divider",95,76,1300,5,skin.rule);Label(panel.transform,note?note.pages[Mathf.Clamp(page,0,note.pages.Length-1)]:readingText,95,90,1260,105,28);Button(panel.transform,note&&page+1<note.pages.Length?"다음":"닫기",1195,212,180,44,NextPage);Label(panel.transform,"Esc 닫기",95,215,820,44,22);}
  void DrawJournal(){Header("기록장");Label(body,"발견한 이야기",465,90,800,80,40,true);if(records.ids.Count==0)Label(body,"여정에서 알게 된 중요한 이야기만 남습니다.",465,220,780,100,28);for(int i=journalPage*6;i<Mathf.Min(records.ids.Count,journalPage*6+6);i++){int n=i;Button(body,records.titles[i],465,220+(i%6)*75,730,55,()=>{note=null;readingTitle=records.titles[n];readingText=records.texts[n];returnScreen=PaperScreen.Journal;Switch(new ReadingState());});}Button(body,"이전",465,700,140,45,()=>{journalPage=Mathf.Max(0,journalPage-1);Draw();});Button(body,"다음",640,700,140,45,()=>{journalPage=Mathf.Min(Mathf.Max(0,(records.ids.Count-1)/6),journalPage+1);Draw();});Button(body,"돌아가기",1050,800,250,50,()=>Switch(new PausedState()));}
  public static string Symbol(Interactable target){if(target.GetComponent<ReadableNote>())return "□";if(target.name.Contains("코럴"))return "○";if(target.name.Contains("청록"))return "△";return "◇";}
 }
}
