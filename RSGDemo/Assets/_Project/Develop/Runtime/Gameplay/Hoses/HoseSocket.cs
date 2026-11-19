using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseSocket : MonoBehaviour, IInteractable
    {
        [SerializeField] private HoseKind _kind;
        [SerializeField] private Transform _attachPoint;

        private Grabber _grabber;

        [Inject]
        public void Construct(Grabber grabber) =>
            _grabber = grabber;

        public Transform AttachPoint => _attachPoint;

        public bool IsOccupied { get; private set; }

        public bool CanInteract => IsOccupied == false && TryGetFittingCoupling(out HoseCoupling _);

        public void Interact(Transform initiator)
        {
            if (IsOccupied || TryGetFittingCoupling(out HoseCoupling coupling) == false)
                return;

            IsOccupied = true;
            _grabber.Release();
            coupling.AttachTo(this, initiator.name);
        }

        public void Release() =>
            IsOccupied = false;

        private bool TryGetFittingCoupling(out HoseCoupling coupling) =>
            _grabber.TryGetHeld(out coupling) && coupling.Kind == _kind;
    }
}
