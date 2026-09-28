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
  void PaperEdge(Button b){var rt=(RectTransform)b.transform;var edge=Rect(rt,"Hand torn paper border",0,0,rt.rect.width,rt.rect.height).gameObject.AddComponent<PaperMenuOrnament>();edge.color=new Color(.47f,.32f,.18f,.63f);edge.raycastTarget=false;}
  void Signature(Transform parent,float x,float y,float w,float h){var g=Rect(parent,"Shared light book signature",x,y,w,h).gameObject.AddComponent<PaperMenuOrnament>();g.signature=true;g.color=new Color(.48f,.34f,.18f,.095f);g.raycastTarget=false;}
  void PauseMenu()
  {
   pauseContent=Rect(dialog,"Single bookmark panel",590,130,740,820);var backdrop=Image(pauseContent,"Bound paper",0,0,740,820,Color.white,pausePaper,true);backdrop.type=UnityEngine.UI.Image.Type.Simple;
   Signature(pauseContent,115,75,520,650);
   Label(pauseContent,"잠시 책갈피를 꽂아둘까요",100,72,560,75,38,true,TextAnchor.MiddleCenter);
   string[] names={"계속하기","설정","기록장","체크포인트로 돌아가기","나가기"};
   System.Action[] actions={Resume,OpenSettings,()=>Switch(new JournalState()),Checkpoint,()=>Switch(new ConfirmationState(PaperScreen.ConfirmQuit))};
   for(int i=0;i<names.Length;i++){
    var b=Button(pauseContent,names[i],120,185+i*91,520,72,actions[i]);PaperBox(b.image);PaperEdge(b);b.transition=Selectable.Transition.None;
    var label=b.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleCenter;label.rectTransform.anchoredPosition=new Vector2(18,0);label.rectTransform.sizeDelta=new Vector2(484,72);label.fontSize=Mathf.RoundToInt(32*TextFactor);
    if(i==3&&StoryActive){b.interactable=false;label.text="장면을 마친 뒤 돌아갈 수 있어요";label.fontSize=Mathf.RoundToInt(22*TextFactor);}
    var marker=Art(b.transform,"Hover bookmark",-18,0,27,72,skin.ribbon);var hover=b.gameObject.AddComponent<PaperMenuHighlight>();hover.strip=b.image;hover.ribbon=marker.gameObject;hover.Set(false);
   }
   var decoration=Image(pauseContent,"Pause leaf",590,684,65,88,Color.white,skin.footerLeaf);decoration.material=leafInk;decoration.preserveAspect=true;
   Label(pauseContent,"Esc  돌아가기",180,716,400,46,25,false,TextAnchor.MiddleCenter);
   if(ScreenState==PaperScreen.ConfirmQuit){var group=pauseContent.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;}
  }
 }
}
