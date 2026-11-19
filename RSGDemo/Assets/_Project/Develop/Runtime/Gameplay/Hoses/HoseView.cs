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

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Ease _inflationEase = Ease.InOutSine;

        private HoseTubeMesh _tube;
        private Func<float, float> _inflationAt;
        private float _waveWidth;
        private float _bulge;
        private float _inflationFront;
        private Tween _tween;

        public float InflationFront => _inflationFront;

        public void Initialize(HoseTubeMesh tube, float waveWidth, float bulge)
        {
            _tube = tube;
            _waveWidth = waveWidth;
            _bulge = bulge;
            _inflationAt = GetInflationFromEnd;
            _meshFilter.sharedMesh = tube.Mesh;
        }

        public void Render(IReadOnlyList<Vector3> points) =>
            _tube.Build(points, _inflationAt);

        public void Inflate(float duration) =>
            AnimateInflationFront(1f, duration);

        public void Deflate(float duration) =>
            AnimateInflationFront(0f, duration);

        private void OnDestroy()
        {
            _tween.Stop();

            if (_tube != null)
                Destroy(_tube.Mesh);
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
