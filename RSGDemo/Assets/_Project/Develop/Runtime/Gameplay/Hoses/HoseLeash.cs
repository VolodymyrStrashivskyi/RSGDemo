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
            Vector3 anchor = _rope.Anchor;
            Vector2 currentOffset = GetPlanarOffset(anchor, currentPosition);
            Vector2 desiredOffset = GetPlanarOffset(anchor, desiredPosition);
            float limit = Mathf.Max(_rope.FreeLength + _slack, currentOffset.magnitude);

            if (desiredOffset.magnitude <= limit)
                return desiredPosition;

            Vector2 clampedOffset = desiredOffset.normalized * limit;
            return new Vector3(anchor.x + clampedOffset.x, desiredPosition.y, anchor.z + clampedOffset.y);
        }

        private static Vector2 GetPlanarOffset(Vector3 from, Vector3 to) =>
            new Vector2(to.x - from.x, to.z - from.z);
    }
}
