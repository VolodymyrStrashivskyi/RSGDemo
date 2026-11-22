using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FootstepConfig", menuName = "Configs/Audio/Footstep Config")]
    public class FootstepConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("Steps on the street, sidewalk and yard")]
        public SoundCue Outdoor { get; private set; }
        [field: SerializeField, Tooltip("Steps on floors inside the house")]
        public SoundCue Indoor { get; private set; }
        [field: SerializeField, Min(0.1f), Tooltip("Distance walked between two steps, m")]
        public float StepLength { get; private set; } = 1.1f;
        [field: SerializeField, Min(0f), Tooltip("Shortest time between two steps, so running does not rattle, s")]
        public float MinInterval { get; private set; } = 0.45f;
        [field: SerializeField, Min(0f), Tooltip("Slower than this does not count as walking, m/s")]
        public float MinSpeed { get; private set; } = 0.3f;
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = 1;
        [field: SerializeField, Min(0.1f), Tooltip("How far below the feet the ground is looked for, m")]
        public float GroundProbe { get; private set; } = 0.4f;
    }
}
