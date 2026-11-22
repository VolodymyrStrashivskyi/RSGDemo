using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "MusicConfig", menuName = "Configs/Audio/Music Config")]
    public class MusicConfig : ScriptableObject
    {
        [field: Header("Layers")]
        [field: SerializeField, Tooltip("Plays while nothing burns: arrival and after the fire is out")]
        public SoundCue Calm { get; private set; }
        [field: SerializeField] public SoundCue Tension { get; private set; }
        [field: SerializeField] public SoundCue Intense { get; private set; }

        [field: Header("When To Switch")]
        [field: SerializeField, Min(1), Tooltip("Burning spots from which the tension layer plays")]
        public int TensionSpots { get; private set; } = 1;
        [field: SerializeField, Min(1), Tooltip("Burning spots from which the intense layer plays")]
        public int IntenseSpots { get; private set; } = 15;
        [field: SerializeField, Range(0f, 1f), Tooltip("Share of a layer's spots the fire has to drop below before the music calms down, so it does not flip back and forth")]
        public float CalmDownShare { get; private set; } = 0.6f;
        [field: SerializeField, Min(0f), Tooltip("How long the layer has to stay chosen before the music switches, s")]
        public float SwitchDelay { get; private set; } = 6f;
        [field: SerializeField, Min(0f), Tooltip("Shortest time a layer plays before the music may switch again, s")]
        public float MinPlayTime { get; private set; } = 25f;

        [field: Header("Cross-Fade")]
        [field: SerializeField, Min(0.1f), Tooltip("How long one layer fades out while the next one fades in, s")]
        public float FadeTime { get; private set; } = 6f;
    }
}
