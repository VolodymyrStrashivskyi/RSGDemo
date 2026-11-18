using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public interface IInteractable
    {
        bool CanInteract { get; }

        void Interact(Transform initiator);
    }
}
