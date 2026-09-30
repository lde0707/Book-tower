using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
 [InitializeOnLoad] public static class RunSettingsVerifier
 {
  const string Flag="RunSettingsQA",Out="Docs/ArtReview/RunSettings";
  static IEnumerator routine;static double next,started;static string report="";static Keyboard keys;static InputSettings previous;static PaperUI ui;
  static RunSettingsVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Storage(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/run-settings-test-"+DateTime.Now.Ticks+".json");}
  public static void Run(){Directory.CreateDirectory(Out);EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Flag,false))return;if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;next=started+1;routine=Test();EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"exit",1));}}
  static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float n?n:.2f);}catch(Exception e){Finish(e);}}
  static void Finish(Exception e){EditorApplication.update-=Tick;if(keys!=null)InputSystem.RemoveDevice(keys);if(previous)InputSystem.settings=previous;File.WriteAllText(Out+"/checks.txt",report+(e==null?"PASS":"FAIL "+e));SessionState.SetInt(Flag+"exit",e==null?0:1);EditorApplication.ExitPlaymode();}
  static void Check(bool ok,string text){if(!ok)throw new Exception(text);report+="PASS: "+text+"\n";}
  static void Capture(string name)
  {
   var canvas=ui.GetComponentInChildren<Canvas>();var cam=Camera.main;var rt=new RenderTexture(1600,900,24);var old=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect,scale=canvas.scaleFactor;
   try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=cam.nearClipPlane+1;cam.targetTexture=rt;cam.aspect=1600f/900;canvas.scaleFactor=1600f/1920;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());Object.Destroy(tex);}
   finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.scaleFactor=scale;cam.targetTexture=old;cam.aspect=aspect;RenderTexture.active=active;rt.Release();Object.Destroy(rt);}
  }
  static void PopupMargins(string panelName,float minimum)
  {
   var panel=ui.GetComponentsInChildren<RectTransform>().First(t=>t.name==panelName);
   foreach(var button in panel.GetComponentsInChildren<Button>())
   {var r=(RectTransform)button.transform;float x=r.anchoredPosition.x,y=-r.anchoredPosition.y;Check(x>=minimum&&y>=minimum&&panel.rect.width-x-r.rect.width>=minimum&&panel.rect.height-y-r.rect.height>=minimum,panelName+" button padding: "+button.name);}
  }
  static IEnumerator Test()
  {
   var legacy=new PaperSettings();legacy.keys=legacy.keys.Take(8).ToArray();legacy.keys[5]=(int)Key.R;legacy.Validate();Check(legacy.keys.Length==9&&legacy.keys[5]==(int)Key.R&&legacy.keys[8]==(int)Key.LeftShift,"Eight-key settings migrate without losing custom bindings");
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   ui=PaperUI.Instance;ui.EndStory();ui.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   var player=Object.FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();player.enabled=false;
   previous=InputSystem.settings;var config=Object.Instantiate(previous);config.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;config.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=config;keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   yield return .3f;ui.Pause();yield return .2f;Capture("pause");ui.OpenSettings();ui.SelectTab(0);yield return .3f;
   Check(ui.GetComponentsInChildren<Button>().Any(b=>b.name=="Binding 8"),"Run has an editable binding button");Capture("controls");
   foreach(string name in new[]{"기본값 복원","취소","적용"}){var r=(RectTransform)ui.GetComponentsInChildren<Button>().First(b=>b.name==name).transform;Check(r.anchoredPosition.x>=150&&r.anchoredPosition.x+r.rect.width<=1290&&-r.anchoredPosition.y+r.rect.height<=830,"Inset footer: "+name);}
   ui.BeginBind(8);yield return .2f;PopupMargins("Modal paper",60);Capture("rebind-popup");Check(!ui.TryBind(Key.W),"Duplicate movement binding rejected");Check(ui.TryBind(Key.R),"Run can be rebound to R");ui.ApplySettings();yield return .25f;
   var settings=PaperSettingsRuntime.Instance;settings.Load();Check(settings.Saved.keys[8]==(int)Key.R,"Run binding persists after reload");ui.Resume();yield return .3f;
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.R));yield return .2f;Check(player.Input.RunHeld&&player.WantsToRun,"Rebound key reaches run input");
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.LeftShift));yield return .2f;Check(!player.Input.RunHeld,"Old Shift no longer runs after rebinding");InputSystem.QueueStateEvent(keys,new KeyboardState());
   ui.Pause();ui.OpenSettings();ui.Defaults();ui.ApplySettings();yield return .2f;Check(settings.Saved.keys[8]==(int)Key.LeftShift,"Restore defaults restores Shift");
   ui.SelectTab(1);yield return .2f;ui.GetComponentsInChildren<Button>().First(b=>b.name=="화면 모드 dropdown").onClick.Invoke();yield return .2f;PopupMargins("Options paper",60);Capture("options-popup");ui.Back();
   for(int tab=0;tab<4;tab++){ui.SelectTab(tab);yield return .15f;Capture("tab-"+tab);}
  }
 }
}
