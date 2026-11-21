using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "DispatchConfig", menuName = "Configs/Dispatch Config")]
    public class DispatchConfig : ScriptableObject
    {
        [field: Header("Texts")]
        [field: SerializeField] public string CallLabel { get; private set; } = "DISPATCH · CALL 45";
        [field: SerializeField] public string Title { get; private set; } = "STRUCTURE FIRE";
        [field: SerializeField] public string Address { get; private set; } = "45 ELM STREET · TWO-STOREY HOUSE";
        [field: SerializeField] public string Objective { get; private set; } = "PUT OUT THE FIRE";
        [field: SerializeField, Tooltip("Shown after the number of burning spots")]
        public string BurningSuffix { get; private set; } = " BURNING";
        [field: SerializeField] public string CompletedLabel { get; private set; } = "FIRE OUT";

        [field: Header("Colors")]
        [field: SerializeField] public Color AlertColor { get; private set; } = new Color(0.89f, 0.25f, 0.18f);
        [field: SerializeField] public Color AccentColor { get; private set; } = new Color(1f, 0.69f, 0.13f);
        [field: SerializeField] public Color CompletedColor { get; private set; } = new Color(0.36f, 0.86f, 0.45f);

        [field: Header("Animations")]
        [field: SerializeField, Tooltip("How far off screen the panel starts, px")]
        public float SlideDistance { get; private set; } = 420f;
        [field: SerializeField] public TweenSettings SlideIn { get; private set; } = new TweenSettings(0.55f, Ease.OutBack, startDelay: 0.6f);
        [field: SerializeField, Tooltip("Blinking of the warning sign when something goes wrong")]
        public TweenSettings AlertBlink { get; private set; } = new TweenSettings(0.22f, Ease.InOutSine, 8, CycleMode.Yoyo);
        [field: SerializeField, Range(0f, 1f), Tooltip("Alpha of the warning sign at the dim end of a blink")]
        public float AlertDimAlpha { get; private set; } = 0.25f;
        [field: SerializeField] public ShakeSettings CountPunch { get; private set; } = new ShakeSettings(new Vector3(0.25f, 0.25f, 0f), 0.3f, 8f);
        [field: SerializeField] public ShakeSettings CompletedPunch { get; private set; } = new ShakeSettings(new Vector3(0.12f, 0.12f, 0f), 0.6f, 6f);
    }
}
