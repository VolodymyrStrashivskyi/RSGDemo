using System;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Interaction;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack
{
    public class HoseNozzle : MonoBehaviour, IInteractable, IGrabbable, IHandGrips, IMovementConstraint, IAimable, IFirstPersonHeld
    {
        private const float GROUND_PROBE_HEIGHT = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 5f;

        public event Action<string> Grabbed;
        public event Action Released;

        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _hoseEnd;
        [SerializeField] private Transform _tip;
        [SerializeField] private Transform _leftHandGrip;
        [SerializeField] private Transform _rightHandGrip;
        [SerializeField] private LayerMask _groundMask = 1;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private FirstPersonViewModel _viewModel;
        [SerializeField, Range(0, 31), Tooltip("Layer the first-person camera does not render, used while held so only the view model shows")]
        private int _firstPersonHiddenLayer;

        private AttackHoseConfig _config;
        private Grabber _grabber;
        private IRaycastService _raycastService;
        private Transform _restParent;
        private GrabAnchor _anchor;
        private IMovementConstraint _leash;
        private Quaternion _aimRotation = Quaternion.identity;
        private Vector3 _aimDirection;
        private int _visibleLayer;
        private string _holder;
        private bool _isAiming;
        private Sequence _sequence;

        [Inject]
        public void Construct(AttackHoseConfig config, Grabber grabber, IRaycastService raycastService)
        {
            _config = config;
            _grabber = grabber;
            _raycastService = raycastService;
        }

        public Transform HoseEnd => _hoseEnd;

        public Vector3 HoseDirection => IsHeld ? Vector3.Slerp(_hoseEnd.forward, Vector3.down, _config.HeldHoseDroop) : _hoseEnd.forward;

        public Transform Tip => _tip;

        public Transform LeftHandGrip => _leftHandGrip;

        public Transform RightHandGrip => _rightHandGrip;

        public FirstPersonViewModel ViewModel => _viewModel;

        public bool IsHeld => _anchor != null;

        public bool CanInteract => _grabber.IsHolding() == false;

        public void Interact(Transform initiator)
        {
            _holder = initiator.name;
            _grabber.TryGrab(this);
        }

        public void OnGrabbed(GrabAnchor anchor)
        {
            _anchor = anchor;
            _collider.enabled = false;
            _renderer.gameObject.layer = _firstPersonHiddenLayer;
            _isAiming = false;
            anchor.Attach(transform);
            _aimDirection = anchor.transform.forward;
            _aimRotation = GetAimRotation(_aimDirection);

            Animate(Sequence.Create()
                .Group(Tween.LocalPosition(transform, new TweenSettings<Vector3>(GetHoldPosition(_aimRotation), _config.GrabTween)))
                .Group(Tween.LocalRotation(transform, new TweenSettings<Quaternion>(_aimRotation, _config.GrabTween)))
                .ChainCallback(this, nozzle => nozzle._isAiming = true));

            Grabbed?.Invoke(_holder);
        }

        public void OnReleased()
        {
            _anchor = null;
            _isAiming = false;
            transform.SetParent(_restParent, true);

            Vector3 restPosition = FindGround(transform.position) + Vector3.up * _config.RestHeight;
            _collider.enabled = true;
            _renderer.gameObject.layer = _visibleLayer;
            Quaternion lyingRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            Animate(Sequence.Create()
                .Group(Tween.Position(transform, new TweenSettings<Vector3>(restPosition, _config.DropTween)))
                .Group(Tween.Rotation(transform, new TweenSettings<Quaternion>(lyingRotation, _config.DropTween))));

            Released?.Invoke();
        }

        public void Aim(Vector3 direction) =>
            _aimDirection = direction;

        public void Park(Transform hook)
        {
            transform.SetParent(hook, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public void SetLeash(IMovementConstraint leash) =>
            _leash = leash;

        public Vector3 Constrain(Vector3 currentPosition, Vector3 desiredPosition) =>
            _leash != null ? _leash.Constrain(currentPosition, desiredPosition) : desiredPosition;

        private void Awake()
        {
            _restParent = transform.parent;
            _visibleLayer = _renderer.gameObject.layer;
        }

        private void Update()
        {
            if (_isAiming == false || _anchor == null)
                return;

            _aimRotation = GetAimRotation(_aimDirection);
            float blend = 1f - Mathf.Exp(-_config.AimSharpness * Time.deltaTime);
            Quaternion rotation = Quaternion.Slerp(transform.localRotation, _aimRotation, blend);
            transform.SetLocalPositionAndRotation(GetHoldPosition(rotation), rotation);
        }

        private Vector3 GetHoldPosition(Quaternion aim) =>
            _config.AimPivot + aim * (_config.HoldOffset - _config.AimPivot);

        private void OnDestroy() =>
            _sequence.Stop();

        private Quaternion GetAimRotation(Vector3 direction)
        {
            Vector3 local = _anchor.transform.InverseTransformDirection(direction);
            float pitch = Mathf.Atan2(local.y, Mathf.Max(local.z, Mathf.Epsilon)) * Mathf.Rad2Deg + _config.AimTilt;
            return Quaternion.Euler(-Mathf.Clamp(pitch, _config.MinAimPitch, _config.MaxAimPitch), 0f, 0f);
        }

        private Vector3 FindGround(Vector3 position)
        {
            Ray ray = new Ray(position + Vector3.up * GROUND_PROBE_HEIGHT, Vector3.down);
            return _raycastService.TryRaycast(ray, GROUND_PROBE_DISTANCE, _groundMask, out RaycastHit hit) ? hit.point : position;
        }

        private void Animate(Sequence sequence)
        {
            _sequence.Stop();
            _sequence = sequence;
        }
    }
}
