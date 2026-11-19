using TMPro;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus
{
    public class WaterStatusView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sourceLabel;
        [SerializeField] private RectTransform _tankFill;
        [SerializeField] private TextMeshProUGUI _tankValue;
        [SerializeField] private RectTransform _pressureFill;
        [SerializeField] private TextMeshProUGUI _pressureValue;

        public void SetSource(string label) =>
            _sourceLabel.text = label;

        public void SetTankLevel(float ratio, string valueText) =>
            SetBar(_tankFill, _tankValue, ratio, valueText);

        public void SetPressure(float ratio, string valueText) =>
            SetBar(_pressureFill, _pressureValue, ratio, valueText);

        private static void SetBar(RectTransform fill, TextMeshProUGUI value, float ratio, string valueText)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), fill.anchorMax.y);
            value.text = valueText;
        }
    }
}
