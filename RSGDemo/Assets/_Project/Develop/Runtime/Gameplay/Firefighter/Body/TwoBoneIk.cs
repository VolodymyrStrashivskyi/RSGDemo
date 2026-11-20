using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    public class TwoBoneIk
    {
        private const float MIN_REACH_RATIO = 0.01f;
        private const float MAX_REACH_RATIO = 0.999f;

        private readonly Transform _upper;
        private readonly Transform _lower;
        private readonly Transform _end;
        private readonly Quaternion _backOfHandCorrection;

        public TwoBoneIk(Transform upper, Transform lower, Transform end, Vector3 backOfHandLocalAxis)
        {
            _upper = upper;
            _lower = lower;
            _end = end;
            _backOfHandCorrection = Quaternion.FromToRotation(backOfHandLocalAxis, Vector3.forward);
        }

        public void Solve(Vector3 target, Vector3 hint, float weight)
        {
            if (weight <= 0f)
                return;

            Vector3 root = _upper.position;
            float upperLength = Vector3.Distance(root, _lower.position);
            float lowerLength = Vector3.Distance(_lower.position, _end.position);
            float fullLength = upperLength + lowerLength;

            Vector3 blendedTarget = Vector3.Lerp(_end.position, target, weight);
            Vector3 toTarget = blendedTarget - root;
            float reach = Mathf.Clamp(toTarget.magnitude, fullLength * MIN_REACH_RATIO, fullLength * MAX_REACH_RATIO);
            Vector3 direction = toTarget.normalized;

            Vector3 elbow = GetElbowPosition(root, direction, reach, upperLength, lowerLength, hint);
            Vector3 reachedTarget = root + direction * reach;

            _upper.rotation = Quaternion.FromToRotation(_lower.position - root, elbow - root) * _upper.rotation;
            _lower.rotation = Quaternion.FromToRotation(_end.position - _lower.position, reachedTarget - _lower.position) * _lower.rotation;
        }

        public void OrientEnd(Vector3 backOfHand, Vector3 fingers, float weight)
        {
            if (weight <= 0f)
                return;

            Quaternion gripRotation = Quaternion.LookRotation(backOfHand, fingers) * _backOfHandCorrection;
            _end.rotation = Quaternion.Slerp(_end.rotation, gripRotation, weight);
        }

        private static Vector3 GetElbowPosition(Vector3 root, Vector3 direction, float reach, float upperLength, float lowerLength, Vector3 hint)
        {
            float alongReach = (upperLength * upperLength - lowerLength * lowerLength + reach * reach) / (2f * reach);
            float bendHeight = Mathf.Sqrt(Mathf.Max(upperLength * upperLength - alongReach * alongReach, 0f));
            Vector3 bendDirection = Vector3.ProjectOnPlane(hint - root, direction).normalized;
            return root + direction * alongReach + bendDirection * bendHeight;
        }
    }
}
