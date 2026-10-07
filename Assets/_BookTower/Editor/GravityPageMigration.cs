using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.Interaction;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class GravityPageMigration
    {
        const string Folder="Assets/_BookTower/Generated/GravityPage";
        [Serializable] public class Set { public Layout[] layouts; }
        [Serializable] public class Cell { public int x,y; }
        [Serializable] public class Cargo { public int x,y,size,gx,gy; }
        [Serializable] public class Layout { public string room;public int family,width,height;public Cell[] walls;public Cargo[] cargo;public int[] route; }
        static Material paper,edge,ink,brass,coral,teal;
        static PhysicsMaterial frictionless;
        static readonly Dictionary<string,Mesh> meshes=new();
        static readonly StringBuilder report=new();
        public static Set Layouts=>JsonUtility.FromJson<Set>(File.ReadAllText("Assets/_BookTower/Design/GravityLayouts.json"));
        public static void Run()
        {
            try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void RebuildFromBackup()
        {
            try{Restore();Apply();EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Restore()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            File.Copy("Logs/GravityMigration/InGame-before.unity","Assets/Scenes/InGame.unity",true);
            AssetDatabase.ImportAsset("Assets/Scenes/InGame.unity",ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        }
        public static void RebuildAndReview()
        {
            try{Restore();Apply();GravityPageReview.Run();}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void ApplyAndReview()
        {
            try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();GravityPageReview.Run();}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void FinishArtAndBake()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();
                PaperAtmosphereBuilder.Apply();PaperAtmospherePolish.Apply();
                PaperIndirectBake.OutputOverride="Docs/ArtReview/GravityImplemented/Lighting";
                PaperIndirectBake.ForceCpu=true;PaperIndirectBake.Run();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [MenuItem("Book Tower/중력 페이지로 현재 방 개편")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before migrating");
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open the modern InGame scene");
            Directory.CreateDirectory("Logs/GravityMigration");
            string backup="Logs/GravityMigration/InGame-before.unity";
            if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();meshes.Clear();report.Clear();
            paper=Mat("Archive paper","WarmMulberry",new Color(.84f,.75f,.59f));
            edge=Mat("Layered edge","LayeredCottonEdges",new Color(.92f,.86f,.71f));
            ink=Mat("Indigo binding","InkBinding",new Color(.11f,.19f,.22f));
            brass=Mat("Direction brass","DullBindingBrass",new Color(.71f,.48f,.19f));
            coral=Mat("Terracotta cargo","RustDyedPaper",new Color(.68f,.24f,.13f));
            teal=Mat("Sage cargo","WarmMulberry",new Color(.19f,.43f,.40f));
            frictionless=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Folder+"/Cargo friction.physicMaterial");
            if(!frictionless){frictionless=new PhysicsMaterial("Paper transport rails");AssetDatabase.CreateAsset(frictionless,Folder+"/Cargo friction.physicMaterial");}
            frictionless.staticFriction=frictionless.dynamicFriction=frictionless.bounciness=0;
            frictionless.frictionCombine=PhysicsMaterialCombine.Minimum;frictionless.bounceCombine=PhysicsMaterialCombine.Minimum;
            var rooms=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var replacements=new Dictionary<PuzzleBase,GravityPagePuzzle>();
            var newByRoom=new Dictionary<string,GravityPagePuzzle>();
            foreach(var layout in Layouts.layouts)
            {
                var room=rooms.Single(r=>r.RoomId==layout.room);
                var old=room.GetComponentsInChildren<PuzzleBase>(true);
                var oldGravity=old.OfType<GravityPagePuzzle>().FirstOrDefault();
                Transform savedDoor=oldGravity?oldGravity.exitLeaf:null;
                var previous=room.transform.Find("GravityPage_RebuiltRoom");
                // Rebind all references before retiring old components, including repeat runs.
                if(previous)previous.gameObject.SetActive(false);
                var puzzle=Build(room,layout);
                if(savedDoor)puzzle.exitLeaf=savedDoor;
                foreach(var p in old)replacements[p]=puzzle;
                newByRoom[room.RoomId]=puzzle;
                RetireLegacy(room,puzzle);
                ClearTransportVolume(room,puzzle,layout);
                var so=new SerializedObject(room);so.FindProperty("goal").stringValue=puzzle.lesson;
                var b=so.FindProperty("boundsSize");var size=b.vector3Value;size.y=Mathf.Max(size.y,22);b.vector3Value=size;
                so.ApplyModifiedPropertiesWithoutUndo();
                RewriteNotes(room,puzzle,layout);
                report.AppendLine($"{room.RoomId}: family {layout.family+1}; {layout.cargo.Length} cargo; shortest grid route {layout.route.Length}; physical validation required");
            }
            foreach(var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!component||component is GravityPagePuzzle)continue;
                var so=new SerializedObject(component);var it=so.GetIterator();
                while(it.Next(true))
                {
                    if(it.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    if(it.objectReferenceValue is PuzzleBase old&&replacements.TryGetValue(old,out var replacement)&&
                       (component is RampGate||component is BindingDoor||component is SolveBridge||it.name=="theatrePuzzle"))
                        it.objectReferenceValue=replacement;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var story=Object.FindFirstObjectByType<BookTower.Story.OpeningStoryDirector>();
            if(story){story.primaryPuzzle=newByRoom["F0"];story.theatrePuzzle=newByRoom["1A"];EditorUtility.SetDirty(story);}
            foreach(var pair in replacements)
            {
                if(!pair.Key)continue;
                if(pair.Key is GravityPagePuzzle)Object.DestroyImmediate(pair.Key.gameObject);
                else Object.DestroyImmediate(pair.Key);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/GravityMigration/migration.txt",report.ToString());
            StructureCollisionFit.Apply(); // Preserve fitted collision and cleared spacing on regeneration.
        }
        static GravityPagePuzzle Build(Room room,Layout layout)
        {
            var root=Group(room.transform,"GravityPage_RebuiltRoom",Vector3.zero);
            float floor=room.RoomId=="F0"?3:0;
            float cell=room.RoomId=="F0"?1.2f:room.RoomId=="1A"?.8f:1f;
            float z=room.RoomId=="F0"?3:room.RoomId=="1A"?7:room.RoomId=="1B"?3:0;
            var chamber=Group(root,"기록 운반실",new Vector3(0,floor+1.75f,z));
            var puzzle=root.gameObject.AddComponent<GravityPagePuzzle>();
            var sp=new SerializedObject(puzzle);sp.FindProperty("puzzleId").stringValue=PuzzleId(room.RoomId);
            sp.FindProperty("activateRadius").floatValue=20;sp.FindProperty("targetSeconds").floatValue=120+layout.route.Length*30;sp.ApplyModifiedPropertiesWithoutUndo();
            puzzle.verificationRoute=layout.route;puzzle.tolerance=cell*.24f;
            string[] lessons={"천장을 길로 · 기록 상자를 같은 문양의 자리로 보내기","꺾인 운반로 · 막힌 길을 돌아 이야기책 돌려놓기","함께 움직이는 기록 · 두 상자를 각자의 자리로 보내기","크기가 다른 기록 · 좁은 길과 넓은 길 나누기","귀환 운반로 · 세 기록의 이동 순서를 함께 계획하기"};
            puzzle.lesson=lessons[layout.family];
            puzzle.hints=new[]{"화살표는 이 운반실의 아래쪽이다. 사람과 바깥 물건은 움직이지 않는다.","같은 문양의 빈 테두리가 도착 자리다. 벽 끝과 천장의 빈 길을 함께 보자.",layout.family>=2?"한 상자를 옮기면 나머지도 움직인다. 먼저 도착시키는 것이 늘 정답은 아니다.":"막힌 벽을 넘으려면 먼저 다른 면을 바닥으로 써 보자."};
            float width=layout.width*cell,height=layout.height*cell;
            if(room.RoomId=="F0"){puzzle.completedPage=chamber;puzzle.completedPageHinge=chamber.localPosition+Vector3.left*width*.5f;}
            // The chamber divides the room into a front observation bay and a return loop.
            Box(chamber,"두꺼운 제본 뒷벽",new(0,height*.5f-cell*.5f,.8f),new(width+.4f,height+.4f,.28f),ink,true);
            Vector3 At(float x,float y)=>new((x-(layout.width-1)*.5f)*cell,y*cell,0);
            foreach(var c in layout.walls)
            {
                var wall=Box(chamber,"고정 선반",At(c.x,c.y),new(cell,cell,1.5f),paper,true);
                // Thin exposed laminations make thickness legible without changing collision.
                for(int k=0;k<3;k++)Box(wall,"겹친 종이 단면",new(0,(k-1)*cell*.22f,-.765f),new(cell*.92f,.018f,.025f),edge,false);
            }
            puzzle.cargo=new Rigidbody[layout.cargo.Length];puzzle.destinations=new Transform[layout.cargo.Length];
            puzzle.railSpacing=cell;puzzle.railOrigins=new Vector3[layout.cargo.Length];
            for(int i=0;i<layout.cargo.Length;i++)
            {
                var c=layout.cargo[i];float side=(c.size-.12f)*cell;var tint=i==0?coral:i==1?teal:brass;
                var cargo=Group(chamber,"기록 상자 "+(i+1),At(c.x+(c.size-1)*.5f,c.y+(c.size-1)*.5f));
                Box(cargo,"천제본 표지",Vector3.zero,new(side,side,.95f),tint,false);
                Box(cargo,"겹친 기록 속지",new(0,0,-.49f),new(side*.84f,side*.82f,.05f),edge,false);
                for(int k=0;k<4;k++)Box(cargo,"제본 실땀",new(side*.34f,(k-1.5f)*side*.16f,-.53f),new(.06f,side*.06f,.02f),ink,false);
                Symbol(cargo,i,new(0,0,-.56f),side*.26f,tint);
                float frame=(c.size-.006f)*cell;
                foreach(int sign in new[]{-1,1})
                {
                    Box(cargo,"운반 가이드 테",new(sign*frame*.49f,0,-.45f),new(.025f,frame,.04f),brass,false);
                    Box(cargo,"운반 가이드 테",new(0,sign*frame*.49f,-.45f),new(frame,.025f,.04f),brass,false);
                }
                var collider=cargo.gameObject.AddComponent<BoxCollider>();collider.size=new(frame,frame,.95f);collider.sharedMaterial=frictionless;collider.contactOffset=.0005f;
                var rb=cargo.gameObject.AddComponent<Rigidbody>();rb.useGravity=false;rb.mass=c.size*c.size;
                rb.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
                rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
                rb.linearDamping=0;rb.angularDamping=0;rb.solverIterations=12;rb.solverVelocityIterations=6;
                puzzle.cargo[i]=rb;
                puzzle.railOrigins[i]=cargo.localPosition;
                var destination=Group(chamber,"도착 자리 "+(i+1),At(c.gx+(c.size-1)*.5f,c.gy+(c.size-1)*.5f));
                puzzle.destinations[i]=destination;
                // Matching outline lives behind cargo; it never physically traps it.
                float extent=c.size*cell*.46f;
                foreach(int sign in new[]{-1,1})
                {
                    Box(destination,"문양 테두리",new(sign*extent,0,.62f),new(.085f,extent*2,.04f),edge,false);
                    Box(destination,"문양 테두리",new(0,sign*extent,.62f),new(extent*2,.085f,.04f),edge,false);
                }
                Symbol(destination,i,new(0,0,.60f),side*.32f,edge);
            }
            var plinth=Box(root,"운반실 적층 기단",new(0,floor+.62f,z+.3f),new(width+1,1.24f,2),ink,true);
            if(room.RoomId=="F0")plinth.SetParent(chamber,true);
            var console=Group(root,"중력 페이지",new(0,floor,z-6.3f));
            Box(console,"접힌 책 기둥",new(0,.4f,0),new(2.8f,.8f,.65f),ink,true);
            Box(console,"펼친 조작 페이지",new(0,1.05f,-.12f),new(4.8f,1.4f,.18f),edge,true);
            var arrow=Group(console,"현재 아래 방향",new(0,1.5f,-.25f));Arrow(arrow,.29f,brass);puzzle.arrow=arrow;
            string[] labels={"아래로 중력","왼쪽으로 중력","위로 중력","오른쪽으로 중력","한 번 되돌리기","처음 배치로"};
            for(int i=0;i<6;i++)
            {
                float x=i<4?(i-1.5f)*.78f:i==4?-1.5f:1.5f;
                var button=Box(console,labels[i],new(x,i<4?.85f:1.5f,-.30f),new(.65f,.48f,.22f),i<4?paper:ink,true);
                var control=button.gameObject.AddComponent<GravityPageControl>();control.puzzle=puzzle;control.command=i;
                var it=button.GetComponent<Interactable>();it.SetPresentation("중력 페이지",labels[i]);var si=new SerializedObject(it);si.FindProperty("animatePosition").boolValue=false;si.FindProperty("range").floatValue=4.5f;si.ApplyModifiedPropertiesWithoutUndo();
                var mark=Group(button,"손으로 읽는 표식",new(0,0,-.14f));
                if(i<4){Arrow(mark,.18f,ink);mark.localRotation=Quaternion.Euler(0,0,-i*90f);}
                else {Symbol(mark,i-4,Vector3.zero,.12f,edge);}
            }
            Box(console,"기록책 제본대",new(3,.48f,0),new(1.1f,.96f,.8f),ink,true);
            var book=Box(console,"운반 기록책",new(3,1.1f,0),new(1.1f,.22f,.8f),coral,true);
            var note=book.gameObject.AddComponent<ReadableNote>();note.id="Gravity."+room.RoomId+".Guide";
            book.GetComponent<Interactable>().SetPresentation("제본사의 운반 기록","읽기");
            // A real, accessible route around the machinery replaces the old central obstacle.
            Box(root,"연속 관찰 회랑",new(0,floor-.16f,z-4),new(width+5,.32f,8),paper,true);
            foreach(int side in new[]{-1,1})
            {
                Box(root,"돌아오는 측면 통로",new(side*(width*.5f+1.4f),floor-.16f,z+1),new(2.8f,.32f,8),paper,true);
                for(int j=0;j<4;j++)
                {
                    var leaf=Box(root,"겹친 관찰 골방",new(side*(width*.5f+2.8f),floor+1.3f,z-3+j*1.1f),new(.18f,2.6f,1.15f),j%2==0?ink:paper,true);
                    leaf.localRotation=Quaternion.Euler(0,side*(j-1.5f)*9,0);
                }
                var arc=new List<Vector3>();
                for(int k=0;k<=24;k++){float a=k*Mathf.PI/24;arc.Add(new Vector3(side*(width*.5f+1.2f)+Mathf.Cos(a)*1.2f,floor+2.5f+Mathf.Sin(a)*1.8f,z-2));}
                MeshObject(root,"접힌 측랑 아치",F0Mesh.Tube(arc,.1f),edge);
            }
            if(room.RoomId=="F0")Box(root,"이전 골을 잇는 고정 제본 바닥",new(0,2.85f,2.5f),new(15.2f,.3f,17.5f),paper,true);
            var light=Group(chamber,"운반실 작업등",new(0,height*.75f,-4)).gameObject.AddComponent<Light>();
            light.transform.SetParent(root,true); // Stationary fixture: solved F0 page folds away below it.
            light.type=LightType.Point;light.color=new Color(1,.83f,.61f);light.intensity=4;light.range=18;light.shadows=LightShadows.Soft;
            return puzzle;
        }
        static void RetireLegacy(Room room,GravityPagePuzzle puzzle)
        {
            foreach(var bridge in room.GetComponentsInChildren<FoldBridgePuzzle>(true))
            {
                foreach(var t in new[]{bridge.hinge,bridge.source,bridge.receiver,bridge.sealVisual,bridge.entranceMirror,bridge.farMirror})if(t)t.gameObject.SetActive(false);
                if(bridge.lightSurfaces!=null)foreach(var surface in bridge.lightSurfaces)if(surface)surface.gameObject.SetActive(false);
            }
            var reading=room.GetComponentInChildren<ReadingRoomPuzzle>(true);
            if(reading)
            {
                puzzle.exitLeaf=reading.door;
                foreach(var t in new[]{reading.lantern,reading.chargingSocket,reading.recordSocket,reading.exitSocket,reading.windowChargeSocket,reading.crank,reading.leftRing,reading.rightRing,reading.chargeRibbon})if(t)t.gameObject.SetActive(false);
                Object.DestroyImmediate(reading);
            }
            foreach(var old in room.GetComponentsInChildren<StorybookLightPuzzle>(true))
            {
                if(old.stage)old.stage.gameObject.SetActive(false);
                foreach(var t in new[]{old.bodyPickup,old.earPickup,old.bodyShadow,old.earShadow})if(t)t.gameObject.SetActive(false);
                Object.DestroyImmediate(old);
            }
            foreach(var it in room.GetComponentsInChildren<Interactable>(true))
            {
                if(it.GetComponentInParent<GravityPagePuzzle>()||it.GetComponent<ReadableNote>())continue;
                if(it.GetComponentInParent<GravityPageControl>())continue;
                it.gameObject.SetActive(false);
            }
            foreach(var p in room.GetComponentsInChildren<PuzzleBase>(true))if(p!=puzzle)p.enabled=false;
            foreach(var fold in room.GetComponentsInChildren<BookTower.Fold.FoldPanel>(true))fold.gameObject.SetActive(false);
            foreach(var rootName in new[]{"F0_Gameplay","1A_Gameplay","종이_시각단서"})
            {var t=room.transform.Find(rootName);if(t)t.gameObject.SetActive(false);}
            foreach(var t in room.GetComponentsInChildren<Transform>(true))if(t.name=="제본실_생활속빛장치")t.gameObject.SetActive(false);
        }
        static void ClearTransportVolume(Room room,GravityPagePuzzle puzzle,Layout layout)
        {
            var chamber=puzzle.transform.Find("기록 운반실");float cell=puzzle.railSpacing;
            var volume=new Bounds(chamber.position+new Vector3(0,(layout.height-1)*cell*.5f,0),new Vector3(layout.width*cell,layout.height*cell,2));
            foreach(var collider in room.GetComponentsInChildren<Collider>())
            {
                if(collider.transform.IsChildOf(puzzle.transform)||!collider.enabled||collider.isTrigger||!collider.bounds.Intersects(volume))continue;
                var bounds=collider.bounds;
                if(bounds.max.y<chamber.position.y+.45f*cell)continue;
                string name=collider.name;
                bool ceiling=name.Contains("천장")||name.ToLowerInvariant().Contains("ceiling")||name.ToLowerInvariant().Contains("roof");
                if(ceiling){collider.transform.position+=Vector3.up*Mathf.Max(0,volume.max.y+1-bounds.min.y);report.AppendLine(room.RoomId+" raised old ceiling: "+name);}
                else {collider.gameObject.SetActive(false);report.AppendLine(room.RoomId+" retired obstruction in transport volume: "+name);}
            }
        }
        static void RewriteNotes(Room room,GravityPagePuzzle puzzle,Layout layout)
        {
            foreach(var note in room.GetComponentsInChildren<ReadableNote>(true))
            {
                note.title=note.id=="F0.Book.Work"?"제본사의 운반 기록":note.id=="1B.Next"?"반대편의 기록":note.id=="F0.Book.Shadow"?"작은 몸, 큰 자리":"돌아올 자리를 남기는 법";
                note.pages=new[]{puzzle.lesson,
                    "화살표가 가리키는 쪽이 운반실의 아래가 된다. 표시된 기록 상자들은 함께 떨어지고, 벽과 선반에 닿으면 멈춘다. 사람은 평소처럼 걷는다.",
                    "상자와 같은 문양의 빈 자리가 목적지다. 도착한 상자도 방향을 바꾸면 다시 움직인다. 모든 기록이 동시에 제자리에 있어야 문이 열린다.",
                    layout.family>=3?"큰 기록은 좁은 틈을 지나갈 수 없다. 무거워서 더 빨리 떨어지는 것은 아니다. 크기와 통로 폭을 비교하자.":"길이 막히면 다른 면을 바닥으로 써 보자. 움직임이 멈추면 다음 방향을 고를 수 있다.",
                    "잘못 옮겨도 괜찮다. 중력 페이지의 되돌리기로 한 번 전, 처음 배치로 모든 기록을 돌릴 수 있다."};
                if(note.id=="1B.Next")note.pages=new[]{"같은 도장이라도 반대편에서 보면 방향이 바뀐다.","글보다 먼저, 종이의 찢긴 모서리와 제본 구멍을 맞춰 볼 것.","이 문은 밖에서 잠긴 것이 아니었다. 안쪽에서 기록을 붙잡아 두려 한 흔적이다."};
                EditorUtility.SetDirty(note);
            }
        }
        static string PuzzleId(string id)=>id switch{"F0"=>"P1","1A"=>"P2","1B"=>"P3","1C"=>"P4","1D"=>"P5","1E"=>"P6","2B"=>"P8","2C"=>"P9","2E"=>"P11","3A"=>"P13","3B"=>"P14",_=>"Gravity."+id};
        static Material Mat(string name,string source,Color color)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/F0PaperIdentity/"+source+".mat");if(!original)throw new Exception(source);mat=new Material(original);AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);EditorUtility.SetDirty(mat);return mat;
        }
        static Transform Group(Transform parent,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
        static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material mat,bool solid)
        {
            var t=Group(parent,name,at);string key=$"{size.x:F3}_{size.y:F3}_{size.z:F3}";
            if(!meshes.TryGetValue(key,out var mesh))
            {
                string path=Folder+"/Box_"+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=F0Mesh.RoundedBox(size,.045f);AssetDatabase.CreateAsset(mesh,path);}meshes[key]=mesh;
            }
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            if(solid){var c=t.gameObject.AddComponent<BoxCollider>();c.size=size;c.sharedMaterial=frictionless;c.contactOffset=.0005f;}
            return t;
        }
        static void MeshObject(Transform parent,string name,Mesh mesh,Material mat)
        {
            string path=Folder+"/Arc_"+meshes.Count+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            meshes[path]=mesh;var t=Group(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static void Arrow(Transform p,float size,Material mat)
        {
            Box(p,"화살표 줄기",new(0,size*.15f,0),new(size*.18f,size*1.3f,.035f),mat,false);
            foreach(int s in new[]{-1,1}){var tip=Box(p,"화살표 끝",new(s*size*.20f,-size*.30f,0),new(size*.14f,size*.66f,.035f),mat,false);tip.localRotation=Quaternion.Euler(0,0,-s*42);}
        }
        static void Symbol(Transform p,int index,Vector3 at,float size,Material mat)
        {
            var t=Group(p,"색에 의존하지 않는 기록 문양",at);
            if(index==0){var diamond=Box(t,"마름모",Vector3.zero,new(size,size,.035f),mat,false);diamond.localRotation=Quaternion.Euler(0,0,45);}
            else for(int k=0;k<index+1;k++)Box(t,"기록 줄",new((k-index*.5f)*size*.55f,0,0),new(size*.24f,size*1.3f,.035f),mat,false);
        }
    }
}
