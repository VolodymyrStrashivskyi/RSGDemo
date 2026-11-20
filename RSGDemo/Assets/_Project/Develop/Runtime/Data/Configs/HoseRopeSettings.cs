using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public class HoseRopeSettings
    {
        private const int DEFAULT_COLLISION_MASK = 257;

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
        [field: SerializeField, Tooltip("Walls, doors, furniture and vehicles the hose bends around instead of passing through")]
        public LayerMask CollisionMask { get; private set; } = DEFAULT_COLLISION_MASK;
        [field: SerializeField, Range(1, 8), Tooltip("How many times per step the hose is pushed out of obstacles")]
        public int CollisionPasses { get; private set; } = 2;
        [field: SerializeField, Min(0.05f), Tooltip("Distance between recorded points of the path the player walked with the hose, m")]
        public float TrailSpacing { get; private set; } = 0.3f;
        [field: SerializeField, Range(0f, 1f), Tooltip("How strongly the carried hose is pulled sideways onto the path the player walked, per step")]
        public float TrailPull { get; private set; } = 0.08f;
        [field: SerializeField, Min(0.05f), Tooltip("How far a node looks for the inside of a hollow wall or floor to step out of it, m")]
        public float HollowEscapeDistance { get; private set; } = 0.25f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Stretch of a hose with no length left to let out, as a share of its length, at which it stops the player")]
        public float LeashStretchRatio { get; private set; } = 0.08f;
    }
}
