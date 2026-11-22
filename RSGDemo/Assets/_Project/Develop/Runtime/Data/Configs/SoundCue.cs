using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "SoundCue", menuName = "Configs/Audio/Sound Cue")]
    public class SoundCue : ScriptableObject
    {
        [field: SerializeField, Tooltip("One of these is picked at random each time the sound plays")]
        public AudioClip[] Clips { get; private set; }
        [field: SerializeField] public SoundChannel Channel { get; private set; } = SoundChannel.Effects;
        [field: SerializeField, Tooltip("Random volume between x and y")]
        public Vector2 Volume { get; private set; } = Vector2.one;
        [field: SerializeField, Tooltip("Random pitch between x and y")]
        public Vector2 Pitch { get; private set; } = Vector2.one;

        [field: Header("Space")]
        [field: SerializeField, Range(0f, 1f), Tooltip("0 plays flat in both ears, 1 plays from its position in the world")]
        public float SpatialBlend { get; private set; } = 1f;
        [field: SerializeField, Min(0.01f), Tooltip("Distance within which the sound plays at full volume, m")]
        public float MinDistance { get; private set; } = 1.5f;
        [field: SerializeField, Min(0.1f), Tooltip("Distance beyond which the sound is no longer heard, m")]
        public float MaxDistance { get; private set; } = 30f;
        [field: SerializeField, Tooltip("How the sound fades with distance")]
        public AudioRolloffMode Rolloff { get; private set; } = AudioRolloffMode.Logarithmic;
    }
}
