using System;
using UnityEngine;
using UnityEngine.UI;
namespace BookTower.UI
{
 public sealed partial class PaperUI
 {
  public Texture2D skinAtlas;public Material inkOnlyMaterial;Material leafInk;
  PaperUISkin skin;
  Image Art(Transform p,string name,float x,float y,float w,float h,Sprite sprite,bool ray=false){var im=Image(p,name,x,y,w,h,Color.white,sprite,ray);if(sprite&&sprite.border.sqrMagnitude>0)im.type=UnityEngine.UI.Image.Type.Sliced;if(inkOnlyMaterial&&(name.StartsWith("Category icon")||name=="Header leaf"))im.material=inkOnlyMaterial;return im;}
  void Rule(float x,float y,float w){if(skin!=null)Art(body,"Fine divider",x,y,w,5,skin.rule);else Image(body,"Fine divider",x,y,w,2,gold);}
  void Category(string title,int index)
  {
   float y=225+index*112;var b=Button(body,title,95,y,265,78,()=>SelectTab(index));
   b.image.sprite=Tab==index?skin.selected:null;b.image.type=UnityEngine.UI.Image.Type.Sliced;b.image.color=Tab==index?Color.white:new Color(1,1,1,0);
   // Keep the selected strip pale coral; the red ribbon is the active-state marker.
   var colors=b.colors;colors.highlightedColor=new Color(1,.91f,.81f);colors.selectedColor=Color.white;colors.pressedColor=new Color(.86f,.73f,.6f);b.colors=colors;
   var caption=b.GetComponentInChildren<Text>();caption.rectTransform.anchoredPosition=new Vector2(110,0);caption.rectTransform.sizeDelta=new Vector2(150,78);caption.alignment=TextAnchor.MiddleLeft;caption.color=ink;
   Art(b.transform,"Category icon "+index,40,15,48,48,skin.categories[index]).preserveAspect=true;
   if(Tab==index)Art(body,"Selected ribbon",72,y-8,38,99,skin.ribbon);
  }
  string optionTitle;string[] optionValues;int optionSelected;Action<int> optionSet;
  sealed class ChoosingState:UIState{public override PaperScreen Kind=>PaperScreen.Choosing;}
  void Choose(string title,string[] values,int selected,Action<int> set){optionTitle=title;optionValues=values;optionSelected=selected;optionSet=set;Switch(new ChoosingState());}
  void DrawOptions()
  {
   var backdrop=Image(dialog,"Close options",0,0,1920,1080,new Color(0,0,0,.35f),null,true);backdrop.gameObject.AddComponent<Button>().onClick.AddListener(()=>Switch(new SettingsState()));
   float height=105+optionValues.Length*65;float top=(1080-height)/2;
   var panel=Image(dialog,"Options paper",670,top,670,height,paper,null,true);Label(panel.transform,optionTitle,30,15,610,55,31,true);
   for(int i=0;i<optionValues.Length;i++){int index=i;var b=Button(panel.transform,optionValues[i],30,85+i*65,610,54,()=>{optionSet(index);Switch(new SettingsState());},i==optionSelected);b.name="Option "+i;}
  }
 }
}
