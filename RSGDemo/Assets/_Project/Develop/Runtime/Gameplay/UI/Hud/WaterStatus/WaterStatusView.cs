using _Project.Develop.Runtime.Data.Configs;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus
{
    public class WaterStatusView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TextMeshProUGUI _sourceLabel;
        [SerializeField] private Image _tankIcon;
        [SerializeField] private SegmentBarView _tankBar;
        [SerializeField] private TextMeshProUGUI _tankValue;
        [SerializeField] private Image _pressureIcon;
        [SerializeField] private SegmentBarView _pressureBar;
        [SerializeField] private TextMeshProUGUI _pressureValue;

        private WaterStatusConfig _config;
        private Color _sourceColor;
        private float _warning;
        private bool _isTankLow;
        private Tween _warningTween;
        private Tween _sourcePunchTween;
        private Tween _sourceFlashTween;
        private Tween _shakeTween;

        [Inject]
        public void Construct(WaterStatusConfig config)
        {
            _config = config;
            _sourceColor = _sourceLabel.color;
            _pressureIcon.color = _config.PressureColor;
        }

        public void SetSource(string label, bool isAnimated)
        {
            _sourceLabel.text = label;

            if (isAnimated == false)
                return;

            _sourcePunchTween.Complete();
            _sourcePunchTween = Tween.PunchScale(_sourceLabel.transform, _config.SourcePunch);
            _sourceFlashTween.Stop();
            _sourceFlashTween = Tween.Color(_sourceLabel, new TweenSettings<Color>(_config.PressureColor, _sourceColor, _config.SourceFlash));
        }

        public void SetTankLevel(float ratio, string valueText)
        {
            Color color = Color.Lerp(_config.TankColor, _config.WarningColor, _warning);
            _tankBar.SetFill(ratio, color, _config.EmptySegmentColor);
            _tankIcon.color = color;
            _tankValue.text = valueText;
        }

        public void SetPressure(float ratio, string valueText)
        {
            _pressureBar.SetFill(ratio, _config.PressureColor, _config.EmptySegmentColor);
            _pressureValue.text = valueText;
        }

        public void SetTankLow(bool isLow)
        {
            if (_isTankLow == isLow)
                return;

            _isTankLow = isLow;
            _warningTween.Stop();
            _warning = 0f;

            if (isLow)
                _warningTween = Tween.Custom(this, 0f, 1f, _config.LowTankBlink, (view, value) => view._warning = value);
        }

        public void Shake()
        {
            if (_shakeTween.isAlive)
                return;

            _shakeTween = Tween.ShakeLocalPosition(_panel, _config.PressureDropShake);
        }

        private void OnDestroy()
        {
            _warningTween.Stop();
            _sourcePunchTween.Stop();
            _sourceFlashTween.Stop();
            _shakeTween.Stop();
        }
    }
}
