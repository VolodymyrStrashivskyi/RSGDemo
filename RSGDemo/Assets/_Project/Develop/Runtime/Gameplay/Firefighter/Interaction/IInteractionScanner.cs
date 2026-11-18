using System;
using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Interaction
{
    public interface IInteractionScanner
    {
        event Action<IInteractable> TargetChanged;

        IInteractable Current { get; }
        bool HasTarget { get; }

        void Scan(Ray ray);
    }
}
