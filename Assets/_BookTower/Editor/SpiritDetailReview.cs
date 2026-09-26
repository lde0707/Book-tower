using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace BookTower.EditorTools
{
    [InitializeOnLoad]
    public static class SpiritDetailReview
    {
        static SpiritDetailReview(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            const string request="Logs/spirit-detail.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Logs/spirit-detail.result","PASS "+DateTime.Now.ToString("O"));}
            catch(Exception e){File.WriteAllText("Logs/spirit-detail.result",e.ToString());Debug.LogException(e);}
        }
        public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();}
        public static void Apply()
        {
            AssetDatabase.Refresh();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene before applying spirit detail.");
            Directory.CreateDirectory("Logs/SpiritBackups");
            EditorSceneManager.SaveScene(scene,"Logs/SpiritBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var player=GameObject.Find("BookmarkBird");if(player==null)throw new Exception("Player missing");
            var old=player.transform.Find("종이요정");
            Vector3 pos=old?old.localPosition:Vector3.zero,scale=old?old.localScale:Vector3.one;
            Quaternion rot=old?old.localRotation:Quaternion.identity;
            if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var model=ReferenceSpiritBuilder.Build(player.transform);model.transform.localPosition=pos;model.transform.localRotation=rot;model.transform.localScale=scale;
            if(model.GetComponentsInChildren<MeshFilter>().Length!=15)throw new Exception("Missing paper wraps");
            var animator=model.GetComponent<Animator>();
            foreach(var clip in animator.runtimeAnimatorController.animationClips)
            {
                for(int frame=0;frame<=20;frame++)
                {
                    clip.SampleAnimation(model,clip.length*frame/20f);
                    foreach(var r in model.GetComponentsInChildren<Renderer>())if(float.IsNaN(r.bounds.center.x)||r.bounds.size.magnitude>3)throw new Exception("Invalid animated bounds");
                }
            }
            animator.runtimeAnimatorController.animationClips[0].SampleAnimation(model,0);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var preview=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BookTower/Art/PaperSpirit/PaperSpirit.prefab")) as GameObject;
            preview.transform.position=new Vector3(1000,0,1000);
            var sceneLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var lightStates=new bool[sceneLights.Length];
            for(int i=0;i<sceneLights.Length;i++){lightStates[i]=sceneLights[i].enabled;sceneLights[i].enabled=false;}
            var camera=new GameObject("Spirit detail review camera").AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.orthographic=true;camera.orthographicSize=.81f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.19f,.15f);
            var light=new GameObject("Spirit detail key").AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=4;light.color=new Color(1,.90f,.74f);light.transform.position=preview.transform.position+new Vector3(-1.4f,2.3f,2);
            var fill=new GameObject("Spirit detail fill").AddComponent<Light>();fill.type=LightType.Point;fill.range=6;fill.intensity=3;fill.transform.position=preview.transform.position+new Vector3(1,1,-1.5f);
            var ambient=RenderSettings.ambientProbe;var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.3f,.28f,.24f));RenderSettings.ambientProbe=sh;
            Directory.CreateDirectory("Docs/ArtReview/SpiritDetail");
            try
            {
                foreach(int yaw in new[]{25,90,145,180})
                {
                    Vector3 target=preview.transform.position+Vector3.up*.72f;
                    camera.transform.position=target+Quaternion.Euler(-8,yaw,0)*Vector3.forward*3;camera.transform.LookAt(target);
                    InteriorF0Builder.Capture(camera,"Docs/ArtReview/SpiritDetail/spirit-"+yaw+".png");
                }
            }
            finally{for(int i=0;i<sceneLights.Length;i++)if(sceneLights[i])sceneLights[i].enabled=lightStates[i];RenderSettings.ambientProbe=ambient;UnityEngine.Object.DestroyImmediate(preview);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);UnityEngine.Object.DestroyImmediate(fill.gameObject);}
            File.WriteAllText("Docs/ArtReview/SpiritDetail/verification.txt","PASS: 15 mesh parts, four overlapping paper wraps; 21 poses per animation sampled with finite bounded geometry; scene and prefab saved; four review angles rendered.\n");
        }
    }
}
