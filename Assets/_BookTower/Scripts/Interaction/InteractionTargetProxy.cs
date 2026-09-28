using UnityEngine;
namespace BookTower.Interaction
{
    // A piece of furniture may share the readable object resting on it as its interaction.
    // This changes only target resolution; it never skips an occluding wall or range check.
    [DisallowMultipleComponent]
    public sealed class InteractionTargetProxy : MonoBehaviour
    {
        public Interactable target;
        public static Interactable Resolve(Collider collider)
        {
            if(!collider)return null;
            var proxy=collider.GetComponent<InteractionTargetProxy>();
            return proxy&&proxy.target?proxy.target:collider.GetComponentInParent<Interactable>();
        }
    }
}
