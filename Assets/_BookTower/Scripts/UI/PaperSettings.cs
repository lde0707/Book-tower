using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace BookTower.UI
{
 [Serializable] public sealed class PaperSettings
 {
  public int version=1;
  public int dialogueSpeed=1;public bool reduceStoryMotion;
  public float sensitivity=1,brightness=50,master=.8f,music=.6f,effects=.85f,ambience=.7f;
  public bool invertY,vsync=true,muteBackground=true,prompts=true,colorAssist=true,shake,reduceFlash=true,autoCamera;
  public int windowMode=1,width=1920,height=1080,quality=1,fps=60,textSize=1;
  public int[] keys={(int)Key.W,(int)Key.S,(int)Key.A,(int)Key.D,(int)Key.Space,(int)Key.F,(int)Key.Q,(int)Key.E,(int)Key.LeftShift};
  public PaperSettings Copy()=>JsonUtility.FromJson<PaperSettings>(JsonUtility.ToJson(this));
  public bool Same(PaperSettings other)=>other!=null&&JsonUtility.ToJson(this)==JsonUtility.ToJson(other);
  public bool DisplayDiffers(PaperSettings other)=>windowMode!=other.windowMode||width!=other.width||height!=other.height;
  public void Validate()
  {
   float Clamp(float v,float lo,float hi,float fallback)=>float.IsNaN(v)||float.IsInfinity(v)?fallback:Mathf.Clamp(v,lo,hi);
   sensitivity=Clamp(sensitivity,.2f,3,1);brightness=Clamp(brightness,0,100,50);
   master=Clamp(master,0,1,.8f);music=Clamp(music,0,1,.6f);effects=Clamp(effects,0,1,.85f);ambience=Clamp(ambience,0,1,.7f);
   dialogueSpeed=Mathf.Clamp(dialogueSpeed,0,2);
   windowMode=Mathf.Clamp(windowMode,0,2);quality=Mathf.Clamp(quality,0,2);textSize=Mathf.Clamp(textSize,0,2);
   width=Mathf.Clamp(width,800,7680);height=Mathf.Clamp(height,600,4320);if(!new[]{30,60,120,144,-1}.Contains(fps))fps=60;
   var defaults=new PaperSettings();
   // Preserve the player's eight existing bindings when adding the run action.
   if(keys!=null&&keys.Length==8)
   {
    int run=new[]{Key.LeftShift,Key.RightShift,Key.LeftCtrl,Key.RightCtrl,Key.R}.Concat(Enum.GetValues(typeof(Key)).Cast<Key>()).Select(k=>(int)k).First(k=>k!=(int)Key.None&&k!=(int)Key.Escape&&!keys.Contains(k));
    keys=keys.Concat(new[]{run}).ToArray();
   }
   if(keys==null||keys.Length!=9)keys=defaults.keys;
   if(keys.Distinct().Count()!=9||keys.Any(k=>!Enum.IsDefined(typeof(Key),k)||k==(int)Key.None||k==(int)Key.Escape))keys=defaults.keys;
  }
 }
}
