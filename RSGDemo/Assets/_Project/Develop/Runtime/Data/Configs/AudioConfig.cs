using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Configs/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [field: SerializeField, Min(1), Tooltip("How many short sounds can play at once")]
        public int OneShotVoices { get; private set; } = 24;

        [field: Header("Channel Volumes")]
        [field: SerializeField, Range(0f, 1f)] public float EffectsVolume { get; private set; } = 1f;
        [field: SerializeField, Range(0f, 1f)] public float AmbienceVolume { get; private set; } = 0.6f;
        [field: SerializeField, Range(0f, 1f)] public float MusicVolume { get; private set; } = 0.45f;
        [field: SerializeField, Range(0f, 1f)] public float InterfaceVolume { get; private set; } = 0.8f;

        public float GetVolume(SoundChannel channel)
        {
            switch (channel)
            {
                case SoundChannel.Ambience:
                    return AmbienceVolume;

                case SoundChannel.Music:
                    return MusicVolume;

                case SoundChannel.Interface:
                    return InterfaceVolume;

                default:
                    return EffectsVolume;
            }
        }
    }
}
