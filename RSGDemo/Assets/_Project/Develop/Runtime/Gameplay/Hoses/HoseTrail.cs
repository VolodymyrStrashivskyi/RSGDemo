using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseTrail
    {
        private const int LOOP_SKIP_POINTS = 3;
        private const int LOOP_SEARCH_POINTS = 200;
        private const float LOOP_CUT_RATIO = 1.5f;
        private const float LINE_OF_SIGHT_HEIGHT = 0.3f;

        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly IRaycastService _raycastService;
        private readonly HoseRopeSettings _settings;

        public HoseTrail(IRaycastService raycastService, HoseRopeSettings settings)
        {
            _raycastService = raycastService;
            _settings = settings;
        }

        public void Follow(HoseRope rope, bool isCarried, Vector3 carrierPosition)
        {
            if (isCarried == false)
            {
                _points.Clear();
                rope.Guide(null);
                return;
            }

            if (_points.Count == 0)
                _points.AddRange(rope.Points);

            Record(rope.ProjectToGround(carrierPosition));
            rope.Guide(_points);
        }

        private void Record(Vector3 groundPosition)
        {
            if (_points.Count > 0 && Vector3.Distance(_points[_points.Count - 1], groundPosition) < _settings.TrailSpacing)
                return;

            CutLoopTo(groundPosition);
            _points.Add(groundPosition);
        }

        private void CutLoopTo(Vector3 position)
        {
            int first = Mathf.Max(0, _points.Count - LOOP_SEARCH_POINTS);
            float cutDistance = _settings.TrailSpacing * LOOP_CUT_RATIO;

            for (int i = _points.Count - 1 - LOOP_SKIP_POINTS; i >= first; i--)
            {
                if (Vector3.Distance(_points[i], position) > cutDistance || IsInSight(_points[i], position) == false)
                    continue;

                _points.RemoveRange(i + 1, _points.Count - i - 1);
                return;
            }
        }

        private bool IsInSight(Vector3 from, Vector3 to)
        {
            Vector3 lift = Vector3.up * LINE_OF_SIGHT_HEIGHT;
            Vector3 delta = to - from;
            Ray ray = new Ray(from + lift, delta.normalized);
            return _raycastService.TryRaycast(ray, delta.magnitude, _settings.CollisionMask, out RaycastHit _) == false;
        }
    }
}
