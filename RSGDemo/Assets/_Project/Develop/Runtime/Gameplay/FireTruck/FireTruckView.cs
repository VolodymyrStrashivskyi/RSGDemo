using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Develop.Runtime.Gameplay.FireTruck
{
    public class FireTruckView : MonoBehaviour
    {
        [SerializeField] private Transform _intakePoint;
        [SerializeField, FormerlySerializedAs("_hoseCoilPoint")] private Transform _hoseLayPoint;

        public Transform IntakePoint => _intakePoint;

        public Transform HoseLayPoint => _hoseLayPoint;
    }
}
