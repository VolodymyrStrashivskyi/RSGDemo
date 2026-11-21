using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireConfig", menuName = "Configs/Fire Config")]
    public class FireConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Extra distance around the water impact that still reaches a fire, m")]
        public float WaterReach { get; private set; } = 0.3f;
    }
}
