using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class F0PaperIdentityVerifier
    {
        public const string Output="Docs/ArtReview/F0PaperIdentityV1/Implementation";
        public static void CheckStructure()
        {
            Directory.CreateDirectory(Output);
            var room=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            var art=room.transform.Find(InteriorF0Builder.RootName+"/"+InteriorF0Builder.IdentityRootName);
            var puzzle=room.GetComponentInChildren<FoldBridgePuzzle>();var report=new StringBuilder();
            void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);report.AppendLine("PASS: "+label);}
            Check(art&&art.childCount==13,"Room art includes all four walls, floor, ceiling and exit passage");
            Check(puzzle.hinge&&puzzle.pageCollider&&puzzle.source&&puzzle.shutter&&puzzle.entranceMirror&&puzzle.farMirror,"All moving puzzle references preserved");
            Check(room.GetComponentsInChildren<FoldBridgeControl>().Length==6,"All six original controls preserved");
            Check(puzzle.lightSurfaces.Length==7,"Original mirrors and light receivers preserved");
            Check(!art.GetComponentsInChildren<Renderer>().Any(r=>!r.sharedMaterial || !r.sharedMaterial.shader || r.sharedMaterial.shader.name=="Hidden/InternalErrorShader"),"New art has valid materials and shaders");
            Check(!art.GetComponentsInChildren<MeshFilter>().Any(f=>!f.sharedMesh||f.sharedMesh.vertexCount==0),"All authored meshes are serialized and nonempty");
            Physics.SyncTransforms();
            var shell=room.transform.Find(InteriorF0Builder.RootName);
            Check(!shell.Find("연속_충돌바닥").GetComponent<Renderer>(),"Retired static renderers cannot return after scene reload");
            foreach(string name in new[]{"WindowMulberry","LanternMulberry","HoneyOpticalLight"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0PaperIdentity/"+name+".mat");
                Check(material&&material.IsKeywordEnabled("_EMISSION"),"Persistent emission: "+name);
            }
            var floor=shell.Find("접는 다리와 빛/시작 책장 대지").GetComponent<Collider>();
            for(int x=-14;x<=14;x+=2)for(int z=-19;z<=-2;z+=2)
                Check(floor.Raycast(new Ray(room.transform.TransformPoint(new Vector3(x,4,z)),Vector3.down),out _,2),$"Entrance collision continuous at {x},{z}");
            foreach(string name in new[]{"서벽","동벽","남벽","제본천장","문벽_좌","문벽_우","문벽_상"})
                Check(shell.Find(name)&&shell.Find(name).GetComponent<Collider>().enabled,"Enclosed shell retained: "+name);
            var renderers=art.GetComponentsInChildren<Renderer>();
            long triangles=art.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.triangles.Length/3);
            report.AppendLine($"New static art: {renderers.Length} renderers, {triangles:N0} triangles; existing world and moving props excluded.");
            report.AppendLine("GPU frame time has not been benchmarked by this structure check.");
            File.WriteAllText(Output+"/structure-check.txt",report.ToString());
        }

        public static IEnumerator CaptureDirections(PlayerController player,PaperCamera camera,Room room)
        {
            if(!room.transform.Find(InteriorF0Builder.RootName+"/"+InteriorF0Builder.IdentityRootName))yield break;
            CheckStructure();
            Directory.CreateDirectory(Output);
            var original=player.transform.position;var rotation=player.transform.rotation;float yaw=camera.AimYaw;
            void Place(Vector3 p){player.Controller.enabled=false;player.transform.position=p;player.Controller.enabled=true;player.VerticalVelocity=0;player.HorizontalVelocity=Vector3.zero;Physics.SyncTransforms();}
            Place(room.transform.TransformPoint(new Vector3(0,3.06f,-6)));player.transform.rotation=Quaternion.identity;
            string[] names={"01-front","02-left","03-right","04-rear"};float[] angles={0,270,90,180};
            for(int i=0;i<angles.Length;i++)
            {
                camera.SetAim(angles[i],-2);yield return .8f;
                Capture(Camera.main,Output+"/"+names[i]+".png");
            }
            Place(room.transform.TransformPoint(new Vector3(-6.7f,3.06f,-10.5f)));camera.SetAim(318,11);yield return .8f;
            Capture(Camera.main,Output+"/05-workbench.png");
            Place(room.transform.TransformPoint(new Vector3(0,3.06f,-12)));camera.SetAim(0,36);yield return .8f;
            Capture(Camera.main,Output+"/06-floor-material.png");
            Place(original);player.transform.rotation=rotation;camera.SetAim(yaw,-12);yield return .5f;
        }

        public static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(1920,1080,24);var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            Texture2D image=null;
            try
            {
                camera.targetTexture=rt;camera.aspect=16f/9;camera.Render();RenderTexture.active=rt;
                image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;if(image)Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
