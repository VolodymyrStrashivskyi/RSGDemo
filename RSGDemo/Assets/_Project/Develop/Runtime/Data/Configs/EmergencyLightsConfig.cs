using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "EmergencyLightsConfig", menuName = "Configs/Fire Truck/Emergency Lights Config")]
    public class EmergencyLightsConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("Beacon colors in flash order; beacon N on the light bar takes color N")]
        public Color[] BeaconColors { get; private set; } = { new Color(1f, 0.08f, 0.05f), new Color(0.1f, 0.3f, 1f) };

        [field: Header("Pattern")]
        [field: SerializeField, Min(0.1f), Tooltip("Time for every beacon to take its turn once, s")]
        public float CycleDuration { get; private set; } = 0.8f;
        [field: SerializeField, Min(1), Tooltip("Flashes a beacon makes during its turn")]
        public int FlashesPerTurn { get; private set; } = 2;
        [field: SerializeField, Range(0.1f, 1f), Tooltip("Part of each flash slot the beacon stays lit")]
        public float FlashDuty { get; private set; } = 0.55f;
        [field: SerializeField, Min(1f), Tooltip("How fast a beacon lights up and dims, 1/s")]
        public float FadeSharpness { get; private set; } = 60f;

        [field: Header("Lens")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Lens color brightness while the beacon is off")]
        public float LensOffBrightness { get; private set; } = 0.35f;
        [field: SerializeField, Min(0f), Tooltip("HDR emission of a lit lens; above 1 it blooms")]
        public float LensEmission { get; private set; } = 6f;

        [field: Header("Light")]
        [field: SerializeField, Min(0f), Tooltip("Intensity of the light a lit beacon casts around the truck")]
        public float LightIntensity { get; private set; } = 6f;
        [field: SerializeField, Min(0.1f), Tooltip("How far the beacon light reaches, m")]
        public float LightRange { get; private set; } = 10f;
    }
}
