using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BookTower.Story;
using BookTower.UI;
namespace BookTower.EditorTools
{
    public static class ContinuousAwakeningPreview
    {
        public static void Render(PaperCutsceneArt art)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory("Logs/ContinuousFrames");
            var cam=new GameObject("Preview camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
            var canvas=new GameObject("Preview",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;canvas.scaleFactor=.5f;
            var view=PaperCutsceneView.Build(canvas.transform,art,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            view.Begin(OpeningStoryData.Promise);view.ShowShot(StoryShot.Knock);
            var rt=new RenderTexture(960,540,24);cam.targetTexture=rt;cam.aspect=16f/9;
            var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
            for(int f=0;f<360;f++)
            {
                if(f==60)view.ShowShot(StoryShot.Knock);
                if(f==120)view.ShowShot(StoryShot.ShareLight);
                if(f==210)view.ShowShot(StoryShot.Wake);
                view.Tick(1f/30);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
                tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();
                File.WriteAllBytes("Logs/ContinuousFrames/"+f.ToString("D4")+".png",tex.EncodeToPNG());
            }
            RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}
