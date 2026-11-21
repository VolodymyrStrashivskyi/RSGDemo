using _Project.Develop.Runtime.Data.Configs;
using PrimeTween;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.InteractionPrompt
{
    public class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _keyLabel;
        [SerializeField] private TextMeshProUGUI _actionLabel;

        private CrosshairConfig _config;
        private bool _isShown;
        private Tween _tween;

        [Inject]
        public void Construct(CrosshairConfig config)
        {
            _config = config;
            _canvasGroup.alpha = 0f;
        }

        public void Show(string keyName, string actionName)
        {
            if (_keyLabel.text != keyName)
                _keyLabel.text = keyName;

            if (_actionLabel.text != actionName)
                _actionLabel.text = actionName;

            SetShown(true);
        }

        public void Hide() =>
            SetShown(false);

        private void OnDestroy() =>
            _tween.Stop();

        private void SetShown(bool isShown)
        {
            if (_isShown == isShown)
                return;

            _isShown = isShown;
            TweenSettings settings = isShown ? _config.HighlightTween : _config.UnhighlightTween;

            _tween.Stop();
            _tween = Tween.Alpha(_canvasGroup, isShown ? 1f : 0f, settings);
        }
    }
}
