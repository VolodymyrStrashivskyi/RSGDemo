using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack
{
    public class AttackHose : MonoBehaviour
    {
        private const float HALF = 0.5f;
        private const float PARKED_SAG_RATIO = 0.25f;
        private const float MAX_BUDGET_SEGMENTS = 3f;

        [SerializeField] private HoseView _view;
        [SerializeField] private HoseNozzle _nozzle;

        private AttackHoseConfig _config;
        private IRaycastService _raycastService;
        private HoseReel _reel;
        private HoseRope _rope;
        private HoseTrail _trail;
        private float _payOutBudget;

        [Inject]
        public void Construct(AttackHoseConfig config, IRaycastService raycastService)
        {
            _config = config;
            _raycastService = raycastService;
        }

        public HoseNozzle Nozzle => _nozzle;

        public float PaidOutLength => _rope != null ? _rope.Length : 0f;

        public void Bind(HoseReel reel, Transform nozzleHook)
        {
            _reel = reel;
            _nozzle.Park(nozzleHook);

            _rope = new HoseRope(_raycastService, _config.Rope, _config.ParkedLength, _config.MaxLength);
            _rope.SurfaceOffset = GetDeflatedHalfHeight();
            _rope.CollisionRadius = _config.Radius * _config.DeflatedRadiusRatio;
            _trail = new HoseTrail(_raycastService, _config.Rope);
            _reel.Initialize(_config, _rope.MaxLength - _rope.Length);
            _rope.Lay(BuildParkedPath(), false);

            _nozzle.SetLeash(new HoseLeash(_rope, _config.LeashSlack));
            _view.Initialize(CreateTube(), CreateTube(), _config.InflationWaveWidth, _config.InflationBulge);
        }

        private void LateUpdate()
        {
            if (_rope == null)
                return;

            _reel.Wind(_rope.MaxLength - _rope.Length, Time.deltaTime);
            _rope.PinStart(_reel.TakeoffPosition, _reel.TakeoffDirection);
            _rope.PinEnd(_nozzle.HoseEnd.position, _nozzle.HoseDirection);
            _trail.Follow(_rope, _nozzle.IsHeld, _nozzle.transform.position);
            _rope.Simulate(Time.deltaTime);
            PayOut();
            _view.Render(_rope.Points, GetFirstPersonHoseEnd());
        }

        private void PayOut()
        {
            if (_nozzle.IsHeld == false)
                return;

            float reach = Vector3.Distance(_reel.TakeoffPosition, _nozzle.HoseEnd.position) + _config.PayOutMargin;
            _payOutBudget = Mathf.Min(_payOutBudget + _config.MaxPayOutSpeed * Time.deltaTime, _rope.SegmentLength * MAX_BUDGET_SEGMENTS);

            while (_payOutBudget >= _rope.SegmentLength)
            {
                bool isShort = _rope.Length < reach;
                bool isPulled = _rope.StretchRatio > _config.PayOutStretchRatio;

                if ((isShort || isPulled) == false || _rope.TryPayOut() == false)
                    return;

                _payOutBudget -= _rope.SegmentLength;
            }
        }

        private List<Vector3> BuildParkedPath()
        {
            Vector3 start = _reel.TakeoffPosition;
            Vector3 end = _nozzle.HoseEnd.position;
            Vector3 middle = Vector3.Lerp(start, end, HALF) + Vector3.down * (_config.ParkedLength * PARKED_SAG_RATIO);
            return new List<Vector3> { start, middle, end };
        }

        private Transform GetFirstPersonHoseEnd() =>
            _nozzle.ViewModel.IsShown ? _nozzle.ViewModel.HoseEnd : null;

        private HoseTubeMesh CreateTube() =>
            new HoseTubeMesh(
                _config.Radius,
                _config.Radius * _config.DeflatedRadiusRatio,
                _config.DeflatedSquash,
                _config.Sides,
                _config.Subdivisions,
                _config.PaletteUv,
                _config.EndTaperLength);

        private float GetDeflatedHalfHeight() =>
            _config.Radius * _config.DeflatedRadiusRatio * _config.DeflatedSquash;
    }
}
