using UnityEngine;
using BookTower.PlayerSystem;
using BookTower.Rooms;
namespace BookTower.Puzzles
{
 public sealed partial class FoldBridgePuzzle : PuzzleBase
 {
  public Transform hinge,source,receiver,sealVisual;
  public BoxCollider pageCollider;
  public LineRenderer beam,connection;
  public Light receiverLight;
  public Transform roomSpace;
  public AudioClip foldSound;
  public bool BridgeDown {get;private set;}
  public bool IsSettled {get;private set;}=true;
  public bool LightConnected {get;private set;}
  public int Recoveries {get;private set;}
  public int FoldCount {get;private set;}
  float angle=-90,lastUse=-10;bool requested,farBank;PlayerController actor;Room room;AudioSource audioSource;
  Vector3 loweredSeal;
  protected override void OnSolved()
  {
   loweredSeal=sealVisual.parent.localPosition-Vector3.up*1.4f;
   var blocker=sealVisual.parent.GetComponent<Collider>();if(blocker)blocker.enabled=false;
  }
  void Awake(){actor=FindFirstObjectByType<PlayerController>();room=GetComponentInParent<Room>();audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=1;audioSource.volume=.4f;audioSource.minDistance=2;audioSource.maxDistance=25;audioSource.playOnAwake=false;BookTower.UI.PaperAudioLevel.Bind(audioSource);hinge.localRotation=Quaternion.Euler(-90,0,0);}
  public bool TryOperate(bool seal,Vector3 position,Vector3 controlPosition,float range)
  {
   if(!isActiveAndEnabled||(room&&!room.IsVisible)||Vector3.Distance(position,controlPosition)>range||Time.time-lastUse<.35f)return false;
   if(seal){if(!LightConnected||!IsSettled||State==PuzzleState.Solved){BookTower.UI.PaperUI.Notify(State==PuzzleState.Solved?"이미 열린 걸쇠입니다.":"빛이 닿지 않는다.");return false;}requested=true;lastUse=Time.time;return true;}
   if(!IsSettled){BookTower.UI.PaperUI.Notify("페이지가 움직이고 있어.");return false;}
   Vector3 p=roomSpace.InverseTransformPoint(position);
   if(p.z>-1.5f&&p.z<7.6f&&Mathf.Abs(p.x)<2.8f&&p.y>2){BookTower.UI.PaperUI.Notify("페이지 위에서는 접을 수 없어.");return false;}
   lastUse=Time.time;BridgeDown=!BridgeDown;FoldCount++;if(foldSound)audioSource.PlayOneShot(foldSound);return true;
  }
  protected override bool CheckSolved()=>requested&&LightConnected&&IsSettled;
  protected override void Update()
  {
   angle=Mathf.MoveTowards(angle,BridgeDown?0:-90,55*Time.deltaTime);hinge.localRotation=Quaternion.Euler(angle,0,0);IsSettled=Mathf.Abs(angle-(BridgeDown?0:-90))<.05f;
   UpdateLightRoute();
   connection.enabled=LightConnected&&(room==null||room.IsVisible);receiverLight.intensity=DirectReceiverLit?12:0;
   sealVisual.localScale=Vector3.one*(LightConnected?1:.55f);
   base.Update();
   if(State==PuzzleState.Solved)sealVisual.parent.localPosition=Vector3.MoveTowards(sealVisual.parent.localPosition,loweredSeal,Time.deltaTime*1.4f);
   if(!actor||!room||!room.IsVisible)return;
   Vector3 p=roomSpace.InverseTransformPoint(actor.transform.position);
   if(Mathf.Abs(p.x)<16&&actor.Controller.isGrounded&&p.y>2.7f){if(p.z>7.7f&&p.z<12)farBank=true;else if(p.z<-2)farBank=false;}
   if(Mathf.Abs(p.x)<16.1f&&p.z>-1.5f&&p.z<7.7f&&p.y<2){actor.Controller.enabled=false;actor.transform.position=roomSpace.TransformPoint(new Vector3(0,3.2f,farBank?9:-4));actor.Controller.enabled=true;actor.VerticalVelocity=0;actor.HorizontalVelocity=Vector3.zero;Recoveries++;}
  }
 }
}
