using System;
using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public class GrabAudio : IInitializable, IDisposable
    {
        private readonly Grabber _grabber;
        private readonly IAudioService _audioService;
        private readonly InteractionAudioConfig _config;

        public GrabAudio(Grabber grabber, IAudioService audioService, InteractionAudioConfig config)
        {
            _grabber = grabber;
            _audioService = audioService;
            _config = config;
        }

        public void Initialize()
        {
            _grabber.Grabbed += OnGrabbed;
            _grabber.Released += OnReleased;
        }

        public void Dispose()
        {
            _grabber.Grabbed -= OnGrabbed;
            _grabber.Released -= OnReleased;
        }

        private void OnGrabbed(IGrabbable item) =>
            Play(_config.PickUp, item);

        private void OnReleased(IGrabbable item) =>
            Play(_config.Drop, item);

        private void Play(SoundCue cue, IGrabbable item)
        {
            if (item is Component component)
                _audioService.Play(cue, component.transform.position);
        }
    }
}
