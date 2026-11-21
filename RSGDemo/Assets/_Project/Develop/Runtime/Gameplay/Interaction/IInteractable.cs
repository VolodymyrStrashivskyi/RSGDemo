using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        InteractionAction Action { get; }

        void Interact(Transform initiator);
    }
}
