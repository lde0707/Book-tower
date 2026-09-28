using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Rooms;
using BookTower.Puzzles;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        public const string IdentityRootName = "F0_종이공간_완성아트";
        static Transform identityRoot;
        static readonly List<Transform> identityMeshRoots = new();

        [MenuItem("Book Tower/F0 네 방향 종이 공간 적용")]
        public static void ApplyPaperIdentity()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            var scene = EditorSceneManager.GetActiveScene();
            if(scene.path != "Assets/Scenes/InGame.unity")throw new InvalidOperationException("Open InGame first");
            Directory.CreateDirectory("Logs/F0PaperIdentityBackups");
            EditorSceneManager.SaveScene(scene,"Logs/F0PaperIdentityBackups/InGame-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            room = Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            var architecture = room.transform.Find(RootName);
            if(!architecture || !room.GetComponentInChildren<FoldBridgePuzzle>())throw new InvalidOperationException("Expected current F0 light-bridge layout");
            var previous = architecture.Find(IdentityRootName); if(previous)Object.DestroyImmediate(previous.gameObject);
            IdentityMaterials(); meshes.Clear(); identityMeshRoots.Clear();
            var door = architecture.GetComponent<BindingDoor>();
            var puzzleRoot = architecture.Find("접는 다리와 빛");
            var props = architecture.Find("F0_개별공예소품");
            var atmosphere = architecture.Find("F0_창빛과먼지");
            // Keep collision, authored references and moving transforms. Retire only static render
            // components: forceRenderingOff is transient and is lost after a scene reload.
            foreach(var r in architecture.GetComponentsInChildren<Renderer>(true))
            {
                bool retained = (door && (r.transform.IsChildOf(door.left)||r.transform.IsChildOf(door.right))) ||
                    (puzzleRoot&&r.transform.IsChildOf(puzzleRoot)) || (props&&r.transform.IsChildOf(props)) ||
                    (atmosphere&&r.transform.IsChildOf(atmosphere));
                if(retained && !(atmosphere&&r.transform.IsChildOf(atmosphere)))
                {
                    r.sharedMaterials=r.sharedMaterials.Select(IdentityReplacement).ToArray();
                    if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                if(!retained)
                {
                    var filter=r.GetComponent<MeshFilter>();
                    Object.DestroyImmediate(r);
                    if(filter)Object.DestroyImmediate(filter);
                }
            }
            foreach(var light in architecture.GetComponentsInChildren<Light>(true))
                if(!light.transform.IsChildOf(puzzleRoot))light.intensity=0;
            // The old flat surface overlays are replaced by consistently spaced laminated sheets.
            foreach(var r in puzzleRoot.GetComponentsInChildren<Renderer>())
                if(new[]{"시작 책장 대지","건너편 책장 대지","골 가장자리 제본 턱","거칠게 뜯긴 장지 표면"}.Any(n=>r.name==n))Object.DestroyImmediate(r);
            root=identityRoot=Group(architecture,IdentityRootName,Vector3.zero);
            identityMeshRoots.Add(root);
            IdentityFloor();
            IdentityWalls();
            IdentityColumnsAndBooks();
            IdentityRopesAndLamps();
            IdentityTransitionShell();
            IdentityBridge(room.GetComponentInChildren<FoldBridgePuzzle>());
            IdentityOptionalProps();
            IdentityLighting();
            // Bound combine size by spatial zone to retain useful camera culling.
            foreach(Transform zone in root)IdentityCombineStatic(zone);
            IdentitySaveMeshes();
            EditorUtility.SetDirty(room);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            F0PaperIdentityVerifier.CheckStructure();
        }

        public static void BuildPaperIdentityBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            ApplyPaperIdentity();
            F0PlayVerifier.Run();
        }

        static Transform IdentityZone(string name)=>Group(identityRoot,name,Vector3.zero);

        static void IdentityFloor()
        {
            foreach(bool far in new[]{false,true})
            {
                var zone=IdentityZone(far?"02_건너편_장지대지":"01_시작_장지대지");
                float z0=far?7.2f:-20,depth=far?4.4f:18.8f;
                Box(zone,"종이대지_적층단면",V(0,1.47f,z0+depth/2),V(32,2.94f,depth),edge);
                int rows=far?1:5;float dz=depth/rows;
                for(int row=0;row<rows;row++)for(int x=0;x<8;x++)
                {
                    float y=3.022f+(x+row)%3*.003f;
                    var sheet=MeshObject(zone,"독립_장지층",IdentitySheet(4.015f,dz+.025f,82+x+row*11,.012f),((x+row)%5==0)?paper:ivory,V(-14+x*4,y,z0+row*dz));
                    sheet.localRotation=Quaternion.Euler(90,0,0);
                    for(int k=0;k<7;k++)
                    {
                        float z=z0+row*dz+.30f+k*(dz-.6f)/6;
                        Tube(zone,"바닥_수제바늘땀",new[]{V(-15.97f+x*4,3.037f,z-.07f),V(-15.91f+x*4,3.063f,z),V(-15.86f+x*4,3.037f,z+.07f)},.012f,rope);
                    }
                }
                // Solid inner core remains below the paper: small deckle gaps never become holes.
                Box(zone,"장지밑_연속속지",V(0,2.994f,z0+depth/2),V(31.96f,.022f,depth),paper);
                foreach(float x in new[]{-2.65f,2.65f})
                    Box(zone,"낡은_먹인쇄_세로선",V(x,3.052f,z0+depth/2),V(.055f,.008f,depth-.25f),ink);
                for(int strand=-1;strand<=1;strand++)
                    Tube(zone,"바닥_제본_가는실",new[]{V(strand*.09f,3.064f,z0+.14f),V(strand*.09f,3.064f,z0+depth-.14f)},.018f,rope);
                for(float z=z0+2;z<z0+depth-.8f;z+=4.3f)
                {
                    foreach(int side in new[]{-1,1})
                    {
                        var mark=Group(zone,"잎_판화",V(side*2.30f,3.050f,z));mark.localRotation=Quaternion.Euler(90,0,side*9);mark.localScale=V(1,1,.05f);Sprig(mark,Vector3.zero,.90f,ink);
                    }
                }
                float rim=far?7.22f:-1.22f;
                foreach(int side in new[]{-1,1})
                {
                    Box(zone,"대지앞_둥근제본테두리",V(side*9.2f,2.91f,rim),V(13.55f,.20f,.24f),oxblood,false,.085f);
                    for(int layer=0;layer<9;layer++)
                        Box(zone,"절벽_층층이보이는속지",V(side*9.2f,2.60f-layer*.24f,rim+(far?.06f:-.06f)),V(13.52f,.021f,.033f),layer%3==0?paper:edge);
                    for(int k=0;k<12;k++)
                    {
                        float x=side*(2.7f+k*1.06f);Tube(zone,"절벽_감싸는봉제",new[]{V(x,3.044f,rim+(far?.16f:-.16f)),V(x,3.08f,rim),V(x,2.8f,rim+(far?-.15f:.15f))},.028f,rope);
                    }
                }
            }
        }

        // Shallow physical folds with modest edge curl, not stone-like random displacement.
        static Mesh IdentitySheet(float width,float height,int seed,float amplitude)
        {
            int nx=Mathf.CeilToInt(width*6),ny=Mathf.CeilToInt(height*6);
            var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
            {
                float u=x/(float)nx,t=y/(float)ny,px=(u-.5f)*width,py=t*height;
                float n=Mathf.PerlinNoise(px*1.3f+seed,py*1.1f+seed*.71f);
                float border=(x==0||x==nx||y==0||y==ny)?1:0;
                float fold=Mathf.Sin(u*11+t*4+seed)*.45f+Mathf.Sin(t*16-u*3)*.25f;
                v.Add(V(px+border*(n-.5f)*.023f,py+border*(n-.5f)*.024f,-amplitude*(fold+.35f*border)));
                uv.Add(new Vector2(px*.53f+seed*.13f,py*.53f));
                if(x<nx&&y<ny){int q=y*(nx+1)+x;tris.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}
            }
            var mesh=new Mesh{name="Soft folded mulberry sheet"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();return mesh;
        }

        static void IdentityWalls()
        {
            foreach(int side in new[]{-1,1})
            {
                var zone=IdentityZone(side<0?"03_서벽_단서와창":"04_동벽_서가와창");
                Box(zone,"제본_연속외피",V(side*16.03f,8.10f,-2),V(.22f,16.4f,36.3f),wood);
                foreach(float z in new[]{-17.3f,-10.5f,.05f,10.4f,14})
                {
                    var panel=Group(zone,"독립장지_벽감",V(side*15.84f,3.10f,z));panel.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
                    Panel(panel,z==14?2.5f:2.7f,11.7f);
                    var sprig=Group(panel,"벽감_잎압인",V(0,7,-.32f));Sprig(sprig,Vector3.zero,2.1f,ink);
                }
                foreach(float z in new[]{-5.2f,5.4f})IdentityWindow(zone,side,z);
                foreach(float z in new[]{-17.2f,-.1f,13.6f})
                {
                    var shelf=Group(zone,"깊은_붙박이장서",V(side*15.45f,3.10f,z));shelf.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
                    ShelfBay(shelf,2.65f,8.9f);
                }
                for(int i=0;i<4;i++)Box(zone,"벽천장_접힌처마",V(side*(15.65f-i*.12f),15.25f+i*.19f,-2),V(.6f+i*.14f,.14f,36),i==2?brass:wood);
            }
            var south=IdentityZone("05_남벽_닫힌제본표지");
            Box(south,"막힌_후면표지",V(0,8.10f,-20.02f),V(32.3f,16.4f,.22f),wood);
            for(int i=-3;i<=3;i++)
            {
                var p=Group(south,"후면_종이벽패널",V(i*4.35f,3.05f,-19.85f));p.localRotation=Quaternion.Euler(0,180,0);
                Panel(p,3.84f,11.95f);Sprig(p,V(0,4.4f,-.36f),2.8f,ink);
            }
            // Continuous horizontal bindings distinguish the rear wall from the actual exit door.
            foreach(float y in new[]{3.32f,6.3f,12.3f,15.2f})
                Box(south,"후면_끊기지않는제본띠",V(0,y,-19.45f),V(31.7f,.20f,.24f),wood);
            var north=IdentityZone("06_북벽_출구프레임");
            foreach(int side in new[]{-1,1})
            {
                Box(north,"출구옆_연속제본외피",V(side*11.2f,8.10f,15.51f),V(10.1f,16.4f,.18f),wood);
                var p=Group(north,"문옆_입체종이패널",V(side*11.1f,3.08f,15.32f));Panel(p,7.75f,11.7f);Sprig(p,V(0,5,-.35f),3.3f,ink);
                Box(north,"출구_세로제본틀",V(side*6.03f,6.84f,15.00f),V(.30f,13.75f,.42f),brass);
            }
            Box(north,"출구위_연속제본외피",V(0,15.0f,15.51f),V(12.5f,2.6f,.18f),wood);
            foreach(float y in new[]{13.95f,14.28f,14.65f,15.10f})Box(north,"출구_접힌인방",V(0,y,15.05f),V(13,.18f,.66f),y==14.28f?brass:wood);
            for(int i=-5;i<=5;i++)Sprig(north,V(i*1.10f,14.84f,14.68f),.58f,identityGold);
            var ceiling=IdentityZone("07_천장_겹표지");
            Box(ceiling,"천장_연속속지",V(0,16.17f,-2),V(32,.15f,36),paper);
            foreach(float z in new[]{-18f,-10f,-2f,6f,14f})
            {
                Box(ceiling,"가로_천장제본보",V(0,15.85f,z),V(32,.40f,.60f),wood);
                Box(ceiling,"보_금박인쇄선",V(0,15.61f,z-.28f),V(31.8f,.045f,.045f),identityGold);
            }
            foreach(float x in new[]{-15.5f,-7.8f,7.8f,15.5f})Box(ceiling,"세로_천장제본보",V(x,15.98f,-2),V(.45f,.30f,36),wood);
        }

        static void IdentityWindow(Transform zone,int side,float z)
        {
            var p=Group(zone,"깊은_격자장지창",V(side*15.86f,4.4f,z));p.localRotation=Quaternion.Euler(0,side<0?-90:90,0);
            Box(p,"창_깊은음영턱",V(0,4.6f,.12f),V(4.9f,9.75f,.36f),wood);
            Box(p,"빛을투과하는_장지",V(0,4.6f,-.1f),V(4.28f,9.05f,.06f),identityWindow);
            foreach(float x in new[]{-2.36f,2.36f})Box(p,"두꺼운_창세로테",V(x,4.6f,-.31f),V(.23f,9.85f,.6f),oxblood);
            foreach(float y in new[]{-.22f,9.42f})Box(p,"두꺼운_창가로테",V(0,y,-.31f),V(4.92f,.23f,.6f),wood);
            for(int i=-2;i<=2;i++)Box(p,"창_세로살",V(i*.72f,4.6f,-.38f),V(.09f,9.2f,.12f),wood);
            for(int i=0;i<=12;i++)Box(p,"창_가로살",V(0,.16f+i*.74f,-.38f),V(4.4f,.08f,.12f),wood);
            var leaf=Group(p,"창호지_잎그림자",V(.86f,3.15f,-.15f));Sprig(leaf,Vector3.zero,3.6f,ochre);
            Box(p,"겹친종이_창턱",V(0,-.37f,-.57f),V(5.0f,.20f,1.1f),oxblood);
            for(int i=0;i<5;i++)FlatBook(p,V(-1.15f+i*.04f,-.26f+i*.19f,-.50f),1.80f,.16f,.95f,i%2==0?umber:oxblood,i*2);
        }

        static void IdentityColumnsAndBooks()
        {
            foreach(int side in new[]{-1,1})
            {
                var zone=IdentityZone(side<0?"08_서쪽_책등과장서":"09_동쪽_책등과장서");
                foreach(float z in new[]{-11.8f,11.1f})IdentityColumn(zone,V(side*10.65f,3,z),12.4f,1.2f);
                foreach(float z in new[]{-13f,-3f,10.8f})
                {
                    Book(zone,"벽을이루는_거대책",V(side*13.4f,0,z),3.6f,15.7f,3.3f,oxblood,side<0?-90:90);
                    Book(zone,"벽틈_거대책",V(side*15.6f,0,z+.4f),1,15.3f,3.2f,umber,side*3);
                }
                foreach(float z in new[]{-16.7f,-.1f,10.5f})
                {
                    for(int layer=0;layer<5;layer++)FlatBook(zone,V(side*(13.1f+Mathf.Sin(layer)*.05f),3.04f+layer*.30f,z),3.7f,.27f,2.0f,layer%2==0?umber:oxblood,(layer%3-1)*3);
                }
            }
            // The replacement books own solid, correctly oriented collision. Retired books must
            // not leave a second invisible footprint at the old orientation.
            foreach(var old in room.transform.Find(RootName).GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("거대제본책_")))
                foreach(var collider in old.GetComponentsInChildren<Collider>())
                {
                    collider.enabled=false;
                    if(PrefabUtility.IsPartOfPrefabInstance(collider))PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                }
            var propsZone=IdentityZone("10_겹장지_소품과낮은아치");
            // Right table is raised to the actual playable floor instead of being buried in it.
            Table(propsZone,V(12,3,2.8f));
            for(int i=0;i<14;i++)
            {
                var page=MeshObject(propsZone,"우측탁자_겹친낱장",IdentitySheet(3.7f,2.0f,310+i,.020f),i%4==0?paper:ivory,V(12+Mathf.Sin(i)*.035f,6.65f+i*.041f,1.8f));page.localRotation=Quaternion.Euler(90,i%3-1,0);
            }
            // Keep the low paper alcove at the perimeter, clear of the shutter-control approach.
            Transform saved=root;root=propsZone;int count=propsZone.childCount;PaperArch(V(-12.6f,3.02f,-16.0f));root=saved;
            var arch=propsZone.GetChild(count);arch.localRotation=Quaternion.Euler(0,28,0);
            for(int i=0;i<24;i++)
            {
                float x=(i%2==0?-1:1)*(8.7f+(i%4)*1.25f),z=-18.0f+(i/4)*2.2f;
                var flake=MeshObject(propsZone,"가장자리_작은종이조각",IdentitySheet(.16f+(i%3)*.06f,.23f,400+i,.023f),paper,V(x,3.06f,z));flake.localRotation=Quaternion.Euler(90,i*37,0);
            }
        }

        static void IdentityColumn(Transform p,Vector3 at,float height,float radius)
        {
            var g=Group(p,"둥근_적갈색종이책등",at);
            var profile=new List<Vector2>{new(0,0),new(radius*1.38f,0),new(radius*1.40f,.08f),new(radius*1.40f,.26f),new(radius*1.1f,.42f)};
            float[] rings={.55f,.86f,2.95f,5.03f,8.02f,10.16f,11.7f,12.17f};
            for(float y=.44f;y<height;y+=.085f)
            {
                float rad=radius;foreach(float ring in rings){float d=Mathf.Abs(y-ring);if(d<.17f)rad+=.12f*Mathf.Sqrt(1-d*d/(.17f*.17f));}profile.Add(new(rad,y));
            }
            profile.Add(new(radius,height));profile.Add(new(0,height));
            var shell=TurnedProfile(profile,height);
            var uv=shell.uv;for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(uv[i].x*4,uv[i].y*6);shell.uv=uv;
            MeshObject(g,"곡면_염색장지책등",shell,oxblood,Vector3.zero);
            var belt=new List<Vector2>{new(radius+.01f,5.15f),new(radius+.10f,5.20f),new(radius+.10f,6.10f),new(radius+.01f,6.15f)};
            MeshObject(g,"무광_황동제본띠",TurnedProfile(belt,height),brass,Vector3.zero);
            for(int face=0;face<8;face++)
            {
                float a=face*Mathf.PI/4;FlowerInlay(g,a,5.65f,radius+.112f);
                foreach(float y in new[]{5.30f,6.0f})Sphere(g,"작은_황동리벳",V(Mathf.Cos(a)*(radius+.12f),y,Mathf.Sin(a)*(radius+.12f)),V(.06f,.06f,.06f),brass);
                var motif=Group(g,"종이_금박압인",Vector3.zero);motif.localRotation=Quaternion.Euler(0,face*45,0);
                foreach(float y in new[]{1.28f,3.55f,6.85f,9.0f,10.65f})Sprig(motif,V(0,y,-radius-.03f),.88f,identityGold);
            }
        }

        static void IdentityRopesAndLamps()
        {
            var zone=IdentityZone("11_제본실과매달린종이등");
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-6f,10.4f})IdentityRopePost(zone,V(side*7.7f,3,z),.8f);
            IdentityRopePost(zone,V(12.2f,3,-9),1.1f);
            Transform saved=root;root=zone;
            foreach(var item in new[]{V(-9.1f,11.1f,-4),V(11.9f,11.3f,-4.5f),V(-10,12,8.5f),V(9,12,-13),V(10.2f,11,8.5f)})Lantern(item,.85f);
            root=saved;
        }

        static void IdentityRopePost(Transform zone,Vector3 at,float scale)
        {
            Box(zone,"책표지_밧줄소켓",at+V(0,.20f,0),V(1.7f,.40f,1.6f)*scale,umber,true,.14f);
            Box(zone,"소켓_황동테",at+V(0,.43f,0),V(1.35f,.16f,1.25f)*scale,brass);
            for(int strand=-1;strand<=1;strand++)Rope(zone,"면실_세겹꼬임",at+V(strand*.20f*scale,.48f,0),V(at.x+strand*.20f*scale,15.95f,at.z),.083f*scale);
            Knot(zone,at+V(0,1.67f*scale,0),.40f*scale,false);
            Knot(zone,V(at.x,15.35f,at.z),.32f*scale,false);
        }

        static void IdentitySaveMeshes()
        {
            int serial=0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var f in identityMeshRoots.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).Distinct())
                {
                    var mesh=f.sharedMesh;if(!mesh||AssetDatabase.Contains(mesh))continue;
                    string path=IdentityFolder+"/Mesh_"+(serial++).ToString("000")+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved)
                    {
                        // Rebuild vertex/index buffers explicitly. CopySerialized on an existing
                        // Unity 6 combined Mesh can retain stale GPU/LOD buffer state after import.
                        saved.Clear();saved.indexFormat=mesh.indexFormat;saved.vertices=mesh.vertices;
                        saved.normals=mesh.normals;saved.tangents=mesh.tangents;saved.uv=mesh.uv;
                        saved.triangles=mesh.triangles;saved.bounds=mesh.bounds;saved.name=mesh.name;
                        saved.UploadMeshData(false);f.sharedMesh=saved;EditorUtility.SetDirty(saved);
                    }
                    else AssetDatabase.CreateAsset(mesh,path);
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            var used=identityMeshRoots.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).ToHashSet();
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{IdentityFolder}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(!used.Contains(path))AssetDatabase.DeleteAsset(path);
            }
        }

        static void IdentityCombineStatic(Transform zone)
        {
            // Render meshes can be merged while their independent BoxColliders stay in place.
            var filters=zone.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()).ToArray();
            foreach(var f in filters)
            {
                Vector3 size=f.sharedMesh.bounds.size;
                if(f.sharedMesh.name=="Beveled book geometry" && Mathf.Min(size.x,Mathf.Min(size.y,size.z))<.085f)
                    f.sharedMesh=IdentitySlimBox(size);
            }
            foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var pieces=group.ToArray();if(pieces.Length<2)continue;
                var mesh=new Mesh{name=zone.name+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(pieces.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=zone.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                MeshObject(zone,"공간_"+group.Key.name,mesh,group.Key,Vector3.zero);
                foreach(var f in pieces){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
            }
        }

        static Mesh IdentitySlimBox(Vector3 size)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                Vector3 u=normal.y!=0?Vector3.right:Vector3.Cross(Vector3.up,normal),v=Vector3.Cross(normal,u);
                float Extent(Vector3 d)=>Vector3.Dot(new Vector3(Mathf.Abs(d.x),Mathf.Abs(d.y),Mathf.Abs(d.z)),size)*.5f;
                float a=Extent(u),b=Extent(v),c=Extent(normal);int first=vertices.Count;
                foreach(var point in new[]{new Vector2(-a,-b),new Vector2(a,-b),new Vector2(a,b),new Vector2(-a,b)})
                {vertices.Add(normal*c+u*point.x+v*point.y);uv.Add(point*.65f);}
                triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            var mesh=new Mesh{name="Slim laminated paper edge"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();return mesh;
        }

        static void IdentityTransitionShell()
        {
            var zone=IdentityZone("13_출구뒤_연속장지복도");
            // Lower-level surfaces continue beneath the raised banks and into the existing 1A passage.
            Box(zone,"연속_기초장지",V(0,-.012f,0),V(55,.06f,44),paper);
            Box(zone,"북쪽복도_연속벽",V(0,8,21.72f),V(56,16,.12f),umber);
            Box(zone,"동쪽출구_남측표지",V(27.71f,4,-6),V(.12f,8,29.6f),umber);
            Box(zone,"동쪽출구_북측표지",V(27.71f,4,18),V(.12f,8,9.2f),umber);
            Box(zone,"동쪽출구_상부표지",V(27.71f,7,11.2f),V(.12f,2,5.6f),wood);
            foreach(float x in new[]{-12f,-6f,0f,6f,12f,18f,24f})
            {
                var p=Group(zone,"복도_겹장지벽감",V(x,.12f,21.60f));Panel(p,5.5f,7.6f);
            }
            Rope(zone,"출구뒤_길잡이실",V(3,.09f,18.3f),V(20,.09f,18.3f),.035f);
            Rope(zone,"출구꺾임_길잡이실",V(20,.09f,18.3f),V(20,.09f,11.2f),.035f);
            Rope(zone,"다음방_길잡이실",V(20,.09f,11.2f),V(28,.09f,11.2f),.035f);
        }
    }
}
