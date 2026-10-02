using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using Object = UnityEngine.Object;

namespace BookTower.EditorTools
{
    [InitializeOnLoad] public static class FoyerPassageOpeningReview
    {
        const string Flag = "FoyerPassageOpeningReview";
        static string Out=>"Docs/ArtReview/FoyerPassageOpening/"+(SessionState.GetBool(Flag+"verify",false)?"After":"Before");
        static IEnumerator run; static double next; static readonly StringBuilder report = new();
        static FoyerPassageOpeningReview() { EditorApplication.playModeStateChanged += Changed; }
        public static void Inspect()=>Start(false);
        public static void Verify()=>Start(true);
        static void Start(bool verify) { SessionState.SetBool(Flag+"verify",verify);Directory.CreateDirectory(Out); EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity"); SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode(); }
        static void Changed(PlayModeStateChange s)
        {
            if (!SessionState.GetBool(Flag, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode) { report.Clear(); run = Probe(); next = EditorApplication.timeSinceStartup + 1; EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode) { SessionState.EraseBool(Flag); EditorApplication.Exit(SessionState.GetInt(Flag+"exit", 1)); }
        }
        static void Tick() { if (EditorApplication.timeSinceStartup < next) return; try { if (!run.MoveNext()) { End(null); return; } next=EditorApplication.timeSinceStartup+(run.Current is float f ? f : .1); } catch(Exception e) { End(e); } }
        static void End(Exception e) { EditorApplication.update -= Tick; File.WriteAllText(Out+"/inspection.txt", report+(e==null?"DONE":"FAIL "+e)); SessionState.SetInt(Flag+"exit", e==null?0:1); EditorApplication.ExitPlaymode(); }
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
        static void Capture(Camera c,string name)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24); var prior=c.targetTexture; var active=RenderTexture.active;
            try { c.targetTexture=rt; c.Render(); RenderTexture.active=rt; var im=new Texture2D(1280,720,TextureFormat.RGB24,false); im.ReadPixels(new Rect(0,0,1280,720),0,0);im.Apply();File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Object.DestroyImmediate(im); }
            finally { c.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt); }
        }
        static IEnumerator Probe()
        {
            foreach(var story in Object.FindObjectsByType<BookTower.Story.OpeningStoryDirector>(FindObjectsSortMode.None)) story.gameObject.SetActive(false);
            BookTower.UI.PaperUI.Instance.EndStory();BookTower.UI.PaperUI.Instance.Resume();BookTower.Core.GameFlow.EnsureExists().Resume();
            var player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;
            var manager=Object.FindFirstObjectByType<RoomManager>();manager.enabled=false;
            foreach(var r in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))r.SetVisible(r.RoomId=="F0"||r.RoomId=="1A");
            foreach(var r in Object.FindObjectsByType<RoomBound>(FindObjectsSortMode.None))r.SetVisible(r.Belongs("F0")||r.Belongs("1A"));
            var link=Object.FindObjectsByType<RampLink>(FindObjectsSortMode.None).First(l=>l.FromRoomId=="F0"&&l.ToRoomId=="1A");
            var so=new SerializedObject(link);Vector3 a=so.FindProperty("fromPoint").vector3Value,b=so.FindProperty("toPoint").vector3Value;
            Vector3 forward=(b-a).normalized,right=Vector3.Cross(Vector3.up,forward).normalized;
            report.AppendLine($"A={a:F4} B={b:F4} width={link.transform.lossyScale.x} path={PathOf(link.transform)}");
            var box=new Bounds((a+b)/2,Vector3.zero);box.Encapsulate(a);box.Encapsulate(b);box.Expand(20);
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!box.Intersects(r.bounds))continue;
                var c=r.bounds.center;float distance=Vector3.Cross((c-a),forward).magnitude;
                if(distance>12)continue;
                report.AppendLine($"R {PathOf(r.transform)} active={r.gameObject.activeInHierarchy} enabled={r.enabled} pos={r.transform.position:F3} bounds={r.bounds.center:F3}/{r.bounds.size:F3} collider={r.GetComponent<Collider>()!=null}");
            }
            Object.FindFirstObjectByType<PaperCamera>().enabled=false;var camera=Camera.main;camera.fieldOfView=65;
            foreach(float t in new[]{-.08f,0,.08f,.75f,.90f,1f,1.06f})
            {
                player.Controller.enabled=false;player.transform.position=Vector3.LerpUnclamped(a,b,t)-forward*1.8f;player.Controller.enabled=true;
                camera.transform.position=Vector3.LerpUnclamped(a,b,t)+Vector3.up*1.8f;
                camera.transform.rotation=Quaternion.LookRotation(forward+Vector3.up*.14f);yield return .3f;
                Capture(camera,"station-"+t.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
                if(t==.90f)
                {
                    var intersect=typeof(HandleUtility).GetMethod("IntersectRayMesh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                    camera.aspect=16f/9;
                    foreach(var uv in new[]{new Vector2(.504f,.085f),new Vector2(.503f,.025f),new Vector2(.99f,.43f),new Vector2(.485f,.74f)})
                    {
                        var ray=camera.ViewportPointToRay(uv);var hits=new System.Collections.Generic.List<(float,string)>();
                        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                        {
                            var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!r.bounds.IntersectRay(ray)||!f.sharedMesh)continue;
                            object[] args={ray,f.sharedMesh,f.transform.localToWorldMatrix,new RaycastHit()};
                            if(intersect!=null&&(bool)intersect.Invoke(null,args)){var hit=(RaycastHit)args[3];hits.Add((hit.distance,PathOf(f.transform)+$" triangle={hit.triangleIndex} point={hit.point:F3}"));}
                        }
                        foreach(var hit in hits.OrderBy(h=>h.Item1).Take(4))report.AppendLine($"PICK {uv} d={hit.Item1} {hit.Item2}");
                    }
                }
                foreach(float x in new[]{-1.5f,0,1.5f})
                {
                    var origin=Vector3.LerpUnclamped(a,b,t)+Vector3.up*1.5f+right*x;
                    foreach(var hit in Physics.RaycastAll(origin,forward,5,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) report.AppendLine($"HIT t={t} lane={x} {PathOf(hit.transform)} d={hit.distance:F3}");
                }
            }
            var foyer=Object.FindObjectsByType<Room>(FindObjectsSortMode.None).First(r=>r.RoomId=="F0");
            foreach(var local in new[]{new Vector3(0,5,17.3f),new Vector3(3,5,19f),new Vector3(10,3,18.3f)})
            {
                camera.transform.position=foyer.transform.TransformPoint(local);camera.transform.rotation=Quaternion.Euler(-35,45,0);yield return .2f;Capture(camera,"door-sky-"+local.x);
                var ray=new Ray(camera.transform.position,Vector3.up);foreach(var h in Physics.RaycastAll(ray,25,~0,QueryTriggerInteraction.Ignore))report.AppendLine($"DOOR_ROOF {local} {PathOf(h.transform)} d={h.distance}");
            }
            if(SessionState.GetBool(Flag+"verify",false))
            {
                // Renderer coverage, not just upward collision rays: the old invisible
                // collision ceiling passed those rays while showing sky in actual play.
                var check=new GameObject("Door roof visual coverage QA").AddComponent<Camera>();
                check.fieldOfView=78;check.nearClipPlane=.05f;check.farClipPlane=100;
                check.clearFlags=CameraClearFlags.SolidColor;check.backgroundColor=Color.magenta;check.allowHDR=false;
                int air=Shader.GetGlobalInt("_PaperAirCount"),beams=Shader.GetGlobalInt("_PaperBeamCount");
                Shader.SetGlobalInt("_PaperAirCount",0);Shader.SetGlobalInt("_PaperBeamCount",0);
                int allHoles=0;
                try
                {
                    foreach(var local in new[]{new Vector3(0,5,18.4f),new Vector3(4,5,19),new Vector3(10,3,18.3f),new Vector3(22,3,14.5f)})
                    foreach(int yaw in new[]{0,90,180,270})
                    {
                        check.transform.position=foyer.transform.TransformPoint(local);check.transform.rotation=Quaternion.Euler(-45,yaw,0);
                        var rt=RenderTexture.GetTemporary(480,300,24);var prev=RenderTexture.active;
                        try {check.targetTexture=rt;check.Render();RenderTexture.active=rt;var im=new Texture2D(480,300,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,480,300),0,0);im.Apply();int holes=im.GetPixels32().Count(c=>c.r>180&&c.b>180&&c.g<85);File.WriteAllBytes(Out+$"/coverage-{local.x}-{yaw}.png",im.EncodeToPNG());report.AppendLine($"COVERAGE {local} yaw={yaw} exposed-pixels={holes}");allHoles+=holes;
                        if(holes>0){for(int py=0;py<300;py++)for(int px=0;px<480;px++){var color=im.GetPixel(px,py);if(color.r>.70f&&color.b>.70f&&color.g<.34f){var rr=check.ViewportPointToRay(new Vector3((px+.5f)/480f,(py+.5f)/300f));report.AppendLine($"HOLE RAY {rr.origin:F4} dir={rr.direction:F4}");foreach(var hh in Physics.RaycastAll(rr,50,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Take(4))report.AppendLine($"HOLE collider {PathOf(hh.transform)} p={hh.point:F4}");}}}Object.DestroyImmediate(im);}
                        finally {check.targetTexture=null;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);}
                    }
                }
                finally {Shader.SetGlobalInt("_PaperAirCount",air);Shader.SetGlobalInt("_PaperBeamCount",beams);Object.DestroyImmediate(check.gameObject);}
                if(allHoles>0)throw new Exception("Visible exterior in roof views: "+allHoles+" pixels; see coverage images");
                // Through the actual arrival opening, including side lanes and both directions.
                foreach(float lane in new[]{-1.25f,0,1.25f})foreach(bool reverse in new[]{false,true})
                {
                    // Follow the diagonal arrival gallery before turning east into 1A;
                    // a straight chord across this bend cuts through its right wall.
                    var turn=b+new Vector3(forward.x,0,forward.z)*6+right*lane+Vector3.up*.2f;
                    var route=new[]{b-forward*5+right*lane+Vector3.up*.2f,turn,turn+Vector3.right*6};
                    if(reverse)Array.Reverse(route);
                    player.Controller.enabled=false;player.transform.position=route[0];player.Controller.enabled=true;Physics.SyncTransforms();
                    for(int segment=0;segment<route.Length-1;segment++)for(int i=1;i<=180;i++)
                    {
                        var goal=Vector3.Lerp(route[segment],route[segment+1],i/180f);player.Controller.Move(goal-player.transform.position);
                        if(Vector3.Distance(player.transform.position,goal)>.6f)
                        {
                            foreach(var hit in Physics.OverlapSphere(player.transform.position+Vector3.up*.7f,1.2f,~0,QueryTriggerInteraction.Ignore))
                                report.AppendLine($"BLOCKER {PathOf(hit.transform)} bounds={hit.bounds} center={hit.transform.position:F3}");
                            throw new Exception($"Arrival blocked lane={lane} reverse={reverse} at {goal}, actual={player.transform.position}");
                        }
                    }
                    report.AppendLine($"PASS arrival traversal lane={lane}, reverse={reverse}");
                }
                report.AppendLine("PASS: 16 upward doorway views have zero exposed background; six arrival traversals.");
            }
        }
    }
}
