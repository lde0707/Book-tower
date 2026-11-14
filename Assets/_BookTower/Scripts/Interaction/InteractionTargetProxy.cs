using UnityEngine;
namespace BookTower.Interaction
{
    // A piece of furniture may share the readable object resting on it as its interaction.
    // This changes only target resolution; it never skips an occluding wall or range check.
    [DisallowMultipleComponent]
    public sealed class InteractionTargetProxy : MonoBehaviour
    {
        public Interactable target;
        public float maximumHitDistance;
        public static Interactable Resolve(Collider collider, Vector3? hitPoint = null)
        {
            if(!collider)return null;
            var proxy=collider.GetComponent<InteractionTargetProxy>();
            if(proxy&&proxy.target&&(proxy.maximumHitDistance<=0||
                (hitPoint.HasValue&&Vector3.Distance(hitPoint.Value,proxy.target.transform.position)<=proxy.maximumHitDistance)))
                return proxy.target;
            return collider.GetComponentInParent<Interactable>();
        }
    }
}
