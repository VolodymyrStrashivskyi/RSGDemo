using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Common.Audio
{
    public interface IAudioService
    {
        void Play(SoundCue cue, Vector3 position);
        void Play(SoundCue cue);
        SoundLoop StartLoop(SoundCue cue);
    }
}
