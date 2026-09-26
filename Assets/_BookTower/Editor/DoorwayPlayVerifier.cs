using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BookTower.Fold;
using BookTower.Puzzles;
using BookTower.Rooms;
using BookTower.PlayerSystem;
using BookTower.CameraSystem;
using BookTower.Interaction;
using Object=UnityEngine.Object;

namespace BookTower.EditorTools
{
    public static class DoorwayPlayVerifier
    {
        const string Flag="DoorwayPlayVerifier.Pending", Output="Docs/ArtReview/Connections";
        static IEnumerator routine;
        static float next;
        static int lastFrame;
        static double started;
        static readonly StringBuilder report=new();
        static PlayerController player;
        static Keyboard keyboard;
        static InputSettings originalInputSettings;

        [InitializeOnLoadMethod]
        static void Init() => EditorApplication.playModeStateChanged += Changed;

        // Batch invocation must omit -quit; the verifier exits after returning to Edit Mode.
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            SessionState.SetBool(Flag,true);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                report.Clear(); started=EditorApplication.timeSinceStartup; next=0; lastFrame=-1;
                Application.targetFrameRate=60; routine=Verify(); EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Flag,false);
                EditorApplication.Exit(SessionState.GetInt(Flag+".Exit",1));
            }
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>300)throw new Exception("Play verification timeout");
                if(Time.frameCount==lastFrame||Time.time<next)return;
                lastFrame=Time.frameCount;
                if(!routine.MoveNext()){Finish(null);return;}
                next=Time.time+(routine.Current is float wait?wait:0);
            }
            catch(Exception error){Finish(error);}
        }
        static void Finish(Exception error)
        {
            EditorApplication.update-=Tick;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalInputSettings!=null)InputSystem.settings=originalInputSettings;
            report.AppendLine(error==null?"PASS: automated Play Mode checks completed.":"FAIL: "+error);
            File.WriteAllText(Output+"/play-verification.txt",report.ToString());
            Debug.Log(report.ToString());
            SessionState.SetInt(Flag+".Exit",error==null?0:1);
            EditorApplication.ExitPlaymode();
        }
        static void Require(bool ok,string message)
        {if(!ok)throw new Exception(message);report.AppendLine("PASS: "+message);}
        static Room Room(string id)=>Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Single(r=>r.RoomId==id);
        static PuzzleBase Puzzle(string id)=>Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None).Single(p=>p.PuzzleId==id);
        static void Teleport(Vector3 point)
        {
            player.Controller.enabled=false;player.transform.position=point;player.Controller.enabled=true;
            Physics.SyncTransforms();
        }
        static IEnumerator Walk(Vector3 target)
        {
            float end=Time.time+12;
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z))>.15f)
            {
                if(Time.time>end)throw new Exception("Walk blocked at "+player.transform.position+" toward "+target+"; nearby: "+string.Join(", ",Physics.OverlapSphere(player.transform.position+Vector3.up*.5f,1.2f).Select(c=>c.name)));
                Vector3 delta=target-player.transform.position;delta.y=0;
                player.Controller.Move(Vector3.ClampMagnitude(delta,4.5f*Time.deltaTime)+Vector3.down*4*Time.deltaTime);
                if(player.transform.position.y<target.y-.35f)throw new Exception("Fell during crossing at "+player.transform.position);
                yield return 0f;
            }
        }
        static void Capture(string name)
        {
            var camera=Camera.main;var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);
        }
        static IEnumerator Verify()
        {
            yield return 1f;
            player=Object.FindFirstObjectByType<PlayerController>();player.enabled=false;
            var level=JsonUtility.FromJson<LevelDef>(File.ReadAllText("Assets/_BookTower/Level/level_layout.json"));
            // 가장 큰 내부 단차가 있는 마지막 장 출구부터 검사해 회귀를 빨리 잡는다.
            foreach(var r in level.ramps.OrderByDescending(x=>x.fromId=="3B"?2:x.fromId=="3A"?1:0))
            {
                var dir=(r.to.V-r.from.V).normalized;
                var forward=Vector3.ProjectOnPlane(dir,Vector3.up).normalized;
                var right=Vector3.Cross(Vector3.up,forward);
                var gate=Object.FindObjectsByType<RampGate>(FindObjectsSortMode.None).FirstOrDefault(g=>g.name=="잠금_"+r.fromId);
                if(gate==null)continue;
                var gateForward=Vector3.ProjectOnPlane(dir,Vector3.up).normalized;
                Require(Quaternion.Angle(gate.transform.localRotation,Quaternion.LookRotation(gateForward)*Quaternion.AngleAxis(90,Vector3.left))<.1f,r.name+" closed gate preserves horizontal doorway orientation");
                var gateCols=gate.GetComponentsInChildren<Collider>();
                Require(gateCols.Any(c=>c.enabled && !c.isTrigger),r.name+" has an enabled solid gate collider ("+string.Join(", ",gateCols.Select(c=>c.name+":"+c.enabled+":"+c.bounds))+ ")");
                Require(gateCols.Where(c=>c.enabled&&!c.isTrigger).All(c=>!Physics.GetIgnoreCollision(c,player.Controller)),r.name+" gate collision is not ignored by player");
                foreach(float side in new[]{0f,-r.width*.5f+.35f,r.width*.5f-.35f,-r.width*.5f-.5f,r.width*.5f+.5f})
                {
                    var source=level.areas.Single(a=>a.id==r.fromId);
                    var bounds=new Bounds(source.pos.V,new Vector3(source.size.x,20,source.size.z));
                    float approach=5f;Vector3 start;
                    do {start=r.from.V-forward*approach+right*side+Vector3.up*.15f;approach+=.25f;}
                    while(approach<12 && (start.x<bounds.min.x+.5f || start.x>bounds.max.x-.5f || start.z<bounds.min.z+.5f || start.z>bounds.max.z-.5f));
                    Require(Room(r.fromId).Contains(start),r.name+" test starts inside source room");
                    Teleport(start);
                    // CharacterController는 프레임당 한 번 이동한다. 한 프레임 안에서
                    // 160번 연속 Move하면 PhysX 접촉 캐시를 우회해 얇은 문을 통과하는
                    // 테스트 자체의 오류가 생긴다.
                    for(int i=0;i<160;i++)
                    {
                        player.Controller.Move(forward*.1f+Vector3.down*.04f);
                        yield return 0f;
                    }
                    float progress=Vector3.Dot(player.transform.position-r.from.V,forward);
                    // V2의 비정형 바닥에서는 문틀 바깥 접근이 방 바닥 끝에서 먼저 떨어지는 것도
                    // 정상적인 차단이다. 목표 방에 들어가지 못했고 안전 복귀 높이 아래라면 통과가 아니다.
                    bool fellBeforeGate=player.transform.position.y<source.pos.y-.5f;
                    bool enteredTarget=Room(r.toId).Contains(player.transform.position);
                    bool blocked=!enteredTarget && (progress<.6f || fellBeforeGate ||
                        (Mathf.Abs(side)>r.width*.5f && Room(r.fromId).Contains(player.transform.position)));
                    Require(blocked,r.name+" locked mouth blocks offset "+side+" (progress "+progress.ToString("F2")+", position "+player.transform.position+")");
                }
            }
            foreach(var gate in Object.FindObjectsByType<RampGate>(FindObjectsSortMode.None))gate.OpenNow();
            yield return .2f;
            foreach(var r in level.ramps)
            foreach(bool reverse in new[]{false,true})
            {
                Vector3 forward=Vector3.ProjectOnPlane(r.to.V-r.from.V,Vector3.up).normalized;
                Teleport((reverse?r.to.V+forward*1.8f:r.from.V-forward*1.8f)+Vector3.up*.2f);
                Vector3 target=reverse?r.from.V-forward*1.8f:r.to.V+forward*1.8f;
                bool captured=false;
                float end=Time.time+25;
                while(Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up).magnitude>.15f)
                {
                    if(Time.time>end)throw new Exception(r.name+" crossing blocked at "+player.transform.position);
                    Vector3 delta=Vector3.ProjectOnPlane(target-player.transform.position,Vector3.up);
                    player.Controller.Move(Vector3.ClampMagnitude(delta,7*Time.deltaTime)+Vector3.down*5*Time.deltaTime);
                    var t=Mathf.Clamp01(Vector3.Dot(player.transform.position-r.from.V,forward)/Vector3.Dot(r.to.V-r.from.V,forward));
                    float floor=Mathf.Lerp(r.from.V.y,r.to.V.y,t);
                    if(player.transform.position.y<floor-.45f)throw new Exception(r.name+" fell at "+player.transform.position);
                    if(!reverse && !captured && t>.4f && t<.7f){captured=true;Require(Room(r.fromId).IsVisible && Room(r.toId).IsVisible,r.name+" both endpoint rooms visible during crossing");Capture(r.fromId+"-"+r.toId);}
                    yield return 0f;
                }
                Require(true,r.name+" opened bridge and both landings traversed "+(reverse?"reverse":"forward"));
            }
        }
    }
}


