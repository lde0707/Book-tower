using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    [InitializeOnLoad]
    public static partial class StorybookRoomBuilder
    {
        const string Dir="Assets/_BookTower/Generated/Storybook1A",Out="Docs/ArtReview/Storybook1A";
        static Transform root;static Room room;static Material butter,cream,coral,teal,ochre,shadow,glow,red;
        static readonly List<Mesh> meshes=new();
        static int seed;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        static StorybookRoomBuilder(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/storybook.request"))return;
            string request=File.ReadAllText("Logs/storybook.request").Trim();File.Delete("Logs/storybook.request");
            try{if(request=="workshop")ApplyWorkshop();else Apply();File.WriteAllText("Logs/storybook.result","PASS "+DateTime.Now.ToString("O"));}
            catch(Exception e){File.WriteAllText("Logs/storybook.result",e.ToString());Debug.LogException(e);}
        }
        public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();}
        [MenuItem("Book Tower/1A 팝업북 회랑 적용")]
        public static void Apply()
        {
            if(Object.FindFirstObjectByType<StorybookLightPuzzle>(FindObjectsInactive.Include)) { ApplyWorkshop(); return; }
            if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene");
            Directory.CreateDirectory("Logs/StorybookBackups");EditorSceneManager.SaveScene(scene,"Logs/StorybookBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Directory.CreateDirectory(Dir);Directory.CreateDirectory(Out);AssetDatabase.Refresh();meshes.Clear();seed=48;
            room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="1A");
            var old=room.transform.Find("V4_팝업북회랑");if(old)Object.DestroyImmediate(old.gameObject);
            var game=room.transform.Find("1A_Gameplay");if(!game)game=Group(room.transform,"1A_Gameplay",Vector3.zero);
            foreach(var p in room.GetComponentsInChildren<PuzzleBase>(true))if(!p.transform.IsChildOf(game))p.transform.SetParent(game,true);
            foreach(var p in room.GetComponentsInChildren<FoldGroup>(true))if(!p.transform.IsChildOf(game))p.transform.SetParent(game,true);
            foreach(var p in room.GetComponentsInChildren<FoldPanel>(true))if(!p.transform.IsChildOf(game))p.transform.SetParent(game,true);
            foreach(Transform child in room.transform)if(child!=game)child.gameObject.SetActive(false);
            game.gameObject.SetActive(true);
            // Rebuild this room's panels, retaining the original P2 puzzle object and its gate references.
            foreach(var p in game.GetComponentsInChildren<FoldPanel>(true))Object.DestroyImmediate(p.gameObject);
            butter=Mat("ButterPaper",new Color(.98f,.84f,.62f));cream=Mat("CreamPaper",new Color(1,.90f,.68f));
            coral=Mat("CoralWax",new Color(.71f,.25f,.16f));teal=Mat("TealWax",new Color(.16f,.45f,.43f));ochre=Mat("Ochre",new Color(.66f,.43f,.16f));shadow=Mat("Shadow",new Color(.20f,.13f,.085f));red=Mat("BindingRed",new Color(.38f,.10f,.045f));glow=Mat("StarLight",new Color(1,.78f,.38f));glow.SetColor("_EmissionColor",new Color(1,.55f,.16f)*1.4f);glow.EnableKeyword("_EMISSION");glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
            root=Group(room.transform,"V4_팝업북회랑",Vector3.zero);
            Shell();Dress();Gameplay(game);Passage();
            var so=new SerializedObject(room);so.FindProperty("boundsCenter").vector3Value=V(0,6,0);so.FindProperty("boundsSize").vector3Value=V(66,13,58);so.FindProperty("ambient").floatValue=.66f;so.FindProperty("sunIntensity").floatValue=.35f;so.FindProperty("fog").floatValue=.001f;so.FindProperty("goal").stringValue="새의 접기를 익히고, 코럴과 청록 두 반쪽 그림자를 토끼로 합친다.";so.ApplyModifiedPropertiesWithoutUndo();
            SaveMeshes();InteriorEnvelopeRepair.Apply();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Dir+"/StorybookHall.prefab");PaperUIBuilder.RefreshSymbols();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Validate();Capture();Debug.Log("[Storybook] Saved 1A and binding-to-picturebook passage");
        }
        static Transform Group(Transform parent,string name,Vector3 pos){var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=pos;return g;}
        static Material Mat(string name,Color color)
        {
            string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0);m.SetFloat("_Metallic",0);m.SetFloat("_Cull",0);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0Reference/Paper_Albedo.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/F0Reference/Paper_Normal.png"));m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;
        }
        static Transform Obj(Transform p,string name,Mesh mesh,Material mat,Vector3 at,bool solid=false)
        {var t=Group(p,name,at);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;if(solid)t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;meshes.Add(mesh);return t;}
        static Transform Box(Transform p,string n,Vector3 at,Vector3 size,Material m,bool solid=false)
        {var t=Obj(p,n,F0Mesh.RoundedBox(size,.13f),m,at);if(solid){var c=t.gameObject.AddComponent<BoxCollider>();c.size=size;}return t;}
        static Transform Paper(Transform p,string n,Vector3 at,float w,float h,Material m,Vector3 angles)
        {var t=Obj(p,n,F0Mesh.Paper(w,h,seed++),m,at);t.localRotation=Quaternion.Euler(angles);return t;}
        static Transform Stroke(Transform p,string n,IList<Vector3> path,Material m,float r=.045f)=>Obj(p,n,F0Mesh.Tube(path,r,6),m,Vector3.zero);
        static Light LightAt(Transform p,Vector3 at,float intensity,float range)
        {var t=Group(p,"따뜻한 종이등 빛",at);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.intensity=intensity;l.range=range;l.color=new Color(1,.91f,.77f);l.shadows=LightShadows.None;return l;}
        static void SaveMeshes()
        {
            var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var colliders=Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            AssetDatabase.StartAssetEditing();try{
            for(int i=0;i<meshes.Count;i++)
            {
                var m=meshes[i];string path=Dir+"/Mesh_"+i.ToString("D4")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!old)AssetDatabase.CreateAsset(m,path);else
                {
                    old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;old.uv=m.uv;old.normals=m.normals;old.tangents=m.tangents;old.triangles=m.triangles;old.bounds=m.bounds;old.UploadMeshData(false);EditorUtility.SetDirty(old);
                    foreach(var f in filters)if(f.sharedMesh==m)f.sharedMesh=old;
                    foreach(var c in colliders)if(c.sharedMesh==m)c.sharedMesh=old;
                    Object.DestroyImmediate(m);
                }
            }
            }finally{AssetDatabase.StopAssetEditing();}
        }
    }
}
