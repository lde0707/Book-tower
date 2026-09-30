using UnityEngine;
using BookTower.Interaction;
namespace BookTower.Puzzles
{
 public sealed partial class StorybookLightPuzzle
 {
  public ShadowDuet Duet {get;private set;}
  void SetupDuet()
  {
   var prefab=Resources.Load<GameObject>("ShadowDuet");
   if(prefab){var instance=Instantiate(prefab,stage);instance.name="토끼와 여우의 세 빛 그림자 극장";Duet=instance.GetComponent<ShadowDuet>();Duet.Setup(this);}
   foreach(var h in stage.GetComponentsInChildren<PaperShadowHandle>())h.FitWholeObject();
  }
  public Vector3 PiecePosition(int index)=>index<2?(index==0?bodyAt:earAt):Duet?Duet.Position(index):Vector3.zero;
  public void DragPiece(int index,Vector3 at){if(Complete||Duet&&Duet.Recorded(index))return;if(index<2)DragPaper(index==1,at);else if(Duet)Duet.Drag(index,at);}
  public void TurnPiece(int index){if(Complete||Duet&&Duet.Recorded(index))return;if(index<2)TurnPaper(index==1);else if(Duet)Duet.Flip(index);}
  public void SelectLight(int index){if(Duet&&!Complete)Duet.SelectLight(index);}
 }
}
