using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Audio;
using BookTower.Rooms;
using BookTower.UI;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static class RegionMusicSetup
    {
        const string Flag="RegionMusicQA";
        static IEnumerator routine;static double next,started;
        [InitializeOnLoadMethod]static void Init()=>EditorApplication.playModeStateChanged+=Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
        {if(SessionState.GetBool(Flag,false))PaperSettingsRuntime.TestStoragePath=Path.GetFullPath("Logs/music-test-"+DateTime.Now.Ticks+".json");}
        public static void RunBatch()
        {
            const string path="Assets/_BookTower/Resources/RegionMusic.asset";
            var library=AssetDatabase.LoadAssetAtPath<RegionMusicLibrary>(path);
            if(!library){library=ScriptableObject.CreateInstance<RegionMusicLibrary>();AssetDatabase.CreateAsset(library,path);}
            var names=new[]{"Region1_PaperLanterns_PianoViolinV3","Region2_FadedMemories_PianoViolinV3","Region3_TitleInTheLight_PianoViolinV3"};library.clips=new AudioClip[3];
            for(int i=0;i<3;i++)
            {
                string p="Assets/_BookTower/Audio/RegionMusic/"+names[i]+".wav";
                library.clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if(!library.clips[i]||library.clips[i].length<50)throw new Exception("Region music missing: "+p);
            }
            library.volume=.5f;library.fadeSeconds=2.5f;EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange s)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;next=started+1;routine=Verify();EditorApplication.update+=Tick;}
            if(s==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Flag);EditorApplication.Exit(SessionState.GetInt(Flag+"Code",1));}
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try {if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Music test timeout");if(!routine.MoveNext()){Finish(null);return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.2f);}
            catch(Exception e){Finish(e);}
        }
        static void Finish(Exception e)
        {
            EditorApplication.update-=Tick;Directory.CreateDirectory("Docs/ArtReview/RegionMusic");
            File.WriteAllText("Docs/ArtReview/RegionMusic/play-check.txt",e==null?"PASS: real InGame startup plays Region 1, no QuietPages overlap, room mapping, same-region continuity, three crossfades, loop wrap, independent music gain, pause gain freeze. Test settings isolated.":e.ToString());
            SessionState.SetInt(Flag+"Code",e==null?0:1);EditorApplication.ExitPlaymode();
        }
        static void Require(bool ok,string msg){if(!ok)throw new Exception(msg);}
        static IEnumerator Verify()
        {
            var music=Object.FindFirstObjectByType<RegionMusicPlayer>();Require(music,"No region music component");
            yield return 3f;Require(music.CurrentRegion==1&&music.Sources[0].isPlaying,"F0 intro must play first score");
            foreach(var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))Require(!s.clip||s.clip.name!="QuietPages"||!s.isPlaying,"Old temporary music overlaps");
            Require(RegionMusicPlayer.RegionFor("F0")==1&&RegionMusicPlayer.RegionFor("1B")==1&&RegionMusicPlayer.RegionFor("2F")==2&&RegionMusicPlayer.RegionFor("3A")==3,"Region mapping");
            // Suspend automatic room sampling while exercising boundary changes deterministically.
            var field=typeof(RegionMusicPlayer).GetField("rooms",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);field.SetValue(music,null);
            float before=music.Sources[0].time;music.SelectRoom("1B");yield return .3f;Require(music.Sources[0].time>before,"Same region restarted music");
            music.SelectRoom("2A");yield return 1f;Require(music.Sources[0].volume>0&&music.Sources[1].volume>0,"Crossfade missing");
            yield return 2f;Require(!music.Sources[0].isPlaying&&music.Sources[1].isPlaying,"Old score did not stop");
            music.SelectRoom("3A");yield return 3f;Require(!music.Sources[1].isPlaying&&music.Sources[2].isPlaying,"Third score not active");
            var settings=PaperSettingsRuntime.Values.Copy();settings.music=.25f;PaperSettingsRuntime.Instance.Apply(settings,false);yield return .2f;
            var gain=music.Sources[2].GetComponent<PaperAudioLevel>();Require(Mathf.Abs(music.Sources[2].volume-gain.baseline*.25f)<.001f,"Music slider not applied");
            music.Sources[2].time=music.Sources[2].clip.length-.2f;yield return .6f;Require(music.Sources[2].isPlaying&&music.Sources[2].time<1.5f,"Loop wrap failed");
            music.SelectRoom("1A");yield return .4f;AudioListener.pause=true;float volume=music.Sources[0].volume;yield return .4f;Require(Mathf.Abs(music.Sources[0].volume-volume)<.001f,"Paused crossfade advanced");AudioListener.pause=false;
        }
    }
}
