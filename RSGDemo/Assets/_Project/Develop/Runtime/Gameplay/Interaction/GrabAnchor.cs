using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public class GrabAnchor : MonoBehaviour
    {
        [SerializeField] private Transform _carrier;

        public Vector3 CarrierPosition => _carrier.position;

        public void Attach(Transform item) =>
            item.SetParent(transform, true);
    }
}
