using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace BookTower.UI
{
 [DefaultExecutionOrder(-300)] public sealed class PaperSettingsRuntime:MonoBehaviour
 {
  public static PaperSettingsRuntime Instance {get;private set;}
  static readonly PaperSettings defaults=new();
  public static PaperSettings Values=>Instance?Instance.Current:defaults;
  public PaperSettings Current {get;private set;}=new();
  public PaperSettings Saved {get;private set;}=new();
  public static string TestStoragePath;
  public string StoragePath=>TestStoragePath??Path.Combine(Application.persistentDataPath,"paper-settings-v1.json");
  UniversalRenderPipelineAsset pipeline;RenderPipelineAsset priorPipeline;Volume volume;ColorAdjustments color;Bloom bloom;
  int baseVsync,baseFps;bool focused=true;
  public static bool Held(int action)=>Keyboard.current!=null&&Keyboard.current[(Key)Values.keys[action]].isPressed;
  public static bool Pressed(int action)=>Keyboard.current!=null&&Keyboard.current[(Key)Values.keys[action]].wasPressedThisFrame;
  public static bool RunHeld=>Held(8)||(Values.keys[8]==(int)Key.LeftShift&&!Values.keys.Contains((int)Key.RightShift)&&Keyboard.current!=null&&Keyboard.current.rightShiftKey.isPressed);
  void Awake(){if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;baseVsync=QualitySettings.vSyncCount;baseFps=Application.targetFrameRate;Load();var asset=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(asset){priorPipeline=QualitySettings.renderPipeline;pipeline=Instantiate(asset);QualitySettings.renderPipeline=pipeline;}volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();color=volume.profile.Add<ColorAdjustments>(false);bloom=volume.profile.Add<Bloom>(false);Apply(Saved,!Application.isEditor);}
  public void Load(){try{Saved=File.Exists(StoragePath)?JsonUtility.FromJson<PaperSettings>(File.ReadAllText(StoragePath)):new PaperSettings();Saved??=new PaperSettings();Saved.Validate();}catch(Exception e){Debug.LogWarning("Settings reset: "+e.Message);Saved=new PaperSettings();}Current=Saved.Copy();}
  public bool Commit(PaperSettings settings,out string error)
  {error=null;settings.Validate();try{string temp=StoragePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(settings,true));if(File.Exists(StoragePath))File.Replace(temp,StoragePath,null);else File.Move(temp,StoragePath);Saved=settings.Copy();Apply(settings);return true;}catch(Exception e){error="설정을 저장하지 못했습니다. "+e.Message;Apply(Saved);return false;}}
  public void Apply(PaperSettings settings,bool display=true)
  {
   Current=settings.Copy();Current.Validate();
   if(display&&!Application.isBatchMode)Screen.SetResolution(Current.width,Current.height,Current.windowMode==0?FullScreenMode.ExclusiveFullScreen:Current.windowMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
   QualitySettings.vSyncCount=Current.vsync?1:0;Application.targetFrameRate=Current.fps;
   if(pipeline)PaperRenderQuality.Apply(pipeline,Current.quality);
   if(color)color.postExposure.Override(.35f+(Current.brightness-50)/50f);
   if(bloom){bloom.intensity.overrideState=Current.reduceFlash;bloom.intensity.value=0;}
   AudioListener.volume=(!focused&&Current.muteBackground)?0:Current.master;
  }
  void OnApplicationFocus(bool on){focused=on;AudioListener.volume=!on&&Current.muteBackground?0:Current.master;}
  void OnDestroy(){if(Instance!=this)return;QualitySettings.renderPipeline=priorPipeline;if(pipeline)Destroy(pipeline);if(volume&&volume.profile)Destroy(volume.profile);QualitySettings.vSyncCount=baseVsync;Application.targetFrameRate=baseFps;AudioListener.volume=1;AudioListener.pause=false;Instance=null;}
 }

}
