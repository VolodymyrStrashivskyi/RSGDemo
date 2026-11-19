using UnityEngine;

namespace _Project.Develop.Runtime.Common.Raycast
{
    public class RaycastService : IRaycastService
    {
        private const int MAX_HITS = 16;

        private readonly RaycastHit[] _hits = new RaycastHit[MAX_HITS];

        public bool TryRaycast(Ray ray, float distance, LayerMask mask, out RaycastHit hit) =>
            Physics.Raycast(ray, out hit, distance, mask, QueryTriggerInteraction.Ignore);

        public bool TryRaycastFarthest(Ray ray, float distance, LayerMask mask, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(ray, _hits, distance, mask, QueryTriggerInteraction.Ignore);
            hit = default;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance > hit.distance)
                    hit = _hits[i];
            }

            return count > 0;
        }

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
