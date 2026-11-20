using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "WaterStreamConfig", menuName = "Configs/Water Stream Config")]
    public class WaterStreamConfig : ScriptableObject
    {
        private const int DEFAULT_COLLISION_MASK = 1409;

        [field: Header("Jet")]
        [field: SerializeField, Min(0f), Tooltip("Water exit speed at full pressure, m/s")]
        public float ExitSpeed { get; private set; } = 10f;
        [field: SerializeField, Min(0f), Tooltip("Longest flight time traced along the arc, s")]
        public float MaxFlightTime { get; private set; } = 1.6f;
        [field: SerializeField, Min(2), Tooltip("Arc segments used for tracing and the stream mesh")]
        public int ArcSegments { get; private set; } = 28;
        [field: SerializeField, Tooltip("Layers the stream hits and splashes on")]
        public LayerMask CollisionMask { get; private set; } = DEFAULT_COLLISION_MASK;

        [field: Header("Shape")]
        [field: SerializeField, Min(0f), Tooltip("Stream radius at the outlet, m")]
        public float NozzleRadius { get; private set; } = 0.06f;
        [field: SerializeField, Min(0f), Tooltip("Stream radius once it has broken up, m")]
        public float SpreadRadius { get; private set; } = 0.22f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Radius scale when pressure is almost gone")]
        public float LowFlowRadiusScale { get; private set; } = 0.45f;
        [field: SerializeField, Min(0.01f), Tooltip("Flight time after which the stream is fully broken into drops, s")]
        public float BreakupTime { get; private set; } = 0.75f;
        [field: SerializeField, Min(0.1f), Tooltip("How the stream widens over its flight: 1 grows evenly, higher stays tight near the outlet and opens up further out")]
        public float SpreadExponent { get; private set; } = 1f;
        [field: SerializeField, Min(3), Tooltip("Sides of the stream tube")]
        public int Sides { get; private set; } = 12;
        [field: SerializeField, Range(0f, 1f), Tooltip("Flow below which the stream fades in or out")]
        public float FadeFlow { get; private set; } = 0.15f;
        [field: SerializeField, Min(0.01f), Tooltip("Length at the stream end over which it fades into the splash, m")]
        public float EndFadeLength { get; private set; } = 0.4f;
        [field: SerializeField, Min(0f), Tooltip("Extra radius at the very end of the stream, fraction of its radius")]
        public float EndSplay { get; private set; } = 0.8f;

        [field: Header("Push")]
        [field: SerializeField, Min(0f), Tooltip("Force the jet applies at full pressure, N")]
        public float PushForce { get; private set; } = 600f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Push scale once the stream has broken into drops")]
        public float BrokenPushScale { get; private set; } = 0.35f;

        [field: Header("Pressure")]
        [field: SerializeField, Range(0f, 0.5f), Tooltip("Random pressure pulsing, fraction of exit speed")]
        public float FlickerAmplitude { get; private set; } = 0.05f;
        [field: SerializeField, Min(0f), Tooltip("Pressure pulsing frequency, Hz")]
        public float FlickerFrequency { get; private set; } = 5f;
        [field: SerializeField, Min(1f), Tooltip("Peak flow of the first spurt when the hydrant opens")]
        public float BurstFlow { get; private set; } = 1.35f;
        [field: SerializeField, Min(0f), Tooltip("Delay before water comes out after the valve starts turning, s")]
        public float OpenDelay { get; private set; } = 0.3f;
        [field: SerializeField]
        public TweenSettings BurstTween { get; private set; } = new TweenSettings(0.25f, Ease.OutQuad);
        [field: SerializeField]
        public TweenSettings SettleTween { get; private set; } = new TweenSettings(0.8f, Ease.InOutSine);
        [field: SerializeField]
        public TweenSettings StopTween { get; private set; } = new TweenSettings(0.9f, Ease.OutQuad);

        [field: Header("Particles")]
        [field: SerializeField, Min(0f), Tooltip("Spray droplets per second at full flow")]
        public float SprayRate { get; private set; } = 220f;
        [field: SerializeField, Min(0f), Tooltip("Mist puffs per second at full flow")]
        public float MistRate { get; private set; } = 28f;
        [field: SerializeField, Min(0f), Tooltip("Splash particles per second at the impact point")]
        public float SplashRate { get; private set; } = 90f;
        [field: SerializeField, Min(0f), Tooltip("Mist puffs per second at the impact point")]
        public float SplashMistRate { get; private set; } = 14f;
        [field: SerializeField, Min(0), Tooltip("Droplets thrown out by the first spurt")]
        public int BurstCount { get; private set; } = 70;

        [field: Header("Puddle")]
        [field: SerializeField, Min(8), Tooltip("Cells per side of the puddle water grid")]
        public int PuddleResolution { get; private set; } = 64;
        [field: SerializeField, Min(0.02f), Tooltip("Size of one puddle grid cell, m")]
        public float PuddleCellSize { get; private set; } = 0.15f;
        [field: SerializeField, Min(1), Tooltip("Most puddles kept at once; a new spot reuses the driest one")]
        public int MaxPuddles { get; private set; } = 6;
        [field: SerializeField, Min(0f), Tooltip("Shortest time between laying out new puddles, so a jittering stream does not rebuild them every frame, s")]
        public float PuddlePlaceInterval { get; private set; } = 0.3f;
        [field: SerializeField, Tooltip("Static surfaces the puddle grid lies on; movable props are left out so no puddle hangs in the air once they are pushed away")]
        public LayerMask PuddleGroundMask { get; private set; } = 1;
        [field: SerializeField, Min(0.05f), Tooltip("How far above the floor the puddle looks for ground; higher ground blocks the water, m")]
        public float PuddleProbeHeight { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f), Tooltip("How far below the floor the puddle still finds ground to run down to; water running off a deeper edge is gone, m")]
        public float PuddleProbeDepth { get; private set; } = 1f;
        [field: SerializeField, Min(0.01f), Tooltip("Floor height difference at which water lands in a separate puddle, m")]
        public float PuddleLevelTolerance { get; private set; } = 0.4f;
        [field: SerializeField, Min(0.005f), Tooltip("Height jump inside one grid cell above which the puddle surface is left open, so it never stands up as a wall at furniture and step edges, m")]
        public float PuddleMaxCellRise { get; private set; } = 0.06f;
        [field: SerializeField, Min(0f), Tooltip("Height above the ground the puddle surface floats at, m")]
        public float PuddleLift { get; private set; } = 0.008f;
        [field: SerializeField, Min(0f), Tooltip("Random unevenness of the ground that makes water pool in patches, m")]
        public float PuddleGroundNoise { get; private set; } = 0.006f;
        [field: SerializeField, Min(0f), Tooltip("Frequency of the ground unevenness, 1/m")]
        public float PuddleNoiseScale { get; private set; } = 0.9f;
        [field: SerializeField, Min(0f), Tooltip("Water volume that lands per second at full flow, m3/s")]
        public float PuddleFillRate { get; private set; } = 0.004f;
        [field: SerializeField, Min(0.01f), Tooltip("Radius over which landing water is spread, m")]
        public float PuddlePourRadius { get; private set; } = 0.3f;
        [field: SerializeField, Min(0f), Tooltip("How fast water flows to lower cells, 1/s")]
        public float PuddleFlowRate { get; private set; } = 8f;
        [field: SerializeField, Min(0f), Tooltip("Water thinner than this stops spreading, m")]
        public float PuddleMinFlowDepth { get; private set; } = 0.0015f;
        [field: SerializeField, Min(0f), Tooltip("Water lost to soaking and evaporation, m/s")]
        public float PuddleEvaporation { get; private set; } = 0.00015f;
        [field: SerializeField, Min(0.0001f), Tooltip("Water depth that looks like standing water, m")]
        public float PuddleFullDepth { get; private set; } = 0.004f;
        [field: SerializeField, Min(0.005f), Tooltip("Puddle simulation time step, s")]
        public float PuddleSimulationStep { get; private set; } = 0.033f;
        [field: SerializeField, Min(0.01f), Tooltip("Time for puddle ripples to calm down once water stops landing, s")]
        public float PuddleActivityFadeTime { get; private set; } = 0.8f;
    }
}
