using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    [RequireComponent(typeof(MeshFilter))]
    public class HoseView : MonoBehaviour
    {
        private const float BELL_PEAK_SCALE = 4f;
        private const int END_NODES = 2;
        private const int DEFAULT_OBSTACLE_MASK = 1281;
        private const float MIN_BEND_DISTANCE = 0.0001f;
        private const float HALF = 0.5f;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Renderer _renderer;
        [SerializeField, Tooltip("Copy of the hose for the first-person camera, led to the view model of the held end")]
        private MeshFilter _firstPersonMeshFilter;
        [SerializeField] private Renderer _firstPersonRenderer;
        [SerializeField, Range(0, 31), Tooltip("Layer the first-person camera does not render, used for the real hose while its copy shows")]
        private int _firstPersonHiddenLayer;
        [SerializeField, Min(0.1f), Tooltip("Length of hose near the held end bent over to reach the view model, m")]
        private float _firstPersonBlendLength = 1.5f;
        [SerializeField, Tooltip("Obstacles the bent first-person copy must not pass through")]
        private LayerMask _firstPersonObstacleMask = DEFAULT_OBSTACLE_MASK;
        [SerializeField, Min(0f), Tooltip("Gap the bent first-person copy keeps from obstacles, m")]
        private float _firstPersonClearance = 0.05f;
        [SerializeField] private Ease _inflationEase = Ease.InOutSine;

        private readonly List<Vector3> _firstPersonPoints = new List<Vector3>();
        private IRaycastService _raycastService;
        private HoseTubeMesh _tube;
        private HoseTubeMesh _firstPersonTube;
        private Func<float, float> _inflationAt;
        private float _waveWidth;
        private float _bulge;
        private float _inflationFront;
        private HoseFillOrigin _fillOrigin;
        private float _leadLength;
        private float _hoseLength;
        private int _visibleLayer;
        private Tween _tween;

        [Inject]
        public void Construct(IRaycastService raycastService) =>
            _raycastService = raycastService;

        public float InflationFront => _inflationFront;

        public bool IsFilledToFarEnd => _inflationFront * (1f + _waveWidth) >= 1f + _waveWidth * HALF;

        public void Initialize(HoseTubeMesh tube, HoseTubeMesh firstPersonTube, float waveWidth, float bulge, HoseFillOrigin fillOrigin)
        {
            _tube = tube;
            _firstPersonTube = firstPersonTube;
            _waveWidth = waveWidth;
            _bulge = bulge;
            _fillOrigin = fillOrigin;
            _inflationAt = GetInflationFromOrigin;
            _meshFilter.sharedMesh = tube.Mesh;
            _firstPersonMeshFilter.sharedMesh = firstPersonTube.Mesh;
            _visibleLayer = _renderer.gameObject.layer;
            _firstPersonRenderer.enabled = false;
        }

        public void SetFillSpan(float leadLength, float hoseLength)
        {
            _leadLength = leadLength;
            _hoseLength = hoseLength;
        }

        public float GetInflationAtDistance(float distanceFromOrigin)
        {
            float spanLength = _leadLength + _hoseLength;
            return EvaluateInflation(spanLength > 0f ? distanceFromOrigin / spanLength : 0f);
        }

        public void Render(IReadOnlyList<Vector3> points, Transform firstPersonEnd)
        {
            _tube.Build(points, _inflationAt);

            bool isFirstPerson = firstPersonEnd != null;
            _firstPersonRenderer.enabled = isFirstPerson;
            _renderer.gameObject.layer = isFirstPerson ? _firstPersonHiddenLayer : _visibleLayer;

            if (isFirstPerson)
                _firstPersonTube.Build(BendEndTo(points, firstPersonEnd), _inflationAt);
        }

        public void Inflate(float duration) =>
            AnimateInflationFront(1f, duration);

        public void Deflate(float duration) =>
            AnimateInflationFront(0f, duration);

        public void ContinueInflating(float fullDuration) =>
            AnimateInflationFront(1f, fullDuration * (1f - _inflationFront));

        public void ContinueDeflating(float fullDuration) =>
            AnimateInflationFront(0f, fullDuration * _inflationFront);

        private void OnDestroy()
        {
            _tween.Stop();

            if (_tube != null)
                Destroy(_tube.Mesh);

            if (_firstPersonTube != null)
                Destroy(_firstPersonTube.Mesh);
        }

        private List<Vector3> BendEndTo(IReadOnlyList<Vector3> points, Transform end)
        {
            int lastIndex = points.Count - 1;
            Vector3 offset = end.position - points[lastIndex];
            float distanceFromEnd = 0f;

            _firstPersonPoints.Clear();

            for (int i = 0; i <= lastIndex; i++)
                _firstPersonPoints.Add(points[i]);

            for (int i = lastIndex; i >= 0; i--)
            {
                if (i < lastIndex)
                    distanceFromEnd += Vector3.Distance(points[i], points[i + 1]);

                float weight = Mathf.SmoothStep(0f, 1f, 1f - distanceFromEnd / _firstPersonBlendLength);

                if (weight <= 0f)
                    break;

                _firstPersonPoints[i] = BendAroundObstacles(points[i], offset * weight);
            }

            int tipIndex = lastIndex - END_NODES + 1;
            float tipLength = Vector3.Distance(points[tipIndex], points[lastIndex]);
            _firstPersonPoints[tipIndex] = end.position + end.forward * tipLength;
            return _firstPersonPoints;
        }

        private Vector3 BendAroundObstacles(Vector3 point, Vector3 shift)
        {
            float distance = shift.magnitude;

            if (_raycastService == null || distance < MIN_BEND_DISTANCE)
                return point + shift;

            Vector3 direction = shift / distance;
            Ray ray = new Ray(point, direction);

            return _raycastService.TryRaycast(ray, distance + _firstPersonClearance, _firstPersonObstacleMask, out RaycastHit hit)
                ? point + direction * Mathf.Max(hit.distance - _firstPersonClearance, 0f)
                : point + shift;
        }

        private void AnimateInflationFront(float target, float duration)
        {
            _tween.Stop();

            if (duration <= 0f)
            {
                _inflationFront = target;
                return;
            }

            _tween = Tween.Custom(this, _inflationFront, target, duration, (view, value) => view._inflationFront = value, _inflationEase);
        }

        private float GetInflationFromOrigin(float normalizedDistance)
        {
            float shareFromOrigin = _fillOrigin == HoseFillOrigin.Start ? normalizedDistance : 1f - normalizedDistance;
            float spanLength = _leadLength + _hoseLength;
            float spanShare = spanLength > 0f ? (_leadLength + shareFromOrigin * _hoseLength) / spanLength : shareFromOrigin;
            return EvaluateInflation(spanShare);
        }

        private float EvaluateInflation(float spanShare)
        {
            float frontPosition = _inflationFront * (1f + _waveWidth);
            float inflation = Mathf.SmoothStep(0f, 1f, (frontPosition - spanShare) / _waveWidth);
            return inflation + GetFrontBulge(inflation);
        }

        private float GetFrontBulge(float inflation) =>
            _bulge * BELL_PEAK_SCALE * inflation * (1f - inflation);
    }
}
