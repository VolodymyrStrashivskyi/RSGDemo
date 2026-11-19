using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FirefighterConfig", menuName = "Configs/Firefighter Config")]
    public class FirefighterConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float WalkSpeed { get; private set; } = 1.6f;
        [field: SerializeField, Min(0f)] public float RunSpeed { get; private set; } = 4f;
        [field: SerializeField] public float Gravity { get; private set; } = -20f;
        [field: SerializeField, Min(0f)] public float JumpHeight { get; private set; } = 0.6f;
        [field: SerializeField, Min(0f), Tooltip("Seconds in the air before the jump animation starts, so it does not twitch on steps")]
        public float AirborneAnimationDelay { get; private set; } = 0.12f;
        [field: SerializeField, Min(0f)] public float AnimationDampTime { get; private set; } = 0.1f;
    }
}
