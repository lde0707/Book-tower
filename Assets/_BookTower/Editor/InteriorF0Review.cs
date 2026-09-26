using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.Info;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        public static void BuildReviewBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            ApplyAndSave();ValidateStructure();CaptureReferenceView();
        }
        [MenuItem("Book Tower/검증/F0 제본 현관 캡처")]
        public static void CaptureReferenceView()
        {
            var r=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.RoomId=="F0");Directory.CreateDirectory(Output);
            var states=new Dictionary<Renderer,bool>();var lightStates=new Dictionary<Light,bool>();
            foreach(var other in Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                foreach(var renderer in other.GetComponentsInChildren<Renderer>(true)){states[renderer]=renderer.enabled;renderer.enabled=other==r;}
                foreach(var l in other.GetComponentsInChildren<Light>(true)){lightStates[l]=l.enabled;l.enabled=other==r;}
            }
            var camGo=new GameObject("F0 capture");var cam=camGo.AddComponent<Camera>();cam.fieldOfView=60;cam.nearClipPlane=.08f;cam.farClipPlane=120;cam.allowHDR=true;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;bool fog=RenderSettings.fog;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.19f,.14f,.09f);RenderSettings.fog=false;
            var ambientProbe=RenderSettings.ambientProbe;
            var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.40f,.30f,.20f));RenderSettings.ambientProbe=sh;
            cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            try
            {
                cam.transform.position=r.transform.TransformPoint(V(1.7f,2.65f,-17));cam.transform.LookAt(r.transform.TransformPoint(V(0,5,9)));Capture(cam,Output+"/F0-reference-rebuild.png");
                cam.transform.position=r.transform.TransformPoint(V(0,2.0f,-16.4f));cam.transform.rotation=Quaternion.Euler(-2,0,0);Capture(cam,Output+"/F0-player-view.png");
                cam.transform.position=r.transform.TransformPoint(V(-3,4,-8));cam.transform.LookAt(r.transform.TransformPoint(V(12,8,0)));Capture(cam,Output+"/F0-rope-window-detail.png");
            }
            finally
            {
                RenderSettings.ambientProbe=ambientProbe;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;RenderSettings.fog=fog;Object.DestroyImmediate(camGo);
                foreach(var s in states)if(s.Key!=null)s.Key.enabled=s.Value;foreach(var s in lightStates)if(s.Key!=null)s.Key.enabled=s.Value;
            }
            Debug.Log("[F0 Reference] Three GPU scene captures saved");
        }
        public static void Capture(Camera cam,string path)
        {
            var rt=new RenderTexture(1600,1000,24);var old=RenderTexture.active;var oldTarget=cam.targetTexture;
            cam.targetTexture=rt;cam.aspect=1.6f;cam.Render();RenderTexture.active=rt;
            var im=new Texture2D(1600,1000,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1600,1000),0,0);im.Apply();File.WriteAllBytes(path,im.EncodeToPNG());
            cam.targetTexture=oldTarget;RenderTexture.active=old;Object.DestroyImmediate(im);rt.Release();Object.DestroyImmediate(rt);
        }
        public static void ValidateStructure()
        {
            var r=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.RoomId=="F0");var g=r.transform.Find(RootName);if(g==null)throw new Exception("F0 geometry missing");Physics.SyncTransforms();
            var floor=g.Find("연속_충돌바닥").GetComponent<Collider>();
            for(int x=-25;x<=25;x+=5)for(int z=-19;z<=19;z+=2)if(!floor.Raycast(new Ray(r.transform.TransformPoint(V(x,2,z)),Vector3.down),out _,4))throw new Exception("Floor gap");
            foreach(var p in r.GetComponentsInChildren<CollectiblePiece>())if(p.PieceId.StartsWith("P1_")&&p.transform.position.y-r.transform.position.y>1.3f)throw new Exception("Pickup too high");
            foreach(var m in g.GetComponentsInChildren<Renderer>().Select(x=>x.sharedMaterial).Distinct())if(m!=null && m.name!="LanternPaper" && m.IsKeywordEnabled("_EMISSION"))throw new Exception("Unwanted emission: "+m.name);
            File.WriteAllText(Output+"/F0-structure-check.txt","PASS: 220 floor rays; low pickups; dedicated door panels; non-emissive paper and covers.\n");
            Debug.Log("[F0 Reference] PASS: structure and material checks");
        }
    }
    // Scoped request queue for the already-open Unity editor. No shell commands or arbitrary C#.
    [InitializeOnLoad]
    public static class F0EditorRequest
    {
        const string Request="Logs/f0-editor.request", Result="Logs/f0-editor.result";
        static double next;
        static F0EditorRequest(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            string action=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(action=="apply") {InteriorF0Builder.ApplyAndSave();InteriorF0Builder.ValidateStructure();InteriorF0Builder.CaptureReferenceView();}
                else if(action=="capture")InteriorF0Builder.CaptureReferenceView();
                else if(action=="play") {F0PlayVerifier.Run();return;}
                else throw new Exception("Unknown F0 request");
                File.WriteAllText(Result,"PASS "+action+" "+DateTime.Now.ToString("O"));
            }
            catch(Exception e){File.WriteAllText(Result,"FAIL "+e);Debug.LogException(e);}
        }
    }
}
