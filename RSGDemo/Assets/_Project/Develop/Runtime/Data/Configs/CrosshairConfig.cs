using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "CrosshairConfig", menuName = "Configs/Crosshair Config")]
    public class CrosshairConfig : ScriptableObject
    {
        [field: SerializeField, Range(0f, 1f), Tooltip("Зовнішній радіус крапки, частка від розміру прицілу")]
        public float DotRadius { get; private set; } = 0.35f;
        [field: SerializeField, Range(0f, 1f)] public float RingOuterRadius { get; private set; } = 1f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Внутрішній радіус кільця: дірка «пончика»")]
        public float RingInnerRadius { get; private set; } = 0.62f;
        [field: SerializeField] public TweenSettings HighlightTween { get; private set; } = new TweenSettings(0.18f, Ease.OutBack);
        [field: SerializeField] public TweenSettings UnhighlightTween { get; private set; } = new TweenSettings(0.14f, Ease.OutQuad);
    }
}
