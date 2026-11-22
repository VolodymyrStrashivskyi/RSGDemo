using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireAudio : IFireAudio, IInitializable, ITickable, IDisposable
    {
        private const float SILENT = 0.001f;
        private const float SOUND_HEIGHT_SHARE = 0.5f;
        private const float WALL_CHECK_MARGIN = 0.4f;

        private readonly IFireSystem _fireSystem;
        private readonly IAudioService _audioService;
        private readonly IRaycastService _raycastService;
        private readonly FireAudioConfig _config;
        private readonly List<FireVoice> _voices = new List<FireVoice>();
        private readonly List<FireSpot> _chosen = new List<FireSpot>();
        private readonly List<float> _chosenScores = new List<float>();
        private readonly List<FireSpot> _listenedSpots = new List<FireSpot>();

        private SoundLoop _blaze;
        private Transform _listener;
        private float _blazeVolume;
        private float _nextSteamTime;
        private float _nextIgnitionTime;

        public FireAudio(IFireSystem fireSystem, IAudioService audioService, IRaycastService raycastService, FireAudioConfig config)
        {
            _fireSystem = fireSystem;
            _audioService = audioService;
            _raycastService = raycastService;
            _config = config;
        }

        public void SetListener(Transform listener) =>
            _listener = listener;

        public void Initialize()
        {
            for (int i = 0; i < _config.MaxVoices; i++)
                _voices.Add(new FireVoice(_audioService.StartLoop(_config.SpotLoop)));

            _blaze = _audioService.StartLoop(_config.BlazeLoop);
            _fireSystem.SpotIgnited += OnSpotIgnited;
            _fireSystem.SpotReignited += OnSpotReignited;
            _fireSystem.SpotBurnedOut += OnSpotBurnedOut;
        }

        public void Tick()
        {
            ListenToNewSpots();
            ChooseAudibleSpots();
            GiveVoicesToChosenSpots();

            float blend = Smoothing.GetBlend(_config.VolumeSharpness, Time.deltaTime);

            foreach (FireVoice voice in _voices)
                UpdateVoice(voice, blend);

            UpdateBlaze(blend);
        }

        public void Dispose()
        {
            _fireSystem.SpotIgnited -= OnSpotIgnited;
            _fireSystem.SpotReignited -= OnSpotReignited;
            _fireSystem.SpotBurnedOut -= OnSpotBurnedOut;

            foreach (FireSpot spot in _listenedSpots)
                spot.Watered -= OnSpotWatered;

            foreach (FireVoice voice in _voices)
                voice.Loop.Release();

            _blaze?.Release();
        }

        private void ListenToNewSpots()
        {
            IReadOnlyList<FireSpot> spots = _fireSystem.Spots;

            for (int i = _listenedSpots.Count; i < spots.Count; i++)
            {
                spots[i].Watered += OnSpotWatered;
                _listenedSpots.Add(spots[i]);
            }
        }

        private void ChooseAudibleSpots()
        {
            _chosen.Clear();
            _chosenScores.Clear();

            foreach (FireSpot spot in _fireSystem.Spots)
            {
                float loudness = GetLoudness(spot);

                if (loudness < _config.MinLoudness)
                    continue;

                float distanceSquared = GetDistanceSquaredToListener(spot);

                if (distanceSquared > _config.SpotLoop.MaxDistance * _config.SpotLoop.MaxDistance)
                    continue;

                TryChoose(spot, loudness / (1f + distanceSquared * _config.DistanceFalloff));
            }
        }

        private void TryChoose(FireSpot spot, float score)
        {
            if (_chosen.Count < _config.MaxVoices)
            {
                _chosen.Add(spot);
                _chosenScores.Add(score);
                return;
            }

            int weakest = 0;

            for (int i = 1; i < _chosenScores.Count; i++)
            {
                if (_chosenScores[i] < _chosenScores[weakest])
                    weakest = i;
            }

            if (_chosenScores[weakest] >= score)
                return;

            _chosen[weakest] = spot;
            _chosenScores[weakest] = score;
        }

        private void GiveVoicesToChosenSpots()
        {
            foreach (FireSpot spot in _chosen)
            {
                if (HasVoice(spot))
                    continue;

                FireVoice free = FindFreeVoice();

                if (free == null)
                    return;

                free.Spot = spot;
                free.Volume = 0f;
                free.Cutoff = _config.OpenCutoff;
                free.Loop.StartAtRandomTime();
                free.Loop.SetPitch(GetPitch(spot));
            }
        }

        private void UpdateVoice(FireVoice voice, float blend)
        {
            if (voice.Spot == null)
                return;

            bool isChosen = _chosen.Contains(voice.Spot);
            Vector3 soundPoint = GetSoundPoint(voice.Spot);
            bool isBehindWall = IsBehindWall(soundPoint);
            float target = isChosen ? GetLoudness(voice.Spot) * (isBehindWall ? _config.OccludedVolume : 1f) : 0f;
            float cutoff = isBehindWall ? _config.OccludedCutoff : _config.OpenCutoff;

            voice.Volume = Mathf.Lerp(voice.Volume, target, blend);
            voice.Cutoff = Mathf.Lerp(voice.Cutoff, cutoff, blend);
            voice.Loop.SetPosition(soundPoint);
            voice.Loop.SetCutoff(voice.Cutoff);
            voice.Loop.SetVolume(voice.Volume);

            if (isChosen == false && voice.Volume < SILENT)
            {
                voice.Spot = null;
                voice.Loop.SetVolume(0f);
            }
        }

        private void UpdateBlaze(float blend)
        {
            float totalHeat = 0f;
            Vector3 weightedPosition = Vector3.zero;

            foreach (FireSpot spot in _fireSystem.Spots)
            {
                if (spot.IsBurning == false)
                    continue;

                totalHeat += spot.Heat;
                weightedPosition += spot.Position * spot.Heat;
            }

            if (totalHeat > 0f)
                _blaze.SetPosition(weightedPosition / totalHeat);

            _blazeVolume = Mathf.Lerp(_blazeVolume, Mathf.Clamp01(totalHeat / _config.FullBlazeHeat), blend);
            _blaze.SetVolume(_blazeVolume);
        }

        private void OnSpotIgnited(FireSpot spot)
        {
            if (Time.time < _nextIgnitionTime)
                return;

            _nextIgnitionTime = Time.time + _config.IgnitionInterval;
            _audioService.Play(_config.Ignited, GetSoundPoint(spot));
        }

        private void OnSpotReignited(FireSpot spot) =>
            _audioService.Play(_config.Reignited, GetSoundPoint(spot));

        private void OnSpotBurnedOut(FireSpot spot) =>
            _audioService.Play(_config.BurnedOut, GetSoundPoint(spot));

        private void OnSpotWatered(FireSpot spot, float liters)
        {
            if (Time.time < _nextSteamTime || Mathf.Max(spot.Heat, spot.Embers) < _config.SteamMinHeat)
                return;

            _nextSteamTime = Time.time + _config.SteamInterval;
            _audioService.Play(_config.Steam, GetSoundPoint(spot));
        }

        private bool IsBehindWall(Vector3 soundPoint)
        {
            if (_listener == null)
                return false;

            Vector3 offset = soundPoint - _listener.position;
            float distance = offset.magnitude - WALL_CHECK_MARGIN;

            if (distance <= 0f)
                return false;

            return _raycastService.TryRaycast(new Ray(_listener.position, offset), distance, _config.OcclusionMask, out RaycastHit _);
        }

        private bool HasVoice(FireSpot spot)
        {
            foreach (FireVoice voice in _voices)
            {
                if (voice.Spot == spot)
                    return true;
            }

            return false;
        }

        private FireVoice FindFreeVoice()
        {
            foreach (FireVoice voice in _voices)
            {
                if (voice.Spot == null)
                    return voice;
            }

            return null;
        }

        private float GetLoudness(FireSpot spot) =>
            spot.IsActive ? Mathf.Clamp01(spot.Heat + spot.Embers * _config.EmberLoudness) : 0f;

        private float GetPitch(FireSpot spot) =>
            Mathf.Lerp(_config.PitchBySize.x, _config.PitchBySize.y, Mathf.InverseLerp(_config.SizeRange.x, _config.SizeRange.y, spot.Radius));

        private float GetDistanceSquaredToListener(FireSpot spot) =>
            _listener != null ? (GetSoundPoint(spot) - _listener.position).sqrMagnitude : 0f;

        private static Vector3 GetSoundPoint(FireSpot spot) =>
            spot.Position + Vector3.up * (spot.Height * SOUND_HEIGHT_SHARE);
    }
}
