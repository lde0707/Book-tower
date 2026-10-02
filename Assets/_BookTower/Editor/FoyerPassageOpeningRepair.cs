using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BookTower.Rooms;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    // Narrow repair: two passage endpoints only. Never invokes a room/scene builder.
    public static class FoyerPassageOpeningRepair
    {
        const string Folder="Assets/_BookTower/Generated/FoyerOpeningRepair";
        const string RootName="F0_문뒤_연속천장보강";
        static int serial;
        static Transform Part(Transform parent,string name,Mesh mesh,Material mat)
        {
            string path=Folder+"/Seal_"+(serial++).ToString("D2")+".asset";
            var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);mesh=previous;}else AssetDatabase.CreateAsset(mesh,path);
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.AddComponent<MeshCollider>().sharedMesh=mesh;return g.transform;
        }
        static Mesh Roof(float left,float right,float back,float front,float y)
        {
            const int nx=40,nz=12;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();int plane=(nx+1)*(nz+1);
            for(int layer=0;layer<2;layer++)for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float u=x/(float)nx,w=z/(float)nz,px=Mathf.Lerp(left,right,u),pz=Mathf.Lerp(back,front,w);
                float arch=.22f*Mathf.Sin(w*Mathf.PI),fold=.025f*Mathf.Sin(u*Mathf.PI*4)*Mathf.Sin(w*Mathf.PI);
                v.Add(new Vector3(px,y+arch+fold+layer*.25f,pz));uv.Add(new Vector2(px*.5f,pz*.5f));
            }
            void Q(int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*(nx+1)+x;Q(i,i+1,i+nx+2,i+nx+1);Q(i+plane,i+plane+nx+1,i+plane+nx+2,i+plane+1);}
            for(int x=0;x<nx;x++){Q(x,x+plane,x+plane+1,x+1);int k=nz*(nx+1)+x;Q(k,k+1,k+plane+1,k+plane);}
            for(int z=0;z<nz;z++){int k=z*(nx+1);Q(k,k+nx+1,k+nx+1+plane,k+plane);k+=nx;Q(k,k+plane,k+plane+nx+1,k+nx+1);}
            var mesh=new Mesh{name="Thick continuous folded vestibule ceiling"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        [MenuItem("Book Tower/Repair F0 doorway roof and 1A entrance decorations")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before applying passage repair");
            var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/InGame.unity")throw new Exception("Open InGame scene first");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory("Logs/FoyerOpeningBackup");
            EditorSceneManager.SaveScene(scene,"Logs/FoyerOpeningBackup/InGame-before.unity",true);
            var rooms=Object.FindObjectsByType<Room>(FindObjectsSortMode.None);var foyer=rooms.Single(r=>r.RoomId=="F0");var theatre=rooms.Single(r=>r.RoomId=="1A");
            var old=foyer.transform.Find(RootName);if(old)Object.DestroyImmediate(old.gameObject);
            serial=0;var roofRoot=new GameObject(RootName).transform;roofRoot.SetParent(foyer.transform,false);
            // The door leaves rise to 14m; sealing at 14.6m preserves their complete swing.
            var paper=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReleaseFinish/DeckledIvory.mat");
            var edge=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BookTower/Generated/ReleaseFinish/ExposedPageEdge.mat");
            if(!paper||!edge)throw new Exception("Approved paper palette missing");
            Part(roofRoot,"문 회전 공간 위를 잇는 두꺼운 장지",Roof(-6.9f,28.6f,15.65f,22.3f,14.6f),paper);
            Part(roofRoot,"꺾이는 회랑의 천장 이음 덮개",Roof(15.9f,28.75f,5.8f,16.2f,8.4f),paper);
            var south=Part(roofRoot,"모서리 회랑 남쪽 벽과 천장의 겹침 장지",F0Mesh.RoundedBox(new Vector3(12.85f,2.7f,.38f),.04f),paper);
            south.localPosition=new Vector3(22.25f,7.45f,6.2f);
            foreach(float z in new[]{15.8f,22.05f})
            {
                var line=new[]{new Vector3(-6.8f,14.52f,z),new Vector3(28.4f,14.52f,z)};
                Part(roofRoot,"천장 제본 단면",F0Mesh.Tube(line,.08f,8),edge);
            }
            // Close the step above the lower gallery, where upward diagonal views saw sky.
            foreach(var row in new[]{(new Vector3(10.8f,7.5f,22.05f),new Vector3(35.6f,15.5f,.32f)),(new Vector3(28.45f,7.5f,18.8f),new Vector3(.32f,15.5f,6.9f)),(new Vector3(-6.8f,7.5f,18.8f),new Vector3(.32f,15.5f,6.9f))})
            {var t=Part(roofRoot,"회랑 높이차를 닫는 접힌 상부 장지",F0Mesh.RoundedBox(row.Item2,.08f),paper);t.localPosition=row.Item1;}
            var alcove=theatre.transform.Find("ReleaseArt04_08_Craft/Side story alcove -1 0");
            if(!alcove)throw new Exception("Expected entrance alcove missing");
            int removed=0;
            foreach(var filter in alcove.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;var verts=source.vertices;var triangles=source.triangles;var kept=new List<int>();
                bool Intrudes(int i){var p=alcove.InverseTransformPoint(filter.transform.TransformPoint(verts[i]));return Mathf.Abs(p.x)<3.66f&&p.y>1.2f&&p.y<6.1f&&p.z>-.9f&&p.z<1f;}
                for(int i=0;i<triangles.Length;i+=3)
                {if(Intrudes(triangles[i])&&Intrudes(triangles[i+1])&&Intrudes(triangles[i+2]))removed++;else kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
                if(kept.Count==triangles.Length)continue;
                // Copy only this entrance mesh; preserve all other alcoves and shared assets.
                var mesh=Object.Instantiate(source);mesh.name=source.name+" clear doorway";mesh.triangles=kept.ToArray();mesh.RecalculateBounds();
                string path=Folder+"/Entrance_"+filter.name.Replace(' ','_')+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(previous){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);mesh=previous;}else AssetDatabase.CreateAsset(mesh,path);
                filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
            }
            foreach(var t in alcove.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Layered shelf lip"||t.name=="Faint crayon relief").ToArray())Object.DestroyImmediate(t.gameObject);
            // These raised guide leaves floated across the narrow arrival sightline.
            // Keep the motifs inside the room; clear only the entrance gallery.
            int leaves=0;
            foreach(var t in theatre.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="별빛을 따라가는 바닥 잎").ToArray())
            {
                var p=theatre.transform.InverseTransformPoint(t.position);
                if(p.x < -20 && p.z > -16 && p.z < -8){Object.DestroyImmediate(t.gameObject);leaves++;}
            }
            var passage=foyer.transform.parent.Find("V4_서문에서동화로");
            if(passage)foreach(var r in passage.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.name!="다음 방 안내 잎"&&r.name!="붉은 리본에서 청록 줄기로")continue;
                var p=theatre.transform.InverseTransformPoint(r.bounds.center);
                if(p.x > -38 && p.x < -20){Object.DestroyImmediate(r.gameObject);leaves++;}
            }
            // Deduplicate only the known ceiling prefab, preserving all other scene roots.
            GameObject retained=null;int copies=0;
            foreach(var root in scene.GetRootGameObjects().Where(g=>g.name==FoyerPassageCeiling.RootName||g.name=="FoyerPassageCeiling"))
            {if(!retained){retained=root;root.name=FoyerPassageCeiling.RootName;}else {Object.DestroyImmediate(root);copies++;}}
            Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/ArtReview/FoyerPassageOpening");File.WriteAllText("Docs/ArtReview/FoyerPassageOpening/repair.txt",$"Targeted roof and entrance repair. Entrance decoration triangles removed={removed}; raised entry leaves removed={leaves}; duplicate ceiling instances removed={copies}. Existing puzzle transforms untouched.\n");
        }
        public static void Run(){try{EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");Apply();FoyerPassageOpeningReview.Verify();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
