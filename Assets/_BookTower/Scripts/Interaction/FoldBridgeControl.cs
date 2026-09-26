using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Interaction
{
 [RequireComponent(typeof(Interactable))]
 public sealed class FoldBridgeControl : MonoBehaviour
 {
  public FoldBridgePuzzle puzzle;public bool seal;
  // 0: bridge/seal, 1: paper shutter, 2: entrance reflector, 3: far reflector.
  public int opticalControl;
  public bool TryUse(Vector3 actor)=>puzzle&&(opticalControl==0
   ?puzzle.TryOperate(seal,actor,transform.position,GetComponent<Interactable>().Range)
   :puzzle.TryAdjustLight(opticalControl,actor,transform.position,GetComponent<Interactable>().Range));
 }
}
