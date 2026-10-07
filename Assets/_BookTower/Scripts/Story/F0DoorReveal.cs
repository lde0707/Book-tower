using UnityEngine;
using BookTower.CameraSystem;
namespace BookTower.Story
{
 public sealed partial class OpeningStoryDirector
 {
  Camera revealCamera;Vector3 revealStart,revealEnd;Quaternion revealRotation,revealStartRotation;float revealTime,revealFov;
  public Bounds DoorRevealBounds {get;private set;}
  void BeginDoorReveal()
  {
   if(!storyCamera)return;
   revealCamera=storyCamera.GetComponent<Camera>();if(!revealCamera)return;
   revealStart=storyCamera.transform.position;revealStartRotation=storyCamera.transform.rotation;revealFov=revealCamera.fieldOfView;revealTime=0;
   bool first=true;Bounds bounds=default;Transform basis=null;
   foreach(var door in openingDoors)
   {
    if(!door||door.puzzle!=EntrancePuzzle)continue;basis=door.left.parent;
    var l=door.left.localRotation;var r=door.right.localRotation;
    for(int a=0;a<=180;a+=15){door.left.localRotation=Quaternion.Euler(0,Mathf.Min(a,170),0);door.right.localRotation=Quaternion.Euler(0,-Mathf.Min(a,170),0);
     foreach(var leaf in new[]{door.left,door.right})foreach(var renderer in leaf.GetComponentsInChildren<Renderer>()){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}}
    door.left.localRotation=l;door.right.localRotation=r;
   }
   if(first)return;
   DoorRevealBounds=bounds;
   Vector3 forward=((basis?basis.forward:Vector3.forward)-Vector3.up*.28f).normalized;
   revealRotation=Quaternion.LookRotation(forward,Vector3.up);
   var up=revealRotation*Vector3.up;var right=revealRotation*Vector3.right;
   float ex=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(right.x),Mathf.Abs(right.y),Mathf.Abs(right.z)));
   float ey=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(up.x),Mathf.Abs(up.y),Mathf.Abs(up.z)));
   float ez=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(forward.x),Mathf.Abs(forward.y),Mathf.Abs(forward.z)));
   float tan=Mathf.Tan(55*Mathf.Deg2Rad*.5f);float distance=Mathf.Max(ey/tan,ex/(tan*revealCamera.aspect))*1.08f+ez;
   revealEnd=bounds.center-forward*distance;
   storyCamera.SetMode(PaperCamera.CameraMode.Cinematic);
  }
  void UpdateDoorReveal()
  {
   if(!revealCamera)return;revealTime+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(revealTime/.85f));
   storyCamera.transform.SetPositionAndRotation(Vector3.Lerp(revealStart,revealEnd,t),Quaternion.Slerp(revealStartRotation,revealRotation,t));revealCamera.fieldOfView=Mathf.Lerp(revealFov,55,t);
  }
  void EndDoorReveal(){if(revealCamera)revealCamera.fieldOfView=revealFov;revealCamera=null;}
 }
}
