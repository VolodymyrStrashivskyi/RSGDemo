using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Configs/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float Sensitivity { get; private set; } = 0.1f;
        [field: SerializeField, Range(-89f, 0f)] public float MinPitch { get; private set; } = -80f;
        [field: SerializeField, Range(0f, 89f)] public float MaxPitch { get; private set; } = 80f;
        [field: SerializeField, Range(30f, 110f)] public float FieldOfView { get; private set; } = 70f;

        [field: Header("Recoil")]
        [field: SerializeField, Min(0f), Tooltip("How far the view climbs per kilonewton of recoil, degrees")]
        public float RecoilClimbPerKilonewton { get; private set; } = 8f;
        [field: SerializeField, Min(0f), Tooltip("Stiffness of the spring pulling the view to its recoil climb, 1/s²")]
        public float RecoilStiffness { get; private set; } = 120f;
        [field: SerializeField, Min(0f), Tooltip("Damping of the recoil spring; low values make the view overshoot when the nozzle opens, 1/s")]
        public float RecoilDamping { get; private set; } = 10f;
        [field: SerializeField, Min(0f), Tooltip("View shake per kilonewton of recoil, degrees")]
        public float RecoilShakePerKilonewton { get; private set; } = 1.2f;
        [field: SerializeField, Min(0f), Tooltip("How fast the view shakes under recoil, Hz")]
        public float RecoilShakeFrequency { get; private set; } = 9f;
        [field: SerializeField, Min(0f), Tooltip("How far the held item is pushed towards the camera per kilonewton of recoil, m")]
        public float HeldKickPerKilonewton { get; private set; } = 0.08f;
        [field: SerializeField, Min(0f), Tooltip("Extra upward tilt of the held item per degree of view climb")]
        public float HeldTiltRatio { get; private set; } = 1f;
        [field: SerializeField, Min(0f), Tooltip("Sideways shake of the held item per kilonewton of recoil, m")]
        public float HeldShakePerKilonewton { get; private set; } = 0.012f;
    }
}
