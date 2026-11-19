using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public class HoseRopeSettings
    {
        [field: SerializeField, Min(0.05f), Tooltip("Length of one physics rope segment, m")]
        public float SegmentLength { get; private set; } = 0.25f;
        [field: SerializeField, Range(1, 50), Tooltip("How many times length constraints are solved per step")]
        public int ConstraintIterations { get; private set; } = 24;
        [field: SerializeField, Range(0f, 1f), Tooltip("Fraction of velocity kept per step in the air")]
        public float AirDamping { get; private set; } = 0.98f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Fraction of horizontal velocity removed by ground friction per step")]
        public float GroundFriction { get; private set; } = 0.6f;
        [field: SerializeField, Range(0f, 1f), Tooltip("How strongly the empty hose resists sharp bends")]
        public float BendStiffness { get; private set; } = 0.15f;
        [field: SerializeField, Range(0f, 1f), Tooltip("How strongly the pressurized hose resists bends: an inflated hose is springy and lies in wide arcs")]
        public float InflatedBendStiffness { get; private set; } = 0.6f;
        [field: SerializeField, Min(0f), Tooltip("How far above the ground a node can be and still count as lying on it, m")]
        public float GroundContactTolerance { get; private set; } = 0.02f;
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = 1;
    }
}
