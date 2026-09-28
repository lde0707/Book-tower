using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BookTower.Rooms;
using BookTower.Puzzles;
using BookTower.Interaction;
using BookTower.UI;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class StorybookRoomBuilder
    {
        public const string WorkshopDir="Assets/_BookTower/Generated/StorybookWorkshop";
        public const string WorkshopOut="Docs/ArtReview/StorybookWorkshop";
        static readonly List<Transform> modules=new();
        static Material edge,ink;
        public static void WorkshopBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            ApplyWorkshop();
            WorkshopVerifier.Run();
        }
        public static void WorkshopStoryBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ApplyWorkshop();OpeningStoryVerifier.RunBatch();
        }
        [MenuItem("Book Tower/1A 그림자 공방 적용")]
        public static void ApplyWorkshop()
        {
            if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
            Directory.CreateDirectory(WorkshopDir);Directory.CreateDirectory(WorkshopOut);AssetDatabase.Refresh();
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene");
            Directory.CreateDirectory("Logs/WorkshopBackups");
            EditorSceneManager.SaveScene(scene,"Logs/WorkshopBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="1A");
            var puzzle=room.GetComponentsInChildren<FoldCombinationPuzzle>(true).First(p=>p.PuzzleId=="P2");
            foreach(var note in room.GetComponentsInChildren<ReadableNote>(true))if(note.id=="1A.Book.Lamp")Object.DestroyImmediate(note.gameObject);
            // Preserve the P2 object referenced by doors and the narrative director.
            foreach(var panel in room.GetComponentsInChildren<BookTower.Fold.FoldPanel>(true))panel.gameObject.SetActive(false);
            var old=room.transform.Find("V4_팝업북회랑");if(old)Object.DestroyImmediate(old.gameObject);
            var prior=puzzle.GetComponent<StorybookLightPuzzle>();if(prior)Object.DestroyImmediate(prior);
            meshes.Clear();modules.Clear();seed=204;
            butter=WorkshopMat("01_Butter_Mulberry",new Color(.95f,.85f,.67f));
            cream=WorkshopMat("02_Ivory_LayeredPaper",new Color(.98f,.87f,.67f));
            coral=WorkshopMat("03_Coral_CrayonPaper",new Color(.66f,.25f,.18f));
            teal=WorkshopMat("04_Teal_CrayonPaper",new Color(.12f,.37f,.34f));
            ochre=WorkshopMat("05_Honey_Cotton",new Color(.60f,.37f,.14f));
            red=WorkshopMat("06_BookBinding",new Color(.31f,.10f,.07f));
            shadow=WorkshopMat("07_ShadowInk",new Color(.17f,.12f,.08f));
            edge=WorkshopMat("08_DeckledEdges",new Color(.69f,.54f,.33f));
            ink=WorkshopMat("09_FadedOutline",new Color(.48f,.37f,.23f));
            glow=WorkshopMat("10_StarLantern",new Color(1,.79f,.42f));
            glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.58f,.21f)*1.2f);
            root=Group(room.transform,"V4_팝업북회랑",Vector3.zero);
            Shell();Dress();
            // Different materials, grain, raised seams and inset paintings cover the shell.
            DecorateWorkshop();
            var workshop=puzzle.gameObject.AddComponent<StorybookLightPuzzle>();puzzle.lightWorkshop=workshop;
            BuildShadowStage(workshop);
            var desk=WorkDesk(V(-22,0,0),0);workshop.drawer=desk.Find("뽑히는 서랍");
            var ears=PaperPuppet(workshop.drawer,"서랍 귀 오림",true,cream);ears.localPosition=V(0,.65f,-1.4f);ears.localRotation=Quaternion.Euler(65,0,0);ears.localScale=Vector3.one*1.2f;
            workshop.earPickup=ears;Control(desk,"반쯤 열린 서랍 · 귀 오림",V(0,1.25f,-1.55f),V(1.3f,.7f,.5f),coral,workshop,StorybookLightControl.Action.FindEars,"서랍 열어 꺼내기");
            var shelf=BookCabinet(V(25,0,1),-18);var body=PaperPuppet(shelf,"책 뒤 몸통 오림",false,cream);body.localPosition=V(-.5f,1.1f,-1.5f);body.localScale=Vector3.one*.85f;workshop.bodyPickup=body;
            Control(shelf,"책갈피 달린 몸통 오림",V(-.5f,1.3f,-1.3f),V(1.6f,1.6f,.2f),teal,workshop,StorybookLightControl.Action.FindBody,"오림 꺼내기");
            var entryDesk=WorkDesk(V(-20,0,-22),-8);ClueBook(entryDesk,V(0,3.1f,0),"1A.Book.Scale","작은 몸, 큰 그림자",new[]{"등 가까이 둔 작은 종이는 벽에서 크게 자란다. 벽 가까이 놓으면 작아진다.","청록 책장에 몸통을, 코럴 서랍에 귀를 보관했다. 두 꽂이는 따로 움직일 수 있다.","귀가 반대로 기울었다면 고리를 돌려 앞뒤를 바꾼다. 꽂이 손잡이: F / Shift+F로 앞뒤 이동."});
            var toy=Group(root,"손으로 돌리는 별 실험대",V(-14,0,-16));modules.Add(toy);Box(toy,"누빈 받침",V(0,.45f,0),V(2.6f,.9f,1.6f),teal,true);workshop.toyStar=Star(toy,V(0,2,0),.85f,cream);Control(toy,"종이별 바람개비",V(0,1.3f,-.8f),V(.65f,.65f,.35f),ochre,workshop,StorybookLightControl.Action.Toy,"돌려 보기");
            PolishWorkshop();RealShadowRoom(workshop);Passage();SealPassage();CoverOutgoingPassage();
            var so=new SerializedObject(room);so.FindProperty("goal").stringValue="토끼의 이름 되찾기 · 종이 오림을 잡아 그림자 맞추기";so.FindProperty("ambient").floatValue=.62f;so.FindProperty("sunIntensity").floatValue=.30f;so.ApplyModifiedPropertiesWithoutUndo();
            var sp=new SerializedObject(puzzle);sp.FindProperty("activateRadius").floatValue=90;sp.FindProperty("targetSeconds").floatValue=480;sp.ApplyModifiedPropertiesWithoutUndo();
            InteriorEnvelopeRepair.Apply();
            SaveWorkshopMeshes();
            foreach(var module in modules)PrefabUtility.SaveAsPrefabAsset(module.gameObject,WorkshopDir+"/"+module.name+".prefab");
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,WorkshopDir+"/1A_ShadowWorkshop.prefab");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            WorkshopVerifier.Structure();CaptureWorkshop();CaptureWorkshopParts();
        }
        static Material WorkshopMat(string name,Color color)
        {
            string path=WorkshopDir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.04f);m.SetFloat("_Cull",0);
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiber.png"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0PaperIdentity/MulberryFiberNormal.png"));m.SetFloat("_BumpScale",.38f);m.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(m);return m;
        }
        static Transform Control(Transform parent,string name,Vector3 at,Vector3 size,Material mat,StorybookLightPuzzle puzzle,StorybookLightControl.Action action,string verb)
        {
            var t=Group(parent,name,at);Obj(t,"오려 접은 손잡이 꽃잎",RoundMedallion(size.x*.53f,size.y*.53f,size.z*.25f),mat,Vector3.zero);var collider=t.gameObject.AddComponent<BoxCollider>();collider.size=size;
            var it=t.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=name;so.FindProperty("verb").stringValue=verb;so.FindProperty("lift").floatValue=0;so.FindProperty("range").floatValue=4.5f;so.ApplyModifiedPropertiesWithoutUndo();
            var c=t.gameObject.AddComponent<StorybookLightControl>();c.puzzle=puzzle;c.action=action;
            if(action==StorybookLightControl.Action.FindBody)Object.DestroyImmediate(t.GetChild(0).gameObject);
            else{var ring=new List<Vector3>();for(int i=0;i<=32;i++){float a=i*Mathf.PI/16;ring.Add(V(Mathf.Cos(a)*size.x*.35f,Mathf.Sin(a)*size.y*.35f,-size.z*.5f-.07f));}Stroke(t,"손잡이의 면실 고리",ring,cream,.047f);}
            if(action==StorybookLightControl.Action.BodyRail||action==StorybookLightControl.Action.EarRail||action==StorybookLightControl.Action.Flip)
            {Stroke(parent,"손잡이 종이 지지대",new[]{at+V(0,-size.y*.4f,.08f),V(at.x,.14f,at.z+.08f)},ochre,.095f);Box(parent,"누빈 작은 받침",V(at.x,.10f,at.z),V(size.x+.2f,.18f,.8f),cream);}
            return t;
        }
        static void ClueBook(Transform parent,Vector3 at,string id,string title,string[] pages)
        {
            var g=Group(parent,"펼쳐진 그림 단서책",at);g.localRotation=Quaternion.Euler(24,0,0);
            for(int s=-1;s<=1;s+=2){var p=Paper(g,"겹친 펼침 면",V(s*.75f,0,0),1.5f,1.9f,cream,V(0,s*-9,0));Star(p,V(0,.7f,-.04f),.35f,s<0?ochre:teal);Stroke(p,"크기 비교 그림",new[]{V(-.55f,.3f,-.06f),V(.5f,.3f,-.06f)},coral,.025f);}
            var col=g.gameObject.AddComponent<BoxCollider>();col.center=V(0,.95f,0);col.size=V(3.2f,2,.3f);
            var it=g.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=title;so.FindProperty("verb").stringValue="살펴보기";so.FindProperty("range").floatValue=5;so.FindProperty("lift").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
            var n=g.gameObject.AddComponent<ReadableNote>();n.id=id;n.title=title;n.pages=pages;
            if(id=="1A.Book.Scale"||id=="1A.Book.Theatre")
            {
                foreach(var star in g.GetComponentsInChildren<Transform>().Where(t=>t.name=="접힌 오각 종이별").ToArray())Object.DestroyImmediate(star.gameObject);
                for(int side=-1;side<=1;side+=2)
                {
                    var diagram=Group(g,"빛 거리의 토끼 그림",V(side*.75f,.38f,-.16f));float s=side<0?.25f:.45f;
                    foreach(bool ears in new[]{false,true})foreach(var profile in Profiles(ears)){var line=SmoothProfile(profile,s,0);line.Add(line[0]);Stroke(diagram,"작은 오림과 큰 투영",line,shadow,.018f);}
                    Star(diagram,V(-.5f,-.15f,0),.12f,ochre);
                    Stroke(diagram,"빛이 벌어지는 선",new[]{V(-.45f,-.1f,0),V(-.1f,.15f,0),V(.5f,.35f,0)},ochre,.012f);
                }
            }
            // The tabletop is a forgiving target for the open book, just as in F0.
            var top=parent.Find("누빈 작업판");if(top){var proxy=top.gameObject.AddComponent<InteractionTargetProxy>();proxy.target=it;}
        }
        static Transform WorkDesk(Vector3 at,float yaw)
        {
            var g=Group(root,"제본 서랍 작업대_"+modules.Count,at);g.localRotation=Quaternion.Euler(0,yaw,0);modules.Add(g);
            Box(g,"누빈 작업판",V(0,2.5f,0),V(5.8f,.35f,3.1f),ochre,true);
            for(int i=0;i<4;i++)Paper(g,"뜨는 작업판 가장자리",V(0,2.74f+i*.09f,-1.45f),5.6f,2.9f,i%2==0?cream:edge,V(90,0,0));
            foreach(float x in new[]{-2.3f,2.3f}){Box(g,"말린 책등 다리",V(x,1.1f,0),V(.65f,2.2f,2.5f),teal,true);Leaves(g,V(x,.25f,-1.29f),.7f);}
            var d=Group(g,"뽑히는 서랍",V(0,1.2f,-.35f));Box(d,"서랍 바닥",V(0,0,0),V(3.8f,.15f,2.5f),butter);
            Box(d,"서랍 전면",V(0,.4f,-1.2f),V(3.8f,.8f,.2f),coral);foreach(int s in new[]{-1,1})Box(d,"서랍 옆",V(s*1.85f,.35f,0),V(.16f,.7f,2.5f),cream);
            for(int j=0;j<18;j++)Stroke(g,"작업판 바느질",new[]{V(-2.7f+j*.31f,2.56f,-1.62f),V(-2.56f+j*.31f,2.66f,-1.62f)},cream,.023f);
            return g;
        }
        static Transform BookCabinet(Vector3 at,float yaw)
        {
            var g=Group(root,"청록 그림책 수납장_"+modules.Count,at);g.localRotation=Quaternion.Euler(0,yaw,0);modules.Add(g);
            Box(g,"휘어진 책등 배판",V(0,3.2f,.95f),V(5.8f,6.4f,.3f),teal,true);
            foreach(int s in new[]{-1,1}){Box(g,"두툼한 종이 기둥",V(s*2.7f,3,0),V(.4f,6,2.2f),ochre,true);Stroke(g,"둥근 제본 모서리",new[]{V(s*2.6f,0,-1),V(s*2.75f,3,-1.06f),V(s*2.55f,6,-.95f)},cream,.12f);}
            for(int row=0;row<3;row++)
            {
                Box(g,"선반",V(0,.25f+row*2,0),V(5.7f,.22f,2.3f),ochre,true);
                for(int i=0;i<7;i++){var b=Group(g,"각기 다른 그림책",V(-2.1f+i*.63f,.4f+row*2,.1f));b.localRotation=Quaternion.Euler(0,0,Mathf.Sin(i*8+row)*5);float h=1.2f+.35f*Mathf.Sin(i+row);Box(b,"종이 단면",V(0,h/2,0),V(.43f,h,1.55f),cream);Obj(b,"부푼 천제본 책등",F0Mesh.Spine(.56f,h,.14f),i%3==0?coral:teal,V(0,0,-.82f));Star(b,V(0,h*.6f,-.98f),.12f,ochre);}
            }
            Leaves(g,V(-.5f,6.4f,.9f),.7f);return g;
        }
        static void DecorateWorkshop()
        {
            // Overlaid wallpaper, cotton stitching, folded ribs and relief motifs, not a bare box.
            foreach(float z in new[]{-26f,-10f,7f,25f})foreach(int side in new[]{-1,1})
            {
                var g=Group(root,"접힌 골판지 벽기둥",V(side*30.9f,0,z));modules.Add(g);
                Obj(g,"말린 종이 기둥",F0Mesh.Spine(1.4f,11.8f,.42f),side<0?coral:teal,V(0,0,0));g.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                for(int i=0;i<12;i++)Stroke(g,"기둥 실매듭",new[]{V(-.68f,i+.5f,-.06f),V(0,i+.58f,-.46f),V(.68f,i+.5f,-.06f)},cream,.042f);
            }
            foreach(float z in new[]{-23f,-1f,19f})foreach(int s in new[]{-1,1})
            {
                var g=Group(root,"동화 창틀과 압화",V(s*31.48f,2,z));g.localRotation=Quaternion.Euler(0,s<0?90:-90,0);modules.Add(g);
                Paper(g,"창의 속지",Vector3.zero,6,6,butter,Vector3.zero);
                for(int i=0;i<3;i++){float h=6-i*.2f;Stroke(g,"종이 창 테두리",new[]{V(-3+i*.1f,0,-i*.06f),V(-3+i*.1f,h,-i*.06f),V(3-i*.1f,h,-i*.06f),V(3-i*.1f,0,-i*.06f)},i%2==0?cream:ochre,.06f);}
                Leaves(g,V(-1.7f,.4f,-.12f),1.6f);Art(g,s<0?"Bird":"Rabbit",V(.8f,.8f,-.16f),2.8f);
            }
            foreach(float z in new[]{-17f,0f,18f})
            {
                var g=Group(root,"천장 제본 리브",V(0,11.5f,z));modules.Add(g);
                var curve=new List<Vector3>();for(int i=0;i<=32;i++){float x=-31+62f*i/32;curve.Add(V(x,-.5f*Mathf.Cos(x/31*Mathf.PI*.5f),0));}Stroke(g,"늘어진 면실과 종이 능선",curve,ochre,.13f);
                for(int x=-24;x<=24;x+=8){var p=Star(g,V(x,-.3f,.2f),.55f,x%16==0?coral:teal);p.localRotation=Quaternion.Euler(90,0,12);}
            }
            foreach(var at in new[]{V(-26,0,-24),V(25,0,-22),V(-25,0,21),V(24,0,17)})
            {
                var before=root.childCount;Books(at,4,at.x);var g=root.GetChild(before);g.name="층층 종이책 섬_"+modules.Count;modules.Add(g);
                for(int j=0;j<7;j++){var p=Paper(g,"불규칙한 겹장 끝",V(0,.17f+j*.4f,-1.45f),4.3f,2.8f,cream,V(90,0,j%2==0?1:-1));}
            }
            // Embossed leaf trail has an arrowhead, so direction is readable without colour.
            Vector3[] route={V(-30,.14f,-12.8f),V(-19,.14f,-16),V(-8,.14f,-11),V(-8,.14f,-2),V(8,.14f,6),V(8,.14f,15),V(18,.14f,23),V(24.5f,.14f,27)};
            for(int i=0;i<route.Length-1;i++){var d=route[i+1]-route[i];for(float f=0;f<d.magnitude;f+=1.8f){var t=Obj(root,"다음 장을 가리키는 잎",OrganicLeaf(.65f,.22f),teal,route[i]+d.normalized*f);t.localRotation=Quaternion.LookRotation(d,Vector3.up)*Quaternion.Euler(90,0,0);}}
            ClueBook(root,V(17,1.1f,24.5f),"1A.Book.Lamp","다음 장의 기억등",new[]{"빛을 옮길 때는 두 고리를 먼저 잠글 것.","그늘진 기록대에 빛을 먼저 나누고, 창문을 연 뒤 다시 채울 것."});
        }
        static Vector2[][] Profiles(bool ears)
        {
            if(ears)return new[]{new[]{new Vector2(-.38f,2.55f),new Vector2(-.75f,3.45f),new Vector2(-.64f,4.15f),new Vector2(-.32f,4.04f),new Vector2(.05f,2.8f)},new[]{new Vector2(.1f,2.7f),new Vector2(.08f,4.05f),new Vector2(.37f,4.45f),new Vector2(.58f,4.16f),new Vector2(.43f,2.65f)}};
            return new[]{new[]{new Vector2(-.7f,0),new Vector2(-1.25f,.5f),new Vector2(-1.2f,1.45f),new Vector2(-.5f,2.1f),new Vector2(-.65f,2.65f),new Vector2(-.15f,3),new Vector2(.55f,2.95f),new Vector2(.9f,2.6f),new Vector2(1.16f,2.5f),new Vector2(.93f,2.2f),new Vector2(.45f,2),new Vector2(.45f,1.35f),new Vector2(.8f,1),new Vector2(.5f,.65f),new Vector2(.36f,.3f),new Vector2(.9f,.14f),new Vector2(.85f,-.08f)}};
        }
        static List<Vector3> SmoothProfile(Vector2[] input,float scale,float depth)
        {
            var points=new List<Vector3>();for(int i=0;i<input.Length;i++)for(int k=0;k<6;k++)
            {float t=k/6f;Vector2 a=input[(i+input.Length-1)%input.Length],b=input[i],c=input[(i+1)%input.Length],d=input[(i+2)%input.Length];Vector2 q=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);points.Add(V(q.x*scale,q.y*scale,depth));}return points;
        }
        static Transform PaperPuppet(Transform parent,string name,bool isEar,Material mat,bool projected=false)
        {
            var g=Group(parent,name,Vector3.zero);float scale=isEar?1/3f:1/1.5f;
            foreach(var outline in Profiles(isEar))
            {
                var p=SmoothProfile(outline,scale,0);
                // Ear-clipping triangulates the concave silhouette; no centre-fan across concavities.
                var uv=p.Select(v=>new Vector2(v.x,v.y)*.65f).ToList();var indices=Triangulate(p);
                Obj(g,"잘라낸 장지 면",Mesh(p,uv,indices),mat,Vector3.zero);
                if(!projected){var loop=new List<Vector3>(p);loop.Add(p[0]);Stroke(g,"오림 가장자리 섬유",loop,edge,.014f);var back=p.Select(v=>v+Vector3.forward*.04f).ToList();Obj(g,"겹친 오림 뒷면",Mesh(back,uv,indices),cream,Vector3.zero);}
            }
            return g;
        }
        static List<int> Triangulate(List<Vector3> p)
        {
            float area=0;for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];area+=a.x*b.y-b.x*a.y;}
            var work=Enumerable.Range(0,p.Count).ToList();if(area<0)work.Reverse();var tris=new List<int>();int guard=0;
            float Cross(Vector3 a,Vector3 b,Vector3 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
            while(work.Count>2&&guard++<p.Count*p.Count)
            {bool clipped=false;for(int i=0;i<work.Count;i++){int a=work[(i+work.Count-1)%work.Count],b=work[i],c=work[(i+1)%work.Count];if(Cross(p[a],p[b],p[c])<=.000001f)continue;bool inside=work.Any(k=>k!=a&&k!=b&&k!=c&&Cross(p[a],p[b],p[k])>=0&&Cross(p[b],p[c],p[k])>=0&&Cross(p[c],p[a],p[k])>=0);if(inside)continue;tris.AddRange(new[]{a,c,b});work.RemoveAt(i);clipped=true;break;}if(!clipped)throw new Exception("Invalid paper outline");}return tris;
        }
        static void BuildShadowStage(StorybookLightPuzzle w)
        {
            var g=Group(root,"토끼 그림자 극장",V(-5,.36f,14));w.stage=g;
            Paper(g,"질긴 투영 스크린",V(0,.1f,12.5f),14,9,cream,Vector3.zero);Arch(g,V(0,0,12.35f),6,2.2f,0);
            foreach(bool ears in new[]{false,true})foreach(var profile in Profiles(ears)){var line=SmoothProfile(profile,1,12.38f);line.Add(line[0]);Stroke(g,"맞춰야 할 토끼 윤곽",line.Select(v=>v+Vector3.up).ToList(),ink,.042f);}
            foreach(float side in new[]{-1f,1f})
            {
                for(int k=0;k<2;k++)Stroke(g,"휘어진 종이 레일",new[]{V(side*.55f+k*.16f,.14f,3),V(side*.55f+k*.16f,.14f,6.5f),V(side*.55f+k*.16f,.14f,10)},ochre,.06f);
                for(int j=3;j<=10;j++)Star(g,V(side*1.1f,.14f,j),.10f,teal).localRotation=Quaternion.Euler(90,0,0);
            }
            w.body=PaperPuppet(g,"몸통 종이 꽂이",false,coral);w.ears=PaperPuppet(g,"작은 귀 종이 꽂이",true,teal);
            foreach(var holder in new[]{w.body,w.ears}){Box(holder,"레일 위의 종이 꽂이 발",V(0,-.78f,0),V(1.1f,.2f,.65f),ochre);Stroke(holder,"오림을 받치는 종이 막대",new[]{V(0,-.75f,.04f),V(0,holder==w.ears?.9f:.3f,.04f)},cream,.06f);}
            w.bodyShadow=PaperPuppet(g,"몸통 실제 비율 투영",false,shadow,true);w.bodyShadow.localPosition=V(0,1,12.30f);
            w.earShadow=PaperPuppet(g,"귀 실제 비율 투영",true,shadow,true);w.earShadow.localPosition=V(0,1,12.28f);
            w.body.gameObject.SetActive(false);w.ears.gameObject.SetActive(false);w.bodyShadow.gameObject.SetActive(false);w.earShadow.gameObject.SetActive(false);
            var lamp=Group(g,"꽃잎 기억등",V(0,0,0));modules.Add(lamp);Box(lamp,"등 받침",V(0,.22f,0),V(1.8f,.44f,1.8f),ochre,true);
            for(int i=0;i<8;i++){var leaf=Obj(lamp,"말린 반투명 종이 꽃잎",OrganicLeaf(1.55f,.6f),cream,V(0,.3f,0));leaf.localRotation=Quaternion.Euler(-25,i*45,0);}
            Star(lamp,V(0,1.1f,0),.65f,glow);w.lantern=LightAt(lamp,V(0,1.4f,0),1.5f,14);
            Control(g,"꽃등 덮개",V(0,.7f,-1.1f),V(.8f,.5f,.5f),ochre,w,StorybookLightControl.Action.Lamp,"빛 열기 / 닫기");
            Control(g,"몸통 꽂이 · 거리 손잡이",V(-3,1.1f,4),V(1.1f,.75f,.65f),coral,w,StorybookLightControl.Action.BodyRail,"끼우기 / 이동 · Shift로 반대");
            Control(g,"귀 꽂이 · 거리 손잡이",V(3,1.1f,4),V(1.1f,.75f,.65f),teal,w,StorybookLightControl.Action.EarRail,"끼우기 / 이동 · Shift로 반대");
            Control(g,"귀 앞뒤 회전 고리",V(3,1.1f,8),V(.8f,.8f,.5f),ochre,w,StorybookLightControl.Action.Flip,"앞뒤 뒤집기");
            foreach(float s in new[]{-1f,1f})Stroke(g,"레일 손잡이 제본실",new[]{V(s*3,.6f,4),V(s*2,.2f,4),V(s*.8f,.18f,4)},cream,.035f);
            w.rabbitPopup=PaperPuppet(g,"이름을 되찾는 팝업 토끼",false,cream);w.rabbitPopup.localPosition=V(6.5f,.2f,10);w.rabbitPopup.localScale=Vector3.one*1.5f;w.rabbitPopup.localRotation=Quaternion.Euler(86,0,0);
            var popupEars=PaperPuppet(w.rabbitPopup,"팝업 귀",true,cream);popupEars.localScale=Vector3.one*2;
            w.rewardStar=Star(g,V(6.5f,5.6f,11),.7f,glow);
            ClueBook(g,V(-7,1.1f,3),"1A.Book.Theatre","빛의 거리",new[]{"작은 오림이 크게 보이려면 빛에 가까이. 큰 오림이 작게 보이려면 벽에 가까이.","몸통과 귀를 따로 옮겨 벽의 선을 채운다. 그림자가 반대로 기울면 귀의 앞뒤를 뒤집자."});
        }
        static void SealPassage()
        {
            var g=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="V4_서문에서동화로");
            var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");var so=new SerializedObject(link);
            float len=Vector3.Distance(so.FindProperty("fromPoint").vector3Value,so.FindProperty("toPoint").vector3Value),width=link.transform.lossyScale.x;
            // One continuous opaque envelope behind the decorative segments prevents seams and backface voids.
            var shell=Group(g,"이음새 없는 통로 외피",Vector3.zero);
            foreach(int side in new[]{-1,1})Box(shell,"연속 통로 벽",V(side*(width*.5f+.38f),3.6f,0),V(.48f,7.5f,len+4),cream,true);
            Box(shell,"연속 통로 천장",V(0,7.1f,0),V(width+1.25f,.5f,len+4),cream,true);
            Box(shell,"연속 통로 하부",V(0,-.27f,0),V(width+1,.35f,len+4),butter,true);
            for(float z=-len*.5f;z<=len*.5f;z+=2){var leaf=Obj(shell,"다음 방 안내 잎",OrganicLeaf(.7f,.27f),teal,V(0,.23f,z));leaf.localRotation=Quaternion.Euler(90,0,0);}
        }
        static void SaveWorkshopMeshes()
        {
            var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None);var colliders=Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            AssetDatabase.StartAssetEditing();try{for(int i=0;i<meshes.Count;i++)
            {
                var m=meshes[i];string path=WorkshopDir+"/Mesh_"+i.ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!old)AssetDatabase.CreateAsset(m,path);else{old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;old.uv=m.uv;old.normals=m.normals;old.tangents=m.tangents;old.triangles=m.triangles;old.bounds=m.bounds;old.UploadMeshData(false);EditorUtility.SetDirty(old);foreach(var f in filters)if(f.sharedMesh==m)f.sharedMesh=old;foreach(var c in colliders)if(c.sharedMesh==m)c.sharedMesh=old;Object.DestroyImmediate(m);}
            }}finally{AssetDatabase.StopAssetEditing();}
        }
        static void CaptureWorkshop()
        {
            var cam=new GameObject("1A review camera").AddComponent<Camera>();cam.fieldOfView=66;cam.nearClipPlane=.08f;cam.farClipPlane=180;
            var rs=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToDictionary(x=>x,x=>x.enabled);
            try{foreach(var r in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))foreach(var re in r.GetComponentsInChildren<Renderer>())re.enabled=r==room;
                Vector3[] poses={V(-13,2.8f,-11),V(-7,2.8f,-4),V(3,2.8f,13),V(5,3.5f,17)};Vector3[] targets={V(-2,4,10),V(9,3,10),V(-4,3.8f,26),V(-6,3.8f,26)};
                for(int i=0;i<poses.Length;i++){cam.transform.position=room.transform.TransformPoint(poses[i]);cam.transform.LookAt(room.transform.TransformPoint(targets[i]));InteriorF0Builder.Capture(cam,WorkshopOut+"/1A-"+(i+1)+".png");}
            }finally{foreach(var kv in rs)if(kv.Key)kv.Key.enabled=kv.Value;Object.DestroyImmediate(cam.gameObject);}
        }
    }
}

