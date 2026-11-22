using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "AttackHoseConfig", menuName = "Configs/Attack Hose Config")]
    public class AttackHoseConfig : ScriptableObject
    {
        [field: Header("Reel")]
        [field: SerializeField, Min(1f), Tooltip("Hose length stored on the reel, m")]
        public float MaxLength { get; private set; } = 20f;
        [field: SerializeField, Min(0.5f), Tooltip("Hose length hanging between the reel and the parked nozzle, m")]
        public float ParkedLength { get; private set; } = 1f;
        [field: SerializeField, Min(0f), Tooltip("Spare hose the reel keeps beyond the straight distance to the nozzle, m")]
        public float PayOutMargin { get; private set; } = 1.5f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Stretch of a pulled hose, as a share of its length, that makes the reel let out more")]
        public float PayOutStretchRatio { get; private set; } = 0.05f;
        [field: SerializeField, Min(0.1f), Tooltip("Fastest the reel lets hose out, m/s")]
        public float MaxPayOutSpeed { get; private set; } = 8f;
        [field: SerializeField, Min(0f), Tooltip("Extra distance between the fully unrolled hose and the player, m")]
        public float LeashSlack { get; private set; } = 0.3f;
        [field: SerializeField, Min(0.01f), Tooltip("Radius of the bare reel drum the hose is wound on, m")]
        public float ReelCoreRadius { get; private set; } = 0.165f;
        [field: SerializeField, Min(0.05f), Tooltip("Space between the reel flanges the hose is wound across, m")]
        public float ReelWidth { get; private set; } = 0.56f;
        [field: SerializeField, Range(6, 64), Tooltip("Smoothness of the hose coils on the reel: points per turn")]
        public int CoilSegmentsPerTurn { get; private set; } = 24;
        [field: SerializeField, Min(0.1f), Tooltip("How fast the reel spins to catch up with the hose being pulled, 1/s")]
        public float ReelSpinSharpness { get; private set; } = 12f;
        [field: SerializeField, Tooltip("How far the reel jerks back when the hose runs out, degrees")]
        public float ReelBrakeKick { get; private set; } = 25f;
        [field: SerializeField] public TweenSettings ReelBrakeTween { get; private set; } = new TweenSettings(0.7f, Ease.OutElastic);
        [field: SerializeField] public HoseRopeSettings Rope { get; private set; } = new HoseRopeSettings();
        [field: SerializeField] public HoseTensionSettings Tension { get; private set; } = new HoseTensionSettings();

        [field: Header("Nozzle")]
        [field: SerializeField, Min(0f), Tooltip("Water the fully open nozzle draws from the pump at the highest pressure setting, L/s")]
        public float NozzleFlowRate { get; private set; } = 25f;
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Lowest pressure setting, as a share of the full flow")]
        public float MinPressureSetting { get; private set; } = 0.25f;
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Pressure setting change per mouse wheel notch")]
        public float PressureStep { get; private set; } = 0.25f;
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Pressure setting of the nozzle at the start")]
        public float StartPressureSetting { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f), Tooltip("Scale of the jet reaction force (water mass flow × exit speed) pushing the nozzle back")]
        public float RecoilScale { get; private set; } = 1f;
        [field: SerializeField, Min(0f), Tooltip("How fast the stream builds up after the nozzle opens, 1/s")]
        public float FlowOpenSharpness { get; private set; } = 10f;
        [field: SerializeField, Min(0f), Tooltip("How fast the stream dies down after the nozzle closes or water runs out, 1/s")]
        public float FlowCloseSharpness { get; private set; } = 7f;

        [field: Header("Charging")]
        [field: SerializeField, Min(0.1f), Tooltip("Speed of the water front running from the reel hub through the coils to the nozzle after it opens, m/s")]
        public float ChargeSpeed { get; private set; } = 25f;
        [field: SerializeField, Min(0f), Tooltip("Shortest time the water takes to reach the nozzle, even on a short hose, s")]
        public float MinChargeDuration { get; private set; } = 0.35f;
        [field: SerializeField, Min(0.01f), Tooltip("Time a fully charged hose takes to go limp after the nozzle closes or water runs out, s")]
        public float DischargeDuration { get; private set; } = 2.5f;

        [field: Header("Look")]
        [field: SerializeField, Min(0.01f), Tooltip("Hose radius under pressure, m")]
        public float Radius { get; private set; } = 0.05f;
        [field: SerializeField, Range(0.1f, 1f), Tooltip("Empty hose radius relative to the pressurized radius")]
        public float DeflatedRadiusRatio { get; private set; } = 0.75f;
        [field: SerializeField, Range(0.1f, 1f), Tooltip("How flat the empty hose is: cross-section height relative to width")]
        public float DeflatedSquash { get; private set; } = 0.8f;
        [field: SerializeField, Min(0f), Tooltip("How much the hose bulges at the inflation wave front")]
        public float InflationBulge { get; private set; } = 0.2f;
        [field: SerializeField, Range(0.01f, 1f), Tooltip("Inflation wave front width relative to the hose length")]
        public float InflationWaveWidth { get; private set; } = 0.15f;
        [field: SerializeField, Min(0f), Tooltip("Length from each end over which the hose narrows to fit the fittings, m")]
        public float EndTaperLength { get; private set; } = 0.15f;
        [field: SerializeField, Range(3, 24)] public int Sides { get; private set; } = 14;
        [field: SerializeField, Range(1, 8), Tooltip("Number of smoothing rings between rope nodes")]
        public int Subdivisions { get; private set; } = 4;
        [field: SerializeField, Min(0.05f), Tooltip("Distance between the woven bands along the hose, m")]
        public float RibSpacing { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 0.3f), Tooltip("How far the woven bands stand out, as a share of the hose radius")]
        public float RibDepth { get; private set; } = 0.06f;
        [field: SerializeField, Tooltip("UV of the color cell in Palette.png")]
        public Vector2 PaletteUv { get; private set; } = new Vector2(0.688f, 0.188f);

        [field: Header("Holding")]
        [field: SerializeField, Tooltip("Position of the held nozzle relative to the grab anchor, within reach of the body's arms, m")]
        public Vector3 HoldOffset { get; private set; } = new Vector3(0f, 0.01f, -0.01f);
        [field: SerializeField, Tooltip("Point the held nozzle swings around when aiming up and down, relative to the grab anchor: between the hands")]
        public Vector3 AimPivot { get; private set; } = new Vector3(0f, 0.01f, -0.17f);
        [field: SerializeField, Tooltip("Lowest nozzle pitch while aiming, degrees (negative is down)")]
        public float MinAimPitch { get; private set; } = -35f;
        [field: SerializeField, Tooltip("Highest nozzle pitch while aiming, degrees")]
        public float MaxAimPitch { get; private set; } = 45f;
        [field: SerializeField, Min(0f), Tooltip("Extra upward tilt of the nozzle over the view direction, degrees")]
        public float AimTilt { get; private set; } = 5f;
        [field: SerializeField, Min(0.1f), Tooltip("How fast the heavy nozzle follows the view, 1/s")]
        public float AimSharpness { get; private set; } = 10f;
        [field: SerializeField, Range(0f, 1f), Tooltip("How much the hose leaving the held nozzle bends straight down")]
        public float HeldHoseDroop { get; private set; } = 0.6f;
        [field: SerializeField] public TweenSettings GrabTween { get; private set; } = new TweenSettings(0.3f, Ease.OutCubic);
        [field: SerializeField] public TweenSettings DropTween { get; private set; } = new TweenSettings(0.35f, Ease.OutBounce);
        [field: SerializeField, Min(0f), Tooltip("Height of the nozzle centre above the ground when dropped, m")]
        public float RestHeight { get; private set; } = 0.04f;
    }
}
