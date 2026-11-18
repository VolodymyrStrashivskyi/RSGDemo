using System;
using _Project.Develop.Runtime.Gameplay.Firefighter.Interaction;
using _Project.Develop.Runtime.Gameplay.Interaction;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair
{
    public class CrosshairPresenter : IDisposable
    {
        private readonly IInteractionScanner _interactionScanner;

        private CrosshairView _view;

        public CrosshairPresenter(IInteractionScanner interactionScanner) =>
            _interactionScanner = interactionScanner;

        public void Bind(CrosshairView view)
        {
            _view = view;
            _view.SetHighlighted(_interactionScanner.HasTarget);
            _interactionScanner.TargetChanged += OnTargetChanged;
        }

        public void Dispose() =>
            _interactionScanner.TargetChanged -= OnTargetChanged;

        private void OnTargetChanged(IInteractable target) =>
            _view.SetHighlighted(target != null);
    }
}
