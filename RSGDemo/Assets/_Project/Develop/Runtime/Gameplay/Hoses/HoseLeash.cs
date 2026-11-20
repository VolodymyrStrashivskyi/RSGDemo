using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseLeash : IMovementConstraint
    {
        private readonly HoseRope _rope;
        private readonly float _slack;

        public HoseLeash(HoseRope rope, float slack)
        {
            _rope = rope;
            _slack = slack;
        }

        public Vector3 Constrain(Vector3 currentPosition, Vector3 desiredPosition)
        {
            Vector3 withinReach = KeepWithin(_rope.Anchor, _rope.FreeLength + _slack, currentPosition, desiredPosition);

            return _rope.IsTaut
                ? StopPullingAway(_rope.FindLeashPivot(), currentPosition, withinReach)
                : withinReach;
        }

        private static Vector3 StopPullingAway(Vector3 pivot, Vector3 currentPosition, Vector3 desiredPosition)
        {
            Vector2 awayFromPivot = GetPlanarOffset(pivot, currentPosition).normalized;
            Vector2 step = GetPlanarOffset(currentPosition, desiredPosition);
            float outward = Vector2.Dot(step, awayFromPivot);

            if (outward <= 0f)
                return desiredPosition;

            step -= awayFromPivot * outward;
            return new Vector3(currentPosition.x + step.x, desiredPosition.y, currentPosition.z + step.y);
        }

        private static Vector3 KeepWithin(Vector3 center, float radius, Vector3 currentPosition, Vector3 desiredPosition)
        {
            Vector2 currentOffset = GetPlanarOffset(center, currentPosition);
            Vector2 desiredOffset = GetPlanarOffset(center, desiredPosition);
            float limit = Mathf.Max(radius, currentOffset.magnitude);

            if (desiredOffset.magnitude <= limit)
                return desiredPosition;

            Vector2 clampedOffset = desiredOffset.normalized * limit;
            return new Vector3(center.x + clampedOffset.x, desiredPosition.y, center.z + clampedOffset.y);
        }

        private static Vector2 GetPlanarOffset(Vector3 from, Vector3 to) =>
            new Vector2(to.x - from.x, to.z - from.z);
    }
}
