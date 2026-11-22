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

        private readonly List<Vector3> _shownPoints = new List<Vector3>();

        private AttackHoseConfig _config;
        private IRaycastService _raycastService;
        private HoseReel _reel;
        private HoseRope _rope;
        private HoseTrail _trail;
        private HoseTension _tension;
        private HoseTubeMesh _tube;
        private HoseTubeMesh _firstPersonTube;
        private float _payOutBudget;
        private bool _isWaterIncoming;

        [Inject]
        public void Construct(AttackHoseConfig config, IRaycastService raycastService)
        {
            _config = config;
            _raycastService = raycastService;
        }

        public HoseNozzle Nozzle => _nozzle;

        public float PaidOutLength => _rope != null ? _rope.Length : 0f;

        public bool IsCharged => _rope != null && _view.IsFilledToFarEnd;

        public void SetWaterIncoming(bool isIncoming)
        {
            if (_rope == null || isIncoming == _isWaterIncoming)
                return;

            _isWaterIncoming = isIncoming;

            if (isIncoming)
                _view.ContinueInflating(GetChargeDuration());
            else
                _view.ContinueDeflating(_config.DischargeDuration);
        }

        public void Bind(HoseReel reel, Transform nozzleHook)
        {
            _reel = reel;
            _nozzle.Park(nozzleHook);

            _rope = new HoseRope(_raycastService, _config.Rope, _config.ParkedLength, _config.MaxLength);
            _rope.SurfaceOffset = GetDeflatedHalfHeight();
            _rope.CollisionRadius = _config.Radius * _config.DeflatedRadiusRatio;
            _trail = new HoseTrail(_raycastService, _config.Rope);
            _tube = CreateTube();
            _firstPersonTube = CreateTube();
            _view.Initialize(_tube, _firstPersonTube, _config.InflationWaveWidth, _config.InflationBulge, HoseFillOrigin.Start);
            _reel.Initialize(_config, _rope.MaxLength - _rope.Length, _view);
            _rope.Lay(BuildParkedPath(), false);

            _tension = new HoseTension(_rope, _config.Tension, _config.Rope, _raycastService);
            _tension.ReachedFullLength += OnReachedFullLength;
            _nozzle.SetLeash(new HoseLeash(_rope, _tension, _config.LeashSlack));
        }

        private void LateUpdate()
        {
            if (_rope == null)
                return;

            _view.SetFillSpan(_reel.CoiledLength, _rope.Length);
            _reel.Wind(_rope.MaxLength - _rope.Length, _rope.Length, Time.deltaTime);
            _rope.PinStart(_reel.TakeoffPosition, _reel.TakeoffDirection);
            _rope.PinEnd(_nozzle.HoseEnd.position, _nozzle.HoseDirection);
            _trail.Follow(_rope, _nozzle.IsHeld, _nozzle.transform.position);
            _rope.Simulate(Time.deltaTime);
            PayOut();
            UpdateTension();
            JoinToReel(_tube);
            JoinToReel(_firstPersonTube);
            _view.Render(_shownPoints, GetFirstPersonHoseEnd());
        }

        private void OnDestroy()
        {
            if (_tension != null)
                _tension.ReachedFullLength -= OnReachedFullLength;
        }

        private void UpdateTension()
        {
            _tension.Update(_nozzle.IsHeld, _nozzle.transform.position, Time.deltaTime);
            float inflation = _view.InflationFront;
            _rope.SurfaceOffset = Mathf.Lerp(GetDeflatedHalfHeight(), _config.Radius, inflation);
            float slackStiffness = Mathf.Lerp(_config.Rope.BendStiffness, _config.Rope.InflatedBendStiffness, inflation);
            _rope.BendStiffness = _tension.GetBendStiffness(slackStiffness);
            _tension.PushProps(Time.deltaTime);
            _tension.Tremble(_rope.Points, _shownPoints);
        }

        private void JoinToReel(HoseTubeMesh tube)
        {
            tube.SetStartTangent(_reel.TakeoffDirection);
            tube.SetStartNormal(_reel.TakeoffNormal);
            tube.SetRibPhase(_rope.Length, 0f);
        }

        private void OnReachedFullLength() =>
            _reel.Brake();

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

        private HoseTubeMesh CreateTube()
        {
            HoseTubeMesh tube = new HoseTubeMesh(
                _config.Radius,
                _config.Radius * _config.DeflatedRadiusRatio,
                _config.DeflatedSquash,
                _config.Sides,
                _config.Subdivisions,
                _config.PaletteUv,
                _config.EndTaperLength);

            tube.SetRibs(_config.RibSpacing, _config.RibDepth);
            tube.SetFittedEnds(false, true);
            return tube;
        }

        private float GetChargeDuration() =>
            Mathf.Max((_reel.CoiledLength + _rope.Length) / _config.ChargeSpeed, _config.MinChargeDuration);

        private float GetDeflatedHalfHeight() =>
            _config.Radius * _config.DeflatedRadiusRatio * _config.DeflatedSquash;
    }
}
