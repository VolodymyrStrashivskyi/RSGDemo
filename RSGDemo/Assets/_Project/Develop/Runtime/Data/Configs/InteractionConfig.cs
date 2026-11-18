using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Configs/Interaction Config")]
    public class InteractionConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float Distance { get; private set; } = 2.2f;
        [field: SerializeField, Tooltip("Без шару Player: промінь іде з камери всередині капсули гравця")]
        public LayerMask Mask { get; private set; } = ~0;
    }
}
