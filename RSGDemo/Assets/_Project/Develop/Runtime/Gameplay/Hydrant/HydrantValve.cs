using _Project.Develop.Runtime.Gameplay.Interaction;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hydrant
{
    public class HydrantValve : MonoBehaviour, IInteractable
    {
        private const float FULL_TURN_DEGREES = 360f;

        [SerializeField] private Vector3 _localAxis = Vector3.right;
        [SerializeField, Min(0f)] private float _turns = 2f;
        [SerializeField] private TweenSettings _turnTween = new TweenSettings(0.8f, Ease.InOutSine);

        private HydrantSource _hydrant;
        private Tween _tween;

        [Inject]
        public void Construct(HydrantSource hydrant) =>
            _hydrant = hydrant;

        public bool CanInteract => _tween.isAlive == false;

        public InteractionAction Action => _hydrant.IsOpen ? InteractionAction.CloseValve : InteractionAction.OpenValve;

        public void Interact(Transform initiator)
        {
            float direction = _hydrant.IsOpen ? -1f : 1f;

            if (_hydrant.IsOpen)
                _hydrant.Close(initiator.name);
            else
                _hydrant.Open(initiator.name);

            Turn(direction);
        }

        private void OnDestroy() =>
            _tween.Stop();

        private void Turn(float direction)
        {
            Vector3 start = transform.localEulerAngles;
            Vector3 end = start + _localAxis * (FULL_TURN_DEGREES * _turns * direction);
            _tween = Tween.LocalEulerAngles(transform, new TweenSettings<Vector3>(start, end, _turnTween));
        }
    }
}
