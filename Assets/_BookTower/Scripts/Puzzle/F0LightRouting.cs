using UnityEngine;
namespace BookTower.Puzzles
{
 public sealed partial class FoldBridgePuzzle
 {
  public Transform shutter,entranceMirror,farMirror;
  public F0LightSurface[] lightSurfaces;
  public LineRenderer[] lightSegments;
  public bool ShutterOpen {get;private set;}
  public int EntranceDirection {get;private set;}
  public int FarDirection {get;private set;}
  public bool OpticsSettled {get;private set;}
  public bool DirectReceiverLit {get;private set;}
  public string BeamStoppedAt {get;private set;}
  public bool TryAdjustLight(int control,Vector3 position,Vector3 handle,float range)
  {
   if(!isActiveAndEnabled||(room&&!room.IsVisible)||Time.timeScale==0||
      Vector3.Distance(position,handle)>range||Time.time-lastUse<.35f||!OpticsSettled)return false;
   if(control==1)ShutterOpen=!ShutterOpen;
   else if(control==2)EntranceDirection=(EntranceDirection+1)%3;
   else if(control==3)FarDirection=(FarDirection+1)%3;
   else return false;
   lastUse=Time.time;OpticsSettled=false;
   BookTower.Audio.SfxDirector.PlayMechanism("mechanism_press",handle,.65f);
   BookTower.Audio.SfxDirector.PlayMechanism(control==1?"foil_move":"mirror_move",(control==1?shutter:control==2?entranceMirror:farMirror).position,.8f);
   return true;
  }
  static Quaternion Face(Vector3 incoming,Vector3 outgoing)=>Quaternion.LookRotation((incoming-outgoing).normalized);
  void UpdateLightRoute()
  {
   UpdateBeamPresentation();
   if(!shutter){LightConnected=false;return;}
   Quaternion cover=Quaternion.Euler(0,0,ShutterOpen?100:0);
   Vector3 firstPosition=new(EntranceDirection==0?2.1f:0,6,3);
   Quaternion first=EntranceDirection==0?Quaternion.identity:Face(Vector3.down,EntranceDirection==1?Vector3.left:Vector3.right);
   Quaternion last=Face(Vector3.forward,FarDirection==0?Vector3.right:FarDirection==1?Vector3.back:Vector3.left);
   shutter.localRotation=Quaternion.RotateTowards(shutter.localRotation,cover,100*Time.deltaTime);
   entranceMirror.localPosition=Vector3.MoveTowards(entranceMirror.localPosition,firstPosition,3*Time.deltaTime);
   entranceMirror.localRotation=Quaternion.RotateTowards(entranceMirror.localRotation,first,100*Time.deltaTime);
   farMirror.localRotation=Quaternion.RotateTowards(farMirror.localRotation,last,100*Time.deltaTime);
   OpticsSettled=Quaternion.Angle(shutter.localRotation,cover)<.1f&&
    Vector3.Distance(entranceMirror.localPosition,firstPosition)<.01f&&
    Quaternion.Angle(entranceMirror.localRotation,first)<.1f&&Quaternion.Angle(farMirror.localRotation,last)<.1f;
   Physics.SyncTransforms();
   foreach(var s in lightSurfaces)s.Lit=false;
   foreach(var segment in lightSegments)segment.enabled=false;
   beam.enabled=false;LightConnected=false;DirectReceiverLit=false;
   bool visible=room==null||room.IsVisible;
   Vector3 origin=source.position+roomSpace.TransformDirection(Vector3.down)*.4f;
   Vector3 direction=roomSpace.TransformDirection(Vector3.down);
   Collider previous=null;
   // Bound the bounce count so facing mirrors cannot loop forever.
   for(int bounce=0;bounce<lightSegments.Length;bounce++)
   {
    RaycastHit nearest=default;float distance=40;bool found=false;
    foreach(var hit in Physics.RaycastAll(origin,direction,40,~0,QueryTriggerInteraction.Ignore))
    {
     if(hit.collider==previous||hit.collider.GetComponentInParent<BookTower.PlayerSystem.PlayerController>()||hit.distance>=distance)continue;
     nearest=hit;distance=hit.distance;found=true;
    }
    var line=lightSegments[bounce];line.enabled=visible;line.SetPosition(0,origin);line.SetPosition(1,origin+direction*distance);
    BeamStoppedAt=found?nearest.collider.name:"room boundary";
    if(!found)break;
    var surface=nearest.collider.GetComponent<F0LightSurface>();
    if(!surface)break;
    surface.Lit=true;
    if(surface.kind==F0LightSurface.Kind.Exit){LightConnected=OpticsSettled&&IsSettled;break;}
    if(surface.kind==F0LightSurface.Kind.Flower){DirectReceiverLit=surface.transform.IsChildOf(receiver);break;}
    // The foil's visible local-Z face is also the optical plane.
    direction=Vector3.Reflect(direction,surface.transform.forward).normalized;
    origin=nearest.point+direction*.06f;previous=nearest.collider;
   }
  }
 }
}
