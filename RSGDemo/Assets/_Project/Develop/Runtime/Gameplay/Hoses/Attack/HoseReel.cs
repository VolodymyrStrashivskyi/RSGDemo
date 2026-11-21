using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack
{
    public class HoseReel : MonoBehaviour
    {
        private const float FULL_TURN = Mathf.PI * 2f;
        private const float HALF = 0.5f;
        private const float START_ANGLE = Mathf.PI * 0.5f;
        private const float REBUILD_THRESHOLD = 0.001f;

        [SerializeField] private Transform _drum;
        [SerializeField] private MeshFilter _coilFilter;

        private readonly List<Vector3> _coilPoints = new List<Vector3>();
        private AttackHoseConfig _config;
        private HoseTubeMesh _coilTube;
        private float _hoseWidth;
        private float _layerStep;
        private float _halfTravel;
        private int _turnsPerLayer;
        private float _shownLength;
        private float _builtLength = -1f;

        public Vector3 TakeoffPosition => _drum.TransformPoint(GetCoilPoint(_shownLength));

        public Vector3 TakeoffDirection => _drum.TransformDirection(GetCoilTangent(_shownLength));

        public void Initialize(AttackHoseConfig config, float woundLength)
        {
            _config = config;
            _hoseWidth = config.Radius * config.DeflatedRadiusRatio * 2f;
            _layerStep = _hoseWidth * config.DeflatedSquash;
            _turnsPerLayer = Mathf.Max(1, Mathf.FloorToInt(config.ReelWidth / _hoseWidth));
            _halfTravel = (config.ReelWidth - _hoseWidth) * HALF;

            _coilTube = new HoseTubeMesh(
                _hoseWidth * HALF,
                _hoseWidth * HALF,
                config.DeflatedSquash,
                config.Sides,
                1,
                config.PaletteUv,
                0f);

            _coilFilter.sharedMesh = _coilTube.Mesh;
            _shownLength = woundLength;
            Refresh();
        }

        public void Wind(float woundLength, float deltaTime)
        {
            float blend = Smoothing.GetBlend(_config.ReelSpinSharpness, deltaTime);
            _shownLength = Mathf.Lerp(_shownLength, woundLength, blend);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_coilTube != null)
                Destroy(_coilTube.Mesh);
        }

        private void Refresh()
        {
            _drum.localRotation = Quaternion.Euler(0f, 0f, (GetWoundAngle(_shownLength) - START_ANGLE) * Mathf.Rad2Deg);

            if (Mathf.Abs(_shownLength - _builtLength) < REBUILD_THRESHOLD)
                return;

            _builtLength = _shownLength;
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
            _coilTube.Build(_coilPoints, GetDeflated);
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

        private static float GetDeflated(float normalizedDistance) =>
            0f;
    }
}
