using UnityEngine;
namespace BookTower.Puzzles
{
 // A visible foil face reflects; paper receivers absorb. No switch combination is a solution.
 public sealed class F0LightSurface : MonoBehaviour
 {
  public enum Kind { Mirror, Exit, Flower }
  public Kind kind;
  public Light glow;
  public Transform petals;
  public bool Lit { get; set; }
  float openness;
  void Update()
  {
   openness=Mathf.MoveTowards(openness,Lit?1:0,Time.deltaTime*1.6f);
   if(glow)glow.intensity=openness*12;
   if(petals)foreach(Transform petal in petals)
    petal.localRotation=Quaternion.Euler(-15-60*openness,0,petal.GetSiblingIndex()*60);
  }
 }
}
