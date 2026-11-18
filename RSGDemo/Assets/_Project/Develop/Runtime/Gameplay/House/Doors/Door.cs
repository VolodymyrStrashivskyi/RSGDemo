using _Project.Develop.Runtime.Gameplay.Interaction;
using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.House.Doors
{
    [RequireComponent(typeof(Rigidbody))]
    public class Door : MonoBehaviour, IInteractable
    {
        private const float CLOSED_THRESHOLD = 0.01f;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField, Range(0f, 180f)] private float _openAngle = 100f;
        [SerializeField] private TweenSettings _openTween = new TweenSettings(0.7f, Ease.OutBack, updateType: UpdateType.FixedUpdate);
        [SerializeField] private TweenSettings _closeTween = new TweenSettings(0.5f, Ease.InOutSine, updateType: UpdateType.FixedUpdate);

        private Quaternion _closedLocalRotation;
        private float _openness;
        private float _swingDirection = 1f;
        private bool _isOpen;
        private Tween _tween;

        public bool CanInteract => true;

        public bool IsOpen => _isOpen;

        public void Interact(Transform initiator)
        {
            if (_isOpen)
                Close();
            else
                OpenAwayFrom(initiator.position);
        }

        public void OpenAwayFrom(Vector3 position)
        {
            if (_openness < CLOSED_THRESHOLD)
                _swingDirection = GetSwingDirectionAwayFrom(position);

            _isOpen = true;
            AnimateTo(1f, _openTween);
        }

        public void Close()
        {
            _isOpen = false;
            AnimateTo(0f, _closeTween);
        }

        private void Awake() =>
            _closedLocalRotation = transform.localRotation;

        private void OnDestroy() =>
            _tween.Stop();

        private float GetSwingDirectionAwayFrom(Vector3 position)
        {
            Vector3 closedForward = GetParentRotation() * _closedLocalRotation * Vector3.forward;
            float side = Vector3.Dot(position - transform.position, closedForward);
            return side > 0f ? -1f : 1f;
        }

        private void AnimateTo(float openness, TweenSettings settings)
        {
            _tween.Stop();
            _tween = Tween.Custom(this, new TweenSettings<float>(_openness, openness, settings), (door, value) => door.ApplyOpenness(value));
        }

        private void ApplyOpenness(float openness)
        {
            _openness = openness;
            Quaternion localRotation = _closedLocalRotation * Quaternion.Euler(0f, _openAngle * openness * _swingDirection, 0f);
            _rigidbody.MoveRotation(GetParentRotation() * localRotation);
        }

        private Quaternion GetParentRotation() =>
            transform.parent != null ? transform.parent.rotation : Quaternion.identity;
    }
}
