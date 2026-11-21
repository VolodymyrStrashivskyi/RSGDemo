using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "WaterStatusConfig", menuName = "Configs/Water Status Config")]
    public class WaterStatusConfig : ScriptableObject
    {
        [field: Header("Texts")]
        [field: SerializeField] public string TankSourceLabel { get; private set; } = "TANK";
        [field: SerializeField] public string HydrantSourceLabel { get; private set; } = "HYDRANT";
        [field: SerializeField] public string NoWaterLabel { get; private set; } = "NO WATER";
        [field: SerializeField] public string TankUnit { get; private set; } = "%";
        [field: SerializeField] public string PressureUnit { get; private set; } = " BAR";

        [field: Header("Colors")]
        [field: SerializeField] public Color TankColor { get; private set; } = new Color(0.33f, 0.72f, 1f);
        [field: SerializeField] public Color PressureColor { get; private set; } = new Color(1f, 0.69f, 0.13f);
        [field: SerializeField] public Color WarningColor { get; private set; } = new Color(1f, 0.27f, 0.22f);
        [field: SerializeField] public Color EmptySegmentColor { get; private set; } = new Color(1f, 1f, 1f, 0.12f);

        [field: Header("Low tank")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Below this share of the tank the bar blinks")]
        public float LowTankRatio { get; private set; } = 0.2f;
        [field: SerializeField] public TweenSettings LowTankBlink { get; private set; } = new TweenSettings(0.35f, Ease.InOutSine, -1, CycleMode.Yoyo);

        [field: Header("Source change")]
        [field: SerializeField] public ShakeSettings SourcePunch { get; private set; } = new ShakeSettings(new Vector3(0.22f, 0.22f, 0f), 0.4f, 8f);
        [field: SerializeField] public TweenSettings SourceFlash { get; private set; } = new TweenSettings(0.6f, Ease.OutQuad);

        [field: Header("Pressure drop")]
        [field: SerializeField, Min(0f), Tooltip("Pressure fall in one frame that shakes the panel, bar")]
        public float PressureDropToShake { get; private set; } = 1f;
        [field: SerializeField] public ShakeSettings PressureDropShake { get; private set; } = new ShakeSettings(new Vector3(12f, 6f, 0f), 0.45f, 18f);
    }
}
