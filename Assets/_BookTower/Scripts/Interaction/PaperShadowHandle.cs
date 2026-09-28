using UnityEngine;
using BookTower.Puzzles;
namespace BookTower.Interaction
{
    [RequireComponent(typeof(Interactable))]
    public sealed class PaperShadowHandle:MonoBehaviour
    {
        public StorybookLightPuzzle puzzle;
        public bool isEar;
    }
}
