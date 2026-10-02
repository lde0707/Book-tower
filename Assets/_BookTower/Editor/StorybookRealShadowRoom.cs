using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class StorybookRoomBuilder
    {
        public static void RealShadowBatch(){EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");ApplyWorkshop();WorkshopVerifier.Run();}
        static void RealShadowRoom(StorybookLightPuzzle w)
        {
            w.realShadows=true;
            if(w.bodyShadow)Object.DestroyImmediate(w.bodyShadow.gameObject);if(w.earShadow)Object.DestroyImmediate(w.earShadow.gameObject);
            if(w.bodyPickup)Object.DestroyImmediate(w.bodyPickup.gameObject);if(w.earPickup)Object.DestroyImmediate(w.earPickup.gameObject);
            foreach(var c in root.GetComponentsInChildren<StorybookLightControl>())if(c.action!=StorybookLightControl.Action.Lamp&&c.action!=StorybookLightControl.Action.Toy)Object.DestroyImmediate(c.gameObject);
            foreach(var t in w.stage.GetComponentsInChildren<Transform>().ToArray())if(t&&(t.name.Contains("손잡이")||t.name=="누빈 작은 받침"))Object.DestroyImmediate(t.gameObject);
            w.body.localPosition=w.BodyPosition;w.ears.localPosition=w.EarPosition;w.body.gameObject.SetActive(true);w.ears.gameObject.SetActive(true);w.ears.localScale=new Vector3(-1,1,1);
            AddGrab(w.body,w,false);AddGrab(w.ears,w,true);
            var player=Object.FindFirstObjectByType<BookTower.PlayerSystem.PlayerController>();if(!player.GetComponent<PaperShadowDrag>())player.gameObject.AddComponent<PaperShadowDrag>();
            var light=w.lantern;light.type=LightType.Spot;light.transform.localPosition=V(0,1,0);light.transform.localRotation=Quaternion.identity;light.spotAngle=76;light.innerSpotAngle=62;light.range=24;light.intensity=160;light.shadows=LightShadows.Hard;light.shadowBias=.012f;light.shadowNormalBias=.015f;light.shadowNearPlane=.15f;light.shadowResolution=LightShadowResolution.VeryHigh;light.renderMode=LightRenderMode.ForcePixel;
            light.gameObject.name="실제 그림자 투사 광원";
            var data=light.GetUniversalAdditionalLightData();var lightData=new SerializedObject(data);lightData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;lightData.ApplyModifiedPropertiesWithoutUndo();
            foreach(var l in root.GetComponentsInChildren<Light>())if(l!=light&&root.InverseTransformPoint(l.transform.position).z>10)l.intensity=Mathf.Min(l.intensity,5);
            foreach(var r in w.stage.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            foreach(var cutout in new[]{w.body,w.ears})foreach(var r in cutout.GetComponentsInChildren<Renderer>())r.shadowCastingMode=r.name.StartsWith("잘라낸")||r.name.StartsWith("겹친 오림")?ShadowCastingMode.TwoSided:ShadowCastingMode.Off;
            // The lamp's flower petals must not obstruct its forward beam.
            foreach(var r in light.transform.parent.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            var screen=w.stage.Find("질긴 투영 스크린");screen.GetComponent<Renderer>().receiveShadows=true;
            var screenMat=WorkshopMat("11_RealShadowScreen",new Color(.95f,.88f,.70f));screenMat.SetFloat("_ReceiveShadows",1);screenMat.DisableKeyword("_RECEIVE_SHADOWS_OFF");screen.GetComponent<Renderer>().sharedMaterial=screenMat;
            foreach(var p in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Mobile_RPAsset.asset"})
            {var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p);var so=new SerializedObject(asset);so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=4096;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);}
            foreach(var note in root.GetComponentsInChildren<BookTower.UI.ReadableNote>())if(note.id=="1A.Book.Scale"||note.id=="1A.Book.Theatre")note.pages=new[]{"등 앞의 오림을 직접 잡아 움직여 보자. 등에 가까우면 그림자가 커지고, 벽에 가까우면 작아진다.","좌클릭을 누른 채 끌거나 F로 잡고 다시 F로 놓는다. 귀를 잡은 채 휠 또는 우클릭을 누르면 앞뒤가 바뀐다.","토끼 몸통과 귀의 크기뿐 아니라 옆으로 어긋난 위치도 맞춘다. 손을 놓고 실제 그림자가 벽의 선에 들어오는지 살펴보자."};
            ReshapePicturebook();
        }
        static void AddGrab(Transform t,StorybookLightPuzzle w,bool ears)
        {
            var it=t.gameObject.AddComponent<Interactable>();var so=new SerializedObject(it);so.FindProperty("label").stringValue=ears?"토끼 귀 오림":"토끼 몸통 오림";so.FindProperty("verb").stringValue="잡아 끌기 · 귀는 휠로 뒤집기";so.FindProperty("range").floatValue=6;so.FindProperty("lift").floatValue=0;so.FindProperty("animatePosition").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            // Collider stays with the real movable cutout; no remote stand-in button.
            var c=t.gameObject.AddComponent<BoxCollider>();c.center=ears?V(0,1.12f,0):V(0,.95f,0);c.size=ears?V(.72f,1.1f,.22f):V(1.7f,2.1f,.22f);
            var handle=t.gameObject.AddComponent<PaperShadowHandle>();handle.puzzle=w;handle.isEar=ears;
        }
        static Material MuralMat(int quadrant)
        {
            var mat=WorkshopMat("12_CrayonMural_"+quadrant,new Color(.87f,.75f,.56f));mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/StorybookV2/CrayonMuralAtlas.png"));
            mat.SetTextureScale("_BaseMap",new Vector2(.48f,.48f));mat.SetTextureOffset("_BaseMap",new Vector2(quadrant%2*.5f+.01f,quadrant<2?.51f:.01f));mat.SetFloat("_BumpScale",.25f);return mat;
        }
        static void Mural(Transform parent,Vector3 at,float width,float height,int image,float yaw=0)
        {
            var mesh=F0Mesh.Paper(width,height,seed++);var verts=mesh.vertices;mesh.uv=verts.Select(p=>new Vector2(p.x/width+.5f,p.y/height)).ToArray();
            var t=Obj(parent,"손그림 크레파스 벽화_"+image,mesh,MuralMat(image),at);t.localRotation=Quaternion.Euler(0,yaw,0);
        }
        static void ReshapePicturebook()
        {
            string[] retired={"구역 분리 접힌 벽","아치 위 찢긴 벽","왼쪽 벽지","오른쪽 벽지","종이 벽감과 식물 장식","동화 창틀과 압화","천장 제본 리브","겹친 그림책 더미","청록 크레파스 잎","접힌 골판지 벽기둥"};
            foreach(Transform t in root.Cast<Transform>().ToArray())
            {
                bool oldPartitionArch=t.name=="겹겹이 찢어진 페이지 아치"&&(Mathf.Abs(t.localPosition.z+7.42f)<.1f||Mathf.Abs(t.localPosition.z-9.58f)<.1f);
                if(retired.Contains(t.name)||t.name.StartsWith("크레파스 ")||oldPartitionArch||t.name=="다음 장을 가리키는 잎") {modules.Remove(t);Object.DestroyImmediate(t.gameObject);continue;}
                if(t.name.StartsWith("제본 서랍")||t.name.StartsWith("청록 그림책")||t.name.StartsWith("층층 종이책")||t.name=="그림책 종이 러그")
                {var p=t.localPosition;p.x=Mathf.Clamp(p.x,-13,13);
                    if(t.name.StartsWith("층층 종이책")){p.x=p.x<0?-13:13;p.z=p.z<0?-15:(p.x>0?10:18);}
                    t.localPosition=p;
                    if(t.name.StartsWith("청록 그림책"))t.localRotation=Quaternion.Euler(0,72,0);
                }
            }
            modules.RemoveAll(t=>!t);
            // Remove the obsolete rails: objects now move freely across this paper mat.
            foreach(var t in root.GetComponentsInChildren<Transform>().ToArray())if(t&&t.name=="휘어진 종이 레일")Object.DestroyImmediate(t.gameObject);
            var interior=Group(root,"레퍼런스 곡면 그림책 실내",Vector3.zero);
            // Rounded perimeter replaces the long box corridors. Backing shell remains sealed.
            var loop=new List<Vector3>();
            Vector2[] centers={new(12,-18),new(12,21),new(-12,21),new(-12,-18)};
            for(int corner=0;corner<4;corner++)for(int i=0;i<=12;i++)
            {float angle=(-90+corner*90+i*7.5f)*Mathf.Deg2Rad;var c=centers[corner];loop.Add(V(c.x+6*Mathf.Cos(angle),0,c.y+6*Mathf.Sin(angle)));}
            var originalLoop=loop.ToArray();loop.Clear();
            for(int k=0;k<originalLoop.Length;k++){var a=originalLoop[k];var b=originalLoop[(k+1)%originalLoop.Length];int n=Mathf.CeilToInt(Vector3.Distance(a,b)/1.5f);for(int j=0;j<n;j++)loop.Add(Vector3.Lerp(a,b,(float)j/n));}
            for(int i=0;i<loop.Count;i++)
            {
                var a=loop[i];var b=loop[(i+1)%loop.Count];var mid=(a+b)*.5f;var d=b-a;if(d.magnitude<.01f)continue;
                if(mid.x<-17&&mid.z>-18&&mid.z<-7)continue;
                if(mid.x>12&&mid.z>17&&mid.z<27)continue;
                if(mid.z>26&&mid.x>-13&&mid.x<4)continue; // deep theatre alcove
                float h=10.6f;var vertices=new List<Vector3>{a,b,b+Vector3.up*h,a+Vector3.up*h};
                var uv=new List<Vector2>{new(0,0),new(d.magnitude*.48f,0),new(d.magnitude*.48f,h*.48f),new(0,h*.48f)};
                Obj(interior,"구불어진 장지 내벽",Mesh(vertices,uv,new List<int>{0,1,2,0,2,3}),cream,Vector3.zero,true);
                if(d.magnitude>5)
                {
                    int count=Mathf.CeilToInt(d.magnitude/6);
                    for(int j=0;j<count;j++)
                    {
                        var spot=Vector3.Lerp(a,b,(j+.5f)/count);var normal=Vector3.Cross(d.normalized,Vector3.up);spot+=normal*.1f;
                        float yaw=Quaternion.LookRotation(-normal).eulerAngles.y;
                        Mural(interior,spot+Vector3.up*1.2f,Mathf.Min(5.5f,d.magnitude/count-.2f),6.5f,(j+i)%3,yaw);
                        Arch(interior,spot,2.7f,4.2f,yaw);
                    }
                }
            }
            foreach(float x in new[]{-17.8f,17.8f})foreach(float z in new[]{-1f,9f})
            {Mural(interior,V(x,.8f,z),7,7,x<0?0:2,x<0?90:-90);Arch(interior,V(x,0,z),3.6f,4,x<0?90:-90);}
            Mural(interior,V(0,.7f,-23.8f),10,7,2,180);Arch(interior,V(0,0,-23.7f),5.1f,3.3f,180);
            // A barrel ceiling has actual curvature and receives a repeated crayon pattern.
            var v=new List<Vector3>();var uv2=new List<Vector2>();var tris=new List<int>();const int nx=48,nz=28;
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {float x=-18+36f*i/nx,z=-24+51f*j/nz,y=7.4f+4*Mathf.Sqrt(Mathf.Max(0,1-x*x/324f));v.Add(V(x,y+.04f*Mathf.Sin(i*.7f+j),z));uv2.Add(new Vector2(i/12f,j/8f));if(i<nx&&j<nz){int k=j*(nx+1)+i;tris.AddRange(new[]{k,k+1,k+nx+1,k+1,k+nx+2,k+nx+1});}}
            var ceiling=Obj(interior,"별과 잎 무늬의 둥근 천장",Mesh(v,uv2,tris),MuralMat(3),Vector3.zero,true);
            // Tile the atlas quadrant with explicit UV fractions so other animals never leak in.
            var ceilingMat=new Material(MuralMat(3));ceilingMat.name="13_VaultCrayonRepeat";string path=WorkshopDir+"/13_VaultCrayonRepeat.mat";
            var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){Object.DestroyImmediate(ceilingMat);ceilingMat=old;}else AssetDatabase.CreateAsset(ceilingMat,path);
            ceilingMat.shader=Shader.Find("BookTower/CrayonAtlasRepeat");ceilingMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BookTower/Textures/StorybookV2/CrayonMuralAtlas.png"));ceiling.GetComponent<Renderer>().sharedMaterial=ceilingMat;
            foreach(float z in new[]{-21f,-9f,4f,17f,26f})
            {var curve=Enumerable.Range(0,49).Select(i=>{float x=-18+36f*i/48;return V(x,7.35f+4*Mathf.Sqrt(Mathf.Max(0,1-x*x/324f)),z);}).ToArray();Stroke(interior,"층층이 휜 천장 제본 리브",curve,ochre,.10f);Stroke(interior,"천장 면실",curve.Select(p=>p+V(0,-.12f,.13f)).ToArray(),cream,.038f);}
            // Real side passages connect the compact room to the authored F0 and 1B endpoints.
            foreach(float z in new[]{-17.6f,-7.8f})Box(interior,"서쪽 입구의 두꺼운 통로 벽",V(-25,4,z),V(14.5f,8,.4f),cream,true);
            Box(interior,"서쪽 입구 낮은 지붕",V(-25,7.8f,-12.7f),V(14.5f,.4f,10.2f),cream,true);Arch(interior,V(-17.7f,0,-12.7f),4.6f,3,90);
            foreach(float z in new[]{17.2f,27.65f})Box(interior,"출구 곁 짧은 회랑 벽",V(22.8f,4,z),V(10,.0f+8,.35f),cream,true);
            // The north wall has the existing doorway: only build the short south return above.
            Object.DestroyImmediate(interior.GetComponentsInChildren<Transform>().First(t=>t.name=="출구 곁 짧은 회랑 벽"&&t.localPosition.z>20).gameObject);
            Box(interior,"출구 회랑 둥근 지붕의 바탕",V(23.5f,9.4f,23),V(12,.4f,10),cream,true);
            Mural(interior,V(9.5f,.15f,26.35f),8,8,1);Arch(interior,V(9.5f,0,26.18f),4.2f,4,0);Star(interior,V(9.5f,3.6f,26.05f),.55f,glow);

            // Entry-to-theatre guidance follows the new open room, not the retired partitions.
            Vector3[] route={V(-31,.22f,-12.8f),V(-13,.22f,-12.8f),V(-10,.22f,0),V(3,.22f,12),V(13,.22f,20),V(24.5f,.22f,26)};
            for(int i=0;i<route.Length-1;i++){var d=route[i+1]-route[i];for(float k=0;k<d.magnitude;k+=1.9f){var at=route[i]+d.normalized*k;if(at.x < -20 && at.z > -16 && at.z < -8)continue;var leaf=Obj(interior,"별빛을 따라가는 바닥 잎",OrganicLeaf(.65f,.24f),teal,at);leaf.rotation=Quaternion.LookRotation(d)*Quaternion.Euler(90,0,0);}}
        }
    }
}
