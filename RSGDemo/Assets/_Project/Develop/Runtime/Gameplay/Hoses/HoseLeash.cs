using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Utils;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseLeash : IMovementConstraint
    {
        private readonly HoseRope _rope;
        private readonly HoseTension _tension;
        private readonly float _slack;

        public HoseLeash(HoseRope rope, HoseTension tension, float slack)
        {
            _rope = rope;
            _tension = tension;
            _slack = slack;
        }

        public Vector3 Pull => _tension.Pull;

        public Vector3 Constrain(Vector3 currentPosition, Vector3 desiredPosition)
        {
            Vector3 withinReach = KeepWithin(_rope.Anchor, _rope.FreeLength + _slack, currentPosition, desiredPosition);
            Vector3 pivot = _rope.FindLeashPivot();
            Vector3 strained = ScalePullingAway(pivot, currentPosition, withinReach, _tension.StrainSpeedRatio);

            if (_tension.IsTaut == false)
                return strained;

            Vector3 stopped = ScalePullingAway(pivot, currentPosition, strained, 0f);
            return DragBack(pivot, stopped, _tension.PullBackSpeed * Time.deltaTime);
        }

        private static Vector3 ScalePullingAway(Vector3 pivot, Vector3 currentPosition, Vector3 desiredPosition, float outwardShare)
        {
            Vector2 awayFromPivot = GetPlanarOffset(pivot, currentPosition).normalized;
            Vector2 step = GetPlanarOffset(currentPosition, desiredPosition);
            float outward = Vector2.Dot(step, awayFromPivot);

            if (outward <= 0f)
                return desiredPosition;

            step -= awayFromPivot * (outward * (1f - outwardShare));
            return new Vector3(currentPosition.x + step.x, desiredPosition.y, currentPosition.z + step.y);
        }

        private static Vector3 DragBack(Vector3 pivot, Vector3 desiredPosition, float distance)
        {
            if (distance <= 0f)
                return desiredPosition;

            Vector2 towardPivot = GetPlanarOffset(desiredPosition, pivot).normalized * distance;
            return new Vector3(desiredPosition.x + towardPivot.x, desiredPosition.y, desiredPosition.z + towardPivot.y);
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
            (to - from).ToGroundPlane();
    }
}
