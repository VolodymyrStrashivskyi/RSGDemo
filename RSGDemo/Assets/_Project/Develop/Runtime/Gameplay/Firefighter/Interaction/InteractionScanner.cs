using System;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Interaction
{
    public class InteractionScanner : IInteractionScanner
    {
        public event Action<IInteractable> TargetChanged;

        private readonly IRaycastService _raycastService;
        private readonly InteractionConfig _config;

        public InteractionScanner(IRaycastService raycastService, InteractionConfig config)
        {
            _raycastService = raycastService;
            _config = config;
        }

        public IInteractable Current { get; private set; }

        public bool HasTarget => Current != null;

        public void Scan(Ray ray)
        {
            IInteractable target = FindTarget(ray);

            if (ReferenceEquals(target, Current))
                return;

            Current = target;
            TargetChanged?.Invoke(Current);
        }

        private IInteractable FindTarget(Ray ray)
        {
            bool isFound = _raycastService.TryFind(ray, _config.Distance, _config.Mask, out IInteractable target, out RaycastHit _);
            return isFound && target.CanInteract ? target : null;
        }
    }
}
