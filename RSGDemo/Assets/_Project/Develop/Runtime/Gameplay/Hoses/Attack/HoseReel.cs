using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack
{
    public class HoseReel : MonoBehaviour
    {
        private const float FULL_TURN = Mathf.PI * 2f;
        private const float HALF = 0.5f;
        private const float START_ANGLE = Mathf.PI * 0.5f;
        private const float REBUILD_THRESHOLD = 0.001f;
        private const float DIAMETER_PER_RADIUS = 2f;

        [SerializeField] private Transform _drum;
        [SerializeField] private MeshFilter _coilFilter;

        private readonly List<Vector3> _coilPoints = new List<Vector3>();
        private AttackHoseConfig _config;
        private IAudioService _audioService;
        private InteractionAudioConfig _audioConfig;
        private HoseTubeMesh _coilTube;
        private HoseView _water;
        private float _turnSpacing;
        private float _layerStep;
        private float _halfTravel;
        private int _turnsPerLayer;
        private float _shownLength;
        private float _builtLength = -1f;
        private float _paidOutLength;
        private float _builtPaidOutLength = -1f;
        private float _builtInflationFront = -1f;
        private float _brakeAngle;
        private Tween _brakeTween;

        [Inject]
        public void Construct(IAudioService audioService, InteractionAudioConfig audioConfig)
        {
            _audioService = audioService;
            _audioConfig = audioConfig;
        }

        public Vector3 TakeoffPosition => _drum.TransformPoint(GetCoilPoint(_shownLength));

        public Vector3 TakeoffDirection => _drum.TransformDirection(GetCoilTangent(_shownLength));

        public Vector3 TakeoffNormal => _drum.TransformDirection(_coilTube.EndNormal);

        public float CoiledLength => _shownLength;

        public void Initialize(AttackHoseConfig config, float woundLength, HoseView water)
        {
            _config = config;
            _water = water;
            float deflatedRadius = config.Radius * config.DeflatedRadiusRatio;
            _turnSpacing = config.Radius * DIAMETER_PER_RADIUS;
            _layerStep = deflatedRadius * DIAMETER_PER_RADIUS * config.DeflatedSquash;
            _turnsPerLayer = Mathf.Max(1, Mathf.FloorToInt(config.ReelWidth / _turnSpacing));
            _halfTravel = (config.ReelWidth - _turnSpacing) * HALF;

            _coilTube = new HoseTubeMesh(
                config.Radius,
                deflatedRadius,
                config.DeflatedSquash,
                config.Sides,
                1,
                config.PaletteUv,
                config.EndTaperLength);

            _coilTube.SetRibs(config.RibSpacing, config.RibDepth);
            _coilTube.SetFittedEnds(true, false);
            _coilFilter.sharedMesh = _coilTube.Mesh;
            _shownLength = woundLength;
            Refresh();
        }

        public void Wind(float woundLength, float paidOutLength, float deltaTime)
        {
            float blend = Smoothing.GetBlend(_config.ReelSpinSharpness, deltaTime);
            _shownLength = Mathf.Lerp(_shownLength, woundLength, blend);
            _paidOutLength = paidOutLength;
            Refresh();
        }

        public void Brake()
        {
            _brakeTween.Stop();
            _brakeTween = Tween.Custom(this, _config.ReelBrakeKick, 0f, _config.ReelBrakeTween, (reel, angle) => reel._brakeAngle = angle);
            _audioService?.Play(_audioConfig.ReelBrake, _drum.position);
        }

        private void OnDestroy()
        {
            _brakeTween.Stop();

            if (_coilTube != null)
                Destroy(_coilTube.Mesh);
        }

        private void Refresh()
        {
            _drum.localRotation = Quaternion.Euler(0f, 0f, (GetWoundAngle(_shownLength) - START_ANGLE) * Mathf.Rad2Deg + _brakeAngle);

            bool isSameLength = Mathf.Abs(_shownLength - _builtLength) < REBUILD_THRESHOLD;
            bool isSamePaidOut = Mathf.Abs(_paidOutLength - _builtPaidOutLength) < REBUILD_THRESHOLD;
            bool isSameWater = Mathf.Abs(_water.InflationFront - _builtInflationFront) < REBUILD_THRESHOLD;

            if (isSameLength && isSamePaidOut && isSameWater)
                return;

            _builtLength = _shownLength;
            _builtPaidOutLength = _paidOutLength;
            _builtInflationFront = _water.InflationFront;
            BuildCoil();
        }

        private void BuildCoil()
        {
            _coilPoints.Clear();
            float distance = 0f;

            while (distance < _shownLength)
            {
                _coilPoints.Add(GetCoilPoint(distance));
                distance += GetLayerRadius(GetLayer(distance)) * FULL_TURN / _config.CoilSegmentsPerTurn;
            }

            _coilPoints.Add(GetCoilPoint(_shownLength));
            _coilTube.SetRibPhase(_shownLength, _paidOutLength);
            _coilTube.SetEndTangent(GetCoilTangent(_shownLength));
            _coilTube.Build(_coilPoints, GetInflation);
        }

        private Vector3 GetCoilPoint(float distance)
        {
            int layer = GetLayer(distance);
            float radius = GetLayerRadius(layer);
            float angle = START_ANGLE - GetWoundAngle(distance);
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, GetAxialOffset(distance, layer));
        }

        private Vector3 GetCoilTangent(float distance)
        {
            int layer = GetLayer(distance);
            float radius = GetLayerRadius(layer);
            float angle = START_ANGLE - GetWoundAngle(distance);
            float axialSlope = GetAxialDirection(layer) * _halfTravel * 2f / (_turnsPerLayer * FULL_TURN * radius);
            return new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), axialSlope).normalized;
        }

        private float GetWoundAngle(float distance)
        {
            int layer = GetLayer(distance);
            return layer * _turnsPerLayer * FULL_TURN + (distance - GetLayerStart(layer)) / GetLayerRadius(layer);
        }

        private float GetAxialOffset(float distance, int layer)
        {
            float progress = (distance - GetLayerStart(layer)) / GetLayerLength(layer);
            return GetAxialDirection(layer) * Mathf.Lerp(-_halfTravel, _halfTravel, progress);
        }

        private int GetLayer(float distance)
        {
            int layer = 0;
            float layerEnd = GetLayerLength(0);

            while (distance > layerEnd)
            {
                layer++;
                layerEnd += GetLayerLength(layer);
            }

            return layer;
        }

        private float GetLayerStart(int layer)
        {
            float start = 0f;

            for (int i = 0; i < layer; i++)
                start += GetLayerLength(i);

            return start;
        }

        private float GetLayerLength(int layer) =>
            _turnsPerLayer * FULL_TURN * GetLayerRadius(layer);

        private float GetLayerRadius(int layer) =>
            _config.ReelCoreRadius + _layerStep * (layer + HALF);

        private static float GetAxialDirection(int layer) =>
            layer % 2 == 0 ? -1f : 1f;

        private float GetInflation(float normalizedDistance) =>
            _water.GetInflationAtDistance(normalizedDistance * _shownLength);
    }
}
