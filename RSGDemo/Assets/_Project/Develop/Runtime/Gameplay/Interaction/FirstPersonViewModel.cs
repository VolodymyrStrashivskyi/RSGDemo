using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public class FirstPersonViewModel : MonoBehaviour, IHandGrips
    {
        [SerializeField] private Transform _leftHandGrip;
        [SerializeField] private Transform _rightHandGrip;
        [SerializeField, Tooltip("Where the first-person copy of the hose joins, facing along the hose")]
        private Transform _hoseEnd;
        [SerializeField, Tooltip("Where water leaves the first-person copy, facing along the stream; empty for items that do not spray")]
        private Transform _muzzle;
        [SerializeField, Tooltip("Position in front of the first-person camera, m")]
        private Vector3 _viewPosition;
        [SerializeField, Tooltip("Rotation relative to the first-person camera, degrees")]
        private Vector3 _viewRotation;

        public Transform LeftHandGrip => _leftHandGrip;

        public Transform RightHandGrip => _rightHandGrip;

        public Transform HoseEnd => _hoseEnd;

        public Transform Muzzle => _muzzle;

        public bool IsShown => gameObject.activeSelf;

        public void Show(Transform viewpoint)
        {
            transform.SetParent(viewpoint, false);
            transform.SetLocalPositionAndRotation(_viewPosition, Quaternion.Euler(_viewRotation));
            gameObject.SetActive(true);
        }

        public void Hide() =>
            gameObject.SetActive(false);
    }
}
