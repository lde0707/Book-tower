using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.Interaction;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        const string EmbeddedFolder="Assets/_BookTower/Generated/F0EmbeddedOptics";
        const string EmbeddedRoot="제본실_생활속빛장치";
        // Keep the existing optical planes and hinges: visible foil and collision still agree.
        [MenuItem("Book Tower/F0 빛 장치를 제본실 소품에 통합")]
        public static void ApplyEmbeddedOptics()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!="Assets/Scenes/InGame.unity")throw new InvalidOperationException("Open InGame in Edit mode");
            Directory.CreateDirectory("Logs/EmbeddedOpticsBackups");
            EditorSceneManager.SaveScene(scene,"Logs/EmbeddedOpticsBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            var puzzle=room.GetComponentInChildren<FoldBridgePuzzle>();
            Material M(string name)=>AssetDatabase.LoadAssetAtPath<Material>(IdentityFolder+"/"+name+".mat")??throw new Exception(name);
            paper=M("WarmMulberry");ivory=M("IvoryMulberry");edge=M("LayeredCottonEdges");oxblood=M("RustDyedPaper");
            wood=M("InkBinding");rope=M("CottonThread");brass=M("DullBindingBrass");ink=M("TerracottaPrint");identityFoil=M("SageSilverFoil");glow=M("LanternMulberry");
            meshes.Clear();EnsureFolder(EmbeddedFolder);
            string foilPath=EmbeddedFolder+"/WarmUsedFoil.mat";var warmFoil=AssetDatabase.LoadAssetAtPath<Material>(foilPath);
            if(!warmFoil){warmFoil=new Material(identityFoil);AssetDatabase.CreateAsset(warmFoil,foilPath);}
            warmFoil.SetColor("_BaseColor",new Color(.72f,.67f,.53f));warmFoil.SetFloat("_Smoothness",.56f);EditorUtility.SetDirty(warmFoil);identityFoil=warmFoil;
            var parent=puzzle.entranceMirror.parent;var old=parent.Find(EmbeddedRoot);if(old)Object.DestroyImmediate(old.gameObject);
            var environment=Group(parent,EmbeddedRoot,Vector3.zero);
            foreach(var surface in puzzle.lightSurfaces.Where(s=>s.kind==F0LightSurface.Kind.Mirror))
            {
                // Legacy frames are decoration only; retain the reflecting collider/component.
                foreach(Transform child in surface.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var renderer=surface.GetComponent<Renderer>();if(renderer)Object.DestroyImmediate(renderer);
                var filter=surface.GetComponent<MeshFilter>();if(filter)Object.DestroyImmediate(filter);
                var art=Group(surface.transform,"생활도구_반사면",Vector3.zero);
                bool fixedHood=surface.transform!=puzzle.entranceMirror&&surface.transform!=puzzle.farMirror;
                if(fixedHood)EmbeddedHood(art);else EmbeddedFolio(art);
                CombineDecorations(art);
            }
            // A suspended drying folio can slide/turn above the gap without a fake floor pedestal.
            Box(environment,"제본책_천장레일",V(.8f,10.6f,3),V(5.1f,.19f,.22f),wood);
            foreach(float x in new[]{-1.6f,3.2f})Rope(environment,"천장레일_제본실",V(x,10.6f,3),V(x,15.3f,3),.032f);
            var suspension=environment.gameObject.AddComponent<F0BookSuspension>();suspension.book=puzzle.entranceMirror;suspension.roomSpace=parent;
            LineRenderer Thread(string name,float side)
            {
                var line=Group(environment,name,Vector3.zero).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=rope;line.widthMultiplier=.032f;line.positionCount=2;line.useWorldSpace=true;
                line.SetPosition(0,parent.TransformPoint(V(2.1f+side,10.6f,3)));line.SetPosition(1,puzzle.entranceMirror.TransformPoint(V(side,1.06f,.14f)));return line;
            }
            suspension.left=Thread("제본책_왼쪽매듭",-.75f);suspension.right=Thread("제본책_오른쪽매듭",.75f);
            // Fixed reflector is the lining of a lamp shade, hung from the same architecture.
            Rope(environment,"반사갓_매다는실",V(6,7.5f,3),V(6,15.3f,3),.033f);
            Box(environment,"반사갓_지지등목",V(6,7.1f,3),V(.13f,.65f,.13f),brass);
            // Far folio belongs to a low binding cabinet. Keep the approach and beam lane clear.
            EmbeddedCabinet(environment,V(6,3.02f,10.05f),2.6f,1.3f,1.0f);
            for(int i=0;i<5;i++)Box(environment,"서가_적층책",V(5.8f,4.40f+i*.18f,10.1f),V(1.7f,.15f,.82f),i%2==0?ivory:oxblood);
            Box(environment,"책받침_회전축",V(6,5.03f,9.14f),V(.13f,1.12f,.13f),brass);
            // Replace repeated control pedestals with distinct, physically connected book tools.
            foreach(var control in room.GetComponentsInChildren<FoldBridgeControl>().Where(c=>c.opticalControl>0))
            {
                foreach(var r in control.GetComponentsInChildren<Renderer>(true)){var f=r.GetComponent<MeshFilter>();Object.DestroyImmediate(r);if(f)Object.DestroyImmediate(f);}
                var prior=control.transform.Find("生活道具");if(prior)Object.DestroyImmediate(prior.gameObject);
                var art=Group(control.transform,"生活道具",Vector3.zero);
                var ribbon=MeshObject(art,"손때묻은_책갈피",IdentitySheet(.39f,.85f,400+control.opticalControl,.018f),oxblood,V(0,-.40f,-.34f));
                Sprig(art,V(0,-.17f,-.38f),.36f,ivory);
                Tube(art,"책갈피_잡는실",new[]{V(-.17f,.30f,-.32f),V(-.18f,.47f,-.25f),V(.18f,.47f,-.25f),V(.17f,.30f,-.32f)},.028f,rope);
                if(control.opticalControl==1)
                {
                    Box(art,"창빛_덮개접는책",V(0,-.14f,.19f),V(1.35f,1.15f,.22f),oxblood);
                    Box(art,"장지_창빛견본",V(0,-.12f,.055f),V(1.15f,.95f,.04f),ivory);
                    Sprig(art,V(.30f,-.46f,.017f),.58f,ink);
                }
                else
                {
                    // A horizontal spool explains why pulling the ribbon rotates a book upstairs.
                    for(int i=0;i<9;i++)
                    {
                        var loop=new System.Collections.Generic.List<Vector3>();
                        for(int k=0;k<=32;k++){float a=k*Mathf.PI/16;loop.Add(V(-.30f+i*.075f,Mathf.Sin(a)*.25f,Mathf.Cos(a)*.25f+.23f));}
                        Tube(art,"책받침_실감개",loop,.021f,rope);
                    }
                    foreach(int side in new[]{-1,1})Box(art,"실감개_책표지날개",V(side*.40f,0,.23f),V(.07f,.66f,.66f),oxblood);
                }
                CombineDecorations(art);
                var it=new SerializedObject(control.GetComponent<Interactable>());
                it.FindProperty("label").stringValue=control.opticalControl==1?"등불 덮개의 책갈피":control.opticalControl==2?"매달린 제본책의 실감개":"서가 책받침의 실감개";
                it.FindProperty("verb").stringValue=control.opticalControl==1?"덮개 책갈피 당기기":"제본실 감기";it.ApplyModifiedPropertiesWithoutUndo();
                if(control.opticalControl<3)EmbeddedCabinet(environment,control.transform.localPosition+V(0,-.98f,.58f),1.65f,.55f,.72f);
            }
            // The book describes the objects and a consistent material rule, not a switch sequence.
            foreach(var note in room.GetComponentsInChildren<ReadableNote>())if(note.id==BookTower.Story.OpeningStoryDirector.WorkNote)
            {
                note.pages=new[]{"제본사의 빛 작업 기록\n은박 속지를 대면 빛이 방향을 바꾼다. 종이 위에서는 멈추지만, 은박에서는 다시 길을 찾는다.","말리는 책, 등불의 안쪽, 서가에 펼친 책.\n작업실의 같은 은박을 썼다. 붉은 책갈피에 이어진 실을 감으면 책의 면이 돌아간다.","빛이 멎은 자리를 따라가 볼 것.\n길이 이어지면 문 옆 잎이 펼쳐진다. 마지막 제본 걸쇠는 손으로 풀어야 한다."};EditorUtility.SetDirty(note);
            }
            // Save only new meshes in a dedicated namespace; no existing room art assets are overwritten.
            int serial=0;
            foreach(var f in room.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&meshes.Contains(f.sharedMesh)).ToArray())
            {
                string path=EmbeddedFolder+"/Mesh_"+(serial++).ToString("000")+".asset";var m=f.sharedMesh;var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved){saved.Clear();saved.indexFormat=m.indexFormat;saved.vertices=m.vertices;saved.normals=m.normals;saved.uv=m.uv;saved.tangents=m.tangents;saved.triangles=m.triangles;saved.RecalculateBounds();saved.UploadMeshData(false);f.sharedMesh=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(m,path);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static void EmbeddedFolio(Transform p)
        {
            Box(p,"은박_속지",Vector3.zero,V(1.65f,1.65f,.06f),identityFoil);
            Box(p,"적갈색_제본표지",V(0,0,.105f),V(1.94f,2.12f,.13f),oxblood);
            foreach(float y in new[]{-.97f,.97f})Box(p,"책등_낡은금박띠",V(0,y,.19f),V(1.88f,.045f,.025f),brass);
            // Curled uncoated pages expose the silver lining and keep the central optical aperture clear.
            foreach(int side in new[]{-1,1})
            {
                Box(p,"종이_두꺼운단면",V(side*.89f,0,-.01f),V(.10f,1.93f,.15f),edge);
                var leaf=MeshObject(p,"접혀넘어간_장지",IdentitySheet(.54f,1.92f,70+side,.055f),ivory,V(side*.96f,-.96f,-.07f));leaf.localRotation=Quaternion.Euler(0,side*32,0);
                for(int i=0;i<6;i++)Tube(p,"책등_바느질",new[]{V(side*.94f,-.76f+i*.30f,-.11f),V(side*1.01f,-.72f+i*.30f,.03f),V(side*.94f,-.68f+i*.30f,.20f)},.014f,rope);
            }
            Box(p,"은박_아래쪽제본띠",V(0,-.88f,-.06f),V(1.8f,.10f,.075f),paper);
            Sprig(p,V(.57f,-.78f,-.098f),.32f,brass);
            var facing=Group(p,"펼쳐진_왼쪽작업기록",V(-.98f,0,.10f));facing.localRotation=Quaternion.Euler(0,-22,0);
            Box(facing,"왼쪽책표지",V(-.63f,0,.04f),V(1.3f,2.12f,.11f),oxblood);
            MeshObject(facing,"휘어진_기록장지",IdentitySheet(1.17f,1.91f,313,.04f),ivory,V(-.64f,-.955f,-.055f));
            Sprig(facing,V(-.96f,-.55f,-.14f),1.1f,ink);
            for(int i=0;i<7;i++)Box(facing,"기록_필적",V(-.40f,.58f-i*.16f,-.13f),V(.37f-(i%3)*.055f,.017f,.013f),ink);
        }
        static void EmbeddedHood(Transform p)
        {
            // Pleated shade, not another framed rectangle. The central lining stays planar.
            var vertices=new System.Collections.Generic.List<Vector3>{Vector3.zero};var uv=new System.Collections.Generic.List<Vector2>{Vector2.one*.5f};var triangles=new System.Collections.Generic.List<int>();
            const int n=48;
            for(int ring=0;ring<2;ring++)for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,r=ring==0?.88f:1.27f+(i%2)*.05f,z=ring==0?0:.38f+(i%2)*.09f;
                vertices.Add(V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z));uv.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(r/1.32f)*.5f+Vector2.one*.5f);
            }
            for(int i=0;i<n;i++)
            {
                int a=1+i,b=1+(i+1)%n,c=a+n,d=b+n;
                triangles.AddRange(new[]{0,b,a,a,b,c,b,d,c});
            }
            var mesh=new Mesh{name="접은_꽃등반사갓"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.normals=vertices.Select(v=>new Vector3(v.x*.3f,v.y*.3f,-1).normalized).ToArray();mesh.RecalculateTangents();
            MeshObject(p,"등불_접힌은박안감",mesh,identityFoil,Vector3.zero);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Tube(p,"반사갓_제본골",new[]{V(Mathf.Cos(a)*.90f,Mathf.Sin(a)*.90f,.018f),V(Mathf.Cos(a)*1.3f,Mathf.Sin(a)*1.3f,.48f)},.023f,oxblood);
            }
            Box(p,"등불갓_매다는고정핀",V(0,1.36f,.39f),V(.13f,.40f,.13f),brass);
        }
        static void EmbeddedCabinet(Transform p,Vector3 at,float w,float h,float d)
        {
            var cabinet=Group(p,"제본_작업가구",at);
            Box(cabinet,"적층표지_상판",V(0,h,0),V(w,.16f,d),oxblood,true);
            Box(cabinet,"서가_하단",V(0,.08f,0),V(w,.16f,d),wood,true);
            foreach(int side in new[]{-1,1})Box(cabinet,"종이목재_측판",V(side*(w/2-.10f),h/2,0),V(.15f,h,d),wood,true);
            for(int i=0;i<4;i++)Box(cabinet,"사용중인_책묶음",V(-w*.20f,.23f+i*.065f,0),V(w*.44f,.05f,d*.84f),ivory);
        }
        public static void EmbeddedOpticsBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ApplyEmbeddedOptics();F0PlayVerifier.Run();}
        public static void CaptureEmbeddedOpticsBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            ApplyEmbeddedOptics();
            room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            foreach(var r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                foreach(var renderer in r.GetComponentsInChildren<Renderer>(true))renderer.enabled=r==room;
                foreach(var light in r.GetComponentsInChildren<Light>(true))light.enabled=r==room;
            }
            var puzzle=room.GetComponentInChildren<FoldBridgePuzzle>();puzzle.hinge.localRotation=Quaternion.identity;
            var go=new GameObject("Embedded optics review");var cam=go.AddComponent<Camera>();cam.fieldOfView=58;cam.nearClipPlane=.05f;cam.farClipPlane=100;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.33f,.25f);
            Directory.CreateDirectory("Docs/ArtReview/F0EmbeddedOptics");
            void Shot(string name,Vector3 from,Vector3 target){cam.transform.position=room.transform.TransformPoint(from);cam.transform.LookAt(room.transform.TransformPoint(target));Capture(cam,"Docs/ArtReview/F0EmbeddedOptics/"+name+".png");}
            Shot("overview",V(9,7,-6),V(3,6,6));
            Shot("hanging-folio",V(2.1f,6,-1),V(2.1f,6,3));
            Shot("lantern-lining",V(3,6,5),V(6,6,3));
            Shot("shelf-folio",V(3,6,6),V(6,5.7f,9));
            EditorApplication.Exit(0);
        }
    }
}
