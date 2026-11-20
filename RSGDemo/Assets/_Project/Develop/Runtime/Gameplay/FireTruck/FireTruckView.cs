using _Project.Develop.Runtime.Gameplay.Hoses.Attack;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Develop.Runtime.Gameplay.FireTruck
{
    public class FireTruckView : MonoBehaviour
    {
        [SerializeField] private Transform _intakePoint;
        [SerializeField, FormerlySerializedAs("_hoseCoilPoint")] private Transform _hoseLayPoint;
        [SerializeField] private HoseReel _hoseReel;
        [SerializeField] private Transform _nozzleHookPoint;

        public Transform IntakePoint => _intakePoint;

        public Transform HoseLayPoint => _hoseLayPoint;

        public HoseReel HoseReel => _hoseReel;

        public Transform NozzleHookPoint => _nozzleHookPoint;
    }
}
