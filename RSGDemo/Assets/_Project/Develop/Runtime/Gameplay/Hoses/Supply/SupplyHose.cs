using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Supply
{
    public class SupplyHose : MonoBehaviour
    {
        private const float INTAKE_BEND_OFFSET = 0.25f;

        [SerializeField] private HoseView _view;
        [SerializeField] private HoseCoupling _coupling;

        private readonly List<Vector3> _shownPoints = new List<Vector3>();

        private SupplyHoseConfig _config;
        private SupplyLine _supplyLine;
        private IRaycastService _raycastService;
        private Transform _intakePoint;
        private HoseRope _rope;
        private HoseTrail _trail;
        private HoseTension _tension;

        [Inject]
        public void Construct(SupplyHoseConfig config, SupplyLine supplyLine, IRaycastService raycastService)
        {
            _config = config;
            _supplyLine = supplyLine;
            _raycastService = raycastService;
        }

        public void Bind(Transform intakePoint, Transform layPoint)
        {
            _intakePoint = intakePoint;
            _rope = new HoseRope(_raycastService, _config.Rope, _config.Length);
            _rope.SurfaceOffset = GetDeflatedHalfHeight();
            _rope.CollisionRadius = _config.Radius * _config.DeflatedRadiusRatio;
            _trail = new HoseTrail(_raycastService, _config.Rope);
            LayOut(layPoint);

            _tension = new HoseTension(_rope, _config.Tension, _config.Rope, _raycastService);
            _coupling.SetLeash(new HoseLeash(_rope, _tension, _config.LeashSlack));
            _view.Initialize(CreateTube(), CreateTube(), _config.InflationWaveWidth, _config.InflationBulge, HoseFillOrigin.End);

            _coupling.Attached += OnAttached;
            _coupling.Detached += OnDetached;
            _supplyLine.FlowChanged += OnFlowChanged;
        }

        private void LateUpdate()
        {
            if (_rope == null)
                return;

            Transform hoseEnd = _coupling.HoseEnd;
            _rope.PinStart(_intakePoint.position, _intakePoint.forward);
            _rope.PinEnd(hoseEnd.position, hoseEnd.forward);
            ApplyInflationToRope();
            _trail.Follow(_rope, _coupling.IsHeld, _coupling.transform.position);
            _rope.Simulate(Time.deltaTime);
            UpdateTension();
            _view.Render(_shownPoints, GetFirstPersonHoseEnd());
        }

        private void OnDestroy()
        {
            if (_rope == null)
                return;

            _coupling.Attached -= OnAttached;
            _coupling.Detached -= OnDetached;
            _supplyLine.FlowChanged -= OnFlowChanged;
        }

        private void LayOut(Transform layPoint)
        {
            Vector3 intakeBend = _intakePoint.position + _intakePoint.forward * INTAKE_BEND_OFFSET;
            Vector3 groundStart = _rope.ProjectToGround(layPoint.position);
            List<Vector3> polyline = new List<Vector3> { _intakePoint.position, intakeBend, groundStart };
            float laidLength = Vector3.Distance(polyline[0], polyline[1]) + Vector3.Distance(polyline[1], polyline[2]);
            Vector3 groundEnd = _rope.ProjectToGround(groundStart + layPoint.forward * (_config.Length - laidLength));
            polyline.Add(groundEnd);
            _rope.Lay(polyline);

            Vector3 approach = _rope.Points[_rope.Points.Count - 2];
            _coupling.PlaceAt(groundEnd, Quaternion.LookRotation(approach - groundEnd));
        }

        private Transform GetFirstPersonHoseEnd() =>
            _coupling.ViewModel.IsShown ? _coupling.ViewModel.HoseEnd : null;

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
            return tube;
        }

        private float GetDeflatedHalfHeight() =>
            _config.Radius * _config.DeflatedRadiusRatio * _config.DeflatedSquash;

        private void ApplyInflationToRope()
        {
            float inflation = _view.InflationFront;
            _rope.SurfaceOffset = Mathf.Lerp(GetDeflatedHalfHeight(), _config.Radius, inflation);
            float slackStiffness = Mathf.Lerp(_config.Rope.BendStiffness, _config.Rope.InflatedBendStiffness, inflation);
            _rope.BendStiffness = _tension.GetBendStiffness(slackStiffness);
        }

        private void UpdateTension()
        {
            _tension.Update(_coupling.IsHeld, _coupling.transform.position, Time.deltaTime);
            _tension.PushProps(Time.deltaTime);
            _tension.Tremble(_rope.Points, _shownPoints);
        }

        private void OnAttached(HoseSocket socket, string initiator) =>
            _supplyLine.Connect(initiator);

        private void OnDetached(HoseSocket socket, string initiator) =>
            _supplyLine.Disconnect(initiator);

        private void OnFlowChanged()
        {
            if (_supplyLine.IsAvailable)
                _view.Inflate(_config.InflateDuration);
            else
                _view.Deflate(_config.DeflateDuration);
        }
    }
}
