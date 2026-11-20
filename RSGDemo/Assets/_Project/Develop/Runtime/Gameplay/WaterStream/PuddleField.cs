using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class PuddleField : MonoBehaviour
    {
        private const float FLOOR_PROBE_LIFT = 0.05f;
        private const float FLOOR_PROBE_DISTANCE = 10f;

        [SerializeField, Tooltip("Inactive puddle copied to every spot the water lands on")]
        private PuddleView _template;

        private readonly List<PuddleView> _puddles = new List<PuddleView>();

        private WaterStreamConfig _config;
        private IRaycastService _raycastService;
        private IObjectResolver _resolver;
        private PuddleView _active;
        private float _nextPlaceTime;

        [Inject]
        public void Construct(WaterStreamConfig config, IRaycastService raycastService, IObjectResolver resolver)
        {
            _config = config;
            _raycastService = raycastService;
            _resolver = resolver;
        }

        public void Pour(RaycastHit impact, float volume)
        {
            if (TryFindFloor(impact, out Vector3 floorPoint) == false)
            {
                Stop();
                return;
            }

            if (TryGetPuddleAt(floorPoint, out PuddleView puddle) == false)
            {
                Stop();
                return;
            }

            if (puddle != _active)
                Stop();

            _active = puddle;
            _active.SetImpact(impact.point, true);
            _active.Pour(floorPoint, volume);
        }

        public void Stop()
        {
            if (_active != null)
                _active.SetImpact(Vector3.zero, false);

            _active = null;
        }

        private void OnDestroy()
        {
            foreach (PuddleView puddle in _puddles)
            {
                if (puddle != null)
                    Destroy(puddle.gameObject);
            }
        }

        private bool TryFindFloor(RaycastHit impact, out Vector3 floorPoint)
        {
            Ray ray = new Ray(impact.point + impact.normal * FLOOR_PROBE_LIFT, Vector3.down);
            bool isFound = _raycastService.TryRaycast(ray, FLOOR_PROBE_DISTANCE, _config.PuddleGroundMask, out RaycastHit floor);
            floorPoint = floor.point;
            return isFound;
        }

        private bool TryGetPuddleAt(Vector3 floorPoint, out PuddleView puddle)
        {
            foreach (PuddleView existing in _puddles)
            {
                if (existing.Contains(floorPoint, _config.PuddlePourRadius))
                {
                    puddle = existing;
                    return true;
                }
            }

            puddle = null;

            if (Time.time < _nextPlaceTime)
                return false;

            _nextPlaceTime = Time.time + _config.PuddlePlaceInterval;
            puddle = _puddles.Count < _config.MaxPuddles ? CreatePuddle() : FindDriest();
            puddle.Place(floorPoint);
            return true;
        }

        private PuddleView CreatePuddle()
        {
            PuddleView puddle = _resolver.Instantiate(_template);
            puddle.gameObject.SetActive(true);
            _puddles.Add(puddle);
            return puddle;
        }

        private PuddleView FindDriest()
        {
            PuddleView driest = _puddles[0];

            foreach (PuddleView puddle in _puddles)
            {
                if (puddle.TotalDepth < driest.TotalDepth)
                    driest = puddle;
            }

            return driest;
        }
    }
}
