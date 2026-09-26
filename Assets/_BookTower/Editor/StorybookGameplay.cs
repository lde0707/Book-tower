using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using BookTower.Rooms;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Interaction;
using Object=UnityEngine.Object;
namespace BookTower.EditorTools
{
    public static partial class StorybookRoomBuilder
    {
        static FoldPanel Panel(Transform parent,string name,Vector3 pos,string animal,Material mat)
        {
            var t=Group(parent,name,pos);var panel=t.gameObject.AddComponent<FoldPanel>();
            var so=new SerializedObject(panel);var angles=so.FindProperty("foldAngles");angles.arraySize=4;for(int i=0;i<4;i++)angles.GetArrayElementAtIndex(i).floatValue=i*35;so.FindProperty("noStanding").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            Paper(t,"접히는 종잇장",Vector3.zero,3.4f,3.6f,cream,V(90,0,0));
            var art=Art(t,animal,V(0,.065f,.15f),3);art.localRotation=Quaternion.Euler(90,0,0);
            var c=t.gameObject.AddComponent<BoxCollider>();c.size=V(3.5f,.2f,3.6f);c.center=V(0,.08f,1.8f);
            var interact=t.gameObject.AddComponent<Interactable>();var si=new SerializedObject(interact);si.FindProperty("label").stringValue=name;si.FindProperty("verb").stringValue="접는다";si.FindProperty("lift").floatValue=0;si.FindProperty("range").floatValue=6;si.ApplyModifiedPropertiesWithoutUndo();
            Stroke(t,"제본 힌지",new[]{V(-1.75f,.12f,0),V(1.75f,.12f,0)},mat,.075f);
            for(int i=0;i<5;i++)Star(t,V(-1.55f+i*.78f,.16f,.08f),.1f,mat);
            return panel;
        }
        static void Gameplay(Transform game)
        {
            var puzzle=game.GetComponentInChildren<FoldCombinationPuzzle>(true);if(!puzzle)throw new Exception("P2 missing");
            var group=game.GetComponentInChildren<FoldGroup>(true);if(!group)group=game.gameObject.AddComponent<FoldGroup>();
            var a=Panel(game,"P2_새의 접기",V(-15,.13f,-17),"Bird",teal);
            var b=Panel(game,"P2_코럴 반쪽",V(-8,.13f,0),"Rabbit",coral);
            var c=Panel(game,"P2_청록 반쪽",V(15,.13f,17),"Rabbit",teal);
            var sg=new SerializedObject(group);var ps=sg.FindProperty("panels");ps.arraySize=3;for(int i=0;i<3;i++)ps.GetArrayElementAtIndex(i).objectReferenceValue=new[]{a,b,c}[i];sg.ApplyModifiedPropertiesWithoutUndo();
            var sp=new SerializedObject(puzzle);sp.FindProperty("group").objectReferenceValue=group;sp.FindProperty("activateRadius").floatValue=80;var answer=sp.FindProperty("answer");answer.arraySize=3;int[] codes={2,1,3};for(int i=0;i<3;i++)answer.GetArrayElementAtIndex(i).intValue=codes[i];sp.ApplyModifiedPropertiesWithoutUndo();
            // Small side-profile samples teach the angles in-world, beside each station.
            for(int i=0;i<3;i++)
            {
                Vector3 at=new[]{a.transform.localPosition,b.transform.localPosition,c.transform.localPosition}[i]+V(-3,0,.5f);
                Box(root,"접기 표본 받침",at+V(0,.35f,0),V(1.8f,.7f,1.7f),butter,true);
                var sample=Paper(root,"접힌 모서리 표본",at+V(0,.72f,-.5f),1.35f,1.5f,cream,V(90-codes[i]*35,0,0));
                Art(sample,"Bird",V(0,.1f,-.07f),1.1f);
                Star(root,at+V(0,2.25f,0),.25f,i==1?coral:teal);
            }
            Paper(root,"최종 그림자 벽",V(-4,0,27.4f),13,10,cream,Vector3.zero);Arch(root,V(-4,0,27.2f),5.8f,3,0);
            var l=Art(root,"RabbitShadow0",V(-4,1.8f,27.08f),7);var r=Art(root,"RabbitShadow1",V(-4,1.8f,27.06f),7);
            var star=Star(root,V(4.2f,4.2f,27.1f),.85f,glow);var light=LightAt(root,V(4.2f,4.2f,25.8f),3,10);
            var theatre=root.gameObject.AddComponent<StorybookShadowTheatre>();theatre.practice=a;theatre.left=b;theatre.right=c;theatre.leftShadow=l;theatre.rightShadow=r;theatre.reward=star;theatre.rewardLight=light;theatre.puzzle=puzzle;
            // Memory vignette: an adult and three small paper lives around a naming book.
            Books(V(-22,0,18),2,-8);Art(root,"Bird",V(-25,2,25.8f),3);Art(root,"Rabbit",V(-21,2,25.8f),3);Art(root,"Fox",V(-17,2,25.8f),3);
        }
        static void Passage()
        {
            // The old ramp apron rails extend six metres into the room and obstruct lateral
            // approaches. The room floor and doorway walls already protect this inner area.
            var outgoing=Object.FindObjectsByType<RampLink>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.FromRoomId=="1A"&&r.ToRoomId=="1B");
            var apron=outgoing.transform.parent.Find(outgoing.name+"_접합부");
            if(apron)foreach(Transform side in apron)if(side.name.StartsWith("접힌측벽_")&&side.name.EndsWith("_0"))side.gameObject.SetActive(false);

            var link=Object.FindObjectsByType<RampLink>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.FromRoomId=="F0"&&r.ToRoomId=="1A");
            var so=new SerializedObject(link);Vector3 a=so.FindProperty("fromPoint").vector3Value,b=so.FindProperty("toPoint").vector3Value;
            link.GetComponent<Renderer>().sharedMaterial=butter;
            var originalJoin=link.transform.parent.Find(link.name+"_접합부");
            if(originalJoin)foreach(var renderer in originalJoin.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=cream;
            var parent=link.transform.parent;var old=parent.Find("V4_서문에서동화로");if(old)Object.DestroyImmediate(old.gameObject);
            var g=Group(parent,"V4_서문에서동화로",(a+b)*.5f);g.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);
            var bound=g.gameObject.AddComponent<RoomBound>();var sb=new SerializedObject(bound);var ids=sb.FindProperty("roomIds");ids.arraySize=2;ids.GetArrayElementAtIndex(0).stringValue="F0";ids.GetArrayElementAtIndex(1).stringValue="1A";sb.ApplyModifiedPropertiesWithoutUndo();
            float length=Vector3.Distance(a,b),width=link.transform.lossyScale.x;
            for(float z=-length*.5f;z<length*.5f;z+=4)
            {
                float t=(z+length*.5f)/length;
                Paper(g,"이어 붙인 통로 장지",V(0,.18f,z),width,Mathf.Min(4,length*.5f-z)+.02f,t<.3f?butter:cream,V(90,0,0));
                foreach(float side in new[]{-1f,1f})
                {
                    Box(g,"통로 벽",V(side*(width*.5f+.18f),2.9f,z+2),V(.30f,5.8f,4.12f),t<.3f?butter:cream,true);
                    if(t<.36f)Box(g,"제본에서 이어진 붉은 책등",V(side*(width*.5f-.02f),2.4f,z+1),V(.2f,4.6f,1.1f),red);
                    else Leaves(g,V(side*(width*.5f-.25f),.9f,z+1),.9f,side<0?90:-90);
                }
                var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
                for(int j=0;j<=24;j++)for(int k=0;k<2;k++){float ang=Mathf.PI*j/24;v.Add(V(Mathf.Cos(ang)*width*.5f,3.5f+Mathf.Sin(ang)*width*.5f,z+k*4.12f));uv.Add(new Vector2(ang*width,z+k*4.12f)*.4f);if(j>0&&k==0){int q=(j-1)*2;tri.AddRange(new[]{q,q+1,q+2,q+1,q+3,q+2});}}
                Obj(g,"휘어진 책장 천장",Mesh(v,uv,tri),cream,Vector3.zero);
                if(((int)(z+length*.5f)/4)%2==0){Arch(g,V(0,0,z),width*.5f-.65f,3.5f,0);Hanging(g,V(0,4.6f,z+1),6,.38f);}
                Stroke(g,"붉은 리본에서 청록 줄기로",new[]{V(-.3f,.15f,z),V(.2f,.15f,z+2),V(0,.15f,z+4)},t<.5f?coral:teal,.038f);
            }
        }
        static void Validate()
        {
            Physics.SyncTransforms();var floor=root.Find("연속 충돌 바닥").GetComponent<Collider>();int rays=0;
            for(int x=-30;x<=30;x+=3)for(int z=-26;z<=26;z+=2){if(!floor.Raycast(new Ray(room.transform.TransformPoint(V(x,2,z)),Vector3.down),out _,4))throw new Exception("Floor gap");rays++;}
            var panels=room.transform.Find("1A_Gameplay").GetComponentsInChildren<FoldPanel>();if(panels.Length!=3)throw new Exception("Expected three fold panels");
            File.WriteAllText(Out+"/structure-check.txt",$"PASS: {rays} floor rays; 3 fold stations; P2 and existing ramp links retained; scene saved.\n");
        }
        static void Capture()
        {
            var states=new Dictionary<Renderer,bool>();var lights=new Dictionary<Light,bool>();
            foreach(var r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include,FindObjectsSortMode.None)){foreach(var re in r.GetComponentsInChildren<Renderer>(true)){states[re]=re.enabled;re.enabled=r==room;}foreach(var l in r.GetComponentsInChildren<Light>(true)){lights[l]=l.enabled;l.enabled=r==room;}}
            var camera=new GameObject("Storybook review").AddComponent<Camera>();camera.fieldOfView=66;camera.nearClipPlane=.08f;camera.farClipPlane=150;camera.backgroundColor=new Color(.2f,.16f,.10f);camera.clearFlags=CameraClearFlags.SolidColor;
            var old=RenderSettings.ambientProbe;var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.55f,.44f,.32f));RenderSettings.ambientProbe=sh;bool fog=RenderSettings.fog;RenderSettings.fog=false;
            var spirit=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BookTower/Art/PaperSpirit/PaperSpirit.prefab")) as GameObject;
            try
            {
                Vector3[] poses={V(-9,2.4f,-25),V(6,2.4f,-3),V(7,2.4f,12)};Vector3[] targets={V(-7,4,-7),V(9,4,11),V(-1,4,27)};
                for(int i=0;i<3;i++){camera.transform.position=room.transform.TransformPoint(poses[i]);camera.transform.LookAt(room.transform.TransformPoint(targets[i]));spirit.transform.position=room.transform.TransformPoint(poses[i]+V(-.6f,-2.35f,4));spirit.transform.rotation=Quaternion.Euler(0,180,0);InteriorF0Builder.Capture(camera,Out+"/1A-room-"+(i+1)+".png");}
                camera.transform.position=V(39,3,-204.4f);camera.transform.LookAt(V(65,6,-194));spirit.SetActive(false);InteriorF0Builder.Capture(camera,Out+"/F0-to-1A.png");
            }
            finally{Object.DestroyImmediate(spirit);Object.DestroyImmediate(camera.gameObject);RenderSettings.ambientProbe=old;RenderSettings.fog=fog;foreach(var kv in states)if(kv.Key)kv.Key.enabled=kv.Value;foreach(var kv in lights)if(kv.Key)kv.Key.enabled=kv.Value;}
        }
    }
}
