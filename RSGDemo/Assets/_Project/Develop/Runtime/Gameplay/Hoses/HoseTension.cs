using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseTension
    {
        private const float FULL_TENSION = 0.99f;
        private const float MIN_FELT_TENSION = 0.05f;
        private const float MIN_BEND_FORCE = 0.01f;
        private const float NOISE_CENTER = 0.5f;
        private const float NOISE_RANGE = 2f;
        private const float NODE_NOISE_STEP = 0.37f;
        private const float VERTICAL_TREMBLE_RATIO = 0.5f;
        private const float VERTICAL_NOISE_SEED = 7.1f;
        private const int OVERSTRETCH_RANGE = 2;

        public event Action ReachedFullLength;

        private readonly HoseRope _rope;
        private readonly HoseTensionSettings _settings;
        private readonly HoseRopeSettings _ropeSettings;
        private readonly IRaycastService _raycastService;

        private bool _isAtFullLength;
        private float _stretch;

        public HoseTension(HoseRope rope, HoseTensionSettings settings, HoseRopeSettings ropeSettings, IRaycastService raycastService)
        {
            _rope = rope;
            _settings = settings;
            _ropeSettings = ropeSettings;
            _raycastService = raycastService;
        }

        public float Value { get; private set; }

        public float Overstretch { get; private set; }

        public float Strain { get; private set; }

        public Vector3 PullDirection { get; private set; }

        public Vector3 Pull => PullDirection * Value;

        public bool IsTaut => _rope.CanPayOut == false && _stretch > _ropeSettings.LeashStretchRatio;

        public float StrainSpeedRatio => Mathf.Lerp(1f, _settings.StrainSpeedRatio, Value);

        public float PullBackSpeed => _settings.PullBackSpeed * Overstretch;

        public void Update(bool isCarried, Vector3 carrierPosition, float deltaTime)
        {
            if (isCarried == false)
            {
                Value = 0f;
                Strain = 0f;
                Overstretch = 0f;
                _stretch = 0f;
                _isAtFullLength = false;
                return;
            }

            float blend = Smoothing.GetBlend(_settings.Sharpness, deltaTime);
            _stretch = Mathf.Lerp(_stretch, _rope.FeltStretchRatio, blend);

            float reach = (carrierPosition - _rope.Anchor).ToGroundPlane().magnitude / _rope.FreeLength;
            float reachTension = Mathf.InverseLerp(_settings.ResistanceStartReach, 1f, reach);
            float stretchTension = _rope.CanPayOut ? 0f : Mathf.InverseLerp(_settings.StretchTensionStart, _ropeSettings.LeashStretchRatio, _stretch);

            Value = Mathf.Max(reachTension, stretchTension);
            Strain = stretchTension;
            Overstretch = _rope.CanPayOut ? 0f : Mathf.InverseLerp(_ropeSettings.LeashStretchRatio, _ropeSettings.LeashStretchRatio * OVERSTRETCH_RANGE, _stretch);
            PullDirection = Vector3.Slerp(PullDirection, FindPullDirection(), blend);
            NotifyIfReachedFullLength();
        }

        public float GetBendStiffness(float slackStiffness) =>
            Mathf.Lerp(slackStiffness, _settings.TautBendStiffness, Value);

        public void Tremble(IReadOnlyList<Vector3> points, List<Vector3> trembling)
        {
            trembling.Clear();
            trembling.AddRange(points);

            float amplitude = _settings.TrembleAmplitude * Strain;

            if (amplitude <= 0f)
                return;

            float time = Time.time * _settings.TrembleFrequency;
            int lastIndex = points.Count - 1;

            for (int i = 1; i < lastIndex; i++)
            {
                float envelope = Mathf.Sin(Mathf.PI * i / lastIndex);
                Vector3 tangent = points[i + 1] - points[i - 1];
                Vector3 side = Vector3.Cross(tangent, Vector3.up).normalized;
                float sideways = GetNoise(time, i * NODE_NOISE_STEP);
                float upward = GetNoise(time, i * NODE_NOISE_STEP + VERTICAL_NOISE_SEED) * VERTICAL_TREMBLE_RATIO;
                trembling[i] = points[i] + (side * sideways + Vector3.up * upward) * (amplitude * envelope);
            }
        }

        public void PushProps(float deltaTime)
        {
            if (Value < MIN_FELT_TENSION || Time.fixedDeltaTime <= 0f)
                return;

            IReadOnlyList<Vector3> points = _rope.Points;
            float probeRadius = _rope.CollisionRadius * _settings.PropProbeRatio;
            float forceScale = _settings.PropPushForce * Value * deltaTime / Time.fixedDeltaTime;

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 bend = (points[i - 1] - points[i]).normalized + (points[i + 1] - points[i]).normalized;

                if (bend.sqrMagnitude < MIN_BEND_FORCE * MIN_BEND_FORCE)
                    continue;

                if (_raycastService.TryFindBody(points[i], probeRadius, _settings.PropMask, out Rigidbody body))
                    body.AddForceAtPosition(bend * forceScale, points[i], ForceMode.Force);
            }
        }

        private Vector3 FindPullDirection()
        {
            IReadOnlyList<Vector3> points = _rope.Points;
            int lastIndex = points.Count - 1;
            Vector3 toward = points[Mathf.Max(0, lastIndex - _settings.PullDirectionNodes)] - points[lastIndex];
            return toward.WithoutHeight().normalized;
        }

        private void NotifyIfReachedFullLength()
        {
            bool isAtFullLength = _rope.CanPayOut == false && Value >= FULL_TENSION;

            if (isAtFullLength && _isAtFullLength == false)
                ReachedFullLength?.Invoke();

            _isAtFullLength = isAtFullLength;
        }

        private static float GetNoise(float time, float seed) =>
            (Mathf.PerlinNoise(time, seed) - NOISE_CENTER) * NOISE_RANGE;
    }
}
