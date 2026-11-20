using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    [RequireComponent(typeof(MeshFilter))]
    public class HoseView : MonoBehaviour
    {
        private const float BELL_PEAK_SCALE = 4f;
        private const int END_NODES = 2;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Renderer _renderer;
        [SerializeField, Tooltip("Copy of the hose for the first-person camera, led to the view model of the held end")]
        private MeshFilter _firstPersonMeshFilter;
        [SerializeField] private Renderer _firstPersonRenderer;
        [SerializeField, Range(0, 31), Tooltip("Layer the first-person camera does not render, used for the real hose while its copy shows")]
        private int _firstPersonHiddenLayer;
        [SerializeField, Min(0.1f), Tooltip("Length of hose near the held end bent over to reach the view model, m")]
        private float _firstPersonBlendLength = 1.5f;
        [SerializeField] private Ease _inflationEase = Ease.InOutSine;

        private readonly List<Vector3> _firstPersonPoints = new List<Vector3>();
        private HoseTubeMesh _tube;
        private HoseTubeMesh _firstPersonTube;
        private Func<float, float> _inflationAt;
        private float _waveWidth;
        private float _bulge;
        private float _inflationFront;
        private int _visibleLayer;
        private Tween _tween;

        public float InflationFront => _inflationFront;

        public void Initialize(HoseTubeMesh tube, HoseTubeMesh firstPersonTube, float waveWidth, float bulge)
        {
            _tube = tube;
            _firstPersonTube = firstPersonTube;
            _waveWidth = waveWidth;
            _bulge = bulge;
            _inflationAt = GetInflationFromEnd;
            _meshFilter.sharedMesh = tube.Mesh;
            _firstPersonMeshFilter.sharedMesh = firstPersonTube.Mesh;
            _visibleLayer = _renderer.gameObject.layer;
            _firstPersonRenderer.enabled = false;
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

                _firstPersonPoints[i] += offset * weight;
            }

            int tipIndex = lastIndex - END_NODES + 1;
            float tipLength = Vector3.Distance(points[tipIndex], points[lastIndex]);
            _firstPersonPoints[tipIndex] = end.position + end.forward * tipLength;
            return _firstPersonPoints;
        }

        private void AnimateInflationFront(float target, float duration)
        {
            _tween.Stop();
            _tween = Tween.Custom(this, _inflationFront, target, duration, (view, value) => view._inflationFront = value, _inflationEase);
        }

        private float GetInflationFromEnd(float normalizedDistance)
        {
            float distanceFromEnd = 1f - normalizedDistance;
            float frontPosition = _inflationFront * (1f + _waveWidth);
            float inflation = Mathf.SmoothStep(0f, 1f, (frontPosition - distanceFromEnd) / _waveWidth);
            return inflation + GetFrontBulge(inflation);
        }

        private float GetFrontBulge(float inflation) =>
            _bulge * BELL_PEAK_SCALE * inflation * (1f - inflation);
    }
}
