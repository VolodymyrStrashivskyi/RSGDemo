using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseRope
    {
        private const float REFERENCE_STEP = 1f / 60f;
        private const float MAX_STEP_TIME = 1f / 30f;
        private const float GROUND_PROBE_HEIGHT = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 10f;
        private const float CEILING_GAP = 0.01f;
        private const float MIN_DISTANCE = 0.0001f;
        private const int PINNED_NODES_PER_END = 2;
        private const int BEND_SPAN = 2;
        private const float PAY_OUT_INSERT_FRACTION = 0.5f;
        private const float STEP_HEIGHT = 0.3f;
        private const float LIP_HEIGHT = 0.03f;
        private const float OBSTACLE_LIFT = 0.01f;
        private const float OBSTACLE_MARGIN = 0.06f;
        private const float TRAVEL_SAFETY = 2f;
        private const float MAX_NODE_SPEED = 12f;
        private const float MAX_GUIDE_PULL = 0.03f;
        private const float MIN_PIVOT_DISTANCE = 1f;
        private const float MAX_COUNTED_SEGMENT_STRETCH = 1.3f;
        private const float HALF = 0.5f;
        private const int MIN_GUIDE_POINTS = 2;
        private const float HOLLOW_ESCAPE_INTERVAL = 1f / 15f;
        private const float MIN_WRAP_PUSH = 0.001f;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _previousPositions = new List<Vector3>();
        private readonly List<Vector3> _velocities = new List<Vector3>();
        private readonly List<Vector3> _pushes = new List<Vector3>();
        private readonly List<Vector3> _smoothedVelocities = new List<Vector3>();
        private readonly List<float> _groundHeights = new List<float>();
        private readonly List<bool> _wrapsObstacle = new List<bool>();
        private readonly List<bool> _isNearObstacle = new List<bool>();
        private readonly IRaycastService _raycastService;
        private readonly HoseRopeSettings _settings;
        private readonly float _segmentLength;

        private IReadOnlyList<Vector3> _guide;
        private float _timeSinceHollowEscape;
        private bool _hasStartPin;
        private bool _hasEndPin;
        private HosePin _startPin;
        private HosePin _endPin;
        private HosePin _appliedStartPin;
        private HosePin _appliedEndPin;

        public HoseRope(IRaycastService raycastService, HoseRopeSettings settings, float length)
            : this(raycastService, settings, length, length)
        {
        }

        public HoseRope(IRaycastService raycastService, HoseRopeSettings settings, float length, float maxLength)
        {
            _raycastService = raycastService;
            _settings = settings;

            int segmentCount = Mathf.Max(PINNED_NODES_PER_END * 2, Mathf.CeilToInt(length / settings.SegmentLength));
            _segmentLength = length / segmentCount;
            MaxLength = Mathf.Max(length, maxLength);
            BendStiffness = settings.BendStiffness;

            for (int i = 0; i <= segmentCount; i++)
                AddNode(_positions.Count, Vector3.zero);
        }

        public IReadOnlyList<Vector3> Points => _positions;

        public float Length => (_positions.Count - 1) * _segmentLength;

        public float MaxLength { get; }

        public bool CanPayOut => Length + _segmentLength <= MaxLength;

        public float Stretch
        {
            get
            {
                float measured = 0f;

                for (int i = 0; i < _positions.Count - 1; i++)
                    measured += Vector3.Distance(_positions[i], _positions[i + 1]);

                return measured - Length;
            }
        }

        public float SegmentLength => _segmentLength;

        public Vector3 Anchor => _positions[PINNED_NODES_PER_END - 1];

        public float StretchRatio => Stretch / Length;

        public float FeltStretchRatio
        {
            get
            {
                float measured = 0f;
                float maxCounted = _segmentLength * MAX_COUNTED_SEGMENT_STRETCH;

                for (int i = 0; i < _positions.Count - 1; i++)
                    measured += Mathf.Min(Vector3.Distance(_positions[i], _positions[i + 1]), maxCounted);

                return (measured - Length) / Length;
            }
        }

        public bool IsTaut => CanPayOut == false && StretchRatio > _settings.LeashStretchRatio;

        public float FreeLength => MaxLength - _segmentLength * (PINNED_NODES_PER_END * 2 - 1);

        public float SurfaceOffset { get; set; }

        public float CollisionRadius { get; set; }

        public float BendStiffness { get; set; }

        public Vector3 FindLeashPivot()
        {
            Vector3 end = _positions[_positions.Count - 1];

            for (int i = _positions.Count - 1 - PINNED_NODES_PER_END; i >= PINNED_NODES_PER_END; i--)
            {
                if (_wrapsObstacle[i] && (_positions[i] - end).ToGroundPlane().magnitude > MIN_PIVOT_DISTANCE)
                    return _positions[i];
            }

            return Anchor;
        }

        public void Lay(IReadOnlyList<Vector3> polyline, bool snapToGround = true)
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
                Vector3 onPath = from + direction * (distance - segmentStart);
                _positions[i] = snapToGround ? ProjectToGround(onPath) : onPath;
                _velocities[i] = Vector3.zero;
            }
        }

        public bool TryPayOut()
        {
            if (CanPayOut == false)
                return false;

            Vector3 anchor = _positions[PINNED_NODES_PER_END - 1];
            Vector3 position = Vector3.Lerp(anchor, _positions[PINNED_NODES_PER_END], PAY_OUT_INSERT_FRACTION);
            AddNode(PINNED_NODES_PER_END, position);
            return true;
        }

        public void Guide(IReadOnlyList<Vector3> path) =>
            _guide = path;

        public void PinStart(Vector3 position, Vector3 direction)
        {
            _startPin = new HosePin(position, direction);

            if (_hasStartPin)
                return;

            _hasStartPin = true;
            _appliedStartPin = _startPin;
        }

        public void PinEnd(Vector3 position, Vector3 direction)
        {
            _endPin = new HosePin(position, direction);

            if (_hasEndPin)
                return;

            _hasEndPin = true;
            _appliedEndPin = _endPin;
        }

        public void Simulate(float deltaTime)
        {
            float stepTime = Mathf.Min(deltaTime, MAX_STEP_TIME);

            if (stepTime > 0f)
                Step(stepTime);

            _appliedStartPin = _startPin;
            _appliedEndPin = _endPin;
            ApplyPins(1f);
        }

        public Vector3 ProjectToGround(Vector3 position) =>
            TryFindGround(position, out float height) ? new Vector3(position.x, height + SurfaceOffset, position.z) : position;

        private void AddNode(int index, Vector3 position)
        {
            _positions.Insert(index, position);
            _previousPositions.Insert(index, position);
            _velocities.Insert(index, Vector3.zero);
            _pushes.Insert(index, Vector3.zero);
            _smoothedVelocities.Insert(index, Vector3.zero);
            _groundHeights.Insert(index, float.MinValue);
            _wrapsObstacle.Insert(index, false);
            _isNearObstacle.Insert(index, false);
        }

        private void ApplyPins(float share)
        {
            SetPinned(0, 1, HosePin.Lerp(_appliedStartPin, _startPin, share));
            SetPinned(_positions.Count - 1, -1, HosePin.Lerp(_appliedEndPin, _endPin, share));
        }

        private void SetPinned(int index, int step, HosePin pin)
        {
            _positions[index] = pin.Position;
            _positions[index + step] = pin.Position + pin.Direction * _segmentLength;
            _velocities[index] = Vector3.zero;
            _velocities[index + step] = Vector3.zero;
        }

        private void Step(float deltaTime)
        {
            int substeps = Mathf.Max(1, Mathf.CeilToInt(_settings.Substeps * deltaTime / REFERENCE_STEP));
            float substepTime = deltaTime / substeps;
            float stepRatio = deltaTime / REFERENCE_STEP;
            float damping = Mathf.Pow(_settings.AirDamping, stepRatio / substeps);
            float bendShare = GetSubstepShare(BendStiffness, substeps);
            float radius = Mathf.Max(CollisionRadius, SurfaceOffset);

            ClearObstacleWraps();
            PullTowardGuide(stepRatio);
            UpdateGroundHeights();
            FindNodesNearObstacles(radius, deltaTime);

            for (int substep = 0; substep < substeps; substep++)
            {
                ApplyPins((float)(substep + 1) / substeps);
                Integrate(substepTime, damping);
                SolveLengths(substep % 2 == 0);
                SolveBends(bendShare);
                SolveGround();
                SolveCollisions(radius);
                UpdateVelocities(substepTime);
            }

            SmoothVelocities();
            ApplyGroundFriction(stepRatio);
            EscapeHollowSolidsSometimes(radius, deltaTime);
        }

        private void SmoothVelocities()
        {
            float share = _settings.VelocitySmoothing;

            if (share <= 0f)
                return;

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
                _smoothedVelocities[i] = Vector3.Lerp(_velocities[i], (_velocities[i - 1] + _velocities[i + 1]) * HALF, share);

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
                _velocities[i] = _smoothedVelocities[i];
        }

        private void Integrate(float deltaTime, float damping)
        {
            Vector3 gravityStep = Physics.gravity * deltaTime;

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                _previousPositions[i] = _positions[i];
                _velocities[i] = (_velocities[i] + gravityStep) * damping;
                _positions[i] += _velocities[i] * deltaTime;
            }
        }

        private void UpdateVelocities(float deltaTime)
        {
            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                Vector3 velocity = (_positions[i] - _previousPositions[i]) / deltaTime;
                Vector3 push = _pushes[i];

                if (push.sqrMagnitude > 0f)
                {
                    Vector3 normal = push.normalized;
                    velocity -= normal * Mathf.Min(Vector3.Dot(velocity, normal), 0f);
                    _pushes[i] = Vector3.zero;
                }

                _velocities[i] = Vector3.ClampMagnitude(velocity, MAX_NODE_SPEED);
            }
        }

        private void Push(int index, Vector3 offset)
        {
            if (GetInverseMass(index) <= 0f)
                return;

            _positions[index] += offset;
            _previousPositions[index] += offset;
            _pushes[index] += offset;
        }

        private void SolveLengths(bool isForward)
        {
            int last = _positions.Count - 2;

            for (int k = 0; k <= last; k++)
            {
                int i = isForward ? k : last - k;
                SolveDistance(i, i + 1, _segmentLength, 1f);
            }
        }

        private void SolveBends(float share)
        {
            float restDistance = _segmentLength * BEND_SPAN;

            for (int i = 0; i < _positions.Count - BEND_SPAN; i++)
            {
                if (Vector3.Distance(_positions[i], _positions[i + BEND_SPAN]) < restDistance)
                    SolveDistance(i, i + BEND_SPAN, restDistance, share);
            }
        }

        private void SolveDistance(int first, int second, float restDistance, float share)
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

            Vector3 correction = delta * ((distance - restDistance) / distance * share / totalWeight);
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
                    Push(i, Vector3.up * (restingHeight - position.y));
            }
        }

        private void SolveCollisions(float radius)
        {
            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                if (_isNearObstacle[i] == false)
                    continue;

                if (_raycastService.TryPushOutSphere(_positions[i], radius, _settings.CollisionMask, GetStepHeight(i), out Vector3 freePosition) == false)
                    continue;

                MarkWrapIfPushedSideways(i, freePosition - _positions[i]);
                Push(i, freePosition - _positions[i]);
            }

            SolveSegmentCollisions(radius);
        }

        private void SolveSegmentCollisions(float radius)
        {
            for (int i = PINNED_NODES_PER_END - 1; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                int next = i + 1;

                if (_isNearObstacle[i] == false && _isNearObstacle[next] == false)
                    continue;

                float firstWeight = GetInverseMass(i);
                float secondWeight = GetInverseMass(next);
                float totalWeight = firstWeight + secondWeight;

                if (totalWeight <= 0f)
                    continue;

                Vector3 middle = (_positions[i] + _positions[next]) * HALF;
                float stepHeight = Mathf.Min(GetStepHeight(i), GetStepHeight(next));

                if (_raycastService.TryPushOutSphere(middle, radius, _settings.CollisionMask, stepHeight, out Vector3 freeMiddle) == false)
                    continue;

                Vector3 push = (freeMiddle - middle) / (totalWeight * HALF);
                Push(i, push * firstWeight);
                Push(next, push * secondWeight);
                MarkWrapIfPushedSideways(i, push);
                MarkWrapIfPushedSideways(next, push);
            }
        }

        private void FindNodesNearObstacles(float radius, float deltaTime)
        {
            for (int i = 0; i < _positions.Count; i++)
            {
                float probeRadius = radius + OBSTACLE_MARGIN + _velocities[i].magnitude * deltaTime * TRAVEL_SAFETY;
                Vector3 center = _positions[i];
                center.y = Mathf.Max(center.y, _groundHeights[i] + probeRadius + OBSTACLE_LIFT);
                _isNearObstacle[i] = _raycastService.IsAnyColliderNear(center, probeRadius, _settings.CollisionMask);
            }
        }

        private void PullTowardGuide(float stepRatio)
        {
            if (_guide == null || _guide.Count < MIN_GUIDE_POINTS)
                return;

            int pathIndex = _guide.Count - 1;
            float pathDistance = 0f;

            for (int i = _positions.Count - 1 - PINNED_NODES_PER_END; i >= PINNED_NODES_PER_END; i--)
            {
                float nodeDistance = (_positions.Count - 1 - i) * _segmentLength;
                float pathSegment = Vector3.Distance(_guide[pathIndex], _guide[pathIndex - 1]);

                while (pathDistance + pathSegment < nodeDistance)
                {
                    pathDistance += pathSegment;
                    pathIndex--;

                    if (pathIndex == 0)
                        return;

                    pathSegment = Vector3.Distance(_guide[pathIndex], _guide[pathIndex - 1]);
                }

                float along = (nodeDistance - pathDistance) / Mathf.Max(pathSegment, MIN_DISTANCE);
                Vector3 target = Vector3.Lerp(_guide[pathIndex], _guide[pathIndex - 1], along);
                Vector3 pull = ((target - _positions[i]) * (_settings.TrailPull * stepRatio)).WithoutHeight();
                _positions[i] += Vector3.ClampMagnitude(pull, MAX_GUIDE_PULL * stepRatio);
            }
        }

        private void ApplyGroundFriction(float stepRatio)
        {
            float kept = Mathf.Pow(1f - _settings.GroundFriction, stepRatio);

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                if (IsOnGround(i) == false)
                    continue;

                Vector3 velocity = _velocities[i];
                bool isResting = velocity.ToGroundPlane().sqrMagnitude < _settings.StaticFrictionSpeed * _settings.StaticFrictionSpeed;
                float planarShare = isResting ? 0f : kept;
                _velocities[i] = new Vector3(velocity.x * planarShare, velocity.y, velocity.z * planarShare);
            }
        }

        private void EscapeHollowSolidsSometimes(float radius, float deltaTime)
        {
            _timeSinceHollowEscape += deltaTime;

            if (_timeSinceHollowEscape < HOLLOW_ESCAPE_INTERVAL)
                return;

            _timeSinceHollowEscape = 0f;

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                if (_isNearObstacle[i] == false)
                    continue;

                if (_raycastService.TryEscapeHollow(_positions[i], radius, _settings.HollowEscapeDistance, _settings.CollisionMask, out Vector3 freePosition) == false)
                    continue;

                _positions[i] = freePosition;
                _velocities[i] = Vector3.zero;
            }
        }

        private void ClearObstacleWraps()
        {
            for (int i = 0; i < _wrapsObstacle.Count; i++)
                _wrapsObstacle[i] = false;
        }

        private void MarkWrapIfPushedSideways(int index, Vector3 push)
        {
            if (push.ToGroundPlane().sqrMagnitude > MIN_WRAP_PUSH * MIN_WRAP_PUSH)
                _wrapsObstacle[index] = true;
        }

        private void UpdateGroundHeights()
        {
            for (int i = 0; i < _positions.Count; i++)
                _groundHeights[i] = TryFindGround(_positions[i], out float height) ? height : float.MinValue;
        }

        private bool IsOnGround(int index) =>
            _positions[index].y <= _groundHeights[index] + SurfaceOffset + _settings.GroundContactTolerance;

        private float GetStepHeight(int index) =>
            IsOnGround(index) ? STEP_HEIGHT : LIP_HEIGHT;

        private float GetInverseMass(int index) =>
            index < PINNED_NODES_PER_END || index >= _positions.Count - PINNED_NODES_PER_END ? 0f : 1f;

        private static float GetSubstepShare(float stiffness, int substeps) =>
            stiffness >= 1f ? 1f : 1f - Mathf.Pow(1f - stiffness, 1f / substeps);

        private bool TryFindGround(Vector3 position, out float height)
        {
            Ray ray = new Ray(position + Vector3.up * GetClearanceAbove(position), Vector3.down);

            if (_raycastService.TryRaycast(ray, GROUND_PROBE_DISTANCE, _settings.GroundMask, out RaycastHit hit))
            {
                height = hit.point.y;
                return true;
            }

            height = float.MinValue;
            return false;
        }

        private float GetClearanceAbove(Vector3 position)
        {
            Ray ray = new Ray(position, Vector3.up);

            return _raycastService.TryRaycast(ray, GROUND_PROBE_HEIGHT, _settings.GroundMask, out RaycastHit ceiling)
                ? Mathf.Max(ceiling.distance - CEILING_GAP, 0f)
                : GROUND_PROBE_HEIGHT;
        }

    }
}
