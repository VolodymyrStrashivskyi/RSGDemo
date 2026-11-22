using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public class HoseTensionSettings
    {
        private const int DEFAULT_PROP_MASK = 1024;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of the full hose reach at which walking away starts to feel the pull")]
        public float ResistanceStartReach { get; private set; } = 0.85f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Stretch of a hose with no length left to let out, as a share of its length, at which the pull starts")]
        public float StretchTensionStart { get; private set; } = 0.04f;
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Speed of walking away from the hose at full tension, as a share of the normal speed")]
        public float StrainSpeedRatio { get; private set; } = 0.3f;
        [field: SerializeField, Min(0f), Tooltip("How fast an overstretched hose drags the player back toward it, m/s")]
        public float PullBackSpeed { get; private set; } = 0.8f;
        [field: SerializeField, Range(1, 16), Tooltip("How many nodes back from the hands set the direction the hose pulls")]
        public int PullDirectionNodes { get; private set; } = 4;
        [field: SerializeField, Min(0.1f), Tooltip("How fast the felt stretch and pull direction follow the jittery rope, 1/s")]
        public float Sharpness { get; private set; } = 6f;

        [field: Header("Taut Look")]
        [field: SerializeField, Range(0f, 1f), Tooltip("How strongly the taut hose resists bends: it straightens between the points it rests on")]
        public float TautBendStiffness { get; private set; } = 0.9f;
        [field: SerializeField, Min(0f), Tooltip("How far the hose trembles sideways when stretched tight with no length left, m")]
        public float TrembleAmplitude { get; private set; } = 0.005f;
        [field: SerializeField, Min(0f), Tooltip("How fast the taut hose trembles, 1/s")]
        public float TrembleFrequency { get; private set; } = 6f;

        [field: Header("Props")]
        [field: SerializeField, Min(0f), Tooltip("Force the fully taut hose presses into a movable prop it bends around, N")]
        public float PropPushForce { get; private set; } = 150f;
        [field: SerializeField, Tooltip("Movable props the taut hose can drag")]
        public LayerMask PropMask { get; private set; } = DEFAULT_PROP_MASK;
        [field: SerializeField, Min(1f), Tooltip("How far around a hose node props are felt, relative to the hose collision radius")]
        public float PropProbeRatio { get; private set; } = 1.6f;
    }
}
