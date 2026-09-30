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
 [InitializeOnLoad] public static class ReadingMusicVerifier
 {
  const string Flag="ReadingMusicQA",Out="Docs/ArtReview/ReadingMusic";
  static IEnumerator routine;static double next,started;static string report="";static Keyboard keys;static InputSettings previous;static PaperUI ui;
  static ReadingMusicVerifier(){EditorApplication.playModeStateChanged+=Changed;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Storage(){if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/reading-music-test-"+DateTime.Now.Ticks+".json");}
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
   foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None))story.gameObject.SetActive(false);
   ui=PaperUI.Instance;ui.EndStory();ui.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
   var player=Object.FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();player.enabled=false;
   var music=Object.FindFirstObjectByType<BookTower.Audio.RegionMusicPlayer>();Check(music,"Region BGM exists in actual InGame scene");
   yield return .5f;var source=music.Sources[music.CurrentRegion-1];var clip=source.clip;Check(source.isPlaying,"BGM plays before reading");
   var note=Object.FindFirstObjectByType<ReadableNote>();Check(note,"Readable book exists");
   int before=source.timeSamples;ui.Read(note);yield return .25f;
   Check(ui.ScreenState==PaperScreen.Reading&&Time.timeScale==0&&PaperUI.BlocksGameplay,"Reading still freezes gameplay");
   Check(source.ignoreListenerPause&&source.isPlaying,"BGM ignores listener pause only during reading");
   Check((source.timeSamples-before+clip.samples)%clip.samples>0,"Playback advances when opening book");
   int reading=source.timeSamples;yield return .6f;Check((source.timeSamples-reading+clip.samples)%clip.samples>0,"BGM continues throughout reading");
   var gain=source.GetComponent<PaperAudioLevel>();Check(source.volume>0&&gain.baseline>0,"Reading BGM has nonzero volume and fade");
   var settings=PaperSettingsRuntime.Instance;var old=settings.Current.Copy();var muted=old.Copy();muted.music=0;settings.Apply(muted,false);yield return .15f;Check(source.volume==0,"Music volume setting still mutes reading BGM");settings.Apply(old,false);yield return .15f;
   int closing=source.timeSamples;ui.Back();yield return .2f;
   Check(ui.ScreenState==PaperScreen.Playing&&source.clip==clip&&source.isPlaying,"Closing book keeps same playing track");
   Check((source.timeSamples-closing+clip.samples)%clip.samples<clip.frequency*2,"Closing book does not restart the track");
   ui.Pause();yield return .2f;int paused=source.timeSamples;yield return .3f;Check(!source.ignoreListenerPause&&source.timeSamples==paused,"ESC pause retains existing music pause behavior");
   ui.Resume();yield return .2f;Check(source.isPlaying,"BGM resumes after ESC menu");
  }
 }
}
