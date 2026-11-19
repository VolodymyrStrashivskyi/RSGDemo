using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseRope
    {
        private const float FIXED_STEP = 1f / 60f;
        private const int MAX_STEPS_PER_FRAME = 4;
        private const float GROUND_PROBE_HEIGHT = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 10f;
        private const float MIN_DISTANCE = 0.0001f;
        private const int PINNED_NODES_PER_END = 2;
        private const int BEND_SPAN = 2;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _previousPositions = new List<Vector3>();
        private readonly List<float> _groundHeights = new List<float>();
        private readonly IRaycastService _raycastService;
        private readonly HoseRopeSettings _settings;
        private readonly float _segmentLength;

        private float _accumulatedTime;

        public HoseRope(IRaycastService raycastService, HoseRopeSettings settings, float length)
        {
            _raycastService = raycastService;
            _settings = settings;

            int segmentCount = Mathf.Max(PINNED_NODES_PER_END * 2, Mathf.CeilToInt(length / settings.SegmentLength));
            _segmentLength = length / segmentCount;
            Length = length;
            BendStiffness = settings.BendStiffness;

            for (int i = 0; i <= segmentCount; i++)
            {
                _positions.Add(Vector3.zero);
                _previousPositions.Add(Vector3.zero);
                _groundHeights.Add(float.MinValue);
            }
        }

        public IReadOnlyList<Vector3> Points => _positions;

        public float Length { get; }

        public float SegmentLength => _segmentLength;

        public Vector3 Anchor => _positions[PINNED_NODES_PER_END - 1];

        public float FreeLength => Length - _segmentLength * (PINNED_NODES_PER_END * 2 - 1);

        public float SurfaceOffset { get; set; }

        public float BendStiffness { get; set; }

        public void Lay(IReadOnlyList<Vector3> polyline)
        {
            int segment = 0;
            float segmentStart = 0f;
            float segmentEnd = Vector3.Distance(polyline[0], polyline[1]);

            for (int i = 0; i < _positions.Count; i++)
            {
                float distance = i * _segmentLength;

                while (distance > segmentEnd && segment < polyline.Count - 2)
                {
                    segment++;
                    segmentStart = segmentEnd;
                    segmentEnd += Vector3.Distance(polyline[segment], polyline[segment + 1]);
                }

                Vector3 from = polyline[segment];
                Vector3 direction = (polyline[segment + 1] - from).normalized;
                Vector3 position = ProjectToGround(from + direction * (distance - segmentStart));
                _positions[i] = position;
                _previousPositions[i] = position;
            }
        }

        public void PinStart(Vector3 position, Vector3 direction) =>
            Pin(0, 1, position, direction);

        public void PinEnd(Vector3 position, Vector3 direction) =>
            Pin(_positions.Count - 1, -1, position, direction);

        public void Simulate(float deltaTime)
        {
            _accumulatedTime = Mathf.Min(_accumulatedTime + deltaTime, FIXED_STEP * MAX_STEPS_PER_FRAME);

            while (_accumulatedTime >= FIXED_STEP)
            {
                Step(FIXED_STEP);
                _accumulatedTime -= FIXED_STEP;
            }
        }

        public Vector3 ProjectToGround(Vector3 position) =>
            TryFindGround(position, out float height) ? new Vector3(position.x, height + SurfaceOffset, position.z) : position;

        private void Pin(int index, int step, Vector3 position, Vector3 direction)
        {
            Vector3 next = position + direction.normalized * _segmentLength;
            SetPinned(index, position);
            SetPinned(index + step, next);
        }

        private void SetPinned(int index, Vector3 position)
        {
            _positions[index] = position;
            _previousPositions[index] = position;
        }

        private void Step(float deltaTime)
        {
            Integrate(deltaTime);
            UpdateGroundHeights();

            for (int iteration = 0; iteration < _settings.ConstraintIterations; iteration++)
            {
                SolveLengths();
                SolveBends();
                SolveGround();
            }

            ApplyGroundFriction();
        }

        private void Integrate(float deltaTime)
        {
            Vector3 gravityStep = Physics.gravity * (deltaTime * deltaTime);

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                Vector3 position = _positions[i];
                Vector3 velocity = (position - _previousPositions[i]) * _settings.AirDamping;
                _previousPositions[i] = position;
                _positions[i] = position + velocity + gravityStep;
            }
        }

        private void UpdateGroundHeights()
        {
            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
                _groundHeights[i] = TryFindGround(_positions[i], out float height) ? height : float.MinValue;
        }

        private void SolveLengths()
        {
            for (int i = 0; i < _positions.Count - 1; i++)
                SolveDistance(i, i + 1, _segmentLength, 1f);
        }

        private void SolveBends()
        {
            float restDistance = _segmentLength * BEND_SPAN;

            for (int i = 0; i < _positions.Count - BEND_SPAN; i++)
            {
                if (Vector3.Distance(_positions[i], _positions[i + BEND_SPAN]) < restDistance)
                    SolveDistance(i, i + BEND_SPAN, restDistance, BendStiffness);
            }
        }

        private void SolveDistance(int first, int second, float restDistance, float stiffness)
        {
            float firstWeight = GetInverseMass(first);
            float secondWeight = GetInverseMass(second);
            float totalWeight = firstWeight + secondWeight;

            if (totalWeight <= 0f)
                return;

            Vector3 delta = _positions[second] - _positions[first];
            float distance = delta.magnitude;

            if (distance < MIN_DISTANCE)
                return;

            Vector3 correction = delta * ((distance - restDistance) / distance * stiffness / totalWeight);
            _positions[first] += correction * firstWeight;
            _positions[second] -= correction * secondWeight;
        }

        private void SolveGround()
        {
            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                Vector3 position = _positions[i];

                float restingHeight = _groundHeights[i] + SurfaceOffset;

                if (position.y < restingHeight)
                    _positions[i] = new Vector3(position.x, restingHeight, position.z);
            }
        }

        private void ApplyGroundFriction()
        {
            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                Vector3 position = _positions[i];

                if (position.y > _groundHeights[i] + SurfaceOffset + _settings.GroundContactTolerance)
                    continue;

                Vector3 previous = _previousPositions[i];
                Vector3 slowed = Vector3.Lerp(previous, position, _settings.GroundFriction);
                _previousPositions[i] = new Vector3(slowed.x, previous.y, slowed.z);
            }
        }

        private float GetInverseMass(int index) =>
            index < PINNED_NODES_PER_END || index >= _positions.Count - PINNED_NODES_PER_END ? 0f : 1f;

        private bool TryFindGround(Vector3 position, out float height)
        {
            Ray ray = new Ray(position + Vector3.up * GROUND_PROBE_HEIGHT, Vector3.down);

            if (_raycastService.TryRaycast(ray, GROUND_PROBE_DISTANCE, _settings.GroundMask, out RaycastHit hit))
            {
                height = hit.point.y;
                return true;
            }

            height = float.MinValue;
            return false;
        }
    }
}
