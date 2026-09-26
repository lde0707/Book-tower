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
using BookTower.Puzzles;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        public const string RootName="V3_제본현관";
        const string Folder="Assets/_BookTower/Generated/F0Reference", Output="Docs/ArtReview/InteriorRoomsV3";
        static Material paper,ivory,edge,oxblood,umber,ochre,rope,brass,ink,wood,glow;
        static readonly List<Mesh> meshes=new();
        static Room room;
        static Transform root,leftDoor,rightDoor;
        static Vector3 V(float x,float y,float z)=>new(x,y,z);
        [MenuItem("Book Tower/F0 레퍼런스 모델 적용")]
        public static void ApplyAndSave()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before rebuilding F0");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/InGame.unity")throw new InvalidOperationException("Open InGame scene first");
            Directory.CreateDirectory("Logs/F0Backups");
            EditorSceneManager.SaveScene(scene,$"Logs/F0Backups/InGame-{DateTime.Now:yyyyMMdd-HHmmss}.unity",true);
            Apply();
            var player=Object.FindFirstObjectByType<PlayerController>();
            if(player!=null){player.transform.position=room.transform.TransformPoint(V(0,.15f,-11.8f));player.transform.rotation=Quaternion.identity;}
            var camera=Object.FindFirstObjectByType<PaperCamera>();
            if(camera!=null)
            {
                var so=new SerializedObject(camera);
                so.FindProperty("pitch").floatValue=10;so.FindProperty("closeDistance").floatValue=4.8f;
                so.FindProperty("closeFocusHeight").floatValue=1.1f;so.FindProperty("perspectiveFov").floatValue=60;
                so.ApplyModifiedPropertiesWithoutUndo();
                camera.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing=true;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[F0 Reference] Saved modeled F0 and mouse-aim camera in InGame");
        }
        public static void Apply()
        {
            room=Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(r=>r.RoomId=="F0");
            if(room==null)throw new InvalidOperationException("F0 missing");
            meshes.Clear();var old=room.transform.Find(RootName);if(old!=null)Object.DestroyImmediate(old.gameObject);
            PreserveGameplayAndRetireLegacy();EnsureFolder(Folder);PrepareMaterials();
            root=Group(room.transform,RootName,Vector3.zero);
            Shell();Door();Library();Ropes();WindowsAndLights();SmallDetails();Gameplay();
            Group(root,"시작점",V(0,.10f,-11.8f));SetupRoomLighting();
            CombineDecorations(root,leftDoor,rightDoor);CombineDecorations(leftDoor);CombineDecorations(rightDoor);
            SaveMeshes();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/F0_BindingVestibule.prefab");AssetDatabase.SaveAssets();
            Debug.Log($"[F0 Reference] Modeled {root.GetComponentsInChildren<MeshFilter>().Length} mesh groups");
        }
        static void PreserveGameplayAndRetireLegacy()
        {
            var gameplay=room.transform.Find("F0_Gameplay");if(gameplay==null)gameplay=Group(room.transform,"F0_Gameplay",Vector3.zero);
            foreach(var p in room.GetComponentsInChildren<PuzzleBase>(true))if(!p.transform.IsChildOf(gameplay))p.transform.SetParent(gameplay,true);
            foreach(var p in room.GetComponentsInChildren<CollectiblePiece>(true))if(!p.transform.IsChildOf(gameplay))p.transform.SetParent(gameplay,true);
            foreach(var p in room.GetComponentsInChildren<AssemblyPoint>(true))if(!p.transform.IsChildOf(gameplay))p.transform.SetParent(gameplay,true);
            // Room.SetVisible re-enables renderers; obsolete hierarchy must actually be inactive.
            foreach(Transform child in room.transform)if(child!=gameplay)child.gameObject.SetActive(false);
            gameplay.gameObject.SetActive(true);
        }
        static void Gameplay()
        {
            var g=room.transform.Find("F0_Gameplay");
            foreach(var piece in g.GetComponentsInChildren<CollectiblePiece>(true))
            {
                bool first=piece.PieceId=="P1_a";if(!first&&piece.PieceId!="P1_b")continue;
                ResetVisual(piece.transform);piece.transform.position=room.transform.TransformPoint(first?V(-9.5f,1.15f,3):V(9.8f,1.15f,-4));
                piece.transform.localScale=Vector3.one;piece.transform.rotation=Quaternion.identity;
                var visual=MeshObject(piece.transform,"찢긴표지조각",F0Mesh.Paper(.70f,.84f,first?531:371),ivory,V(0,-.2f,0));visual.localRotation=Quaternion.Euler(0,first?20:-20,8);
                Sprig(piece.transform,V(0,-.1f,-.08f),.55f,ink);
                var col=piece.GetComponent<BoxCollider>();if(col==null)col=piece.gameObject.AddComponent<BoxCollider>();col.enabled=true;col.size=V(.85f,.95f,.35f);col.center=V(0,.18f,0);
                Box(root,"조각진열대",room.transform.InverseTransformPoint(piece.transform.position)+V(0,-.7f,0),V(1.5f,.7f,1.2f),umber,true);
                Box(root,"조각진열대황동",room.transform.InverseTransformPoint(piece.transform.position)+V(0,-.32f,0),V(1.6f,.09f,1.3f),brass);piece.gameObject.SetActive(true);
            }
            var puzzle=g.GetComponentInChildren<PieceAssemblyPuzzle>(true);
            if(puzzle!=null)puzzle.transform.position=room.transform.TransformPoint(V(0,.6f,12.5f));
            var assembly=g.GetComponentInChildren<AssemblyPoint>(true);
            if(assembly!=null)
            {
                ResetVisual(assembly.transform);assembly.transform.position=room.transform.TransformPoint(V(0,.65f,12.5f));assembly.transform.localScale=Vector3.one;assembly.transform.rotation=Quaternion.identity;
                Box(assembly.transform,"표지걸쇠받침",Vector3.zero,V(2.7f,.22f,1.3f),brass,true);
                Box(assembly.transform,"걸쇠빈자리",V(0,.13f,0),V(1.55f,.025f,.88f),umber);
                foreach(float x in new[]{-.4f,.4f})Box(assembly.transform,"두조각모양",V(x,.15f,0),V(.55f,.016f,.66f),paper);
                var col=assembly.GetComponent<BoxCollider>();if(col==null)col=assembly.gameObject.AddComponent<BoxCollider>();col.enabled=true;col.center=Vector3.zero;col.size=V(2.7f,.3f,1.4f);assembly.gameObject.SetActive(true);
            }
            root.GetComponent<BindingDoor>().puzzle=puzzle;
        }
        static void ResetVisual(Transform p)
        {
            var renderer=p.GetComponent<Renderer>();if(renderer!=null)Object.DestroyImmediate(renderer);
            var filter=p.GetComponent<MeshFilter>();if(filter!=null)Object.DestroyImmediate(filter);
            foreach(var c in p.GetComponents<Collider>())c.enabled=false;
            foreach(Transform child in p)child.gameObject.SetActive(false);
        }
        static void SetupRoomLighting()
        {
            var so=new SerializedObject(room);so.FindProperty("sunIntensity").floatValue=.15f;so.FindProperty("ambient").floatValue=.55f;so.FindProperty("fog").floatValue=.002f;so.FindProperty("shadowStrength").floatValue=.9f;so.ApplyModifiedPropertiesWithoutUndo();
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/F0_Grade.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/F0_Grade.asset");}
            if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var grade))grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.35f);grade.contrast.Override(13);grade.saturation.Override(5);
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.2f);bloom.intensity.Override(.16f);
            foreach(var c in profile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,profile);EditorUtility.SetDirty(profile);
            var vol=Group(root,"실내색보정",V(0,8,0));var trigger=vol.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=V(56,24,44);
            var volume=vol.gameObject.AddComponent<Volume>();volume.isGlobal=false;volume.priority=5;volume.blendDistance=1;volume.sharedProfile=profile;
        }
        static void PrepareMaterials()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles("Assets/_BookTower/Textures/F0Reference","*.png"))
            {
                var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(importer==null)continue;
                var type=file.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                if(importer.textureType!=type || importer.textureCompression!=TextureImporterCompression.Uncompressed)
                {importer.textureType=type;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.SaveAndReimport();}
            }
            paper=Mat("WarmPaper",new Color(.80f,.64f,.42f),"Paper");ivory=Mat("IvoryPaper",new Color(.98f,.84f,.62f),"Paper");
            edge=Mat("LayeredPages",new Color(.85f,.72f,.51f),"Edges");oxblood=Mat("OxbloodBook",new Color(.36f,.09f,.035f),"Cover");
            umber=Mat("UmberBook",new Color(.115f,.064f,.026f),"Cover");ochre=Mat("OchreBook",new Color(.39f,.21f,.058f),"Cover");
            rope=Mat("CottonRope",new Color(.81f,.67f,.45f),"Rope");brass=Mat("AgedBrass",new Color(.53f,.30f,.08f),"Cover",.58f);
            ink=Mat("TerracottaInk",new Color(.40f,.115f,.045f),"Paper");wood=Mat("DarkBinding",new Color(.15f,.062f,.021f),"Cover");glow=Mat("LanternPaper",new Color(1,.77f,.43f),"Paper",0,2.4f);
        }
        static Material Mat(string name,Color color,string texture,float metallic=0,float emission=0)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_BookTower/Textures/F0Reference/{texture}_Albedo.png"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_BookTower/Textures/F0Reference/{texture}_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",texture=="Paper"?.70f:.55f);
            m.SetFloat("_Smoothness",metallic>0?.32f:.18f);m.SetFloat("_Metallic",metallic);m.SetFloat("_Cull",0);
            m.SetColor("_EmissionColor",color*emission);if(emission>0)m.EnableKeyword("_EMISSION");else m.DisableKeyword("_EMISSION");m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static Texture2D WindowCookie()
        {
            string path=Folder+"/WindowCookie.asset";var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(t!=null)return t;
            int n=128;t=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Window lattice light",wrapMode=TextureWrapMode.Clamp};var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){bool frame=x<5||x>122||y<5||y>122||x%21<2||y%16<2;float a=frame?0:1;pixels[y*n+x]=new Color(a,a,a,a);}
            t.SetPixels(pixels);t.Apply();AssetDatabase.CreateAsset(t,path);return t;
        }
        static Transform Group(Transform p,string name,Vector3 at){var g=new GameObject(name).transform;g.SetParent(p,false);g.localPosition=at;return g;}
        static Transform MeshObject(Transform p,string name,Mesh mesh,Material material,Vector3 at)
        {var g=Group(p,name,at);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;meshes.Add(mesh);return g;}
        static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material material,bool collision=false,float bevel=.045f)
        {var g=MeshObject(p,name,F0Mesh.RoundedBox(size,bevel),material,at);if(collision)g.gameObject.AddComponent<BoxCollider>().size=size;return g;}
        static void Sphere(Transform p,string name,Vector3 at,Vector3 size,Material material)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void Tube(Transform p,string name,IList<Vector3> points,float radius,Material material)=>MeshObject(p,name,F0Mesh.Tube(points,radius),material,Vector3.zero);
        static void CombineDecorations(Transform parent,params Transform[] exclude)
        {
            var filters=parent.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Collider>()==null && !exclude.Any(e=>f.transform==e||f.transform.IsChildOf(e))).ToArray();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                var items=group.ToArray();if(items.Length<2)continue;
                var combine=items.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=parent.name+"_"+group.Key.name};mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();MeshObject(parent,"장식_"+group.Key.name,mesh,group.Key,Vector3.zero);
                foreach(var f in items){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
            }
        }
        static void SaveMeshes()
        {
            int serial=0;var all=room.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null && meshes.Contains(f.sharedMesh)).Select(f=>f.sharedMesh).Distinct().ToArray();
            foreach(var m in all)
            {
                string path=$"{Folder}/Mesh_{serial++:000}.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null)AssetDatabase.CreateAsset(m,path);
                else {EditorUtility.CopySerialized(m,existing);EditorUtility.SetDirty(existing);foreach(var f in room.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==m)f.sharedMesh=existing;}
            }
        }
        static void EnsureFolder(string path)
        {var parts=path.Split('/');string current=parts[0];for(int i=1;i<parts.Length;i++){string next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}}
    }
}
