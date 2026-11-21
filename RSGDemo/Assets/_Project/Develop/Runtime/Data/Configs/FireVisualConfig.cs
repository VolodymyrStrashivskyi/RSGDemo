using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireVisualConfig", menuName = "Configs/Fire Visual Config")]
    public class FireVisualConfig : ScriptableObject
    {
        [field: Header("Flames")]
        [field: SerializeField, Min(0f), Tooltip("Flame particles per second at full heat per metre of spot radius")]
        public float FlameRatePerRadius { get; private set; } = 140f;
        [field: SerializeField, Min(0f), Tooltip("Flame particle size relative to the spot radius")]
        public float FlameSizePerRadius { get; private set; } = 2.2f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Flame size of a barely burning spot relative to full heat")]
        public float MinFlameScale { get; private set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Flame height of a barely burning spot relative to the spot height")]
        public float MinFlameRise { get; private set; } = 0.4f;
        [field: SerializeField, Min(0.1f), Tooltip("How fast the visuals follow the fire heat, 1/s")]
        public float HeatSharpness { get; private set; } = 4f;
        [field: SerializeField, Min(0f), Tooltip("Sparks per second at full heat")]
        public float SparkRate { get; private set; } = 14f;
        [field: SerializeField, Min(0), Tooltip("Sparks thrown out when a spot catches or flares up again")]
        public int IgnitionSparkBurst { get; private set; } = 30;

        [field: Header("Embers")]
        [field: SerializeField, Min(0f), Tooltip("Glowing ember particles per second per metre of spot radius while smoldering")]
        public float EmberRatePerRadius { get; private set; } = 80f;

        [field: Header("Smoke")]
        [field: SerializeField, Min(0f), Tooltip("Smoke puffs per second at full heat per metre of spot radius")]
        public float SmokeRatePerRadius { get; private set; } = 9f;
        [field: SerializeField, Min(0f), Tooltip("Smoke puffs per second of fresh embers per metre of spot radius")]
        public float SmolderSmokeRatePerRadius { get; private set; } = 16f;
        [field: SerializeField, Tooltip("Smoke over open flames")]
        public Color BurningSmokeColor { get; private set; } = new Color(0.12f, 0.11f, 0.1f, 0.75f);
        [field: SerializeField, Tooltip("Smoke over embers")]
        public Color SmolderSmokeColor { get; private set; } = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        [field: SerializeField, Range(0f, 1f), Tooltip("Height share of the spot where smoke starts")]
        public float SmokeStartHeight { get; private set; } = 0.8f;

        [field: Header("Steam")]
        [field: SerializeField, Min(0f), Tooltip("Steam puffs per litre of water hitting a hot spot")]
        public float SteamPerLiter { get; private set; } = 3f;
        [field: SerializeField, Min(0f), Tooltip("Most steam puffs per second from one spot")]
        public float MaxSteamRate { get; private set; } = 70f;
        [field: SerializeField, Min(0.1f), Tooltip("How fast steam follows the water hitting the spot, 1/s")]
        public float SteamSharpness { get; private set; } = 6f;
        [field: SerializeField, Min(0), Tooltip("Steam puffs released when the flames are knocked down to embers")]
        public int SmolderSteamBurst { get; private set; } = 25;
        [field: SerializeField, Min(0), Tooltip("Steam puffs released when a spot goes out for good")]
        public int ExtinguishSteamBurst { get; private set; } = 45;

        [field: Header("Light")]
        [field: SerializeField, Min(0f), Tooltip("Light intensity at full heat")]
        public float LightIntensity { get; private set; } = 1.8f;
        [field: SerializeField, Min(0f), Tooltip("Light range around a spot, m")]
        public float LightRange { get; private set; } = 2.5f;
        [field: SerializeField, Min(0f), Tooltip("Extra light range per metre of spot radius, m")]
        public float LightRangePerRadius { get; private set; } = 4f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Light of fresh embers relative to full heat")]
        public float EmberLightShare { get; private set; } = 0.35f;
        [field: SerializeField, Min(0f), Tooltip("How fast the light flickers, Hz")]
        public float FlickerFrequency { get; private set; } = 7f;
        [field: SerializeField, Range(0f, 1f), Tooltip("How deep the light flickers, share of its intensity")]
        public float FlickerDepth { get; private set; } = 0.35f;
    }
}
