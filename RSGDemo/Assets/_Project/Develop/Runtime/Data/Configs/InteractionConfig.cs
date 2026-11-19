using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Configs/Interaction Config")]
    public class InteractionConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float Distance { get; private set; } = 2.2f;
        [field: SerializeField, Tooltip("Exclude the Player layer: the ray starts from the camera inside the player capsule")]
        public LayerMask Mask { get; private set; } = ~0;
    }
}
