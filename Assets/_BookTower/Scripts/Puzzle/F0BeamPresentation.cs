using UnityEngine;
namespace BookTower.Puzzles
{
 public sealed partial class FoldBridgePuzzle
 {
  LineRenderer[] halos;Material beamCore,beamHalo;
  void UpdateBeamPresentation()
  {
   if(halos!=null||lightSegments==null)return;
   var shader=Resources.Load<Shader>("F0PaperLight");if(!shader)return;
   beamCore=new Material(shader);beamCore.SetColor("_Color",new Color(3.8f,2.8f,1.2f,.9f));
   beamHalo=new Material(shader);beamHalo.SetColor("_Color",new Color(1.5f,.75f,.18f,.28f));
   halos=new LineRenderer[lightSegments.Length+1];
   for(int i=0;i<halos.Length;i++){
    var original=i<lightSegments.Length?lightSegments[i]:connection;if(!original)continue;
    original.sharedMaterial=beamCore;original.startWidth=original.endWidth=.10f;
    var go=new GameObject("빛줄기 부드러운 금빛 번짐");go.transform.SetParent(transform,false);var halo=go.AddComponent<LineRenderer>();halo.sharedMaterial=beamHalo;halo.useWorldSpace=true;halo.startWidth=halo.endWidth=.48f;halo.numCapVertices=6;halo.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;halo.receiveShadows=false;halos[i]=halo;
   }
  }
  void LateUpdate(){if(halos==null)return;for(int i=0;i<halos.Length;i++){var line=i<lightSegments.Length?lightSegments[i]:connection;var halo=halos[i];if(!halo||!line)continue;halo.enabled=line.enabled&&(room==null||room.IsVisible);halo.positionCount=line.positionCount;for(int j=0;j<line.positionCount;j++)halo.SetPosition(j,line.useWorldSpace?line.GetPosition(j):line.transform.TransformPoint(line.GetPosition(j)));}}
  void OnDestroy(){if(beamCore)Destroy(beamCore);if(beamHalo)Destroy(beamHalo);}
 }
}
