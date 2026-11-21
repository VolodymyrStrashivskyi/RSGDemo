using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSpotFollower : MonoBehaviour
    {
        private const float MIN_STEP = 0.01f;

        private FireSpot _spot;
        private FireSpotMarker _marker;

        public void Bind(FireSpot spot, FireSpotMarker marker)
        {
            _spot = spot;
            _marker = marker;
        }

        private void Update()
        {
            if (_spot == null)
                return;

            Vector3 position = _marker.SpotPosition;

            if ((position - _spot.Position).sqrMagnitude >= MIN_STEP * MIN_STEP)
                _spot.MoveTo(position);
        }
    }
}
