using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSpotView : MonoBehaviour
    {
        private const float HALF = 0.5f;
        private const float FLICKER_SEED_RANGE = 100f;
        private const float FLICKER_RANGE = 2f;

        [SerializeField] private ParticleSystem _flames;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private ParticleSystem _embers;
        [SerializeField] private ParticleSystem _smoke;
        [SerializeField] private ParticleSystem _steam;
        [SerializeField] private Light _light;

        private FireVisualConfig _config;
        private FireSpot _spot;
        private float _flameLifetime;
        private float _flameLevel;
        private float _steamRate;
        private float _waterThisFrame;
        private float _flickerSeed;

        [Inject]
        public void Construct(FireVisualConfig config) =>
            _config = config;

        public FireSpot Spot => _spot;

        public void Bind(FireSpot spot)
        {
            Unbind();
            _spot = spot;
            transform.SetPositionAndRotation(spot.Position, Quaternion.identity);
            _flickerSeed = Random.value * FLICKER_SEED_RANGE;
            FitToSpot();

            _spot.Ignited += OnFlaredUp;
            _spot.Reignited += OnFlaredUp;
            _spot.Smoldered += OnSmoldered;
            _spot.Extinguished += OnExtinguished;
            _spot.Watered += OnWatered;
        }

        private void Awake()
        {
            ParticleSystem.MinMaxCurve lifetime = _flames.main.startLifetime;
            _flameLifetime = (lifetime.constantMin + lifetime.constantMax) * HALF;
            SetRate(_flames, 0f);
            SetRate(_sparks, 0f);
            SetRate(_embers, 0f);
            SetRate(_smoke, 0f);
            SetRate(_steam, 0f);
            _light.enabled = false;
        }

        private void Update()
        {
            if (_spot == null || _config == null)
                return;

            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            float heat = _spot.IsBurning ? _spot.Heat : 0f;
            _flameLevel = Mathf.Lerp(_flameLevel, heat, 1f - Mathf.Exp(-_config.HeatSharpness * deltaTime));
            float embers = _spot.State == FireState.Smoldering ? _spot.Embers : 0f;

            UpdateFlames();
            UpdateSmoke(embers);
            UpdateSteam(deltaTime);
            SetRate(_embers, _config.EmberRatePerRadius * _spot.Radius * embers);
            UpdateLight(embers);
        }

        private void OnDestroy() =>
            Unbind();

        private void Unbind()
        {
            if (_spot == null)
                return;

            _spot.Ignited -= OnFlaredUp;
            _spot.Reignited -= OnFlaredUp;
            _spot.Smoldered -= OnSmoldered;
            _spot.Extinguished -= OnExtinguished;
            _spot.Watered -= OnWatered;
            _spot = null;
        }

        private void FitToSpot()
        {
            SetShapeRadius(_flames, _spot.Radius);
            SetShapeRadius(_embers, _spot.Radius);
            SetShapeRadius(_sparks, _spot.Radius);
            SetShapeRadius(_steam, _spot.Radius);
            SetShapeRadius(_smoke, _spot.Radius * HALF);
            _smoke.transform.localPosition = Vector3.up * (_spot.Height * _config.SmokeStartHeight);
            _steam.transform.localPosition = Vector3.up * (_spot.Height * HALF);
            _light.transform.localPosition = Vector3.up * (_spot.Height * HALF);
            _light.range = _config.LightRange + _config.LightRangePerRadius * _spot.Radius;
        }

        private void UpdateFlames()
        {
            ParticleSystem.MainModule main = _flames.main;
            float rise = Mathf.Lerp(_config.MinFlameRise, 1f, _flameLevel);
            main.startSpeedMultiplier = _flameLifetime > 0f ? _spot.Height * rise / _flameLifetime : 0f;
            main.startSizeMultiplier = _spot.Radius * _config.FlameSizePerRadius * Mathf.Lerp(_config.MinFlameScale, 1f, _flameLevel);
            SetRate(_flames, _config.FlameRatePerRadius * _spot.Radius * _flameLevel);
            SetRate(_sparks, _config.SparkRate * _flameLevel);
        }

        private void UpdateSmoke(float embers)
        {
            float burningRate = _config.SmokeRatePerRadius * _flameLevel;
            float smolderRate = _config.SmolderSmokeRatePerRadius * embers;
            SetRate(_smoke, (burningRate + smolderRate) * _spot.Radius);

            ParticleSystem.MainModule main = _smoke.main;
            main.startColor = _spot.IsBurning ? _config.BurningSmokeColor : _config.SmolderSmokeColor;
        }

        private void UpdateSteam(float deltaTime)
        {
            float targetRate = Mathf.Min(_waterThisFrame / deltaTime * _config.SteamPerLiter, _config.MaxSteamRate);
            _waterThisFrame = 0f;
            _steamRate = Mathf.Lerp(_steamRate, targetRate, 1f - Mathf.Exp(-_config.SteamSharpness * deltaTime));
            SetRate(_steam, _steamRate);
        }

        private void UpdateLight(float embers)
        {
            float level = _flameLevel + embers * _config.EmberLightShare;
            float flicker = (Mathf.PerlinNoise(Time.time * _config.FlickerFrequency, _flickerSeed) - HALF) * FLICKER_RANGE;
            _light.intensity = _config.LightIntensity * level * (1f + flicker * _config.FlickerDepth);
            _light.enabled = _light.intensity > 0f;
        }

        private void OnFlaredUp(FireSpot spot) =>
            _sparks.Emit(_config.IgnitionSparkBurst);

        private void OnSmoldered(FireSpot spot) =>
            _steam.Emit(_config.SmolderSteamBurst);

        private void OnExtinguished(FireSpot spot) =>
            _steam.Emit(_config.ExtinguishSteamBurst);

        private void OnWatered(FireSpot spot, float liters) =>
            _waterThisFrame += liters;

        private static void SetRate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTimeMultiplier = rate;
        }

        private static void SetShapeRadius(ParticleSystem system, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = radius;
        }
    }
}
