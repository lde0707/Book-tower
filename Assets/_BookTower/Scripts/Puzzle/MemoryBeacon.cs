using UnityEngine;

namespace BookTower.Puzzles
{
    public class MemoryBeacon : MonoBehaviour
    {
        public PuzzleBase[] prerequisites;
        public Light glow;
        public bool IsLit { get; private set; }
        public bool Ready
        {
            get
            {
                if (prerequisites == null || prerequisites.Length == 0) return false;
                foreach (var p in prerequisites) if (p == null || p.State != PuzzleBase.PuzzleState.Solved) return false;
                return true;
            }
        }
        public bool TryIgnite(Vector3 playerPosition)
        {
            if (!Ready || Vector3.Distance(playerPosition,transform.position)>3.5f) return false;
            IsLit=true;
            if(glow!=null) glow.intensity=3;
            return true;
        }
    }
}
