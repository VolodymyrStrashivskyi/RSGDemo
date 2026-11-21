using _Project.Develop.Runtime.Data.Configs;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Dispatch
{
    public class DispatchView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _alertIcon;
        [SerializeField] private TextMeshProUGUI _callLabel;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _address;
        [SerializeField] private Image _objectiveIcon;
        [SerializeField] private TextMeshProUGUI _objective;
        [SerializeField] private TextMeshProUGUI _count;
        [SerializeField] private Sprite _burningSprite;
        [SerializeField] private Sprite _completedSprite;

        private DispatchConfig _config;
        private Vector2 _shownPosition;
        private Tween _slideTween;
        private Tween _alertTween;
        private Tween _countTween;
        private Tween _objectiveTween;

        [Inject]
        public void Construct(DispatchConfig config)
        {
            _config = config;
            _shownPosition = _panel.anchoredPosition;
            _alertIcon.color = _config.AlertColor;
            _callLabel.color = _config.AlertColor;
        }

        public void Setup(string callLabel, string title, string address)
        {
            _callLabel.text = callLabel;
            _title.text = title;
            _address.text = address;
        }

        public void SlideIn()
        {
            _panel.anchoredPosition = _shownPosition + Vector2.right * _config.SlideDistance;
            _slideTween.Stop();
            _slideTween = Tween.UIAnchoredPosition(_panel, _shownPosition, _config.SlideIn);
            Alert();
        }

        public void ShowProgress(string objective, string count)
        {
            bool wasCompleted = _objectiveIcon.sprite == _completedSprite;

            _objectiveIcon.sprite = _burningSprite;
            _objectiveIcon.color = _config.AccentColor;
            _objective.text = objective;
            _objective.color = Color.white;
            _count.color = _config.AccentColor;

            if (_count.text != count && string.IsNullOrEmpty(count) == false && wasCompleted == false)
                Punch(ref _countTween, _count.transform, _config.CountPunch);

            _count.text = count;
        }

        public void ShowCompleted(string label)
        {
            _objectiveIcon.sprite = _completedSprite;
            _objectiveIcon.color = _config.CompletedColor;
            _objective.text = label;
            _objective.color = _config.CompletedColor;
            _count.text = string.Empty;
            Punch(ref _objectiveTween, _objective.transform, _config.CompletedPunch);
        }

        public void Alert()
        {
            _alertTween.Stop();
            _alertTween = Tween.Alpha(_alertIcon, new TweenSettings<float>(1f, _config.AlertDimAlpha, _config.AlertBlink));
        }

        private void OnDestroy()
        {
            _slideTween.Stop();
            _alertTween.Stop();
            _countTween.Stop();
            _objectiveTween.Stop();
        }

        private static void Punch(ref Tween tween, Transform target, ShakeSettings settings)
        {
            tween.Complete();
            tween = Tween.PunchScale(target, settings);
        }
    }
}
