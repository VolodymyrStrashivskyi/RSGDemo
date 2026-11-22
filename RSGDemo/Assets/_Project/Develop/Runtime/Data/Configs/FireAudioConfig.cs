using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireAudioConfig", menuName = "Configs/Audio/Fire Audio Config")]
    public class FireAudioConfig : ScriptableObject
    {
        [field: Header("Crackle Of Each Spot")]
        [field: SerializeField] public SoundCue SpotLoop { get; private set; }
        [field: SerializeField, Min(1), Tooltip("How many fire spots crackle at once: the loudest and closest ones get a voice")]
        public int MaxVoices { get; private set; } = 6;
        [field: SerializeField, Range(0f, 1f), Tooltip("Loudness of glowing embers compared with open flame")]
        public float EmberLoudness { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Spots quieter than this are not given a voice")]
        public float MinLoudness { get; private set; } = 0.03f;
        [field: SerializeField, Min(0f), Tooltip("How much distance lowers a spot's chance to get a voice")]
        public float DistanceFalloff { get; private set; } = 0.02f;
        [field: SerializeField, Tooltip("Pitch of a small spot (x) and of a large one (y)")]
        public Vector2 PitchBySize { get; private set; } = new Vector2(1.1f, 0.8f);
        [field: SerializeField, Tooltip("Spot radius that counts as small (x) and as large (y), m")]
        public Vector2 SizeRange { get; private set; } = new Vector2(0.3f, 1.5f);
        [field: SerializeField, Min(0.1f), Tooltip("How fast the crackle follows the fire and fades in or out, 1/s")]
        public float VolumeSharpness { get; private set; } = 4f;

        [field: Header("Blaze Of The Whole House")]
        [field: SerializeField] public SoundCue BlazeLoop { get; private set; }
        [field: SerializeField, Min(0.1f), Tooltip("Total heat of all spots at which the blaze roars at full volume")]
        public float FullBlazeHeat { get; private set; } = 12f;

        [field: Header("Walls")]
        [field: SerializeField, Tooltip("What muffles fire behind it")]
        public LayerMask OcclusionMask { get; private set; } = 1;
        [field: SerializeField, Min(10f), Tooltip("Low-pass cutoff of fire heard through a wall, Hz")]
        public float OccludedCutoff { get; private set; } = 900f;
        [field: SerializeField, Min(10f), Tooltip("Low-pass cutoff of fire in plain view, Hz")]
        public float OpenCutoff { get; private set; } = 22000f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Volume of fire heard through a wall")]
        public float OccludedVolume { get; private set; } = 0.55f;

        [field: Header("Events")]
        [field: SerializeField] public SoundCue Ignited { get; private set; }
        [field: SerializeField, Min(0f), Tooltip("Shortest time between two ignition sounds, so a spreading fire does not whoosh on every spot, s")]
        public float IgnitionInterval { get; private set; } = 1.2f;
        [field: SerializeField] public SoundCue Reignited { get; private set; }
        [field: SerializeField] public SoundCue BurnedOut { get; private set; }
        [field: SerializeField] public SoundCue Steam { get; private set; }
        [field: SerializeField, Range(0f, 1f), Tooltip("Heat or embers a spot needs for water to hiss on it")]
        public float SteamMinHeat { get; private set; } = 0.15f;
        [field: SerializeField, Min(0f), Tooltip("Shortest time between two steam hisses, s")]
        public float SteamInterval { get; private set; } = 0.35f;
    }
}
