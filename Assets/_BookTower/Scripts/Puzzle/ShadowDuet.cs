using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using BookTower.Interaction;
using BookTower.UI;
using BookTower.Rooms;
namespace BookTower.Puzzles
{
 public sealed class ShadowDuet:MonoBehaviour
 {
  public Transform foxBody,foxTail;
  public Light[] lamps;
  public Renderer[] indicators;
  public Renderer[] rabbitRecord,foxRecord;
  public Vector3[] bodyProbes,tailProbes,bodyTargets,tailTargets;
  public const float RabbitX=-2.7f,FoxX=2.8f;
  public const float ProjectionIntensity=115;
  // Screen-space metres: allow hand-placement / shadow-edge imprecision, while
  // requiring the whole outline (not just its centre) to agree with the drawing.
  public const float FoxContourTolerance=.22f,FoxAverageTolerance=.14f;
  public static readonly Vector3 FoxBodySolution=new(-.2222222f,1,12.5f/1.8f),FoxTailSolution=new(-1.28f,1,5);
  StorybookLightPuzzle puzzle;Room room;
  Vector3 bodyAt=new(3.2f,1,8.6f),tailAt=new(-4.4f,1,6.2f);
  float rabbitHold,foxHold;bool tailFlipped;
  bool foxWasAligned,foxWasHeld;float nextFoxAlignmentNotice;
  Renderer[] art;MaterialPropertyBlock block;
  public int LightIndex{get;private set;}=1;
  public bool RabbitRecorded{get;private set;}
  public bool FoxRecorded{get;private set;}
  public bool AllRecorded=>RabbitRecorded&&FoxRecorded;
  public bool Recorded(int index)=>index<2?RabbitRecorded:FoxRecorded;
  public Vector3 Position(int index)=>index==2?bodyAt:tailAt;
  // Recorded paper slides into the wings; the remembered projection remains on the page.
  // Logical positions stay unchanged so recording order cannot alter puzzle conditions.
  public Vector3 VisualPosition(int index,Vector3 original)=>!Recorded(index)?original:
   index switch {0=>new Vector3(-6.8f,1,3.4f),1=>new Vector3(-5.9f,1,2.2f),2=>new Vector3(6.8f,1,3.4f),_=>new Vector3(5.9f,1,2.2f)};
  public string Goal=>AllRecorded?"두 친구의 그림자가 돌아왔다 · 별등을 따라 다음 열람실로":RabbitRecorded?"여우의 그림자 되찾기 · 세 빛의 방향과 몸통·꼬리의 거리 맞추기":FoxRecorded?"토끼의 그림자 되찾기 · 몸통과 귀의 크기·방향 맞추기":"두 친구의 그림자 되찾기 · 세 방향의 빛으로 토끼와 여우 완성하기";
  Vector3 Source=>puzzle.stage.InverseTransformPoint(puzzle.lantern.transform.position);
  public void Setup(StorybookLightPuzzle p)
  {
   puzzle=p;room=p.GetComponentInParent<Room>();block=new MaterialPropertyBlock();
   var original=p.lantern;original.intensity=0;original.enabled=false;
   // Keep only the new three matching projectors, so the old fourth flower lamp is not misleading.
   original.transform.parent.gameObject.SetActive(false);
   foreach(var control in p.stage.GetComponentsInChildren<StorybookLightControl>())
    if(control.action==StorybookLightControl.Action.Lamp)control.gameObject.SetActive(false);
   foreach(var line in p.stage.GetComponentsInChildren<Transform>())if(line.name=="맞춰야 할 토끼 윤곽")line.localPosition+=Vector3.right*RabbitX;
   rabbitRecord=p.stage.GetComponentsInChildren<Renderer>().Where(r=>r.name=="맞춰야 할 토끼 윤곽").ToArray();
   for(int i=0;i<lamps.Length;i++){
    lamps[i].cookie=Resources.Load<Texture2D>("ReleaseProjectionCookie");
    lamps[i].color=original.color;lamps[i].range=24;lamps[i].spotAngle=100;lamps[i].innerSpotAngle=85;
    lamps[i].shadows=LightShadows.Hard;lamps[i].shadowBias=.012f;lamps[i].shadowNormalBias=.015f;lamps[i].shadowNearPlane=.15f;lamps[i].shadowResolution=LightShadowResolution.VeryHigh;
    var c=lamps[i].GetComponentInParent<StorybookLightControl>();c.puzzle=p;
   }
   SelectLight(1,false);
   foreach(var pair in new[]{(foxBody,2),(foxTail,3)}){var h=pair.Item1.GetComponent<PaperShadowHandle>();h.puzzle=p;h.extraPart=pair.Item2;h.FitWholeObject();}
   foxBody.localPosition=bodyAt;foxTail.localPosition=tailAt;foxTail.localScale=new Vector3(-1,1,1);
   foreach(var note in p.GetComponentInParent<Room>().GetComponentsInChildren<ReadableNote>())
    if(note.id=="1A.Book.Scale"||note.id=="1A.Book.Theatre")note.pages=new[]{
     "두 친구가 서로 다른 빛 아래 숨어 있다. 세 꽃등 중 하나를 켜고, 벽의 토끼와 여우 윤곽을 살펴보자.",
     "기둥이나 오림을 좌클릭으로 잡고 누른 채 옮긴다. 마우스 상하·좌우로 앞뒤와 옆을 함께 조절하고, 걸으면 오림도 같은 방향으로 따라온다. 놓으면 고정된다. 우클릭은 뒤집기, Shift는 정밀 조절.",
     "토끼는 곧게 들어오는 빛을 좋아했다. 등에 가까운 작은 귀는 커지고, 멀리 놓인 몸통은 작아진다.",
     "여우의 종이는 접혀 있다. 왼쪽 창에서 비스듬히 오는 빛이 주름을 펴 준다. 몸통과 꼬리는 서로 다른 거리에 놓아야 한다.",
     "손을 놓으면 맞춘 그림자가 벽에 기억된다. 두 친구가 모두 돌아오면 다음 장이 열린다."};
   art=GetComponentsInChildren<Renderer>(true);
  }
  public void SelectLight(int index,bool sound=true)
  {
   if(!puzzle||index<0||index>=lamps.Length)return;
   foreach(var light in lamps){light.intensity=0;light.enabled=false;}
   LightIndex=index;puzzle.lantern=lamps[index];lamps[index].enabled=room==null||room.IsVisible;lamps[index].intensity=ProjectionIntensity;
   if(sound)BookTower.Audio.SfxDirector.PlayMechanism("mechanism_press",lamps[index].transform.position,.55f);
  }
  public void Drag(int index,Vector3 at){if(Recorded(index))return;at=new Vector3(Mathf.Clamp(at.x,-5.5f,5.5f),1,Mathf.Clamp(at.z,3,10));if(index==2)bodyAt=at;else tailAt=at;}
  public void Flip(int index){if(index==3)tailFlipped=!tailFlipped;}
  public bool TailFlipped=>tailFlipped;
  public Vector3 Project(Vector3 position)
  {
   var s=Source;float dz=position.z-s.z;if(dz<.1f)return new Vector3(1000,1000,12.5f);
   return s+(position-s)*((StorybookLightPuzzle.ScreenDistance-s.z)/dz);
  }
  public bool RabbitAligned=>puzzle&&puzzle.LampOn&&puzzle.Flipped&&Mathf.Abs(puzzle.BodyRatio-1.5f)<.08f&&Mathf.Abs(puzzle.EarRatio-3)<.12f&&Mathf.Abs(Project(puzzle.BodyPosition).x-RabbitX)<.18f&&Mathf.Abs(Project(puzzle.EarPosition).x-RabbitX)<.18f;
  public bool FoxAligned=>puzzle&&puzzle.LampOn&&tailFlipped&&MatchesContour(foxBody,bodyProbes,bodyTargets)&&MatchesContour(foxTail,tailProbes,tailTargets);
  bool MatchesContour(Transform part,Vector3[] probes,Vector3[] targets)
  {
   float worst=MeasureProjection(part,probes,targets,out float average);
   return worst<=FoxContourTolerance&&average<=FoxAverageTolerance;
  }
  public float ProjectionError(Transform part,Vector3[] probes,Vector3[] targets)
   =>MeasureProjection(part,probes,targets,out _);
  float MeasureProjection(Transform part,Vector3[] probes,Vector3[] targets,out float average)
  {
   average=float.PositiveInfinity;
   if(!puzzle||!puzzle.stage||!puzzle.lantern||!part||probes==null||targets==null||probes.Length<3||probes.Length!=targets.Length)return float.PositiveInfinity;
   float worst=0,sum=0;
   for(int i=0;i<probes.Length;i++)
   {
    var local=puzzle.stage.InverseTransformPoint(part.TransformPoint(probes[i]));
    float error=Vector3.SqrMagnitude(Project(local)-targets[i]);
    if(!float.IsFinite(error))return float.PositiveInfinity;
    worst=Mathf.Max(worst,error);sum+=error;
   }
   average=Mathf.Sqrt(sum/probes.Length);return Mathf.Sqrt(worst);
  }
  public void Tick(float dt)
  {
   if(!puzzle)return;
   foxBody.localPosition=Vector3.MoveTowards(foxBody.localPosition,VisualPosition(2,bodyAt),dt*(FoxRecorded?3:9));foxTail.localPosition=Vector3.MoveTowards(foxTail.localPosition,VisualPosition(3,tailAt),dt*(FoxRecorded?3:9));foxTail.localScale=new Vector3(tailFlipped?1:-1,1,1);
   bool settled=!PaperShadowDrag.Holding&&(FoxRecorded||(Vector3.Distance(foxBody.localPosition,bodyAt)<.02f&&Vector3.Distance(foxTail.localPosition,tailAt)<.02f));
   bool rabbitSettled=RabbitRecorded||(Vector3.Distance(puzzle.body.localPosition,puzzle.BodyPosition)<.02f&&Vector3.Distance(puzzle.ears.localPosition,puzzle.EarPosition)<.02f);
   rabbitHold=!RabbitRecorded&&RabbitAligned&&settled&&rabbitSettled?rabbitHold+dt:0;
   bool foxAligned=!FoxRecorded&&FoxAligned;
   bool held=PaperShadowDrag.Holding;
   if(foxAligned&&(held&&!foxWasHeld||!foxWasAligned&&Time.unscaledTime>=nextFoxAlignmentNotice))
   {
    PaperUI.Notify(held?"여우 그림자가 맞았다 · 마우스를 놓으면 책장에 기록된다.":"여우 그림자가 맞았다 · 잠시 그대로 두면 책장에 기록된다.");
    nextFoxAlignmentNotice=Time.unscaledTime+2;
   }
   foxWasAligned=foxAligned;foxWasHeld=held;
   foxHold=foxAligned&&settled?foxHold+dt:0;
   if(rabbitHold>1.25f){RabbitRecorded=true;Stamp(puzzle.body,puzzle.ears,rabbitRecord);PaperUI.Notify("토끼의 그림자가 책장에 남았다.");}
   if(foxHold>1.25f){FoxRecorded=true;Stamp(foxBody,foxTail,foxRecord);PaperUI.Notify("여우의 그림자가 책장에 남았다.");}
  }
  void Stamp(Transform a,Transform b,Renderer[] record)
  {
   foreach(var t in new[]{a,b})foreach(var r in t.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
   foreach(var r in record){var ink=new Color(.70f,.36f,.10f);block.SetColor("_BaseColor",ink);block.SetColor("_Color",ink);block.SetColor("_EmissionColor",new Color(.28f,.11f,.02f));r.SetPropertyBlock(block);if(r is LineRenderer line){line.startColor=ink;line.endColor=ink;}}
   BookTower.Audio.SfxDirector.PlayMechanism("foil_move",a.position,.6f);
  }
  void LateUpdate()
  {
   if(!puzzle||room==null)return;foreach(var r in art)if(r)r.enabled=room.IsVisible;
   for(int i=0;i<lamps.Length;i++){lamps[i].enabled=room.IsVisible&&i==LightIndex;
    if(indicators!=null&&i<indicators.Length&&indicators[i]){bool on=i==LightIndex&&puzzle.LampOn;block.SetColor("_BaseColor",on?new Color(1,.85f,.35f):new Color(.25f,.19f,.12f));block.SetColor("_EmissionColor",on?new Color(3,1.9f,.3f):Color.black);indicators[i].SetPropertyBlock(block);}}
  }
 }
}
