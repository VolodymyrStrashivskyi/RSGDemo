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
        private const float CEILING_GAP = 0.01f;
        private const float MIN_DISTANCE = 0.0001f;
        private const int PINNED_NODES_PER_END = 2;
        private const int BEND_SPAN = 2;
        private const float PAY_OUT_INSERT_FRACTION = 0.5f;
        private const float SWEEP_MIN_MOVE_RATIO = 0.5f;
        private const float SWEEP_SKIN = 0.002f;
        private const float MAX_OBSTACLE_UPWARD_NORMAL = 0.7f;
        private const float STEP_HEIGHT = 0.3f;
        private const float STEP_PROBE_INSET = 0.02f;
        private const float MIN_STEP_RISE = 0.02f;
        private const float STEP_PROBE_DEPTH_RATIO = 2f;
        private const float HALF = 0.5f;
        private const int MIN_GUIDE_POINTS = 2;
        private const int STEPS_PER_HOLLOW_ESCAPE = 4;
        private const float MIN_WRAP_PUSH = 0.001f;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _previousPositions = new List<Vector3>();
        private readonly List<float> _groundHeights = new List<float>();
        private readonly List<Vector3> _sweepStarts = new List<Vector3>();
        private readonly List<bool> _wrapsObstacle = new List<bool>();
        private readonly IRaycastService _raycastService;
        private readonly HoseRopeSettings _settings;
        private readonly float _segmentLength;

        private IReadOnlyList<Vector3> _guide;
        private float _accumulatedTime;
        private int _stepsSinceHollowEscape;

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
            {
                _positions.Add(Vector3.zero);
                _previousPositions.Add(Vector3.zero);
                _groundHeights.Add(float.MinValue);
                _wrapsObstacle.Add(false);
            }
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

        public bool IsTaut => CanPayOut == false && StretchRatio > _settings.LeashStretchRatio;

        public Vector3 FindLeashPivot()
        {
            for (int i = _positions.Count - 1 - PINNED_NODES_PER_END; i >= PINNED_NODES_PER_END; i--)
            {
                if (_wrapsObstacle[i])
                    return _positions[i];
            }

            return Anchor;
        }

        public float FreeLength => MaxLength - _segmentLength * (PINNED_NODES_PER_END * 2 - 1);

        public float SurfaceOffset { get; set; }

        public float CollisionRadius { get; set; }

        public float BendStiffness { get; set; }

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
                Vector3 position = snapToGround ? ProjectToGround(onPath) : onPath;
                _positions[i] = position;
                _previousPositions[i] = position;
            }
        }

        public bool TryPayOut()
        {
            if (CanPayOut == false)
                return false;

            Vector3 anchor = _positions[PINNED_NODES_PER_END - 1];
            Vector3 position = Vector3.Lerp(anchor, _positions[PINNED_NODES_PER_END], PAY_OUT_INSERT_FRACTION);
            _positions.Insert(PINNED_NODES_PER_END, position);
            _previousPositions.Insert(PINNED_NODES_PER_END, position);
            _groundHeights.Insert(PINNED_NODES_PER_END, float.MinValue);
            _wrapsObstacle.Insert(PINNED_NODES_PER_END, false);
            return true;
        }

        public void Guide(IReadOnlyList<Vector3> path) =>
            _guide = path;

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
            CaptureSweepStarts();
            ClearObstacleWraps();
            Integrate(deltaTime);
            PullTowardGuide();
            UpdateGroundHeights();

            int lastIteration = _settings.ConstraintIterations - 1;
            int iterationsPerCollision = Mathf.Max(1, _settings.ConstraintIterations / _settings.CollisionPasses);

            for (int iteration = 0; iteration <= lastIteration; iteration++)
            {
                SolveLengths();
                SolveBends();
                SolveGround();

                if ((iteration + 1) % iterationsPerCollision == 0 || iteration == lastIteration)
                {
                    SolveCollisions();
                    CaptureSweepStarts();
                }
            }

            _stepsSinceHollowEscape++;

            if (_stepsSinceHollowEscape >= STEPS_PER_HOLLOW_ESCAPE)
            {
                _stepsSinceHollowEscape = 0;
                EscapeHollowSolids();
            }

            ApplyGroundFriction();
        }

        private void PullTowardGuide()
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
                Vector3 pull = (target - _positions[i]) * _settings.TrailPull;
                pull.y = 0f;
                _positions[i] += pull;
                _previousPositions[i] += pull;
            }
        }

        private void EscapeHollowSolids()
        {
            float radius = Mathf.Max(CollisionRadius, SurfaceOffset);

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                if (_raycastService.TryEscapeHollow(_positions[i], radius, _settings.HollowEscapeDistance, _settings.CollisionMask, out Vector3 freePosition) == false)
                    continue;

                _positions[i] = freePosition;
                _previousPositions[i] = freePosition;
            }
        }

        private void CaptureSweepStarts()
        {
            _sweepStarts.Clear();
            _sweepStarts.AddRange(_positions);
        }

        private void SolveCollisions()
        {
            float radius = Mathf.Max(CollisionRadius, SurfaceOffset);

            for (int i = PINNED_NODES_PER_END; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                float stepHeight = IsOnGround(i) ? STEP_HEIGHT : 0f;
                Vector3 position = Sweep(_sweepStarts[i], _positions[i], radius, stepHeight);

                if (_raycastService.TryPushOutSphere(position, radius, _settings.CollisionMask, stepHeight, out Vector3 freePosition))
                    position = freePosition;

                MarkWrapIfPushedSideways(i, position - _positions[i]);
                _positions[i] = position;
            }

            SolveSegmentCollisions(radius);
        }

        private void SolveSegmentCollisions(float radius)
        {
            for (int i = PINNED_NODES_PER_END - 1; i < _positions.Count - PINNED_NODES_PER_END; i++)
            {
                int next = i + 1;
                float firstWeight = GetInverseMass(i);
                float secondWeight = GetInverseMass(next);
                float totalWeight = firstWeight + secondWeight;

                if (totalWeight <= 0f)
                    continue;

                Vector3 middle = (_positions[i] + _positions[next]) * HALF;
                float stepHeight = IsOnGround(i) && IsOnGround(next) ? STEP_HEIGHT : 0f;

                if (_raycastService.TryPushOutSphere(middle, radius, _settings.CollisionMask, stepHeight, out Vector3 freeMiddle) == false)
                    continue;

                Vector3 push = (freeMiddle - middle) / (totalWeight * HALF);
                _positions[i] += push * firstWeight;
                _positions[next] += push * secondWeight;
                MarkWrapIfPushedSideways(i, push);
                MarkWrapIfPushedSideways(next, push);
            }
        }

        private void ClearObstacleWraps()
        {
            for (int i = 0; i < _wrapsObstacle.Count; i++)
                _wrapsObstacle[i] = false;
        }

        private void MarkWrapIfPushedSideways(int index, Vector3 push)
        {
            if (new Vector2(push.x, push.z).sqrMagnitude > MIN_WRAP_PUSH * MIN_WRAP_PUSH)
                _wrapsObstacle[index] = true;
        }

        private bool IsOnGround(int index) =>
            _positions[index].y <= _groundHeights[index] + SurfaceOffset + _settings.GroundContactTolerance;

        private Vector3 Sweep(Vector3 from, Vector3 to, float radius, float stepHeight)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;

            if (distance <= radius * SWEEP_MIN_MOVE_RATIO)
                return to;

            Vector3 direction = delta / distance;
            Ray ray = new Ray(from, direction);

            if (TryHitObstacle(ray, radius, distance, stepHeight, out RaycastHit hit) == false)
                return to;

            Vector3 contact = from + direction * Mathf.Max(hit.distance - SWEEP_SKIN, 0f);
            Vector3 slide = Vector3.ProjectOnPlane(to - contact, hit.normal);
            float slideDistance = slide.magnitude;

            if (slideDistance <= radius * SWEEP_MIN_MOVE_RATIO)
                return contact;

            Vector3 slideDirection = slide / slideDistance;
            Ray slideRay = new Ray(contact, slideDirection);

            return TryHitObstacle(slideRay, radius, slideDistance, stepHeight, out RaycastHit slideHit)
                ? contact + slideDirection * Mathf.Max(slideHit.distance - SWEEP_SKIN, 0f)
                : contact + slide;
        }

        private bool TryHitObstacle(Ray ray, float radius, float distance, float stepHeight, out RaycastHit hit)
        {
            if (_raycastService.TrySphereCast(ray, radius, distance, _settings.CollisionMask, out hit) == false)
                return false;

            if (hit.normal.y > MAX_OBSTACLE_UPWARD_NORMAL)
                return ray.GetPoint(distance).y < hit.point.y;

            return IsStep(hit, ray.origin.y, radius, stepHeight) == false;
        }

        private static bool IsStep(RaycastHit hit, float height, float radius, float stepHeight)
        {
            if (stepHeight <= 0f)
                return false;

            Vector3 above = new Vector3(hit.point.x, height + stepHeight, hit.point.z) - hit.normal * STEP_PROBE_INSET;
            Ray down = new Ray(above, Vector3.down);
            return hit.collider.Raycast(down, out RaycastHit top, stepHeight * STEP_PROBE_DEPTH_RATIO)
                && top.normal.y > MAX_OBSTACLE_UPWARD_NORMAL
                && top.point.y > height - radius + MIN_STEP_RISE;
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
