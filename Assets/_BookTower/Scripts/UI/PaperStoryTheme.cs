using UnityEngine;
using UnityEngine.UI;
namespace BookTower.UI
{
 public sealed partial class PaperUI
 {
  public Texture2D storyAtlas;
  Sprite pausePaper,textPaper;RectTransform pauseContent;
  void InitStoryTheme(){if(!storyAtlas)storyAtlas=Resources.Load<Texture2D>("StoryPanels");if(!storyAtlas)return;pausePaper=Sprite.Create(storyAtlas,new Rect(28,1024-879,666,748),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(85,40,40,40));textPaper=Sprite.Create(storyAtlas,new Rect(709,1024-615,801,202),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(35,35,35,35));}
  void PaperBox(Image image){if(!textPaper)return;image.sprite=textPaper;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;}
  void PauseMenu()
  {
   pauseContent=Rect(dialog,"Single bookmark panel",590,130,740,820);var backdrop=Image(pauseContent,"Bound paper",0,0,740,820,Color.white,pausePaper,true);backdrop.type=UnityEngine.UI.Image.Type.Simple;
   Label(pauseContent,"잠시 책갈피를 꽂아둘까요",110,70,550,65,32,true);
   string[] names={"계속하기","설정","기록장","체크포인트로 돌아가기","나가기"};
   System.Action[] actions={Resume,OpenSettings,()=>Switch(new JournalState()),Checkpoint,()=>Switch(new ConfirmationState(PaperScreen.ConfirmQuit))};
   for(int i=0;i<names.Length;i++){
    var b=Button(pauseContent,names[i],110,180+i*78,530,60,actions[i]);b.image.sprite=skin.selected;b.transition=Selectable.Transition.None;
    var label=b.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleLeft;label.rectTransform.anchoredPosition=new Vector2(42,0);label.rectTransform.sizeDelta=new Vector2(475,60);
    var marker=Art(b.transform,"Hover bookmark",-23,-7,27,74,skin.ribbon);var hover=b.gameObject.AddComponent<PaperMenuHighlight>();hover.strip=b.image;hover.ribbon=marker.gameObject;hover.Set(false);
   }
   var decoration=Image(pauseContent,"Pause leaf",590,677,76,103,Color.white,skin.footerLeaf);decoration.material=leafInk;decoration.preserveAspect=true;
   Label(pauseContent,"Esc  돌아가기",110,740,420,36,21);
   if(ScreenState==PaperScreen.ConfirmQuit){var group=pauseContent.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;}
  }
 }
}
