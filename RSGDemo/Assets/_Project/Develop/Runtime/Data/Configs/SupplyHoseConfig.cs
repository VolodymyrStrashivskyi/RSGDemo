using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "SupplyHoseConfig", menuName = "Configs/Supply Hose Config")]
    public class SupplyHoseConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Total length of the supply hose, m")]
        public float Length { get; private set; } = 10f;
        [field: SerializeField, Min(0f), Tooltip("Extra distance between the taut hose and the player: hands are in front of the body")]
        public float LeashSlack { get; private set; } = 0f;
        [field: SerializeField] public HoseRopeSettings Rope { get; private set; } = new HoseRopeSettings();
        [field: SerializeField] public HoseTensionSettings Tension { get; private set; } = new HoseTensionSettings();

        [field: Header("Look")]
        [field: SerializeField, Min(0.01f), Tooltip("Hose radius under pressure, m")]
        public float Radius { get; private set; } = 0.09f;
        [field: SerializeField, Range(0.1f, 1f), Tooltip("Empty hose radius relative to the pressurized radius")]
        public float DeflatedRadiusRatio { get; private set; } = 0.55f;
        [field: SerializeField, Range(0.1f, 1f), Tooltip("How flat the empty hose is: cross-section height relative to width")]
        public float DeflatedSquash { get; private set; } = 0.85f;
        [field: SerializeField, Min(0f), Tooltip("How much the hose bulges at the inflation wave front beyond its normal thickness")]
        public float InflationBulge { get; private set; } = 0.25f;
        [field: SerializeField, Min(0f), Tooltip("Length from each end over which the hose narrows to empty size to fit the coupling, m")]
        public float EndTaperLength { get; private set; } = 0.3f;
        [field: SerializeField, Range(3, 24)] public int Sides { get; private set; } = 14;
        [field: SerializeField, Range(1, 8), Tooltip("Number of smoothing rings between rope nodes")]
        public int Subdivisions { get; private set; } = 4;
        [field: SerializeField, Min(0.05f), Tooltip("Distance between the woven bands along the hose, m")]
        public float RibSpacing { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 0.3f), Tooltip("How far the woven bands stand out, as a share of the hose radius")]
        public float RibDepth { get; private set; } = 0.06f;
        [field: SerializeField, Tooltip("UV of the color cell in Palette.png")]
        public Vector2 PaletteUv { get; private set; } = new Vector2(0.5f, 0.5f);

        [field: Header("Inflation")]
        [field: SerializeField, Min(0.01f)] public float InflateDuration { get; private set; } = 1.4f;
        [field: SerializeField, Min(0.01f)] public float DeflateDuration { get; private set; } = 0.9f;
        [field: SerializeField, Range(0.01f, 1f), Tooltip("Inflation wave front width relative to the hose length")]
        public float InflationWaveWidth { get; private set; } = 0.15f;
    }
}
