using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "PumpAudioConfig", menuName = "Configs/Audio/Pump Audio Config")]
    public class PumpAudioConfig : ScriptableObject
    {
        [field: SerializeField] public SoundCue Engine { get; private set; }
        [field: SerializeField, Min(0.1f), Tooltip("Water the pump delivers when it works at full power, l/s")]
        public float FullFlow { get; private set; } = 25f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Engine volume while the pump idles")]
        public float IdleVolume { get; private set; } = 0.35f;
        [field: SerializeField, Tooltip("Engine pitch while idling (x) and at full power (y)")]
        public Vector2 PitchByLoad { get; private set; } = new Vector2(0.85f, 1.25f);
        [field: SerializeField, Min(0.1f), Tooltip("How fast the engine follows the load, 1/s")]
        public float Sharpness { get; private set; } = 2f;
    }
}
