using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Fire;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Soundtrack
{
    public class MusicDirector : IInitializable, ITickable, IDisposable
    {
        private const float FULL = 1f;
        private const float QUARTER_TURN = Mathf.PI * 0.5f;
        private const int CALM = 0;
        private const int TENSION = 1;
        private const int INTENSE = 2;

        private readonly IFireSystem _fireSystem;
        private readonly IAudioService _audioService;
        private readonly MusicConfig _config;
        private readonly List<SoundLoop> _layers = new List<SoundLoop>();
        private readonly List<float> _weights = new List<float>();

        private int _playing = CALM;
        private int _wanted = CALM;
        private float _wantedSince;
        private float _playingSince;

        public MusicDirector(IFireSystem fireSystem, IAudioService audioService, MusicConfig config)
        {
            _fireSystem = fireSystem;
            _audioService = audioService;
            _config = config;
        }

        public void Initialize()
        {
            AddLayer(_config.Calm);
            AddLayer(_config.Tension);
            AddLayer(_config.Intense);
            _weights[CALM] = FULL;
            _layers[CALM].SetVolume(FULL);
            _playingSince = Time.time;
        }

        public void Tick()
        {
            ChooseLayer();
            float step = Time.deltaTime / _config.FadeTime;

            for (int i = 0; i < _layers.Count; i++)
            {
                float target = i == _playing ? FULL : 0f;
                _weights[i] = Mathf.MoveTowards(_weights[i], target, step);
                _layers[i].SetVolume(Mathf.Sin(_weights[i] * QUARTER_TURN));
            }
        }

        public void Dispose()
        {
            foreach (SoundLoop layer in _layers)
                layer.Release();
        }

        private void AddLayer(SoundCue cue)
        {
            _layers.Add(_audioService.StartLoop(cue));
            _weights.Add(0f);
        }

        private void ChooseLayer()
        {
            int wanted = GetLayerFor(_fireSystem.ActiveCount);

            if (wanted != _wanted)
            {
                _wanted = wanted;
                _wantedSince = Time.time;
            }

            bool isSettled = Time.time - _wantedSince >= _config.SwitchDelay;
            bool hasPlayedEnough = Time.time - _playingSince >= _config.MinPlayTime;

            if (_wanted == _playing || isSettled == false || hasPlayedEnough == false)
                return;

            if (_weights[_wanted] <= 0f)
                _layers[_wanted].Restart();

            _playing = _wanted;
            _playingSince = Time.time;
        }

        private int GetLayerFor(int burningSpots)
        {
            if (IsAbove(burningSpots, _config.IntenseSpots, INTENSE))
                return INTENSE;

            return IsAbove(burningSpots, _config.TensionSpots, TENSION) ? TENSION : CALM;
        }

        private bool IsAbove(int burningSpots, int layerSpots, int layer)
        {
            if (_playing >= layer)
                return burningSpots >= layerSpots * _config.CalmDownShare && burningSpots > 0;

            return burningSpots >= layerSpots;
        }
    }
}
