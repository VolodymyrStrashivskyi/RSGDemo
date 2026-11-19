using System;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Interaction;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseCoupling : MonoBehaviour, IInteractable, IGrabbable, IHandGrips, IMovementConstraint
    {
        private const float GROUND_PROBE_HEIGHT = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 5f;
        private const float FULL_TURN_DEGREES = 360f;

        public event Action<string> Grabbed;
        public event Action Released;
        public event Action<HoseSocket, string> Attached;
        public event Action<HoseSocket, string> Detached;

        [SerializeField] private HoseKind _kind;
        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _hoseEnd;
        [SerializeField] private Transform _leftHandGrip;
        [SerializeField] private Transform _rightHandGrip;
        [SerializeField] private LayerMask _groundMask = 1;
        [SerializeField, Min(0f)] private float _restHeight = 0.05f;
        [SerializeField] private Vector3 _holdEulerAngles = new Vector3(37f, 180f, 0f);
        [SerializeField] private TweenSettings _grabTween = new TweenSettings(0.25f, Ease.OutCubic);
        [SerializeField] private TweenSettings _dropTween = new TweenSettings(0.35f, Ease.OutBounce);
        [SerializeField] private TweenSettings _approachTween = new TweenSettings(0.25f, Ease.OutCubic);
        [SerializeField] private TweenSettings _screwTween = new TweenSettings(0.6f, Ease.InOutSine);
        [SerializeField, Min(0f)] private float _screwTurns = 2f;
        [SerializeField, Min(0f)] private float _screwApproachDistance = 0.06f;

        private Grabber _grabber;
        private IRaycastService _raycastService;
        private Transform _restParent;
        private GrabAnchor _anchor;
        private IMovementConstraint _leash;
        private HoseSocket _socket;
        private string _holder;
        private Sequence _sequence;

        [Inject]
        public void Construct(Grabber grabber, IRaycastService raycastService)
        {
            _grabber = grabber;
            _raycastService = raycastService;
        }

        public HoseKind Kind => _kind;

        public Transform HoseEnd => _hoseEnd;

        public Transform LeftHandGrip => _leftHandGrip;

        public Transform RightHandGrip => _rightHandGrip;

        public bool IsHeld => _anchor != null;

        public Vector3 CarrierPosition => IsHeld ? _anchor.CarrierPosition : transform.position;

        public bool IsAttached => _socket != null;

        public bool CanInteract => _grabber.IsHolding() == false;

        public void Interact(Transform initiator)
        {
            _holder = initiator.name;

            if (IsAttached)
                DetachFromSocket();

            _grabber.TryGrab(this);
        }

        public void OnGrabbed(GrabAnchor anchor)
        {
            _anchor = anchor;
            _collider.enabled = false;
            anchor.Attach(transform);

            Animate(Sequence.Create()
                .Group(Tween.LocalPosition(transform, new TweenSettings<Vector3>(Vector3.zero, _grabTween)))
                .Group(Tween.LocalRotation(transform, new TweenSettings<Quaternion>(Quaternion.Euler(_holdEulerAngles), _grabTween))));

            Grabbed?.Invoke(_holder);
        }

        public void OnReleased()
        {
            _anchor = null;
            transform.SetParent(_restParent, true);

            Vector3 restPosition = FindGround(transform.position) + Vector3.up * _restHeight;
            _collider.enabled = true;
            Quaternion lyingRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            Animate(Sequence.Create()
                .Group(Tween.Position(transform, new TweenSettings<Vector3>(restPosition, _dropTween)))
                .Group(Tween.Rotation(transform, new TweenSettings<Quaternion>(lyingRotation, _dropTween))));

            Released?.Invoke();
        }

        public void AttachTo(HoseSocket socket, string initiator)
        {
            _socket = socket;
            transform.SetParent(socket.AttachPoint, true);

            Vector3 approachPosition = Vector3.forward * _screwApproachDistance;
            Vector3 screwedEulerAngles = Vector3.forward * (FULL_TURN_DEGREES * _screwTurns);

            Animate(Sequence.Create()
                .Group(Tween.LocalPosition(transform, new TweenSettings<Vector3>(approachPosition, _approachTween)))
                .Group(Tween.LocalRotation(transform, new TweenSettings<Quaternion>(Quaternion.identity, _approachTween)))
                .Chain(Tween.LocalPosition(transform, new TweenSettings<Vector3>(Vector3.zero, _screwTween)))
                .Group(Tween.LocalEulerAngles(transform, new TweenSettings<Vector3>(Vector3.zero, screwedEulerAngles, _screwTween))));

            Attached?.Invoke(socket, initiator);
        }

        public void PlaceAt(Vector3 groundPosition, Quaternion rotation) =>
            transform.SetPositionAndRotation(groundPosition + Vector3.up * _restHeight, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));

        public void SetLeash(IMovementConstraint leash) =>
            _leash = leash;

        public Vector3 Constrain(Vector3 currentPosition, Vector3 desiredPosition) =>
            _leash != null ? _leash.Constrain(currentPosition, desiredPosition) : desiredPosition;

        private void Awake() =>
            _restParent = transform.parent;

        private void OnDestroy() =>
            _sequence.Stop();

        private void DetachFromSocket()
        {
            HoseSocket socket = _socket;
            _socket = null;
            socket.Release();
            Detached?.Invoke(socket, _holder);
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
