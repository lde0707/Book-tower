using System.Collections.Generic;
using UnityEngine;
namespace BookTower.UI
{
 // Sprite rectangles reference the approved source PNG; its pixels remain unchanged.
 public sealed class PaperUISkin
 {
  readonly Texture2D atlas;readonly List<Sprite> owned=new();
  public Sprite button,primary,field,selected,ribbon,knob,check,rule,dot,headerLeaf,footerLeaf,track,fill;
  public Sprite[] categories;
  public PaperUISkin(Texture2D texture)
  {
   atlas=texture;
   button=Cut(216,805,207,57,8);primary=Cut(1333,804,180,59,9);
   field=Cut(896,231,588,59,9);selected=Cut(334,294,161,87,8);
   ribbon=Cut(181,284,44,115);knob=Cut(1101,378,41,41);check=Cut(896,656,51,49,5);
   rule=Cut(587,154,908,10,6);dot=Cut(590,743,24,25);headerLeaf=Cut(207,73,54,78);
   track=Cut(1200,391,141,14,6);fill=Cut(900,391,170,14,6);
   footerLeaf=Cut(1445,626,99,157);
   // Avoid the adjacent dropdown corner without changing the source image.
   var points=new[]{new Vector2(42,0),new Vector2(99,0),new Vector2(99,157),new Vector2(0,157),new Vector2(0,95),new Vector2(42,35)};
   for(int i=0;i<points.Length;i++)points[i]=new Vector2((points[i].x-49.5f)/100,(78.5f-points[i].y)/100);
   footerLeaf.OverrideGeometry(points,new ushort[]{0,1,5,1,2,5,2,3,5,3,4,5});
   categories=new[]{Cut(250,218,53,39),Cut(250,320,54,51),Cut(249,425,56,47),Cut(251,525,51,57)};
  }
  Sprite Cut(int x,int y,int width,int height,int border=0){var s=Sprite.Create(atlas,new Rect(x,atlas.height-y-height,width,height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));owned.Add(s);return s;}
  public void Dispose(){foreach(var s in owned)Object.Destroy(s);}
 }
}
