using UnityEngine;

namespace _Project.Develop.Runtime.Common.Raycast
{
    public interface IRaycastService
    {
        bool TryRaycast(Ray ray, float distance, LayerMask mask, out RaycastHit hit);
        bool TryRaycastFarthest(Ray ray, float distance, LayerMask mask, out RaycastHit hit);
        bool TryFind<T>(Ray ray, float distance, LayerMask mask, out T target, out RaycastHit hit) where T : class;
        bool TrySphereCast(Ray ray, float radius, float distance, LayerMask mask, out RaycastHit hit);
        bool TryPushOutSphere(Vector3 center, float radius, LayerMask mask, float stepHeight, out Vector3 freeCenter);
        bool IsAnyColliderNear(Vector3 center, float radius, LayerMask mask);
        bool TryFindBody(Vector3 center, float radius, LayerMask mask, out Rigidbody body);
        bool HasHeadroom(Collider collider, Vector3 surfacePoint, float radius);
        bool TryEscapeHollow(Vector3 center, float radius, float distance, LayerMask mask, out Vector3 freeCenter);
    }
}
