using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "WaterSupplyConfig", menuName = "Configs/Water Supply Config")]
    public class WaterSupplyConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Pump outlet pressure, bar")]
        public float PumpPressure { get; private set; } = 8f;
        [field: SerializeField, Min(0f), Tooltip("Truck tank capacity, L")]
        public float TankCapacity { get; private set; } = 1500f;
        [field: SerializeField, Min(0f), Tooltip("How fast the hydrant refills the tank, L/s")]
        public float TankRefillRate { get; private set; } = 40f;
        [field: SerializeField, Min(0f), Tooltip("Water an open hydrant pours out without a hose, L/s")]
        public float HydrantOutflowRate { get; private set; } = 30f;
    }
}
