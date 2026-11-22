using UnityEngine;

namespace _Project.Develop.Runtime.Common.Raycast
{
    public class RaycastService : IRaycastService
    {
        private const int MAX_HITS = 16;
        private const int MAX_OVERLAPS = 8;
        private const int IGNORE_RAYCAST_LAYER = 2;
        private const string PROBE_NAME = "SphereProbe";
        private const float MIN_STEP_TOP_NORMAL = 0.7f;
        private const float HEADROOM_START = 0.005f;
        private const float HEADROOM_DIAMETERS = 2f;
        private const float HEADROOM_FIT_RATIO = 0.9f;

        private static readonly Vector3[] _escapeDirections =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back, Vector3.up, Vector3.down
        };

        private readonly RaycastHit[] _hits = new RaycastHit[MAX_HITS];
        private readonly Collider[] _overlaps = new Collider[MAX_OVERLAPS];

        private SphereCollider _probe;

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

        public bool TrySphereCast(Ray ray, float radius, float distance, LayerMask mask, out RaycastHit hit) =>
            Physics.SphereCast(ray, radius, out hit, distance, mask, QueryTriggerInteraction.Ignore);

        public bool TryPushOutSphere(Vector3 center, float radius, LayerMask mask, float stepHeight, out Vector3 freeCenter)
        {
            freeCenter = center;
            int count = Physics.OverlapSphereNonAlloc(center, radius, _overlaps, mask, QueryTriggerInteraction.Ignore);

            if (count == 0)
                return false;

            SphereCollider probe = GetProbe();
            probe.radius = radius;
            bool isPushed = false;

            for (int i = 0; i < count; i++)
            {
                Collider other = _overlaps[i];
                Transform otherTransform = other.transform;
                Ray stepRay = new Ray(freeCenter + Vector3.up * stepHeight, Vector3.down);

                bool isStep = other.Raycast(stepRay, out RaycastHit stepTop, stepHeight + radius)
                    && stepTop.normal.y > MIN_STEP_TOP_NORMAL
                    && HasHeadroom(other, stepTop.point, radius);

                if (isStep)
                {
                    freeCenter.y = Mathf.Max(freeCenter.y, stepTop.point.y + radius);
                    isPushed = true;
                    continue;
                }

                if (Physics.ComputePenetration(probe, freeCenter, Quaternion.identity, other, otherTransform.position, otherTransform.rotation, out Vector3 direction, out float depth))
                {
                    freeCenter += direction * depth;
                    isPushed = true;
                }
            }

            return isPushed;
        }

        public bool IsAnyColliderNear(Vector3 center, float radius, LayerMask mask) =>
            Physics.CheckSphere(center, radius, mask, QueryTriggerInteraction.Ignore);

        public bool TryFindBody(Vector3 center, float radius, LayerMask mask, out Rigidbody body)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _overlaps, mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                body = _overlaps[i].attachedRigidbody;

                if (body != null && body.isKinematic == false)
                    return true;
            }

            body = null;
            return false;
        }

        public bool HasHeadroom(Collider collider, Vector3 surfacePoint, float radius)
        {
            bool hitsBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Ray up = new Ray(surfacePoint + Vector3.up * HEADROOM_START, Vector3.up);
            bool isBlocked = collider.Raycast(up, out RaycastHit _, radius * HEADROOM_DIAMETERS);
            Physics.queriesHitBackfaces = hitsBackfaces;

            if (isBlocked)
                return false;

            SphereCollider probe = GetProbe();
            probe.radius = radius * HEADROOM_FIT_RATIO;
            Vector3 center = surfacePoint + Vector3.up * (radius + HEADROOM_START);
            Transform colliderTransform = collider.transform;
            return Physics.ComputePenetration(probe, center, Quaternion.identity, collider, colliderTransform.position, colliderTransform.rotation, out Vector3 _, out float _) == false;
        }

        public bool TryEscapeHollow(Vector3 center, float radius, float distance, LayerMask mask, out Vector3 freeCenter)
        {
            freeCenter = center;
            float nearest = float.MaxValue;
            bool hitsBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            foreach (Vector3 direction in _escapeDirections)
            {
                bool isInside = Physics.Raycast(center, direction, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore)
                    && Vector3.Dot(hit.normal, direction) > 0f;

                if (isInside && hit.distance < nearest)
                {
                    nearest = hit.distance;
                    freeCenter = hit.point + direction * radius;
                }
            }

            Physics.queriesHitBackfaces = hitsBackfaces;
            return nearest < float.MaxValue;
        }

        private SphereCollider GetProbe()
        {
            if (_probe != null)
                return _probe;

            GameObject probeObject = new GameObject(PROBE_NAME) { layer = IGNORE_RAYCAST_LAYER, hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(probeObject);
            _probe = probeObject.AddComponent<SphereCollider>();
            _probe.isTrigger = true;
            return _probe;
        }
    }
}
