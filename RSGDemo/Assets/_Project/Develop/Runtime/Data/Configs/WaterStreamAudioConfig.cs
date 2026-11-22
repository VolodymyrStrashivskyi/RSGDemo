using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "WaterStreamAudioConfig", menuName = "Configs/Audio/Water Stream Audio Config")]
    public class WaterStreamAudioConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("Rush of water at the outlet")]
        public SoundCue Outlet { get; private set; }
        [field: SerializeField, Tooltip("Splatter where the stream lands")]
        public SoundCue Impact { get; private set; }
        [field: SerializeField, Tooltip("Splash when the stream starts")]
        public SoundCue Opened { get; private set; }
        [field: SerializeField, Tooltip("Pitch at the weakest flow (x) and at full flow (y)")]
        public Vector2 PitchByFlow { get; private set; } = new Vector2(0.75f, 1.1f);
        [field: SerializeField, Range(0f, 1f), Tooltip("Flow above which the stream counts as started")]
        public float OpenFlow { get; private set; } = 0.15f;
        [field: SerializeField, Min(0.1f), Tooltip("How fast the sound follows the flow, 1/s")]
        public float Sharpness { get; private set; } = 10f;
    }
}
