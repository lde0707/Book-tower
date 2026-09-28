using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.CameraSystem;
using BookTower.PlayerSystem;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.Story;
using BookTower.UI;
using BookTower.Interaction;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class OpeningStoryBuilder
    {
        const string Dir = "Assets/_BookTower/Generated/OpeningStory";
        static Material paper, ink, ochre;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity"); Apply();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        [MenuItem("Book Tower/스토리/F0부터 1A 이야기 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new Exception("Exit Play first");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene");
            Directory.CreateDirectory("Logs/StoryBackups");
            EditorSceneManager.SaveScene(scene,"Logs/StoryBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Directory.CreateDirectory(Dir); AssetDatabase.Refresh();
            var previous = GameObject.Find("Opening Story · F0 to 1A"); if(previous)Object.DestroyImmediate(previous);
            var rooms=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var f0=rooms.First(r=>r.RoomId=="F0");var a1=rooms.First(r=>r.RoomId=="1A");
            var player=Object.FindFirstObjectByType<PlayerController>();var ui=Object.FindFirstObjectByType<PaperUI>();
            var root=new GameObject("Opening Story · F0 to 1A");var director=root.AddComponent<OpeningStoryDirector>();
            paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Art/PaperSpirit/PaperWrap.mat");
            ink=Mat("MemoryInk",new Color(.18f,.085f,.035f));ochre=Mat("FoldEdge",new Color(.67f,.38f,.12f));
            director.player=player;director.storyCamera=Object.FindFirstObjectByType<PaperCamera>();director.foyer=f0;director.theatreRoom=a1;
            director.bridge=f0.GetComponentInChildren<FoldBridgePuzzle>(true);director.theatrePuzzle=a1.GetComponentsInChildren<PuzzleBase>(true).First(p=>p.PuzzleId=="P2");
            director.rustle=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_BookTower/Audio/BindingPlay/PaperRustle.wav");
            director.nameMotif=AssetDatabase.LoadAssetAtPath<AudioClip>(Dir+"/MoaName.wav");
            director.companion=Bird(root.transform);
            director.companion.transform.position=player.transform.TransformPoint(V(.72f,.025f,-.48f));
            director.companion.player=player;
            PrefabUtility.SaveAsPrefabAsset(director.companion.gameObject,Dir+"/MoaPaperBird.prefab");
            var crafted=f0.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="02_OpenClueBook");
            if(crafted)
            {
                Note(crafted,OpeningStoryDirector.WorkNote,"제본사의 작업책",new[]{"빛은 종이에서 길을 찾는다.\n끊긴 곳이 보이면, 빛이 마지막으로 닿은 면부터 살펴볼 것.","한쪽에는 접힌 다리, 다른 쪽에는 꽃 문양이 그려져 있다.\n문에 빛이 닿아도 마지막 걸쇠는 직접 확인해야 한다."},V(0,.58f,.0f),V(3,.9f,2),4.5f);
                director.clueBookPage=crafted.Find("기울어진펼친책");
            }
            Book(f0.transform,"다음장의그림책",V(3.2f,3.65f,11.1f),OpeningStoryDirector.ShadowNote,"작은 몸, 큰 자리",new[]{"작은 몸이 큰 자리를 채울 때도 있다.","등 가까이에 놓인 작은 토끼는 벽에 커다란 그림자를 남긴다.\n다음 장에는 같은 토끼가 서로 다른 크기로 그려져 있다."});
            Book(a1.transform,"기억등작업기록",V(8.2f,.85f,24.2f),OpeningStoryDirector.LampNote,"빛을 가져가는 방법",new[]{"빛을 옮길 때는 두 고리를 먼저 잠글 것.","제본사의 다음 작업 도면.\n등 양쪽의 고리를 닫은 뒤, 어두운 책상 아래로 옮기는 손이 그려져 있다."});
            // Keep authored art intact; replace the old conflicting generic foyer note text.
            foreach(var note in f0.GetComponentsInChildren<ReadableNote>(true))
                if(note.id=="f0-binder") {note.title="제본 현관의 메모";note.pages=new[]{"누군가 돌아올 수 있도록, 이 책의 마지막 장은 비워 두었다.","잊힌 기록은 읽어 줄 이를 기다린다."};EditorUtility.SetDirty(note);}
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            PaperCutsceneBuilder.Apply();
            Debug.Log("[OpeningStory] Scene wired: illustrated story, companion and three readable clues.");
        }
        static Material Mat(string name,Color color)
        {
            string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0);m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);return m;
        }
        static Transform Group(Transform parent,string name,Vector3 at)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
        static Transform Mesh(Transform parent,string name,UnityEngine.Mesh mesh,Material material,Vector3 at)
        {
            string path=Dir+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
            if(!saved) {saved=mesh;AssetDatabase.CreateAsset(mesh,path);}else {saved.Clear();saved.vertices=mesh.vertices;saved.triangles=mesh.triangles;saved.uv=mesh.uv;saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.bounds=mesh.bounds;saved.UploadMeshData(false);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var t=Group(parent,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        static Transform Polygon(Transform parent,string name,Vector3[] points,Material material,Vector3 at)
        {
            var m=new UnityEngine.Mesh();m.vertices=points;var tris=new List<int>();
            for(int i=1;i<points.Length-1;i++)tris.AddRange(new[]{0,i+1,i});
            m.triangles=tris.ToArray();m.uv=points.Select(p=>new Vector2(p.x,p.y+p.z)).ToArray();m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return Mesh(parent,name,m,material,at);
        }
        static PaperCompanion Bird(Transform parent)
        {
            var sculpt=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BookTower/Art/MoaSculpt/MoaSculpt.prefab");
            if(!sculpt)return MoaBirdBuilder.Create(parent);
            var bird=(GameObject)PrefabUtility.InstantiatePrefab(sculpt);bird.transform.SetParent(parent,false);
            return bird.GetComponent<PaperCompanion>();
        }
        static void Note(Transform t,string id,string title,string[] pages,Vector3 center,Vector3 size,float range)
        {
            var note=t.GetComponent<ReadableNote>();if(!note)note=t.gameObject.AddComponent<ReadableNote>();note.id=id;note.title=title;note.pages=pages;
            var interact=t.GetComponent<Interactable>();var so=new SerializedObject(interact);so.FindProperty("label").stringValue=title;so.FindProperty("verb").stringValue="책 읽기";so.FindProperty("range").floatValue=range;so.FindProperty("lift").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
            var c=t.GetComponent<BoxCollider>();if(!c)c=t.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;EditorUtility.SetDirty(note);
        }
        static void Book(Transform parent,string name,Vector3 at,string id,string title,string[] pages)
        {
            var old=parent.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
            var t=Group(parent,name,at);
            Mesh(t,name+"Stand",F0Mesh.RoundedBox(V(2,.9f,1.25f),.06f),ochre,V(0,-.45f,0));
            var book=Mesh(t,name+"Page",F0Mesh.Paper(1.85f,1.3f,47),paper,V(0,.1f,.4f));book.localRotation=Quaternion.Euler(-68,0,0);
            if(id==OpeningStoryDirector.LampNote)
            {
                Mesh(book,name+"Lantern",F0Mesh.RoundedBox(V(.48f,.6f,.03f),.04f),ink,V(0,.65f,-.07f));
                foreach(int side in new[]{-1,1}) {var path=new List<Vector3>();for(int j=0;j<=24;j++){float a=j*Mathf.PI*2/24;path.Add(V(side*.38f+Mathf.Cos(a)*.09f,.68f+Mathf.Sin(a)*.13f,-.08f));}Mesh(book,name+"Ring"+side,F0Mesh.Tube(path,.021f,6),ochre,Vector3.zero);}
            }
            else for(int i=0;i<2;i++)Polygon(book,name+"Rabbit"+i,new[]{V(-.15f,0,-.02f),V(.15f,0,-.02f),V(.17f,.23f,-.02f),V(.08f,.28f,-.02f),V(.14f,.55f,-.02f),V(.07f,.57f,-.02f),V(0,.3f,-.02f),V(-.07f,.52f,-.02f),V(-.13f,.5f,-.02f),V(-.09f,.24f,-.02f)},ink,V(i==0?-.48f:.42f,.35f,0)).localScale=Vector3.one*(i==0?.55f:1);
            Note(t,id,title,pages,V(0,.1f,0),V(2,.65f,1.5f),4);
        }
    }
}
