using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Input;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Firefighter.Interaction;
using _Project.Develop.Runtime.Gameplay.Interaction;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.InteractionPrompt
{
    public class InteractionPromptPresenter : ITickable
    {
        private readonly IInteractionScanner _interactionScanner;
        private readonly IInputService _inputService;
        private readonly Dictionary<InteractionAction, string> _labels = new Dictionary<InteractionAction, string>();

        private InteractionPromptView _view;

        public InteractionPromptPresenter(IInteractionScanner interactionScanner, IInputService inputService, InteractionPromptConfig config)
        {
            _interactionScanner = interactionScanner;
            _inputService = inputService;

            foreach (InteractionActionLabel label in config.Labels)
                _labels[label.Action] = label.Label;
        }

        public void Bind(InteractionPromptView view) =>
            _view = view;

        public void Tick()
        {
            if (_view == null)
                return;

            IInteractable target = _interactionScanner.Current;

            if (target == null)
            {
                _view.Hide();
                return;
            }

            _view.Show(_inputService.InteractKeyName, GetLabel(target.Action));
        }

        private string GetLabel(InteractionAction action) =>
            _labels.TryGetValue(action, out string label) ? label : action.ToString();
    }
}
