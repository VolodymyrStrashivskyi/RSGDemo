using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _Project.Develop.Runtime.Common.Audio
{
    public class AudioService : IAudioService, IDisposable
    {
        private const string ROOT_NAME = "Audio";
        private const string ONE_SHOT_NAME = "OneShot";
        private const string LOOP_NAME = "Loop";
        private const float OPEN_CUTOFF = 22000f;

        private readonly AudioConfig _config;
        private readonly List<AudioSource> _oneShots = new List<AudioSource>();

        private Transform _root;
        private int _nextOneShot;

        public AudioService(AudioConfig config) =>
            _config = config;

        public void Play(SoundCue cue, Vector3 position) =>
            PlayOneShot(cue, position, true);

        public void Play(SoundCue cue) =>
            PlayOneShot(cue, Vector3.zero, false);

        public SoundLoop StartLoop(SoundCue cue)
        {
            AudioSource source = CreateSource(LOOP_NAME);
            AudioLowPassFilter filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = OPEN_CUTOFF;
            Setup(source, cue, true);
            source.loop = true;
            return new SoundLoop(source, filter, GetVolume(cue), GetPitch(cue));
        }

        public void Dispose()
        {
            if (_root != null)
                Object.Destroy(_root.gameObject);
        }

        private void PlayOneShot(SoundCue cue, Vector3 position, bool isPlaced)
        {
            if (HasClips(cue) == false)
                return;

            AudioSource source = TakeOneShotSource();
            Setup(source, cue, isPlaced);
            source.loop = false;
            source.transform.position = position;
            source.volume = GetVolume(cue);
            source.pitch = GetPitch(cue);
            source.Play();
        }

        private AudioSource TakeOneShotSource()
        {
            if (_oneShots.Count < _config.OneShotVoices)
            {
                AudioSource created = CreateSource(ONE_SHOT_NAME);
                _oneShots.Add(created);
                return created;
            }

            for (int i = 0; i < _oneShots.Count; i++)
            {
                AudioSource candidate = _oneShots[(_nextOneShot + i) % _oneShots.Count];

                if (candidate.isPlaying == false)
                    return candidate;
            }

            AudioSource oldest = _oneShots[_nextOneShot];
            _nextOneShot = (_nextOneShot + 1) % _oneShots.Count;
            return oldest;
        }

        private static void Setup(AudioSource source, SoundCue cue, bool isPlaced)
        {
            source.clip = HasClips(cue) ? cue.Clips[Random.Range(0, cue.Clips.Length)] : null;
            source.spatialBlend = isPlaced ? cue.SpatialBlend : 0f;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.rolloffMode = cue.Rolloff;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
        }

        private AudioSource CreateSource(string name)
        {
            GameObject sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(GetRoot(), false);
            return sourceObject.AddComponent<AudioSource>();
        }

        private Transform GetRoot()
        {
            if (_root != null)
                return _root;

            GameObject root = new GameObject(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            _root = root.transform;
            return _root;
        }

        private float GetVolume(SoundCue cue) =>
            Random.Range(cue.Volume.x, cue.Volume.y) * _config.GetVolume(cue.Channel);

        private static float GetPitch(SoundCue cue) =>
            Random.Range(cue.Pitch.x, cue.Pitch.y);

        private static bool HasClips(SoundCue cue) =>
            cue != null && cue.Clips != null && cue.Clips.Length > 0;
    }
}
