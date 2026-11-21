using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus
{
    public class SegmentBarView : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;

        public void SetFill(float ratio, Color fillColor, Color emptyColor)
        {
            float filledSegments = Mathf.Clamp01(ratio) * _segments.Length;

            for (int i = 0; i < _segments.Length; i++)
                _segments[i].color = Color.Lerp(emptyColor, fillColor, Mathf.Clamp01(filledSegments - i));
        }
    }
}
