using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static partial class InteriorF0Builder
    {
        static Transform IdentityMechanismRoot(Transform parent)
        {
            var old=parent.Find("종이공예_외형");if(old)Object.DestroyImmediate(old.gameObject);
            var skin=Group(parent,"종이공예_외형",Vector3.zero);identityMeshRoots.Add(skin);return skin;
        }

        static void IdentityBridge(FoldBridgePuzzle puzzle)
        {
            var skin=IdentityMechanismRoot(puzzle.hinge);
            var sheet=MeshObject(skin,"접히는다리_적층장지",IdentitySheet(4.37f,8.37f,991,.020f),ivory,V(0,.012f,.02f));sheet.localRotation=Quaternion.Euler(90,0,0);
            foreach(int side in new[]{-1,1})
            {
                Box(skin,"다리_접힌책표지측면",V(side*2.18f,-.065f,4.2f),V(.14f,.20f,8.4f),oxblood);
                Rope(skin,"다리_가장자리꼬인실",V(side*2.08f,.12f,.18f),V(side*2.08f,.12f,8.20f),.044f);
                foreach(float z in new[]{.25f,8.1f})
                {
                    Box(skin,"다리_모서리_보강판",V(side*1.97f,.03f,z),V(.42f,.065f,.45f),brass);
                    Knot(skin,V(side*1.99f,.15f,z),.22f,true);
                }
                for(int i=0;i<16;i++)Tube(skin,"다리_경계실땀",new[]{V(side*1.93f,.065f,.36f+i*.49f),V(side*2.15f,.14f,.41f+i*.49f),V(side*2.25f,-.04f,.43f+i*.49f)},.014f,rope);
                Box(skin,"다리_인쇄테두리",V(side*1.72f,.06f,4.2f),V(.046f,.008f,7.92f),ink);
            }
            Box(skin,"다리_앞제본턱",V(0,-.06f,.05f),V(4.38f,.20f,.20f),oxblood);
            Box(skin,"다리_앞황동걸쇠",V(0,-.035f,-.072f),V(.8f,.37f,.08f),brass);
            var motif=Group(skin,"걸쇠잎압인",V(0,-.18f,-.124f));Sprig(motif,Vector3.zero,.29f,wood);
            for(int strand=-1;strand<=1;strand++)Tube(skin,"다리_중앙가는실",new[]{V(strand*.055f,.066f,.18f),V(strand*.055f,.066f,8.20f)},.012f,rope);
            CombineDecorations(skin);

            foreach(var control in room.GetComponentsInChildren<FoldBridgeControl>())
            {
                var p=IdentityMechanismRoot(control.transform);
                Box(p,"고리뒤_잎표지",V(0,0,-.225f),V(.67f,.65f,.035f),oxblood);
                var ring=new List<Vector3>();
                for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;ring.Add(V(Mathf.Sin(a)*.235f,Mathf.Cos(a)*.235f,-.32f));}
                Tube(p,"손으로당기는_황동고리",ring,.038f,brass);
                Sphere(p,"고리_고정리벳",V(0,.27f,-.30f),V(.105f,.105f,.09f),brass);
                Box(p,"고리_제본받침기둥",V(0,-.60f,.10f),V(.20f,.64f,.30f),wood);
                Box(p,"고리_제본받침바닥",V(0,-.92f,.10f),V(.78f,.16f,.60f),oxblood);
                var tag=MeshObject(p,"제본_작은종이표찰",IdentitySheet(.25f,.40f,119+control.opticalControl,.017f),paper,V(.39f,-.24f,-.20f));tag.localRotation=Quaternion.Euler(0,0,-8);
                CombineDecorations(p);
            }
            foreach(var surface in puzzle.lightSurfaces)
            {
                if(surface.kind!=F0LightSurface.Kind.Mirror)continue;
                var p=IdentityMechanismRoot(surface.transform);
                foreach(int side in new[]{-1,1})
                {
                    Box(p,"은박_접힌종이모서리",V(side*.90f,0,-.075f),V(.08f,1.90f,.065f),paper);
                    foreach(float y in new[]{-.88f,.88f})
                        Box(p,"반사판_모서리금박",V(side*.77f,y,-.087f),V(.28f,.055f,.05f),brass);
                }
                for(int k=0;k<5;k++)Tube(p,"반사판_책등봉제",new[]{V(-.95f,-.7f+k*.34f,-.075f),V(-1.0f,-.66f+k*.34f,-.1f),V(-.94f,-.62f+k*.34f,.06f)},.012f,rope);
                CombineDecorations(p);
            }
            var lamp=IdentityMechanismRoot(puzzle.source);
            lamp.localRotation=Quaternion.Inverse(puzzle.source.localRotation);
            var originalLamp=puzzle.source.Find("종이 광원");if(originalLamp&&originalLamp.GetComponent<Renderer>())Object.DestroyImmediate(originalLamp.GetComponent<Renderer>());
            Box(lamp,"광원_접은장지",Vector3.zero,V(.92f,.64f,.92f),glow);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(lamp,"광원_제본프레임",V(x*.50f,0,z*.50f),V(.07f,.83f,.07f),wood);
            foreach(float y in new[]{-.40f,.40f})foreach(int side in new[]{-1,1})
            {
                Box(lamp,"광원_테두리",V(side*.50f,y,0),V(.07f,.07f,1.06f),brass);
                Box(lamp,"광원_테두리",V(0,y,side*.50f),V(1.06f,.07f,.07f),brass);
            }
            Rope(lamp,"광원_천장실",V(0,.48f,0),V(0,3.75f,0),.035f);
            CombineDecorations(lamp);
            var cover=IdentityMechanismRoot(puzzle.shutter);
            foreach(float x in new[]{-2.53f,-.07f})Box(cover,"덮개_장지단면",V(x,.075f,0),V(.025f,.03f,2.48f),edge);
            foreach(float z in new[]{-1.2f,1.2f})Box(cover,"덮개_금박압인선",V(-1.3f,.076f,z),V(2.38f,.015f,.035f),identityGold);
            CombineDecorations(cover);
            foreach(var line in puzzle.lightSegments){line.sharedMaterial=identityBeam;line.widthMultiplier=.045f;line.numCapVertices=3;}
            puzzle.connection.sharedMaterial=identityBeam;puzzle.connection.widthMultiplier=.04f;
            foreach(var line in puzzle.hinge.parent.GetComponentsInChildren<LineRenderer>())
            {
                if(!line.name.Contains("과 연결된 제본실")||line.positionCount<2)continue;
                Vector3 start=line.GetPosition(0),end=line.GetPosition(line.positionCount-1);
                float ceiling=room.transform.position.y+15.3f;
                line.positionCount=4;
                line.SetPositions(new[]{start,new Vector3(start.x,ceiling,start.z),new Vector3(end.x,ceiling,end.z),end});
                line.widthMultiplier=.035f;line.sharedMaterial=rope;
            }
        }

        static void IdentityOptionalProps()
        {
            var note=room.transform.Find("제본사의 읽는 기록");
            if(note)
            {
                note.localPosition=V(-11.75f,4.78f,-8.65f);note.localRotation=Quaternion.identity;note.localScale=Vector3.one;
                if(note.GetComponent<Renderer>())Object.DestroyImmediate(note.GetComponent<Renderer>());
                if(note.GetComponent<MeshFilter>())Object.DestroyImmediate(note.GetComponent<MeshFilter>());
                var collider=note.GetComponent<BoxCollider>();if(collider){collider.size=V(.76f,.86f,.16f);collider.center=Vector3.zero;}
                var skin=IdentityMechanismRoot(note);
                Box(skin,"메모_제본받침",Vector3.zero,V(.76f,.86f,.055f),oxblood);
                MeshObject(skin,"메모_찢긴장지",IdentitySheet(.68f,.77f,224,.012f),ivory,V(0,-.385f,-.050f));
                for(int i=0;i<5;i++)Box(skin,"메모_잉크필적",V(-.025f,.10f-i*.076f,-.069f),V(.39f-(i%3)*.045f,.012f,.008f),ink);
                Sprig(skin,V(.21f,.17f,-.075f),.16f,ink);
                Sphere(skin,"메모_작은집게",V(0,.4f,-.075f),V(.17f,.08f,.08f),brass);
                CombineDecorations(skin);
                if(PrefabUtility.IsPartOfPrefabInstance(note))PrefabUtility.RecordPrefabInstancePropertyModifications(note);
                if(collider&&PrefabUtility.IsPartOfPrefabInstance(collider))PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
            var toys=room.transform.Find("F0_살아있는제본실");if(!toys)return;
            foreach(var r in toys.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(IdentityReplacement).ToArray();
            foreach(var touch in toys.GetComponentsInChildren<BindingTouch>())
            {
                if(touch.kind==BindingTouch.TouchKind.TapBook)
                {
                    touch.transform.localPosition=V(-11.1f+touch.index*.95f,3.76f,-9.16f);
                    var skin=IdentityMechanismRoot(touch.moving);
                    Box(skin,"반응책_종이단면",V(.40f,0,.035f),V(.035f,1.36f,.39f),ivory);
                    foreach(float y in new[]{-.6f,.58f})Box(skin,"반응책_제본띠",V(0,y,-.27f),V(.78f,.065f,.04f),brass);
                    Sprig(skin,V(0,-.28f,-.286f),.55f,identityGold);CombineDecorations(skin);
                }
                else if(touch.kind==BindingTouch.TouchKind.BrushDust)
                {
                    touch.transform.localPosition=V(-8.1f,4.40f,-8.12f);
                }
                else if(touch.kind==BindingTouch.TouchKind.FlutterPage)
                {
                    touch.transform.localPosition=V(12.1f,3.24f,-12.7f);
                    var skin=IdentityMechanismRoot(touch.transform);
                    Box(skin,"자유페이지_낮은책받침",V(0,-.12f,.12f),V(1.65f,.22f,.8f),oxblood);
                    foreach(int side in new[]{-1,1})Sphere(skin,"페이지_바닥핀",V(side*.46f,.04f,0),V(.11f,.10f,.12f),brass);
                    CombineDecorations(skin);
                }
                if(PrefabUtility.IsPartOfPrefabInstance(touch.transform))PrefabUtility.RecordPrefabInstancePropertyModifications(touch.transform);
            }
        }

        static void IdentityLighting()
        {
            var zone=IdentityZone("12_창빛과종이반사광");
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-5.2f,5.4f})
            {
                var p=Group(zone,"종이창_투광",V(side*15.20f,11.8f,z));
                p.LookAt(room.transform.TransformPoint(V(side<0?1.5f:-3,3.3f,z+6.5f)));
                var l=p.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=side<0?new Color(1,.88f,.71f):new Color(.80f,.86f,1);
                l.intensity=side<0?185:48;l.range=36;l.spotAngle=74;l.innerSpotAngle=46;l.cookie=WindowCookie();
                l.shadows=side<0?LightShadows.Soft:LightShadows.None;l.shadowBias=.025f;l.shadowNormalBias=.10f;l.shadowStrength=.78f;
            }
            void Fill(string name,Vector3 at,Vector3 target,float power,float range,Color color)
            {
                var p=Group(zone,name,at);p.LookAt(room.transform.TransformPoint(target));var l=p.gameObject.AddComponent<Light>();
                l.type=LightType.Spot;l.spotAngle=110;l.innerSpotAngle=70;l.range=range;l.intensity=power;l.color=color;l.shadows=LightShadows.None;
            }
            Fill("시작대지_장지반사",V(0,11,-11),V(0,3,-8),115,22,new Color(1,.92f,.82f));
            Fill("서쪽책등_부드러운반사",V(-2,10,-13),V(-11,8,-10),105,21,new Color(1,.85f,.70f));
            Fill("동쪽책등_부드러운반사",V(3,10,-10),V(12,8,-9),100,22,new Color(.88f,.91f,1));
            Fill("문과건너편_반사",V(0,11,4),V(0,7,15),150,24,new Color(1,.92f,.78f));
            Fill("후면표지_반사",V(0,10,-6),V(0,8,-20),130,24,new Color(1,.92f,.81f));
            var so=new SerializedObject(room);so.FindProperty("ambient").floatValue=.67f;so.FindProperty("fog").floatValue=.0023f;so.ApplyModifiedPropertiesWithoutUndo();
            // F0 owns a local grade: no change to other room profiles or global quality settings.
            string path=IdentityFolder+"/F0PaperGrade.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var grade))grade=profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(.30f);grade.contrast.Override(7);grade.saturation.Override(-6);
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.16f);bloom.threshold.Override(1.5f);bloom.scatter.Override(.6f);
            foreach(var c in profile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,profile);
            var volume=room.transform.Find(RootName).GetComponentInChildren<Volume>();if(volume)volume.sharedProfile=profile;
            EditorUtility.SetDirty(profile);
            // Remove only the buried legacy table collision; replacement furniture has matching solids.
            var table=room.transform.Find(RootName+"/책상");if(table)foreach(var c in table.GetComponentsInChildren<Collider>())c.enabled=false;
        }
    }
}
