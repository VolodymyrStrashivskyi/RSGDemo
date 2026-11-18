using UnityEngine;

namespace _Project.Develop.Runtime.Common.Raycast
{
    public class RaycastService : IRaycastService
    {
        public bool TryRaycast(Ray ray, float distance, LayerMask mask, out RaycastHit hit) =>
            Physics.Raycast(ray, out hit, distance, mask, QueryTriggerInteraction.Ignore);

        public bool TryFind<T>(Ray ray, float distance, LayerMask mask, out T target, out RaycastHit hit) where T : class
        {
            target = null;

            if (TryRaycast(ray, distance, mask, out hit) == false)
                return false;

            target = hit.collider.GetComponentInParent<T>();
            return target != null;
        }
    }
}
