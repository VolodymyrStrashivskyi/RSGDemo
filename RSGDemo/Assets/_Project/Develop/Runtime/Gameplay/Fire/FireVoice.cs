using _Project.Develop.Runtime.Common.Audio;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireVoice
    {
        public FireVoice(SoundLoop loop) =>
            Loop = loop;

        public SoundLoop Loop { get; }

        public FireSpot Spot { get; set; }

        public float Volume { get; set; }

        public float Cutoff { get; set; }
    }
}
