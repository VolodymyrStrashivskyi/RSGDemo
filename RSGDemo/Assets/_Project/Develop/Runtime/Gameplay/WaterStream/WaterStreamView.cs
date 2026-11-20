using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class WaterStreamView : MonoBehaviour
    {
        private const float MIN_VISIBLE_FLOW = 0.01f;
        private const float HALF = 0.5f;
        private const float PULSE_RANGE = 2f;
        private const float FLICKER_SEED = 37.1f;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private ParticleSystem _spray;
        [SerializeField] private ParticleSystem _mist;
        [SerializeField] private ParticleSystem _splash;
        [SerializeField] private ParticleSystem _splashMist;
        [SerializeField] private PuddleField _puddles;
        [SerializeField, Tooltip("Own tuning for this stream; the shared water stream config is used when empty")]
        private WaterStreamConfig _ownConfig;
        [SerializeField, Tooltip("Copy of the stream drawn only by the first-person camera; empty for streams nobody holds")]
        private MeshFilter _firstPersonMeshFilter;
        [SerializeField] private MeshRenderer _firstPersonRenderer;
        [SerializeField, Range(0, 31), Tooltip("Layer the first-person camera does not render, used for the real stream while the copy shows")]
        private int _firstPersonHiddenLayer;
        [SerializeField, Min(0.01f), Tooltip("Length over which the first-person copy bends from the on-screen nozzle onto the real stream, m")]
        private float _firstPersonBlendLength = 2f;

        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<float> _progress = new List<float>();
        private readonly List<float> _opacities = new List<float>();
        private readonly List<Vector3> _firstPersonPoints = new List<Vector3>();

        private WaterStreamConfig _config;
        private IRaycastService _raycastService;
        private WaterStreamMesh _mesh;
        private WaterStreamMesh _firstPersonMesh;
        private Collider _pushedCollider;
        private IWaterPushable _pushable;
        private Transform _muzzle;
        private Transform _firstPersonMuzzle;
        private int _visibleLayer;
        private Vector3 _impactVelocity;
        private float _impactBreakup;

        [Inject]
        public void Construct(WaterStreamConfig config, IRaycastService raycastService)
        {
            _config = _ownConfig != null ? _ownConfig : config;
            _raycastService = raycastService;
            _mesh = new WaterStreamMesh(_config.Sides);
            _meshFilter.sharedMesh = _mesh.Mesh;
            _visibleLayer = _meshRenderer.gameObject.layer;

            if (_firstPersonMeshFilter != null)
            {
                _firstPersonMesh = new WaterStreamMesh(_config.Sides);
                _firstPersonMeshFilter.sharedMesh = _firstPersonMesh.Mesh;
                _firstPersonRenderer.enabled = false;
            }
        }

        public float Flow { get; set; }

        public void Burst() =>
            _spray.Emit(_config.BurstCount);

        public void Follow(Transform muzzle, Transform firstPersonMuzzle)
        {
            _muzzle = muzzle;
            _firstPersonMuzzle = firstPersonMuzzle;
        }

        private void LateUpdate()
        {
            if (_mesh == null)
                return;

            if (_muzzle != null)
                transform.SetPositionAndRotation(_muzzle.position, _muzzle.rotation);

            bool isFlowing = Flow > MIN_VISIBLE_FLOW;
            float speed = isFlowing ? GetExitSpeed() : 0f;
            bool hasImpact = false;
            RaycastHit impact = default;

            if (isFlowing)
            {
                hasImpact = TraceArc(speed, out impact);
                ApplyEndFade();
                _mesh.Build(_points, _radii, _progress, _opacities);
            }

            if (hasImpact)
                Push(impact);

            _meshRenderer.enabled = isFlowing;
            RenderFirstPersonCopy(isFlowing);
            float flowRate = isFlowing ? Flow : 0f;
            UpdateEmitter(_spray, _config.SprayRate * flowRate, speed);
            UpdateEmitter(_mist, _config.MistRate * flowRate, speed);
            UpdateSplash(hasImpact, impact, flowRate);
            UpdatePuddle(hasImpact, impact, flowRate);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh.Mesh);

            if (_firstPersonMesh != null)
                Destroy(_firstPersonMesh.Mesh);
        }

        private void RenderFirstPersonCopy(bool isFlowing)
        {
            if (_firstPersonMesh == null)
                return;

            bool isShown = isFlowing && _firstPersonMuzzle != null;
            _firstPersonRenderer.enabled = isShown;
            _meshRenderer.gameObject.layer = _firstPersonMuzzle != null ? _firstPersonHiddenLayer : _visibleLayer;

            if (isShown)
                _firstPersonMesh.Build(BendStartTo(transform.InverseTransformPoint(_firstPersonMuzzle.position)), _radii, _progress, _opacities);
        }

        private List<Vector3> BendStartTo(Vector3 start)
        {
            _firstPersonPoints.Clear();
            Vector3 offset = start - _points[0];
            float distance = 0f;

            for (int i = 0; i < _points.Count; i++)
            {
                if (i > 0)
                    distance += Vector3.Distance(_points[i - 1], _points[i]);

                float weight = 1f - Mathf.SmoothStep(0f, 1f, distance / _firstPersonBlendLength);
                _firstPersonPoints.Add(_points[i] + offset * weight);
            }

            return _firstPersonPoints;
        }

        private float GetExitSpeed()
        {
            float pulse = (Mathf.PerlinNoise(Time.time * _config.FlickerFrequency, FLICKER_SEED) - HALF) * PULSE_RANGE;
            return _config.ExitSpeed * Mathf.Sqrt(Flow) * (1f + pulse * _config.FlickerAmplitude);
        }

        private bool TraceArc(float speed, out RaycastHit impact)
        {
            _points.Clear();
            _radii.Clear();
            _progress.Clear();
            _opacities.Clear();

            Vector3 gravity = Physics.gravity;
            Vector3 position = transform.position;
            Vector3 velocity = transform.forward * speed;
            float step = _config.MaxFlightTime / _config.ArcSegments;
            float radiusScale = Mathf.Lerp(_config.LowFlowRadiusScale, 1f, Mathf.Clamp01(Flow));
            AddArcPoint(position, 0f, radiusScale);

            for (int segment = 1; segment <= _config.ArcSegments; segment++)
            {
                Vector3 next = position + velocity * step + gravity * (HALF * step * step);
                Vector3 offset = next - position;
                float time = segment * step;

                if (_raycastService.TryRaycast(new Ray(position, offset), offset.magnitude, _config.CollisionMask, out impact))
                {
                    float hitTime = time - step * (1f - impact.distance / offset.magnitude);
                    AddArcPoint(impact.point, hitTime, radiusScale);
                    _impactVelocity = velocity + gravity * (hitTime - (time - step));
                    _impactBreakup = Mathf.Clamp01(hitTime / _config.BreakupTime);
                    return true;
                }

                AddArcPoint(next, time, radiusScale);
                velocity += gravity * step;
                position = next;
            }

            impact = default;
            return false;
        }

        private void AddArcPoint(Vector3 worldPosition, float flightTime, float radiusScale)
        {
            float breakup = Mathf.Clamp01(flightTime / _config.BreakupTime);
            _points.Add(transform.InverseTransformPoint(worldPosition));
            float spread = Mathf.Pow(breakup, _config.SpreadExponent);
            _radii.Add(Mathf.Lerp(_config.NozzleRadius, _config.SpreadRadius, spread) * radiusScale);
            _progress.Add(breakup);
            _opacities.Add(Mathf.Clamp01(Flow / _config.FadeFlow));
        }

        private void ApplyEndFade()
        {
            float distanceFromEnd = 0f;

            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (i < _points.Count - 1)
                    distanceFromEnd += Vector3.Distance(_points[i], _points[i + 1]);

                if (distanceFromEnd >= _config.EndFadeLength)
                    return;

                float closeness = 1f - distanceFromEnd / _config.EndFadeLength;
                _opacities[i] *= 1f - Mathf.SmoothStep(0f, 1f, closeness);
                _radii[i] *= 1f + _config.EndSplay * closeness * closeness;
            }
        }

        private void Push(RaycastHit impact)
        {
            float strength = _config.PushForce * Flow * Mathf.Lerp(1f, _config.BrokenPushScale, _impactBreakup);
            Vector3 impulse = _impactVelocity.normalized * (strength * Time.deltaTime);

            if (TryGetPushable(impact.collider, out IWaterPushable pushable))
                pushable.AddWaterPush(impulse);
            else if (impact.rigidbody != null)
                impact.rigidbody.AddForceAtPosition(impulse, impact.point, ForceMode.Impulse);
        }

        private bool TryGetPushable(Collider target, out IWaterPushable pushable)
        {
            if (target != _pushedCollider)
            {
                _pushedCollider = target;
                _pushable = target.GetComponentInParent<IWaterPushable>();
            }

            pushable = _pushable;
            return pushable != null;
        }

        private static void UpdateEmitter(ParticleSystem system, float rate, float speed)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTimeMultiplier = rate;

            if (speed > 0f)
            {
                ParticleSystem.MainModule main = system.main;
                main.startSpeedMultiplier = speed;
            }
        }

        private void UpdateSplash(bool hasImpact, RaycastHit impact, float flowRate)
        {
            float impactFlow = hasImpact ? flowRate : 0f;
            UpdateEmitter(_splash, _config.SplashRate * impactFlow, 0f);
            UpdateEmitter(_splashMist, _config.SplashMistRate * impactFlow, 0f);

            if (hasImpact)
                _splash.transform.SetPositionAndRotation(impact.point, Quaternion.LookRotation(impact.normal));
        }

        private void UpdatePuddle(bool hasImpact, RaycastHit impact, float flowRate)
        {
            if (hasImpact)
                _puddles.Pour(impact, _config.PuddleFillRate * flowRate * Time.deltaTime);
            else
                _puddles.Stop();
        }
    }
}
